using System; 
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Abstractions.Repositories
{
    public interface IBaseRepository<T>
         where T : IBaseEntity
    { 
    }
}
