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

        public void SetSelectedChannel(Channel? channel)
        {
            foreach (var ch in _channels)
            {
                if (ch == channel)
                {
                    ch.Selected = true;
                } else
                {
                    ch.Selected = false;
                }
                ch.NotifyChanges();
            }
        }

        public void SelectFirstsChannel()
        {
            foreach (var ch in _channels)
            {
                SetSelectedChannel(ch);
                break;
            }
        }

        public void SelectNextChannel()
        {
            var selFound = false;
            var selected = false;
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
                    selected = true;
                    break;
                }
            }

            if (!selected)
            {
                SelectFirstsChannel();
            }
        }
    }
}

