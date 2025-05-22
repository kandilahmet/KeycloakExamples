using CrossCutting.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using OrderService.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Persistence.EFCore.Repositories
{
    public class EFCoreBaseRepository<T,TContext> : IBaseRepository<T>
        where T : BaseEntity
        where TContext :DbContext

    {
        protected readonly TContext _context;
        public EFCoreBaseRepository(TContext context)
        {
            _context = context; 
        }
        public DbSet<T> Table { get => _context.Set<T>(); }
    }
}
