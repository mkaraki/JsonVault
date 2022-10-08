using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
