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
            foreach (var ch in _channels)
            {
                ch.Selected = false;
                ch.NotifyChanges();
            }
        }

        public void SetSelectedChannel(Channel? channel)
        {
            foreach (var ch in _channels)
            {
                if (ch == channel)
                {
                    ch.Selected = true;
                    ch.Focused = true;
                } else
                {
                    ch.Selected = false;
                    ch.Focused = false;
                }
                ch.NotifyChanges();
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

