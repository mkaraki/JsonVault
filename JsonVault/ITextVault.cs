using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonVault
{
    internal interface ITextVault : IVault
    {
        public Task<string?> GetAsync(string identifier);

        public Task AddAsync(string identifier, string jsonFile);

        public Task UpdateAsync(string identifier, string jsonFile);
    }
}
