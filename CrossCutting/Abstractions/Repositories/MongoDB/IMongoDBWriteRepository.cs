using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Abstractions.Repositories.MongoDB
{
    public interface IMongoDBWriteRepository<T>:IWriteRepository<T>
        where T:IBaseEntity
    {
    }
}
