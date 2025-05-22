
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Repositories.EFCore;
using OrderService.Infrastructure.Persistence.EFCore.DbContexts;
using OrderService.Core.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.EFCore.Repositories
{
    public class EFCoreWriteRepository<T> : EFCoreBaseRepository<T,DbContext>,IEFCoreWriteRepository<T> where T : BaseEntity
    {
        public EFCoreWriteRepository(MsSqlOrderDbContext msSqlDbContext):base(msSqlDbContext)
        {
            
        }
        public async Task<bool> AddAsync(T Entity)
        {
            ArgumentNullException.ThrowIfNull(Entity);

            EntityEntry<T> entityEntry = await Table.AddAsync(Entity);
            return entityEntry.State == EntityState.Added;
        }

        public async Task AddRangeAsync(List<T> Entities)
        {
            await Table.AddRangeAsync(Entities);
        }

        public Task RemoveRange(List<T> Entities)
        {
            //if (!Entities.Any())
            //    throw new ArgumentNullException(nameof(Entities));

            Table.RemoveRange(Entities);//Entities boş oldugunda RemoveRange hata vermiyor fakat kontrol etmek iyidir.
            return Task.CompletedTask;
        }

        public Task Remove(T Entity)
        {
            Table.Remove(Entity);

           return Task.CompletedTask;
        }
        public async Task<bool> SaveAsync()
        {
           return await _context.SaveChangesAsync() > 0;
        }

        public Task UpdateRange(List<T> Entities)
        {
            if (!Entities.Any())
                throw new ArgumentNullException(nameof(Entities));

            Table.UpdateRange(Entities);

            return Task.CompletedTask;
        }


        public Task Update(T Entity)
        {
            Table.Update(Entity);

            return Task.CompletedTask;
        }
    }
}
