using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LoggerService
{
    public class TCPIPLoggingService : ILoggingService
    {
        public string Url { get; set; }
        private LoggingLevelEnum _minLevel;

        public LoggingLevelEnum MinLevel { get => _minLevel; set => _minLevel = value; }

        public TCPIPLoggingService(string url, LoggingLevelEnum minLevel = LoggingLevelEnum.Debug)
        {
            Url = url;
            MinLevel = minLevel;
        }

        private async Task Send(string message, LoggingLevelEnum level)
        {
            try
            {
                if ((int)level < (int)MinLevel)
                    return;

                if (String.IsNullOrEmpty(message))
                    return;

                message = message.Replace("=", "_").Replace("&", "_");
                var sb = new StringBuilder();

                sb.Append($"text={message}");
                sb.Append($"&time={DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}");
                sb.Append($"&level={level}");

                var data = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = data.Length;
                webRequest.Timeout = 1000;

                using (Stream webpageStream = webRequest.GetRequestStream())
                {
                    await webpageStream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger error: {ex}");
            }
        }

        public void Debug(string message)
        {
            Task.Run( async () =>  await Send(message, LoggingLevelEnum.Debug));
        }

        public void Error(Exception ex, string message = null)
        {
            Task.Run(async () => await Send(message, LoggingLevelEnum.Error));
        }

        public void Error( string message)
        {
            Task.Run(async () => await Send(message, LoggingLevelEnum.Error));
        }

        public void Info(string message)
        {
            Task.Run(async () => await Send(message, LoggingLevelEnum.Info));
        }
    }
}
