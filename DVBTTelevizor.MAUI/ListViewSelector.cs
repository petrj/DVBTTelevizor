using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    internal class ListViewSelector
    {
        private ObservableCollection<Channel> _channels;

        public ListViewSelector(ObservableCollection<Channel> channels)
        {
            _channels = channels;
        }

        public Action OnChannelChanged { get; set; }

        public Channel? GetSelectedChannel()
        {
            foreach (var ch in _channels)
            {
                if (ch.Selected)
                    return ch;
            }

            return null;
        }

        public void DeselectAll()
        {
            SetSelectedChannel(null);
        }

        public void SetSelectedChannel(Channel? channel)
        {
            bool fireOnChanged = false;

            foreach (var ch in _channels)
            {
                var fireNotifyChannelChange = false;

                if (ch == channel)
                {
                    if (!ch.Selected && OnChannelChanged != null)
                    {
                        fireOnChanged = true;
                        fireNotifyChannelChange = true;
                        ch.Selected = true;
                        ch.Focused = true;
                    }
                } else
                {
                    if (ch.Selected)
                    {
                        ch.Selected = false;
                        fireNotifyChannelChange = true;
                    }

                    if (ch.Focused)
                    {
                        ch.Focused = false;
                        fireNotifyChannelChange = true;
                    }
                }

                if (fireNotifyChannelChange)
                {
                    ch.NotifyChanges();
                }
            }

            if (fireOnChanged)
            {
                OnChannelChanged();
            }
        }

        public Channel? SelectFirstChannel()
        {
            foreach (var ch in _channels)
            {
                SetSelectedChannel(ch);
                return ch;
            }

            return null;
        }

        public Channel? SelectLastChannel()
        {
            foreach (var ch in _channels.Reverse())
            {
                SetSelectedChannel(ch);
                return ch;
            }

            return null;
        }

        public Channel? SelectChannelByNumber(string num)
        {
            foreach (var ch in _channels)
            {
                if (ch.Number == num)
                {
                    SetSelectedChannel(ch);
                    return ch;
                }
            }

            return null;
        }



        public Channel? SelectPreviousChannel()
        {
            var selFound = false;
            foreach (var ch in _channels.Reverse())
            {
                if (!selFound)
                {
                    if (ch.Selected)
                    {
                        selFound = true;
                    }
                }
                else
                {
                    SetSelectedChannel(ch);
                    return ch;
                }
            }

            return SelectLastChannel();
        }

        public Channel? SelectNextChannel()
        {
            var selFound = false;
            foreach (var ch in _channels)
            {
                if (!selFound)
                {
                    if (ch.Selected)
                    {
                        selFound = true;
                    }
                } else
                {
                    SetSelectedChannel(ch);
                    return ch;
                }
            }

            return SelectFirstChannel();
        }
    }
}

