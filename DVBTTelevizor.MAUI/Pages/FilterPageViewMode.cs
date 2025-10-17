using DVBTTelevizor.TV;
using LoggerService;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class FilterPageViewModel : BaseViewModel
    {
        public ObservableCollection<MultiplexInfo> Multiplexes { get; } = new ObservableCollection<MultiplexInfo>();
        private ITVConfiguration _tvConfiguration;
        private ILoggingService _loggingService;

        public FilterPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _tvConfiguration = tvConfiguration;
            _loggingService = loggingService;
        }

        public void FillMultiplexes()
        {
            _loggingService.Debug("FilterPageViewModel FillMultiplexes");

            try
            {
                Multiplexes.Clear();

                var chs = _tvConfiguration.GetChannels();
                var nameToInfo = new Dictionary<string, MultiplexInfo>();

                foreach (var ch in chs)
                {
                    if (!nameToInfo.ContainsKey(ch.ProviderName))
                    {
                        var info = new MultiplexInfo(ch.ProviderName);

                        nameToInfo.Add(ch.ProviderName, info);
                        Multiplexes.Add(info);

                        info.NotifyChanges();
                    }
                }

                OnPropertyChanged(nameof(Multiplexes));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public void DeSelectAll(bool notify = true)
        {
            foreach (var mux in Multiplexes)
            {
                mux.Selected = false;
                if (notify)
                    mux.NotifyChanges();
            }
        }

        public bool SelectedLast()
        {
            foreach (var mux in Multiplexes.Reverse())
            {
                if (mux.Selected)
                {
                    return true;
                }
                return false;
            }

            return false;
        }

        public MultiplexInfo SelectNext(bool reversed = false)
        {
            try
            {
                var found = false;
                MultiplexInfo first = null;

                var mxs = reversed ? Multiplexes.Reverse() : Multiplexes;

                foreach (var mux in mxs)
                {
                    if (first == null)
                    {
                        first = mux;
                    }

                    if (found)
                    {
                        mux.Selected = true;
                        mux.NotifyChanges();
                        return mux;
                    }
                    else
                    {
                        if (mux.Selected)
                        {
                            mux.Selected = false;
                            mux.NotifyChanges();
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    if (first != null)
                    {
                        first.Selected = true;
                        first.NotifyChanges();
                    }
                    return first;
                }

                return null;
            } finally
            {
                OnPropertyChanged(nameof(Multiplexes));
            }
        }
    }
}

