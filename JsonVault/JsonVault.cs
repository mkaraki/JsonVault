using System.Text;
using System.Text.Json;

namespace JsonVault
{
    public class JsonVault
    {
        private List<VaultManifest> _manifests = new();
        private string _manifestFile = string.Empty;
        private string _jsonStoreDirectory = string.Empty;

        /// <summary>
        /// Load vault from manifest and vault name.
        /// </summary>
        /// <param name="filepath">vault manifest file path</param>
        /// <param name="vaultName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception">vault name</exception>
        public async Task LoadVauldAsync(string filepath, string vaultName)
        { 
            if (!File.Exists(filepath))
                throw new FileNotFoundException();

            _manifestFile = new FileInfo(filepath).FullName;
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

        /// <summary>
        /// Create vault with manifest file path and vault name.
        /// </summary>
        /// <param name="filepath">manifest file path</param>
        /// <param name="vaultName">vault name</param>
        /// <returns></returns>
        public async Task CreateVault(string filepath, string vaultName)
        {
            _manifests = new();
            _manifestFile = new FileInfo(filepath).FullName;
            _jsonStoreDirectory = Path.Combine(new FileInfo(filepath).Directory.FullName, vaultName);
            await SaveVaultAsync();

            if (!Directory.Exists(_jsonStoreDirectory))
                Directory.CreateDirectory(_jsonStoreDirectory);
        }

        public async Task SaveVaultAsync()
        {
            string jsonManifest = JsonSerializer.Serialize(_manifests);
            await File.WriteAllTextAsync(_manifestFile, jsonManifest);
        }

        /// <summary>
        /// Add json/text data with encoding.
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <param name="jsonFile">Raw content</param>
        /// <param name="encoding">Encoding</param>
        /// <exception cref="Exception"></exception>
        public async Task AddAsync(string identifier, string jsonFile, Encoding encoding)
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

        /// <summary>
        /// Get file with identifier.
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>null (no exact identifier file) or file content</returns>
        public async Task<string?> GetAsync(string identifier, Encoding encoding)
        {
            var file = _manifests.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return null;

            string uuid = file.First().Name;

            return await File.ReadAllTextAsync(Path.Combine(_jsonStoreDirectory, uuid + ".json"));
        }

        /// <summary>
        /// Delete file with identifier
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>Success or not</returns>
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