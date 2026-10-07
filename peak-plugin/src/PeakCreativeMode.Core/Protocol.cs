using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PeakCreativeMode.Core
{
    /// Wire format shared with the Minecraft mod: one JSON object per line, string field "t" = type.
    public static class Protocol
    {
        public const int Version = 2;
        public const int DefaultPort = 47655;
        public static string WelcomeSeed(JObject welcome,string requested){var token=welcome?["mapSeed"];if(token?.Type!=JTokenType.String)return requested;string seed=(string)token;return string.IsNullOrWhiteSpace(seed)||seed.Length>128||seed.IndexOf('\0')>=0?requested:seed;}


        public static string Hello(string playerId, string mapSeed, System.Collections.Generic.IEnumerable<string> biomes = null) =>
            new JObject
            {
                ["t"] = "hello", ["protocol"] = Version, ["playerId"] = playerId,
                ["mapSeed"] = mapSeed, ["biomes"] = biomes == null ? new JArray() : new JArray(biomes),
            }.ToString(Formatting.None);

        /// Unity-space pose. Returns null if any component is NaN/Infinity (never sent).
        public static string Pose(float x, float y, float z, float yaw, float pitch)
        {
            if (!Finite(x) || !Finite(y) || !Finite(z) || !Finite(yaw) || !Finite(pitch)) return null;
            return new JObject
            {
                ["t"] = "pose", ["x"] = x, ["y"] = y, ["z"] = z, ["yaw"] = yaw, ["pitch"] = pitch,
            }.ToString(Formatting.None);
        }

        public static JObject Parse(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            try
            {
                return JToken.Parse(line) is JObject o && o["t"]?.Type == JTokenType.String ? o : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
