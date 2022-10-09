using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace JsonVault
{
    public class TextVault : IDisposable, ITextVault
    {
        internal VaultManifest _manifest = new();
        protected string _manifestFile = string.Empty;
        protected string _jsonStoreDirectory = string.Empty;

        protected MemoryCache _cache;

        protected Dictionary<string, LockLevel> _locks = new();

        public TextVault(): this(new MemoryCacheOptions())
        {
        }

        public TextVault(MemoryCacheOptions memoryCacheOptions)
        {
            _cache = new(memoryCacheOptions);
        }

        /// <summary>
        /// Load vault from manifest.
        /// </summary>
        /// <param name="filepath">vault manifest file path</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception">vault name</exception>
        public virtual async Task LoadVauldAsync(string filepath)
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
        public virtual async Task CreateVaultAsync(string filepath, string directoryName)
        {
            _manifest = new();
            _manifestFile = new FileInfo(filepath).FullName;
            _jsonStoreDirectory = Path.Combine(GetParentDirectoryFullPath(filepath), directoryName);

            if (!Directory.Exists(_jsonStoreDirectory))
                Directory.CreateDirectory(_jsonStoreDirectory);
            
            _manifest.DirectoryName = directoryName;
            await SaveVaultAsync();
        }

        /// <summary>
        /// Save vault manifest json
        /// </summary>
        /// <returns></returns>
        public virtual async Task SaveVaultAsync()
        {
            string jsonManifest = JsonSerializer.Serialize(_manifest);
            await File.WriteAllTextAsync(_manifestFile, jsonManifest);
        }

        /// <summary>
        /// Add text data.
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <param name="jsonFile">Raw content</param>
        /// <exception cref="Exception"></exception>
        public virtual async Task AddAsync(string identifier, string jsonFile)
        {
            if (Contains(identifier))
                throw new Exception("Identifier already exist");

            await UpdateAsync(identifier, jsonFile);
        }

        /// <summary>
        /// Update text data
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <param name="jsonFile">Raw content</param>
        /// <returns></returns>
        public virtual async Task UpdateAsync(string identifier, string jsonFile)
        {
            if (_locks.ContainsKey(identifier) && _locks[identifier] != LockLevel.None)
                throw new Exception("Locked");

            Lock(identifier, LockLevel.Exclusive);

            Guid uuid = Guid.NewGuid();
            string strUuid = uuid.ToString();

            if (Contains(identifier))
            {
                strUuid = _manifest.Files.Where(v => v.Identifier == identifier).First().Name;
            }
            else
            { 
                _manifest.Files.Add(new() {
                    Identifier = identifier,
                    Name = strUuid
                });;
            }

            await File.WriteAllTextAsync(Path.Combine(_jsonStoreDirectory, strUuid), jsonFile, Encoding.UTF8);
            _cache.Set(identifier, jsonFile);
            Unlock(identifier);
        }

        /// <summary>
        /// Get file with identifier.
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>null (no exact identifier file or locked) or file content</returns>
        public virtual async Task<string?> GetAsync(string identifier)
        {
            if (_locks.ContainsKey(identifier) && _locks[identifier] == LockLevel.Exclusive)
                return null;

            if (_cache.TryGetValue(identifier, out string cachedfile))
                return cachedfile;

            var file = _manifest.Files.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return null;

            string uuid = file.First().Name;

            string actualfile = await File.ReadAllTextAsync(Path.Combine(_jsonStoreDirectory, uuid), Encoding.UTF8);

            _cache.Set(identifier, actualfile);

            return actualfile;
        }

        /// <summary>
        /// Delete file with identifier
        /// </summary>
        /// <param name="identifier">Unique string id</param>
        /// <returns>Success or not</returns>
        public virtual bool Delete(string identifier)
        {
            if (_locks.ContainsKey(identifier) && _locks[identifier] != LockLevel.None)
                return false;

            var file = _manifest.Files.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return false;

            string uuid = file.First().Name;

            File.Delete(Path.Combine(_jsonStoreDirectory, uuid));

            _manifest.Files.RemoveAll(v => v.Identifier == identifier);
            _cache.Remove(identifier);

            return true;
        }

        public virtual void Dispose()
        {
            _cache.Dispose();
        }

        public virtual bool Contains(string identifier)
        {
            return _manifest.Files.Any(v => v.Identifier == identifier);
        }

        public void Lock(string identifier, LockLevel lockLevel)
        {
            if (lockLevel == LockLevel.None)
            { 
                Unlock(identifier);
                return;
            }

            if (_locks.ContainsKey(identifier))
                _locks[identifier] = lockLevel;
            else
                _locks.Add(identifier, lockLevel);
        }

        public void Unlock(string identifier)
        {
            if (_locks.ContainsKey(identifier))
                _locks.Remove(identifier);
        }

        public LockLevel GetLockStatus(string identifier)
        {
            if (_locks.ContainsKey(identifier))
                return _locks[identifier];
            else
                return LockLevel.None;
        }
    }
}