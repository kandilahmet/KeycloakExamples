using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Abstractions
{
    public interface ICacheService
    {
        void AddString<T>(string key, T value, TimeSpan? sldExp);
        Task AddAsyncString<T>(string key, T value, TimeSpan? sldExp);
        T GetString<T>(string key);
        Task<T> GetAsyncString<T>(string key);
        void DeleteString(string key);
        Task DeleteAsyncString(string key);
        void AddList<T>(string key, T value);
        Task AddAsyncList<T>(string key, T value);
        List<T> GetList<T>(string key) where T : class, new();
        Task<List<T>> GetAsyncList<T>(string key) where T : class, new();
        void DeleteList<T>(string key, T value) where T : class, new();
        bool IsExists(string key);

    }
}
