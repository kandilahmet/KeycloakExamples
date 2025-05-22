using CrossCutting.Abstractions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories.EFCore
{
    public interface IEFCoreWriteRepository<T> : IWriteRepository<T>
        where T : IBaseEntity
    {
        public Task<bool> SaveAsync();
    }
}
