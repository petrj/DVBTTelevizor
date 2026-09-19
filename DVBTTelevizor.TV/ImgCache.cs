using LoggerService;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;

namespace DVBTTelevizor.TV
{
    public class ImgCache
    {
        public const string ImgCacheFolderName = "img";
        private string _publicDirectory;
        private string _cacheDirectory;

        private ILoggingService _loggingService;

        public ImgCache(ILoggingService loggingService, IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingService;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _cacheDirectory = Path.Combine(_publicDirectory, ImgCacheFolderName);
        }

        public string CacheFolder
        {
            get
            {
                return _cacheDirectory;
            }
        }

        public async Task<bool> DownloadDABSlide(Channel ch, DABSlide slide)
        {
            _loggingService.Info($"ImgCache: DownloadDABSlide: channel={ch?.Name}, name={slide?.ContentName}");

            try
            {
                if (ch == null)
                {
                    _loggingService.Info($"ImgCache: DownloadDABSlide: Invalid channel");
                    return false;
                }

                if (slide == null || slide.ImageBytes == null || slide.ImageBytes.Length == 0)
                {
                    _loggingService.Info($"ImgCache: DownloadDABSlide: Invalid slide data.");
                    return false;
                }

                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }

                var mimeParts = slide.MimeType.Split('/');

                if (mimeParts == null || mimeParts.Length != 2)
                {
                    throw new InvalidDataException();
                }

                var mimeType = mimeParts[0];    // "image"
                var mimeExt = mimeParts[1];    // "png"

                var normalizedFileName = ToNormalizedFileName(ch.UniqueIdentifier) + "." + mimeExt;

                var imgPath = Path.Join(_cacheDirectory, normalizedFileName);

                if (File.Exists(imgPath))
                {
                    _loggingService.Info($"ImgCache: DownloadDABSlide: File already exists: {imgPath}");

                    var existingBytes = File.ReadAllBytes(imgPath);
                    var cacheHash = MD5.HashData(existingBytes);
                    var imgHash = MD5.HashData(slide.ImageBytes);

                    if (cacheHash.AsSpan().SequenceEqual(imgHash))
                    {
                        return false; // same image
                    } else
                    {
                        File.Delete(imgPath);
                    }
                }

                ch.IconUrl = normalizedFileName;
                System.IO.File.WriteAllBytes(imgPath, slide.ImageBytes);

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"ImgCache: DownloadDABSlide: Exception: {ex.Message}");
                return false;
            }
        }

        public async Task DownloadChannelsIcons(ObservableCollection<Channel> channels)
        {
            _loggingService.Info($"ImgCache: DownloadChannelsIcons: channels.Count={channels?.Count}");

            if (channels == null || channels.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                foreach (var channel in channels)
                {
                    if ((channel.ChannelType != ChannelTypeEnum.SledovaniTV) || (channel.Name == null))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(channel.IconUrl))
                    {
                        var ext = GetFileExtensionFromUrl(channel.IconUrl);
                        string normalizedFileName = ToNormalizedFileName(channel.UniqueIdentifier) + ext;
                        string localFilePath = Path.Combine(_cacheDirectory, normalizedFileName);

                        if (!Directory.Exists(_cacheDirectory))
                        {
                            Directory.CreateDirectory(_cacheDirectory);
                        }

                        if (!File.Exists(localFilePath))
                        {
                            try
                            {
                                using (var client = new System.Net.WebClient())
                                {
                                    client.DownloadFile(channel.IconUrl, localFilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                _loggingService.Error($"ImgCache: Failed to download icon for channel {channel.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            });
        }

        public static string GetFileExtensionFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return ".png";
            }

            try
            {
                // try to read it as file name extension
                var ext = System.IO.Path.GetExtension(url);

                if (ext != null && ext.Length > 1 && ext.StartsWith("."))
                {
                    return ext;
                }

                // Parse the main URL
                var uri = new Uri(url);

                // Check if there is an embedded URL in the query string (e.g., image=http...)
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                string targetUrl = queryParams["image"] ?? url;

                // Unescape in case the target URL is URL-encoded
                string unescapedUrl = Uri.UnescapeDataString(targetUrl);

                // Get the absolute path without query parameters or fragments
                var targetUri = new Uri(unescapedUrl);
                string absolutePath = targetUri.AbsolutePath;

                // Extract the extension (e.g., ".png")
                return Path.GetExtension(absolutePath);
            }
            catch (UriFormatException)
            {
                return ".png";
            }
        }

        public static string ToNormalizedFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // 1. Normalize diacritics
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                // Keep non-spacing marks out (this removes accents)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString().Normalize(NormalizationForm.FormC);

            // 2. Convert to Lowercase
            result = result.ToLowerInvariant();

            // 3. Remove spaces
            result = result.Replace(" ", string.Empty);

            // 4. Remove invalid file/path characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                result = result.Replace(invalidChar.ToString(), string.Empty);
            }

            // 5. Optional: Clean up any remaining non-alphanumeric characters (if needed)
            // result = Regex.Replace(result, @"[^a-z0-9._-]", string.Empty);

            return result;
        }
    }
}
