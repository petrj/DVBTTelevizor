using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Services
{
    public static class GeoHelper
    {
        public static async Task<(string? position, string? description)> GetGeoPositionAsync()
        {
            try
            {
                // Permission requests must be invoked on the main thread on some platforms
                var permissionStatus = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Permissions.RequestAsync<Permissions.LocationWhenInUse>());
                if (permissionStatus != PermissionStatus.Granted)
                    return (null, null);

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var location = await Geolocation.GetLocationAsync(request);
                if (location == null)
                    return (null, null);

                var pos = string.Format(CultureInfo.InvariantCulture, "{0:0.000000},{1:0.000000}", location.Latitude, location.Longitude);

                // Reverse geocoding omitted here (platform differences). Return coordinates only.
                return (pos, null);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
