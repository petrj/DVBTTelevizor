using Android.Content;
using Android.Preferences;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public abstract class CustomSharedPreferencesObject
    {
        private ISharedPreferences _sharedPrefs;
        private ISharedPreferencesEditor _prefsEditor;
        private Context _context;

        public CustomSharedPreferencesObject()
        {
            _context = Android.App.Application.Context;
            _sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            _prefsEditor = _sharedPrefs.Edit();
        }

        protected void SavePersistingSettingValue<T>(string key, T value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    _prefsEditor.PutString(key, value.ToString());
                }
                if (typeof(T) == typeof(bool))
                {
                    _prefsEditor.PutBoolean(key, Convert.ToBoolean(value));
                }
                if (typeof(T) == typeof(int))
                {
                    _prefsEditor.PutInt(key, Convert.ToInt32(value));
                }

                _prefsEditor.Commit();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        protected T GetPersistingSettingValue<T>(string key, T defaultValue = default(T))
        {
            T result = defaultValue;

            try
            {
                object val;

                if (typeof(T) == typeof(string))
                {
                    val = _sharedPrefs.GetString(key, default(string));
                }
                else
                if (typeof(T) == typeof(bool))
                {
                    val = _sharedPrefs.GetBoolean(key, default(bool));
                }
                else
                if (typeof(T) == typeof(int))
                {
                    val = _sharedPrefs.GetInt(key, default(int));
                }
                else
                {
                    val = default(T);
                }

                result = (T)Convert.ChangeType(val, typeof(T));

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }

    }
}
