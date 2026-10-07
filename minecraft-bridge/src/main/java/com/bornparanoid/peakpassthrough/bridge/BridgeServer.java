package com.bornparanoid.peakpassthrough.bridge;

import com.google.gson.JsonObject;
import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.atomic.AtomicInteger;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** Localhost TCP server for PEAK clients. Network threads only enqueue; the game thread drains with poll(). */
public final class BridgeServer {
	static final Logger LOGGER = LoggerFactory.getLogger("passpeakthrough/bridge");

	public record Inbound(int clientId, JsonObject msg, boolean disconnected) {}

	private final int requestedPort;
	private final java.util.concurrent.LinkedBlockingQueue<Inbound> inbox = new java.util.concurrent.LinkedBlockingQueue<>(16384);
	private final ConcurrentLinkedQueue<Inbound> closedInbox=new ConcurrentLinkedQueue<>();
	private final Map<Integer, ClientConnection> clients = new ConcurrentHashMap<>();
	private final AtomicInteger nextId = new AtomicInteger(1);
	private volatile ServerSocket socket;
	private java.util.function.BiFunction<Integer,String,String> outbound=(id,line)->line;
	public void outbound(java.util.function.BiFunction<Integer,String,String> mapper){outbound=mapper;}

	public BridgeServer(int port) {
		this.requestedPort = port;
	}

	public void start() throws IOException {
		ServerSocket s = new ServerSocket();
		try {
			s.bind(new InetSocketAddress(InetAddress.getLoopbackAddress(), requestedPort));
		} catch (IOException e) {
			s.close();
			throw e;
		}
		socket = s;
		Thread accept = new Thread(this::acceptLoop, "PeakBridge-accept");
		accept.setDaemon(true);
		accept.start();
		LOGGER.info("PEAK bridge listening on 127.0.0.1:{}", port());
	}

	public int port() {
		return socket.getLocalPort();
	}

	private void acceptLoop() {
		ServerSocket s = socket;
		while (!s.isClosed()) {
			try {
				Socket client = s.accept();
				client.setTcpNoDelay(true);
				int id = nextId.getAndIncrement();
				ClientConnection c = new ClientConnection(id, client, this);
				clients.put(id, c);
				c.start();
				LOGGER.info("PEAK client {} connected", id);
			} catch (IOException e) {
				if (!s.isClosed()) LOGGER.warn("Accept failed: {}", e.toString());
			}
		}
	}

	void onMessage(int id, JsonObject msg) {
		if(!inbox.offer(new Inbound(id,msg,false))){ClientConnection c=clients.get(id);if(c!=null)c.close();}
	}

	void onClosed(int id) {
		if (clients.remove(id) != null) {
            // Preserve FIFO with a later hello: inventory must be saved before reconnect.
            Inbound closed = new Inbound(id,null,true);
            if (!inbox.offer(closed)) closedInbox.add(closed);
        }
	}

	public Inbound poll() {
		Inbound in=inbox.poll();return in!=null?in:closedInbox.poll();
	}

	public void send(int clientId, String line) {
		ClientConnection c = clients.get(clientId);
		if (c != null) {String mapped=outbound.apply(clientId,line);if(mapped!=null)c.send(mapped);}
	}

	public void sendAndClose(int clientId, String line) {
		ClientConnection c = clients.get(clientId);
		if (c != null) c.sendAndClose(line);
	}

	public void stop() {
		try {
			if (socket != null) socket.close();
		} catch (IOException ignored) {
		}
		clients.values().forEach(ClientConnection::close);
	}
}
