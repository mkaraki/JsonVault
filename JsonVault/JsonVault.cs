using Microsoft.Extensions.Caching.Memory;
using PeterO.Cbor;
using System.Text;

namespace JsonVault
{
    public class JsonVault : TextVault
    {
        public override async Task<string?> GetAsync(string identifier)
        {
            if (_cache.TryGetValue(identifier, out string cachedfile))
                return cachedfile;

            var file = _manifest.Files.Where(v => v.Identifier == identifier);

            if (file.Count() == 0)
                return null;

            string uuid = file.First().Name;

            using (var fs = new FileStream(Path.Combine(_jsonStoreDirectory, uuid), FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                var cbor = await Task.Run (() => CBORObject.Read(fs));
                await Task.Run(() => cbor.WriteJSONTo(ms));

                string text = Encoding.UTF8.GetString(ms.ToArray());

                _cache.Set(identifier, text);

                return text;
            }
        }

        public override async Task AddAsync(string identifier, string jsonFile)
        {
            if (Contains(identifier))
                throw new Exception("Identifier already exist");

            using (var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonFile)))
            {
                var cbor = CBORObject.ReadJSON(jsonStream);

                Guid uuid = Guid.NewGuid();
                string strUuid = uuid.ToString();

                _manifest.Files.Add(new() {
                    Identifier = identifier,
                    Name = strUuid
                });;

                await Task.Run (() => {
                    using (var cborStream = new FileStream(Path.Combine(_jsonStoreDirectory, strUuid), FileMode.Create, FileAccess.Write))
                        cbor.WriteTo(cborStream);
                });
            }

            _cache.Set(identifier, jsonFile);
        }
    }
}
