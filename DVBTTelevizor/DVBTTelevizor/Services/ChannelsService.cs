using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using SQLite;

namespace DVBTTelevizor
{
    public class ChannelsService
    {
        public List<Channel> Channels = new List<Channel>();

        private ILoggingService _log = new BasicLoggingService();
        private DVBTDriverManager _driver;

        public ChannelsService(ILoggingService logingService, DVBTDriverManager driver)
        {
            _log = logingService;
            _driver = driver;
         }

        public static string DBPath
        {
            get
            {
                return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "channels.sqlLite");
            }
        }

        public async Task<bool> Load()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Channels.Clear();

                    var db = new SQLiteConnection(DBPath);

                   foreach (var channel in db.Table<Channel>())
                   {
                        Channels.Add(channel);
                   }

                   return true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex);

                    return false;
                }
            });
        }

        public async Task<bool> Save()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var db = new SQLiteConnection(DBPath);

                    db.DropTable<Channel>();

                    db.CreateTable<Channel>();

                    foreach (var channel in Channels)
                    {
                        db.Insert(channel);
                    }

                    db.Close();

                    return true;

                } catch (Exception ex)
                {
                    _log.Error(ex);

                    return false;
                }
            });
        }
    }
}
