package com.bornparanoid.peakpassthrough.bridge;

import static com.bornparanoid.peakpassthrough.bridge.BridgeServer.LOGGER;
import static java.nio.charset.StandardCharsets.UTF_8;

import com.google.gson.JsonObject;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.Writer;
import java.net.Socket;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

/** One PEAK client: a reader thread (lines → inbox) and a writer thread (outbox → socket). */
final class ClientConnection {
	private static final int OUTBOX_LIMIT = 10_000;
	private static final String CLOSE_MARKER = new String("__close__"); // compared by identity

	private final int id;
	private final Socket socket;
	private final BridgeServer owner;
	private final LinkedBlockingQueue<String> outbox = new LinkedBlockingQueue<>(OUTBOX_LIMIT);
	private final AtomicBoolean closed = new AtomicBoolean();
	private boolean warnedMalformed;

	ClientConnection(int id, Socket socket, BridgeServer owner) {
		this.id = id;
		this.socket = socket;
		this.owner = owner;
	}

	void start() {
		Thread r = new Thread(this::readLoop, "PeakBridge-read-" + id);
		r.setDaemon(true);
		r.start();
		Thread w = new Thread(this::writeLoop, "PeakBridge-write-" + id);
		w.setDaemon(true);
		w.start();
	}

	private void readLoop() {
		try (BufferedReader in = new BufferedReader(new InputStreamReader(socket.getInputStream(), UTF_8))) {
			String line;
			while ((line = in.readLine()) != null) {
				JsonObject msg = Protocol.parse(line);
				if (msg == null) {
					if (!warnedMalformed) {
						warnedMalformed = true;
						LOGGER.warn("PEAK client {} sent a malformed line (further ones dropped silently): {}",
								id, line.length() > 120 ? line.substring(0, 120) + "…" : line);
					}
					continue;
				}
				owner.onMessage(id, msg);
			}
		} catch (IOException ignored) {
			// connection reset or closed: treated as a disconnect
		} finally {
			close();
		}
	}

	private void writeLoop() {
		try (Writer out = new BufferedWriter(new OutputStreamWriter(socket.getOutputStream(), UTF_8))) {
			while (!closed.get()) {
				String line = outbox.poll(250, TimeUnit.MILLISECONDS);
				if (line == null) continue;
				if (line == CLOSE_MARKER) {
					out.flush();
					break;
				}
				out.write(line);
				out.write('\n');
				if (outbox.isEmpty()) out.flush();
			}
		} catch (IOException | InterruptedException ignored) {
		} finally {
			close();
		}
	}

	void send(String line) {
		if (closed.get()) return;
		if (!outbox.offer(line)) {
			LOGGER.warn("PEAK client {} is not reading (outbox full); disconnecting", id);
			close();
		}
	}

	void sendAndClose(String line) {
		send(line);
		outbox.offer(CLOSE_MARKER);
	}

	void close() {
		if (!closed.compareAndSet(false, true)) return;
		try {
			socket.close();
		} catch (IOException ignored) {
		}
		owner.onClosed(id);
		LOGGER.info("PEAK client {} disconnected", id);
	}
}
