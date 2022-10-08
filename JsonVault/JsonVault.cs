using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonVault
{
    public class JsonVault
    {
        private List<VaultManifest> _manifests;
        private string _jsonStoreDirectory;

        public async Task LoadVauldAsync(string filepath, string vaultName)
        { 
            if (!File.Exists(filepath))
                throw new FileNotFoundException();

            _jsonStoreDirectory = Path.Combine(new FileInfo(filepath).Directory.FullName, vaultName);

            using (var file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                _manifests = await JsonSerializer.DeserializeAsync<List<VaultManifest>>(file) ?? new();

            if (!ValidateChildJsons())
                throw new Exception();
        }

        private bool ValidateChildJsons()
        {
            foreach (var jsonFile in _manifests)
            {
                if (!File.Exists(Path.Combine(_jsonStoreDirectory, jsonFile.Name + ".json")))
                    return false;
            }

            return true;
        }

        public async Task CreateVault(string filepath, string vaultName)
        {
            _manifests = new();
            _jsonStoreDirectory = Path.Combine(new FileInfo(filepath).Directory.FullName, vaultName);
            await SaveVaultAsync(filepath);

            if (!Directory.Exists(_jsonStoreDirectory))
                Directory.CreateDirectory(_jsonStoreDirectory);
        }

        public async Task SaveVaultAsync(string filepath)
        {
            string jsonManifest = JsonSerializer.Serialize(_manifests);
            await File.WriteAllTextAsync(filepath, jsonManifest);
        }

        public async Task AddAsync(string identifier, string jsonFile)
        {
            if (_manifests.Any(v => v.Identifier == identifier))
            {
                throw new Exception();
            }

            Guid uuid = Guid.NewGuid();
            string strUuid = uuid.ToString();

            _manifests.Add(new() {
                Identifier = identifier,
                Name = strUuid
            });;

            await File.WriteAllTextAsync(Path.Combine(_jsonStoreDirectory, strUuid + ".json"), jsonFile);
        }

        public async Task<string?> GetAsync(string identifier)
        {
            var file = _manifests.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return null;

            string uuid = file.First().Name;

            return await File.ReadAllTextAsync(Path.Combine(_jsonStoreDirectory, uuid + ".json"));
        }

        public bool Delete(string identifier)
        {
            var file = _manifests.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return false;

            string uuid = file.First().Name;

            File.Delete(Path.Combine(_jsonStoreDirectory, uuid + ".json"));

            _manifests.RemoveAll(v => v.Identifier == identifier);

            return true;
        }
    }
}