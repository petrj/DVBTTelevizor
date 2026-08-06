using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Services
{
    public static class GeoHelper
    {
        public static async Task<(string? position, string? description)> GetGeoPositionAsync()
        {
            // Ensure the whole permission request + location retrieval runs on the main thread
            if (!MainThread.IsMainThread)
            {
                return await MainThread.InvokeOnMainThreadAsync(async () => await GetGeoPositionAsync());
            }

            try
            {
                // Permission requests must be invoked on the main thread on some platforms
                var permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus != PermissionStatus.Granted)
                    return (null, null);

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var location = await Geolocation.GetLocationAsync(request);
                if (location == null)
                    return (null, null);

                var pos = string.Format(CultureInfo.InvariantCulture, "{0:0.000000},{1:0.000000}", location.Latitude, location.Longitude);

                string? description = null;

                try
                {
                    // Simple reverse-geocoding: fill description on Android and Windows (WinUI) using Geocoding API
                    var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
                    var p = placemarks?.FirstOrDefault();
                    if (p != null)
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        if (!string.IsNullOrWhiteSpace(p.Thoroughfare)) parts.Add(p.Thoroughfare);
                        if (!string.IsNullOrWhiteSpace(p.SubLocality)) parts.Add(p.SubLocality);
                        if (!string.IsNullOrWhiteSpace(p.Locality)) parts.Add(p.Locality);
                        if (!string.IsNullOrWhiteSpace(p.AdminArea)) parts.Add(p.AdminArea);
                        if (!string.IsNullOrWhiteSpace(p.CountryName)) parts.Add(p.CountryName);

                        if (parts.Count > 0)
                            description = string.Join(", ", parts);
                        else if (!string.IsNullOrWhiteSpace(p.FeatureName))
                            description = p.FeatureName;
                    }

                }
                catch
                {
                    // ignore reverse geocoding failures, return coordinates only
                }

                return (pos, description);
            }
            catch (Exception ex)
            {
                return (null, null);
            }
        }
    }
}
