using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Abstractions.Repositories
{
    public interface IReadRepository<T> : IBaseRepository<T>
        where T : IBaseEntity
    {
        public Task<List<T>> GetAll();
        public Task<List<T>> GetWhere(Expression<Func<T, bool>> expression);
        public Task<T?> GetSingleAsync(Expression<Func<T, bool>> expression);
    }
}
