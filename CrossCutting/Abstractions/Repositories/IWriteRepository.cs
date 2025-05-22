using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Abstractions.Repositories
{
    public interface IWriteRepository<T>: IBaseRepository<T>
        where T : IBaseEntity
    {
        public Task<bool> AddAsync(T Entity);
        public Task AddRangeAsync(List<T> Entities);
        public Task UpdateRange(List<T> Entities);
        public Task Update(T Entity);
        public Task RemoveRange(List<T> Entities);
        public Task Remove(T Entity);
        

    }
}
