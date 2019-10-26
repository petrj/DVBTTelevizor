using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class MainPageViewModel :  INotifyPropertyChanged
    {
        //private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private ChannelsService _channelsService;
        private DVBTDriverManager _driver;
        private DVBTTelevizorConfiguration _config;

        private long _tuneFrequency = 730;
        private long _tuneBandwidth = 8;
        private int _tuneDVBTType = 0;

        private string _status;        

        bool isBusy = false;

        public Command RefreshCommand { get; set; }
        public Command AutomaticTuneCommand { get; set; }

        private Channel _selectedChannel;

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();


        #region INotifyPropertyChanged

        protected bool SetProperty<T>(ref T backingStore, T value,
           [CallerMemberName]string propertyName = "",
           Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public MainPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _channelsService = new ChannelsService(_loggingService, _driver, config);
            _config = config;

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfiguration", (message) =>
            {
                _driver.Configuration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
                Status = $"Initialized ({_driver.Configuration.DeviceName})";
                _driver.Start();
            });

            MessagingCenter.Subscribe<string>(this, "DVBTDriverConfigurationFailed", (message) =>
            {                
                Status = $"Initialization failed ({message})";                
            });

            RefreshCommand = new Command(async () => await Refresh());
            AutomaticTuneCommand = new Command(async () => await AutomaticTune());

            RefreshCommand.Execute(null);
        }

        public Channel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _selectedChannel = value;                

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public long TuneFrequency
        {
            get
            {
                return _tuneFrequency;
            }
            set
            {
                _tuneFrequency = value;

                OnPropertyChanged(nameof(TuneFrequency));
            }
        }

        public long TuneBandwidth
        {
            get
            {
                return _tuneBandwidth;
            }
            set
            {
                _tuneBandwidth = value;

                OnPropertyChanged(nameof(TuneBandwidth));
            }
        }

        public int TuneDVBTType
        {
            get
            {
                return _tuneDVBTType;
            }
            set
            {
                _tuneDVBTType = value;

                OnPropertyChanged(nameof(TuneDVBTType));
            }
        }
        
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                //SetProperty(ref isBusy, value);
                //isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private async Task Refresh()
        {
            _loggingService.Info($"Refreshing channels");

            await RunWithPermission(Permission.Storage, async () =>
            {
                try
                {                   

                    IsBusy = true;

                    await _channelsService.Load();
                    Channels.Clear();

                    foreach (var ch in _channelsService.Channels)
                    {
                        Channels.Add(ch);
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }

        private async Task AutomaticTune()
        {
            Status = "Searching channels ...";
            
            Channels.Clear();
            _channelsService.Channels.Clear();
            
            try
            {
                IsBusy = true;

                long freq = TuneFrequency * 1000000;
                long bandWidth = TuneBandwidth * 1000000;
                int type = TuneDVBTType;
                var tuneRes = await _driver.Tune(freq, bandWidth, type);

                Status = $"Tuning freq. {TuneFrequency} Mhz ...";

                var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(freq, bandWidth, type);

                switch (searchMapPIDsResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        Status = "Search error";
                        break;
                    case SearchProgramResultEnum.NoSignal:
                        Status = "No signal";
                        break;
                    case SearchProgramResultEnum.NoProgramFound:
                        Status = "No program found";
                        break;
                    case SearchProgramResultEnum.OK:
                        var mapPIDs = new List<long>();
                        foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                        {
                            mapPIDs.Add(sd.Value);
                        }
                        Status = $"Program MAP PIDs found: {String.Join(",", mapPIDs)}";
                        break;
                }

                if (searchMapPIDsResult.Result != SearchProgramResultEnum.OK)
                {
                    return;
                }

                // searching PIDs

                foreach (var sDescriptor in searchMapPIDsResult.ServiceDescriptors)
                {
                    Status = $"Searching Map PID {sDescriptor.Value}";

                    var searchPIDsResult = await _driver.SearchProgramPIDs(Convert.ToInt32(sDescriptor.Value));

                    switch (searchPIDsResult.Result)
                    {
                        case SearchProgramResultEnum.Error:
                            Status = $"Error scanning Map PID {sDescriptor.Value}";
                            break;
                        case SearchProgramResultEnum.NoSignal:
                            Status = "No signal";
                            break;
                        case SearchProgramResultEnum.NoProgramFound:
                            Status = "No program found";
                            break;
                        case SearchProgramResultEnum.OK:
                            var pids = string.Join(",", searchPIDsResult.PIDs);

                            var ch = new Channel();
                            ch.PIDs = pids;
                            ch.ProgramMapPID = sDescriptor.Value;
                            ch.Name = sDescriptor.Key.ServiceName;
                            ch.ProviderName = sDescriptor.Key.ProviderName;
                            ch.Frequency = freq;
                            ch.Bandwdith = bandWidth;
                            ch.Number = Channels.Count + 1;
                            ch.DVBTType = type;

                            Channels.Add(ch);
                            //_channelsService.Channels.Add(ch);

                            Status = $"Added {sDescriptor.Key.ServiceName}";

                            break;
                    }
                }

                Status = $"Tuning finished";

                await SaveChannels();

            }
            catch (Exception ex)
            {
                Status = $"Request failed ({ex.Message})";
            }
            finally
            {
                IsBusy = false;                
            }            
        }

        private async Task SaveChannels()
        {
            await RunWithPermission(Permission.Storage, async () =>
            {
                _channelsService.Channels.Clear();
                _channelsService.Channels.AddRange(Channels);
                await _channelsService.Save();
            });            
        }

        public async Task RunWithPermission(Permission perm, List<Command> commands)
        {
            var f = new Func<Task>(
                 async () =>
                 {
                     foreach (var command in commands)
                     {
                         await Task.Run(() => command.Execute(null));
                     }
                 });

            await RunWithPermission(perm, f);
        }

        public async Task RunWithPermission(Permission perm, Func<Task> action)
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(perm);
                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(perm))
                    {
                        await _dialogService.Information("Application requires permission", "Information");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(perm);

                    if (results.ContainsKey(perm))
                        status = results[perm];
                }

                if (status == PermissionStatus.Granted)
                {
                    await action();
                }
                else
                {
                    await _dialogService.Error("Missing permissions", "Error");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }     

    }
}
