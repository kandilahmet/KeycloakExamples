using CrossCutting.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace OrderService.Infrastructure.Cache.Redis
{
    public class RedisService : ICacheService
    {
        

        private readonly IDatabase _db;
        public RedisService(RedisDbContext redisDbContext)
        {
            _db = redisDbContext.GetDatabase(0);
        }
        public async Task AddAsyncList<T>(string key, T value) => await _db.ListRightPushAsync(key, JsonConvert.SerializeObject(value));
        public void AddList<T>(string key, T value) => _db.ListRightPush(key, JsonConvert.SerializeObject(value));
        public List<T> GetList<T>(string key) where T : class, new()
        {
            List<T> model = new List<T>();
            _db.ListRange(key, 0, -1).ToList().ForEach(x => model.Add(JsonConvert.DeserializeObject<T>(x)));
            return model;
        }
        public async Task<List<T>> GetAsyncList<T>(string key) where T : class, new()
        {
            List<T> model = new List<T>();
            _db.ListRangeAsync(key, 0, -1).Result.ToList().ForEach(x => model.Add(JsonConvert.DeserializeObject<T>(x)));
            return model;
        }
        public void DeleteList<T>(string key, T value) where T : class, new()
        {
            _db.ListRemove(key, JsonConvert.SerializeObject(value));
        }
        public async Task AddAsyncString<T>(string key, T value, TimeSpan? sldExp) => await _db.StringSetAsync(key, JsonConvert.SerializeObject(value), sldExp);
        public void AddString<T>(string key, T value, TimeSpan? sldExp) => _db.StringSet(key, JsonConvert.SerializeObject(value), sldExp);
        public async Task DeleteAsyncString(string key) => await _db.KeyDeleteAsync(key);
        public void DeleteString(string key) => _db.KeyDelete(key);
        public async Task<T?> GetAsyncString<T>(string key) => JsonConvert.DeserializeObject<T>(_db.StringGetAsync(key).Result);
        public T? GetString<T>(string key) => JsonConvert.DeserializeObject<T>(_db.StringGet(key) ); 
        public bool IsExists(string key) => _db.KeyExists(key);
 
    }


}
