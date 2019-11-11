using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using Newtonsoft.Json;
using SQLite;

namespace DVBTTelevizor
{
    public class JSONChannelsService : ChannelService
    {
        public JSONChannelsService(ILoggingService logingService, DVBTTelevizorConfiguration config)
            :base(logingService, config)
        {

        }

        public override string DBPath
        {
            get
            {
                return Path.Combine(_config.StorageFolder, "channels.json");
            }
        }

        public override async Task<ObservableCollection<DVBTChannel>> LoadChannels()
        {
            return await Task.Run(() =>
            {
                var res = new ObservableCollection<DVBTChannel>();                

                if (File.Exists(DBPath))
                {
                    var jsonFromFile = File.ReadAllText(DBPath);

                    res = JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(jsonFromFile);
                }
                
                return res;
            });
        }

        public override async Task<bool> SaveChannels(ObservableCollection<DVBTChannel> channels)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(DBPath))
                    {
                        File.Delete(DBPath);
                    }

                    File.WriteAllText(DBPath, JsonConvert.SerializeObject(channels));

                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex);

                    return false;
                }
            });
        }
        /*

        public async Task<bool> Load()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Channels.Clear();

                    var db = new SQLiteConnection(DBPath);

                   foreach (var channel in db.Table<DVBTChannel>())
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

                    db.DropTable<DVBTChannel>();

                    db.CreateTable<DVBTChannel>();

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
        */
    }
}
