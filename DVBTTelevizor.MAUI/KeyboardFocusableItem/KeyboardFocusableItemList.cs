using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class KeyboardFocusableItemList
    {
        private KeyboardFocusableItem _focusedItem = null;

        public IList<KeyboardFocusableItem> Items { get; set; }

        public event KeyboardFocusableItemEventDelegate OnItemFocusedEvent;
        public event KeyboardFocusableItemEventDelegate OnItemUnFocusedEvent;

        public KeyboardFocusDirection LastFocusDirection { get; private set; } = KeyboardFocusDirection.UnKnown;
        public string LastFocusedItemName { get; set; } = null;

        public KeyboardFocusableItem FocusedItem
        {
            get
            {
                return _focusedItem;
            }
        }

        public string FocusedItemName
        {
            get
            {
                if (_focusedItem == null)
                    return null;

                return _focusedItem.Name;
            }
        }

        public KeyboardFocusableItemList()
        {
            Items = new List<KeyboardFocusableItem>();
        }

        public KeyboardFocusableItemList AddItem(KeyboardFocusableItem item)
        {
            Items.Add(item);
            return this;
        }

        public KeyboardFocusableItem GetItemByName(string name)
        {
            foreach (var item in Items)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }

        public void FocusNextItem(bool onlyVisible=false)
        {
            if (Items.Count == 0)
                return;

            var selectNext = false;

            KeyboardFocusableItem? itemToSelect = null;
            KeyboardFocusableItem? first = null;

            foreach (var item in Items)
            {
                // select first available
                if (
                    (_focusedItem == null) &&
                    (
                        (onlyVisible == false) || (onlyVisible && item.IsVisible)
                    )
                   )
                {
                    itemToSelect = item;
                    break;
                }

                if (
                    (first == null) &&
                    (
                        (onlyVisible == false) || (onlyVisible && item.IsVisible)
                    )
                   )
                {
                    first = item;
                }

                if (selectNext &&
                    (
                        (onlyVisible == false) || (onlyVisible && item.IsVisible)
                    )
                   )
                {
                    itemToSelect = item;
                    break;
                } else
                {
                    if (item == _focusedItem)
                    {
                        selectNext = true;
                    }
                }
            }

            if (itemToSelect == null)
            {
                itemToSelect = first;
            }

            if (itemToSelect != null)
            {
                FocusItem(itemToSelect.Name, KeyboardFocusDirection.Next);
            }
        }

        public void FocusPreviousItem(bool onlyVisible = false)
        {
            if (Items.Count == 0)
                return;

            if (_focusedItem == null)
            {
                FocusItem(Items[Items.Count - 1].Name, KeyboardFocusDirection.Previous);
                return;
            }

            KeyboardFocusableItem? itemToSelect = null;
            KeyboardFocusableItem? prevItem = null;

            foreach (var item in Items)
            {
                // select last available
                if (
                        (_focusedItem == null) &&
                        (
                            (onlyVisible == false) || (onlyVisible && item.IsVisible)
                        )
                   )
                {
                    itemToSelect = item;
                }
                else
                if (prevItem == null)
                {
                    prevItem = item;
                }
                else
                {
                    if (item == _focusedItem)
                    {
                        break;
                    }

                    if ((onlyVisible == false) || (onlyVisible && item.IsVisible))
                    {
                        itemToSelect = item;
                    }
                }
            }

            if (itemToSelect == null)
            {
                itemToSelect = prevItem;
            }

            if (itemToSelect != null)
            {
                FocusItem(itemToSelect.Name, KeyboardFocusDirection.Previous);
            }
        }

        public void FocusItem(string name, KeyboardFocusDirection focusDirection = KeyboardFocusDirection.UnKnown)
        {
            _focusedItem = null;
            DeFocusAll();

            var item = GetItemByName(name);

            if (item != null)
            {
                _focusedItem = item;
                item.Focus();

                LastFocusedItemName = name;
                LastFocusDirection = focusDirection;

                // raise event
                if (OnItemFocusedEvent != null)
                    OnItemFocusedEvent(new KeyboardFocusableItemEventArgs(item));
            }
        }

        public void DeFocusAll()
        {
            foreach (var item in Items)
            {
                item.DeFocus();

                // raise event
                if (OnItemUnFocusedEvent != null)
                    OnItemUnFocusedEvent(new KeyboardFocusableItemEventArgs(item));
            }

            _focusedItem = null;
        }

        public void FocusAll()
        {
            foreach (var item in Items)
            {
                item.Focus();

                // raise event
                if (OnItemUnFocusedEvent != null)
                    OnItemUnFocusedEvent(new KeyboardFocusableItemEventArgs(item));
            }
        }
    }
}
