using System.Collections.Generic;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PeakCreativeMode.Plugin
{
    /// Logs every PEAK input binding once (first scene load) and warns about collisions with our keys.
    internal static class BindingDump
    {
        private static bool _done;

        public static void Install(KeyConfig keys)
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (_done) return;
                _done = true;
                var peak = new List<(string, string)>();
                foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
                    foreach (var action in asset)
                        foreach (var b in action.bindings)
                        {
                            peak.Add((asset.name + "/" + action.name, b.effectivePath));
                            Plugin.Log.LogInfo($"[bindings] {asset.name}/{action.name} -> {b.effectivePath}");
                        }
                var collisions = KeyCollisions.Find(peak, keys.All());
                foreach (var line in collisions) Plugin.Log.LogWarning("[bindings] COLLISION: " + line);
                Plugin.Log.LogInfo($"[bindings] {peak.Count} PEAK bindings, {collisions.Count} collisions with PeakCreativeMode keys");
            };
        }
    }
}
