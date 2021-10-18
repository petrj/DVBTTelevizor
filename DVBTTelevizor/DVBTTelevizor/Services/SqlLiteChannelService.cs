using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using SQLite;

namespace DVBTTelevizor
{
    public class SqlLiteChannelService : ChannelService
    {
        public SqlLiteChannelService(ILoggingService logingService, DVBTTelevizorConfiguration config) : base(logingService, config)
        {
        }

        public override string DBPath
        {
            get
            {
                return Path.Combine(BaseViewModel.AndroidMediaDirectory, "channels.sqllite");
            }
        }

        public override async Task<ObservableCollection<DVBTChannel>> LoadChannels()
        {
            return await Task.Run(() =>
            {
                var res = new ObservableCollection<DVBTChannel>();

                if (File.Exists(DBPath))
                {
                    var db = new SQLiteConnection(DBPath);

                    foreach (var channel in db.Table<DVBTChannel>())
                    {
                        res.Add(channel);
                    }
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
                    var db = new SQLiteConnection(DBPath);

                    db.DropTable<DVBTChannel>();

                    db.CreateTable<DVBTChannel>();

                    foreach (var channel in channels)
                    {
                        db.Insert(channel);
                    }

                    db.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex);

                    return false;
                }
            });
        }
    }
}
