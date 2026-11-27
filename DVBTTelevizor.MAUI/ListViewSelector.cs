using DVBTTelevizor.TV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    internal class ListViewSelector
    {
        private ObservableCollection<IListViewSelector> _items;

        public ListViewSelector(ObservableCollection<IListViewSelector> items)
        {
            SetItems(items);
        }

        public void SetItems(ObservableCollection<IListViewSelector> items)
        {
            _items = items;
        }

        public Action OnItemChanged { get; set; }

        public IListViewSelector? GetSelectedItem()
        {
            foreach (var ch in _items)
            {
                if (ch.Selected)
                    return ch;
            }

            return null;
        }

        public void DeselectAll()
        {
            SetSelectedItem(null);
        }

        public void SetSelectedItem(IListViewSelector? item)
        {
            bool fireOnChanged = false;

            foreach (var ch in _items)
            {
                var fireNotifyChannelChange = false;

                if (item != null && ch.UniqueIdentifier == item.UniqueIdentifier)
                {
                    if (!ch.Selected || !ch.Focused)
                    {
                        ch.Selected = true;
                        ch.Focused = true;
                        fireOnChanged = true;
                        fireNotifyChannelChange = true;
                    }
                } else
                {
                    if (ch.Selected || ch.Focused)
                    {
                        ch.Selected = false;
                        fireNotifyChannelChange = true;
                        fireOnChanged = true;
                    }
                }

                if (fireNotifyChannelChange)
                {
                    ch.NotifyChanges();
                }
            }

            if (fireOnChanged)
            {
                if (OnItemChanged != null)
                {
                    OnItemChanged();
                }
            }
        }

        public IListViewSelector? SelectFirstItem()
        {
            foreach (var ch in _items)
            {
                SetSelectedItem(ch);
                return ch;
            }

            return null;
        }

        public IListViewSelector? SelectLastItem()
        {
            foreach (var ch in _items.Reverse())
            {
                SetSelectedItem(ch);
                return ch;
            }

            return null;
        }

        public IListViewSelector? SelectItemByNumber(string num)
        {
            foreach (var ch in _items)
            {
                if (ch.Number == num)
                {
                    SetSelectedItem(ch);
                    return ch;
                }
            }

            return null;
        }

        public IListViewSelector? SelectPreviousItem()
        {
            var selFound = false;
            foreach (var ch in _items.Reverse())
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
                    SetSelectedItem(ch);
                    return ch;
                }
            }

            return SelectLastItem();
        }

        public IListViewSelector? SelectNextItem()
        {
            var selFound = false;
            foreach (var ch in _items)
            {
                if (!selFound)
                {
                    if (ch.Selected)
                    {
                        selFound = true;
                    }
                } else
                {
                    SetSelectedItem(ch);
                    return ch;
                }
            }

            return SelectFirstItem();
        }
    }
}

