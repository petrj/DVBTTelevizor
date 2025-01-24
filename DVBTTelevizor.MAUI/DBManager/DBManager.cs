using DVBTTelevizor.MAUI;
using LibVLCSharp.Shared;
using LoggerService;
using MPEGTS;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace DVBTTelevizor.DBManager
{
    public abstract class DBManager<T> where T : new()
    {
        public virtual string Key { get; set; } = "DB";

        protected static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        protected ILoggingService _log;
        protected IDriverConnector _driver;
        protected string _publicDirectory;

        private ConcurrentQueue<Dictionary<string, List<T>>> _saveQueue = new ConcurrentQueue<Dictionary<string, List<T>>>();
        protected ConcurrentDictionary<string, List<T>> _freqValues { get; set; } = new ConcurrentDictionary<string, List<T>>();

        public DBManager(ILoggingService loggingService, IPublicDirectoryProvider publicDirectoryProvider, IDriverConnector driver)
        {
            _log = loggingService;
            _driver = driver;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

            var saveDBsWorker = new BackgroundWorker();
            saveDBsWorker.DoWork += SaveWorker_DoWork;
            saveDBsWorker.RunWorkerAsync();
        }

        public virtual void AddItemsToDB(long freq, long programMapPID, List<T> items)
        {
            var freqKey = GetKey(freq, programMapPID);
            AddItemsToDB(freqKey, items);
        }

        public virtual void AddItemsToDB(string key, List<T> items)
        {
            var dict = new Dictionary<string, List<T>>();
            dict.Add(key, items);
            _saveQueue.Enqueue(dict);
        }

        /// <summary>
        /// FIFO queue for saving items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void SaveWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (_saveQueue.Count > 0)
                {
                    if (_saveQueue.TryDequeue(out var dict))
                    {
                        foreach (var kvp in dict)
                        {
                            var dbName = GetDBFullPath(kvp.Key);

                            var db = new SQLiteConnection(dbName);

                            db.DropTable<T>();

                            db.CreateTable<T>();

                            foreach (var ev in kvp.Value)
                            {
                                db.Insert(ev);
                            }

                            db.Close();

                            _freqValues[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else
                {
                    // waiting for some items to save
                    Thread.Sleep(200);
                }
            }
        }

        protected string GetKey(long freq, long programMapPID)
        {
            return $"{freq}.{programMapPID}";
        }

        public virtual void Clear()
        {
            _log.Debug($"[IDBManager] Clear");

            try
            {
                var folder = Path.Combine(_publicDirectory, Key);
                Directory.Delete(folder, true);

                _freqValues.Clear();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public virtual string GetDBFullPath(long freq, long programMapPID)
        {
            var folder = Path.Combine(_publicDirectory, Key);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{GetKey(freq,programMapPID)}.sqllite");
        }

        public virtual string GetDBFullPath(string freqKey)
        {
            var folder = Path.Combine(_publicDirectory, Key);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{freqKey}.sqllite");
        }

        public virtual List<T> GetValues(long freq, long programMapPID)
        {
            try
            {
                var res = new List<T>();

                var freqKey = GetKey(freq, programMapPID);

                if (_freqValues.ContainsKey(freqKey) &&
                    _freqValues[freqKey] != null &&
                    _freqValues[freqKey].Count > 0)
                {
                    // in cache

                    return _freqValues[freqKey];
                }

                if (!_freqValues.ContainsKey(freqKey))
                {
                    _freqValues.TryAdd(freqKey, res);
                }

                var dbPath = GetDBFullPath(freq, programMapPID);
                if (!File.Exists(dbPath))
                {
                    return null;
                }

                // adding to cache

                var db = new SQLiteConnection(dbPath);

                foreach (var item in db.Table<T>())
                {
                    res.Add(item);
                }

                db.Close();

                return res;
            } catch (Exception ex)
            {
                _log.Error(ex);

                return null;
            }
        }
    }
}
