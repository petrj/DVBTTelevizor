using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class VLCLauncher
    {
        public static void RunInWindows(string url)
        {
            string vlcPath = @"C:\Program Files\VideoLAN\VLC\vlc.exe"; // Adjust if VLC is installed elsewhere

            if (!System.IO.File.Exists(vlcPath))
            {
                Console.WriteLine("VLC not found at " + vlcPath);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = vlcPath,
                Arguments = $"\"{url}\" --fullscreen --quiet",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            try
            {
                using Process process = Process.Start(startInfo);
                Console.WriteLine("VLC started with URL: " + url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting VLC: {ex.Message}");
            }
        }
    }
}
