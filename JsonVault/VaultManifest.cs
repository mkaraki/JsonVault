using System.Text.Json.Serialization;

namespace JsonVault
{
    internal class VaultManifest
    {

        public string DirectoryName { get; set; } = string.Empty;

        public List<FileManifest> Files { get; set; } = new List<FileManifest>();

        internal class FileManifest
        {

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Identifier { get; set; } = string.Empty;

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string Name { get; set; } = string.Empty;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, string>? Attributes { get; set; } = null;

        }
    }
}
