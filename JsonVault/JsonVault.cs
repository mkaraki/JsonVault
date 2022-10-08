using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace JsonVault
{
    public class JsonVault : IDisposable
    {
        private VaultManifest _manifest = new();
        private string _manifestFile = string.Empty;
        private string _jsonStoreDirectory = string.Empty;

        private MemoryCache _cache = new(new MemoryCacheOptions());

        /// <summary>
        /// Load vault from manifest.
        /// </summary>
        /// <param name="filepath">vault manifest file path</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception">vault name</exception>
        public async Task LoadVauldAsync(string filepath)
        { 
            if (!File.Exists(filepath))
                throw new FileNotFoundException();

            _manifestFile = new FileInfo(filepath).FullName;

            using (var file = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                _manifest = await JsonSerializer.DeserializeAsync<VaultManifest>(file) ?? new();

            _jsonStoreDirectory = Path.Combine(GetParentDirectoryFullPath(filepath), _manifest.DirectoryName);

            if (!ValidateChildJsons())
                throw new Exception();
        }

        private string GetParentDirectoryFullPath(string filepath)
        { 
            var parent = new FileInfo(filepath).Directory;
            if (parent == null)
                throw new Exception();
        
            return parent.FullName;
        }

        private bool ValidateChildJsons()
        {
            foreach (var jsonFile in _manifest.Files)
            {
                if (!File.Exists(Path.Combine(_jsonStoreDirectory, jsonFile.Name)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Create vault with manifest file path and file directory name
        /// </summary>
        /// <param name="filepath">manifest file path</param>
        /// <param name="directoryName">file directory name</param>
        /// <returns></returns>
        public async Task CreateVaultAsync(string filepath, string directoryName)
        {
            _manifest = new();
            _manifestFile = new FileInfo(filepath).FullName;
            _jsonStoreDirectory = Path.Combine(GetParentDirectoryFullPath(filepath), directoryName);

            if (!Directory.Exists(_jsonStoreDirectory))
                Directory.CreateDirectory(_jsonStoreDirectory);
            
            _manifest.DirectoryName = directoryName;
            await SaveVaultAsync();
        }

        public async Task SaveVaultAsync()
        {
            string jsonManifest = JsonSerializer.Serialize(_manifest);
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
            if (_manifest.Files.Any(v => v.Identifier == identifier))
            {
                throw new Exception();
            }

            Guid uuid = Guid.NewGuid();
            string strUuid = uuid.ToString();

            _manifest.Files.Add(new() {
                Identifier = identifier,
                Name = strUuid
            });;

            await File.WriteAllTextAsync(Path.Combine(_jsonStoreDirectory, strUuid), jsonFile, encoding);
            _cache.Set(identifier, jsonFile);
        }

        /// <summary>
        /// Get file with identifier.
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>null (no exact identifier file) or file content</returns>
        public async Task<string?> GetAsync(string identifier, Encoding encoding)
        {
            if (_cache.TryGetValue(identifier, out string cachedfile))
                return cachedfile;

            var file = _manifest.Files.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return null;

            string uuid = file.First().Name;

            string actualfile = await File.ReadAllTextAsync(Path.Combine(_jsonStoreDirectory, uuid), encoding);

            _cache.Set(identifier, actualfile);

            return actualfile;
        }

        /// <summary>
        /// Delete file with identifier
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>Success or not</returns>
        public bool Delete(string identifier)
        {
            var file = _manifest.Files.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return false;

            string uuid = file.First().Name;

            File.Delete(Path.Combine(_jsonStoreDirectory, uuid));

            _manifest.Files.RemoveAll(v => v.Identifier == identifier);
            _cache.Remove(identifier);

            return true;
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}