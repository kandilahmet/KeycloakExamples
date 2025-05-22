using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Cache.Redis
{
    public class RedisDbContext
    {
        private readonly string _connectionString; 

        private ConnectionMultiplexer _ConnectionMultiplexer;

        public RedisDbContext(string connectionString)
        {
            _connectionString=connectionString;
        }
        public IDatabase GetDatabase(int dbIndex)
        { 
            _ConnectionMultiplexer = ConnectionMultiplexer.Connect(_connectionString);
            return _ConnectionMultiplexer.GetDatabase(dbIndex);
        }
    }
}
