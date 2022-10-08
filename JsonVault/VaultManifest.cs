using System.Text.Json.Serialization;

namespace JsonVault
{
    internal class VaultManifest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Identifier { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Name { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Attributes { get; set; } = null;
    }
}
