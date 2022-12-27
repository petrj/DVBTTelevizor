using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class KeyboardFocusableItemList
    {
        private KeyboardFocusableItem _focusedItem = null;

        public IList<KeyboardFocusableItem> Items { get; set; }

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

        public void FocusNextItem()
        {
            if (Items.Count == 0)
                return;

            if (_focusedItem == null)
            {
                FocusItem(Items[0].Name);
                return;
            }

            var selectNext = false;

            KeyboardFocusableItem itemToSelect = null;
            KeyboardFocusableItem firstItem = null;

            foreach (var item in Items)
            {
                if (firstItem == null)
                {
                    firstItem = item;
                }

                if (selectNext)
                {
                    itemToSelect = item;
                    break;
                }

                if (item == _focusedItem)
                {
                    selectNext = true;
                }
            }

            if (itemToSelect == null)
            {
                itemToSelect = firstItem;
            }

            FocusItem(itemToSelect.Name);
        }

        public void FocusPreviousItem()
        {
            if (Items.Count == 0)
                return;

            if (_focusedItem == null)
            {
                FocusItem(Items[Items.Count-1].Name);
                return;
            }

            KeyboardFocusableItem itemToSelect = null;
            KeyboardFocusableItem prevItem = null;

            foreach (var item in Items)
            {
                if (prevItem == null)
                {
                    prevItem = item;
                }
                else
                {
                    if (item == _focusedItem)
                    {
                        itemToSelect = prevItem;
                        break;
                    }
                    else
                    {
                        prevItem = item;
                    }
                }
            }

            if (itemToSelect == null)
            {
                itemToSelect = prevItem; // the last one item in collection
            }

            /*



                    if (firstItem == null)
                {
                    firstItem = item;

                    //
                }

                if (selectNext)
                {
                    itemToSelect = item;
                    break;
                }

                if (item == _focusedItem)
                {
                    selectNext = true;
                }*/




            FocusItem(itemToSelect.Name);
        }

        public void FocusItem(string name)
        {
            _focusedItem = null;
            DeFocusAll();

            var item = GetItemByName(name);

            if (item != null)
            {
                _focusedItem = item;
                item.Focus();
            }
        }

        public void DeFocusAll()
        {
            foreach (var item in Items)
            {
                item.DeFocus();
            }
        }

        public void FocusAll()
        {
            foreach (var item in Items)
            {
                item.Focus();
            }
        }
    }
}
