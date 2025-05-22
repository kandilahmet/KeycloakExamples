using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Repositories.EFCore;
using Microsoft.EntityFrameworkCore;
using OrderService.Core.Domain.Entities;
using OrderService.Infrastructure.Persistence.EFCore.DbContexts;

namespace OrderService.Infrastructure.Persistence.EFCore.Repositories
{
    public class EFCoreReadRepository<T>
        : EFCoreBaseRepository<T, DbContext>,
            IEFCoreReadRepository<T>
        where T : BaseEntity
    {
        public EFCoreReadRepository(MsSqlOrderDbContext msSqlDbContext)
            : base(msSqlDbContext) { }

        public async Task<List<T>> GetAll()
        {
            var queryable = Table;

            return await queryable.ToListAsync();
        }

        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> expression)
        {
            return await Table.Where(expression).FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetWhere(Expression<Func<T, bool>> expression)
        {
            return await Table.Where(expression).ToListAsync();
        }
    }
}
