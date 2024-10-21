using Serilog;
using ServerEngine.Config;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ServerEngine.Database.Redis
{
    // Redis의 경우 외부 config를 수정할 경우 RedisClient 및 서버 재시작 필요 
    //  - initialize가 아닌 생성자에서 초기화 
    public static class RedisHashIndex
    {
        public static readonly int HASH_COUNT = 10;
        public static string GetHashIndexString(long uid) => GetHashIndex(uid).ToString();
        public static int GetHashIndex(long uid) => (int)(uid % HASH_COUNT);
    }

    public abstract class BaseRedis
    {
        private bool mDisposed = false;
        private readonly NewtonsoftSerializer mSerializer = new NewtonsoftSerializer();
        protected Log.ILogger Logger { get; private set; }
        protected IConfigRedis Config { get; private set; }

        protected ConnectionMultiplexer? Redis { get; private set; }

        public BaseRedis(IConfigRedis config, Log.ILogger logger)
        {
            this.Config = config;
            this.Logger = logger;

            var ip = config.address.ip;
            var port = config.address.port;
            var db_index = config.address.db_index;

            if (string.IsNullOrEmpty(ip) || port <= 0)
            {
                Logger.Error($"Error in BaseRedis() - Address is null. ip = {ip}, port = {port}");
                return;
            }

            var redis_config = new ConfigurationOptions();
            redis_config.EndPoints.Add(ip, config.address.port);
            redis_config.AllowAdmin = true;
            redis_config.ConnectRetry = config.connect_retry;
            redis_config.ReconnectRetryPolicy = new LinearRetry(maxRetryElapsedTimeAllowedMilliseconds: config.reconnect_timeout_sec * 1000);
            redis_config.ClientName = config.name;
            redis_config.Password = config.password;
            redis_config.DefaultDatabase = db_index;
            redis_config.Ssl = config.ssl;
            redis_config.KeepAlive = config.keep_alive_sec;
            redis_config.SyncTimeout = config.sync_timeout_sec * 1000;

            try
            {
                Redis = ConnectionMultiplexer.Connect(redis_config);
                if (false == Redis.IsConnected)
                {
                    Logger.Error($"Error in BaseRedis() - Fail to Connect Redis. name = {config.name}, ip = {ip}, port = {port}, db_index = {db_index}");
                    Redis.Dispose();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in BaseRedis() - {ex.Message} - {ex.StackTrace}");
                Redis?.Dispose();
            }
        }

        public IServer? GetServer(EndPoint endpoint, object? asyncState = null)
        {
            return Redis?.GetServer(endpoint, asyncState);
        }

        public IServer? GetServer(IPAddress address, int port)
        {
            return Redis?.GetServer(address, port);
        }

        public IDatabase? GetDatabase()
        {
            return Redis?.GetDatabase();
        }

        public ISubscriber? GetSubscriber()
        {
            return Redis?.GetSubscriber();
        }

        public EndPoint[]? GetEndPoints(bool configure_only = false)
        {
            return Redis?.GetEndPoints(configure_only);
        }

        #region Command


        #endregion
    }

    public abstract class BaseMultiRedis : IDisposable
    {
        private bool mDisposed = false;
        private readonly NewtonsoftSerializer mSerializer = new NewtonsoftSerializer();
        protected Log.ILogger Logger { get; private set; }
        protected IConfigMultiRedis Config { get; private set; }
        protected Dictionary<int, ConnectionMultiplexer> ListRedis { get; private set; } = new Dictionary<int, ConnectionMultiplexer>();

        public BaseMultiRedis(IConfigMultiRedis config, Log.ILogger logger)
        {
            this.Config = config;
            this.Logger = logger;
            
            foreach(var address in config.list_address)
            {
                if (string.IsNullOrEmpty(address.ip) || address.port <= 0)
                {
                    logger.Error($"Error in BaseMultiRedis() - Address is null. ip = {address.ip}, port = {address.port}");
                    continue;
                }

                var redis_config = new ConfigurationOptions();
                redis_config.EndPoints.Add(address.ip, address.port);
                redis_config.AllowAdmin = true;
                redis_config.ConnectRetry = config.connect_retry;
                redis_config.ReconnectRetryPolicy = new LinearRetry(maxRetryElapsedTimeAllowedMilliseconds: config.reconnect_timeout_sec * 1000);
                redis_config.ClientName = config.name;
                redis_config.Password = config.password;
                redis_config.DefaultDatabase = address.db_index;
                redis_config.Ssl = config.ssl;
                redis_config.KeepAlive = config.keep_alive_sec;
                redis_config.SyncTimeout = config.sync_timeout_sec * 1000;

                ConnectionMultiplexer? redis = null;
                try
                {
                    redis = ConnectionMultiplexer.Connect(redis_config); 
                    if (false == redis.IsConnected)
                    {
                        Logger.Error($"Error in BaseMultiRedis() - Fail to Connect Redis. name = {config.name}, ip = {address.ip}, port = {address.port}, db_index = {address.db_index}");
                        redis.Dispose();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception in BaseMultiRedis() - {ex.Message} - {ex.StackTrace}");
                    redis?.Dispose();
                    continue;
                }

                ListRedis.Add(address.db_index, redis);
            }
        }

        public IServer? GetServerByDBIdex(int db_index)
        {
            if (false == ListRedis.TryGetValue(db_index, out var redis))
                return null;

            var endpoint = (IPEndPoint?)redis.GetEndPoints().FirstOrDefault();
            return null != endpoint ? redis.GetServer(endpoint.Address, endpoint.Port) : null;
        }

        public IServer? GetServerByAddress(string address, int port, object? async_state = null)
        {
            foreach(var redis in ListRedis)
            {
                var server = GetServerByDBIdex(redis.Key);
                if (null != server)
                {
                    var endpoint = (IPEndPoint)server.EndPoint;
                    if (IPAddress.Parse(address) == endpoint.Address && port == endpoint.Port)
                        return server;
                }
            }

            return null;
        }

        public IEnumerable<IServer>? GetListServerByDBIndex(int db_index)
        {
            if (false == ListRedis.TryGetValue(db_index, out var redis))
                yield break;

            var endpoints = (IPEndPoint[])redis.GetEndPoints();
            foreach (var endpoint in endpoints)
                yield return redis.GetServer(endpoint.Address, endpoint.Port);
        }

        public IDatabase? GetDatabaseByHashIndex(long uid)
        {
            var index = RedisHashIndex.GetHashIndex(uid);
            if (false == ListRedis.TryGetValue(index, out var redis))
                return null;

            return redis.GetDatabase();
        }

        public ISubscriber? GetSubscriberByHashIndex(long uid)
        {
            var index = RedisHashIndex.GetHashIndex(uid);
            if (false == ListRedis.TryGetValue(index, out var redis))
                return null;

            return redis.GetSubscriber();
        }

        #region Command
        public bool KeyDelete(long uid, string key, CommandFlags flags = CommandFlags.None)
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return db.KeyDelete(key, flags);
        }

        public async ValueTask<bool> KeyDeleteAsync(long uid, string key, CommandFlags flags = CommandFlags.None)
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return await db.KeyDeleteAsync(key, flags);
        }

        public bool SetData<T>(long uid, string key, T value, int expire_sec) where T : class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return db.StringSet(key, mSerializer.Serialize(value), TimeSpan.FromSeconds(expire_sec));
               
        }

        public async ValueTask<bool> SetDataAsync<T>(long uid, string key, T value, int expire_sec) where T : class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return await db.StringSetAsync(key, mSerializer.Serialize(value), TimeSpan.FromSeconds(expire_sec));
        }

        public bool SetHashData<T>(long uid, string key, string hash_field_key, T value, When when = When.Always, CommandFlags flags = CommandFlags.None) where T : class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return db.HashSet(key, hash_field_key, mSerializer.Serialize(value), when, flags);    
        }

        public async ValueTask<bool> SetHashDataAsync<T>(long uid, string key, string hash_field_key, T value, When when = When.Always, CommandFlags flags = CommandFlags.None) where T : class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return false;

            return await db.HashSetAsync(key, hash_field_key, mSerializer.Serialize(value), when, flags);
        }

        public T? GetHashData<T>(long uid, string key, string hash_field_key, CommandFlags flags = CommandFlags.None) where T: class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return default;

            RedisValue hash_field_value = db.HashGet(key, hash_field_key, flags);
            if (false == hash_field_value.HasValue)
                return default;

            byte[]? bytes = (byte[]?)hash_field_value;
            if (null == bytes)
                return default;
            
            return mSerializer.Deserialize<T>(serializedObject: bytes);
        }
 
        public async ValueTask<T?> GetHashDataAsync<T>(long uid, string key, string hash_field_key, CommandFlags flag = CommandFlags.None) where T : class
        {
            var db = GetDatabaseByHashIndex(uid);
            if (null == db)
                return default;

            RedisValue hash_field_value = await db.HashGetAsync(key, hash_field_key, flag);
            if (false == hash_field_value.HasValue)
                return default;

            byte[]? bytes = (byte[]?)hash_field_value;
            if (null == bytes)
                return default;

            return mSerializer.Deserialize<T>(serializedObject: bytes);
        }

        public bool FlushAll()
        {
            try
            {
                foreach(var redis in ListRedis)
                {
                    var list_server = redis.Value.GetServers();
                    foreach (var server in list_server)
                        server.FlushAllDatabases();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in BaseRedis.FlushAll() - {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        public async ValueTask<bool> FlushAllAsync()
        {
            try
            {
                foreach(var redis in ListRedis)
                {
                    var list_server = redis.Value.GetServers();
                    foreach (var server in list_server)
                        await server.FlushAllDatabasesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in BaseRedis.FlushAll() - {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        #endregion

        public virtual void Dispose()
        {
            if (mDisposed)
                return;

            foreach (var redis in ListRedis.Values)
                redis.Dispose();

            mDisposed = true;
        }
    }
}
