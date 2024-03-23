using DVBTTelevizor.Models;
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

namespace DVBTTelevizor.Services
{
    public abstract class IDVBTManager<T> where T : new()
    {
        public virtual string Key { get; set; } = "DVB";

        protected static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        protected ILoggingService _log;
        protected IDVBTDriverManager _driver;

        private ConcurrentQueue<Dictionary<string, List<T>>> _saveQueue = new ConcurrentQueue<Dictionary<string, List<T>>>();
        private Dictionary<string, List<T>> _freqValues { get; set; } = new Dictionary<string, List<T>>();

        public IDVBTManager(ILoggingService loggingService, IDVBTDriverManager driver)
        {
            _log = loggingService;
            _driver = driver;

            var saveDBsWorker = new BackgroundWorker();
            saveDBsWorker.DoWork += SaveWorker_DoWork;
            saveDBsWorker.RunWorkerAsync();
        }

        public virtual void AddItemsToDB(long freq, long programMapPID, List<T> items)
        {
            var freqKey = GetKey(freq, programMapPID);
            var dict = new Dictionary<string, List<T>>();
            dict.Add(freqKey, items);
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

        private string GetKey(long freq, long programMapPID)
        {
            return $"{freq}.{programMapPID}";
        }

        public virtual void Clear()
        {
            _log.Debug($"[IDVBTManager] Clear");

            try
            {
                var folder = Path.Combine(BaseViewModel.AndroidAppDirectory, Key);
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
            var folder = Path.Combine(BaseViewModel.AndroidAppDirectory, Key);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{GetKey(freq,programMapPID)}.sqllite");
        }

        public virtual string GetDBFullPath(string freqKey)
        {
            var folder = Path.Combine(BaseViewModel.AndroidAppDirectory, Key);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, $"{freqKey}.sqllite");
        }

        public virtual List<T> GetValues(long freq, long programMapPID)
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
                _freqValues.Add(freqKey, res);
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
        }
    }
}
