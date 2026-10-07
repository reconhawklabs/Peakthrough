using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakCreativeMode.Plugin
{
    /// F8: small IMGUI panel showing bridge status and the Minecraft stand-in's position.
    internal sealed class DebugPanel : MonoBehaviour
    {
        private bool _visible;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[Plugin.Keys.DebugPanel.Value].wasPressedThisFrame) _visible = !_visible;
        }

        private void OnGUI()
        {
            var bridge = BridgeBehaviour.Instance;
            if (!_visible || bridge == null) return;
            var c = bridge.Client;
            string mc = "—";
            var s = bridge.LastDebugState;
            if (s != null)
                mc = $"{(double)s["x"]:F1}, {(double)s["y"]:F1}, {(double)s["z"]:F1}  yaw {(float)s["yaw"]:F0}  pitch {(float)s["pitch"]:F0}\n{(string)s["dim"]}";
            string status = c.Status == BridgeStatus.Rejected ? $"rejected: {c.LastError}" : c.Status.ToString().ToLowerInvariant();
            GUI.Box(new Rect(10, 10, 750, 190),
                $"Peakthrough {BuildInfo.Version}\nMC: {status}\nrun: {bridge.MapSeed} / poses sent: {bridge.PosesSent}\nMC stand-in: {mc}\nblocks: {BlockRenderer.Instance?.Grid.Count}, chunks: {BlockRenderer.Instance?.ChunkCount}, quads: {BlockRenderer.Instance?.QuadCount}\nframe avg: {BlockRenderer.Instance?.AverageFrameMs:F1} ms / chunk rebuild: {BlockRenderer.Instance?.LastRebuildMs:F2} ms\n{BlockRenderer.Instance?.AssetStatus}");
        }
    }
}
