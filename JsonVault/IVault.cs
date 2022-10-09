using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonVault
{
    internal interface IVault
    {
        public Task LoadVauldAsync(string filepath);

        public Task CreateVaultAsync(string filepath, string directoryName);

        public Task SaveVaultAsync();

        public bool Delete(string identifier);

        public bool Contains(string identifier);

        public void Lock(string identifier, LockLevel lockLevel);

        public void Unlock(string identifier);

        public LockLevel GetLockStatus(string identifier);
    }
}
