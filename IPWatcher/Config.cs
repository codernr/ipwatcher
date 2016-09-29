using System;
using System.Threading.Tasks;
using Windows.Storage;

using Newtonsoft.Json;

namespace IPWatcher
{
    internal class Config
    {
        #region Config items

        /// <summary>
        /// The name of the device the app runs on (preconfigured)
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Period of checking external IP (preconfigured)
        /// </summary>
        public int UpdateHours { get; set; }

        /// <summary>
        /// The site to check external IP on (preconfigured)
        /// </summary>
        public string ExternalIPCheckAddress { get; set; }

        /// <summary>
        /// Recipient e-mail where the updates are sent (preconfigured)
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// SMTP server address (preconfigured)
        /// </summary>
        public string SmtpServer { get; set; }

        /// <summary>
        /// Port number (preconfigured)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Use SSL (preconfigured)
        /// </summary>
        public bool Ssl { get; set; }

        /// <summary>
        /// SMTP username (preconfigured)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// SMTP password (preconfigured)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Subject of notification email (preconfigured)
        /// </summary>
        public string EmailSubject { get; set; }

        /// <summary>
        /// Formatted string of email body with DeviceName and IP placeholders (preconfigured)
        /// e.g. "The IP address of {0} has changed to {1}"
        /// </summary>
        public string EmailBodyFormat { get; set; }

        /// <summary>
        /// Exponential backoff retry count
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Exponential backoff base
        /// e. g. if you set this to 2 and there is a connection exception, retry comes in
        /// 2, 4, 8, ..., 2 ^ RetryCount seconds
        /// </summary>
        public int RetryBaseSeconds { get; set; }

        /// <summary>
        /// External ip of the device
        /// </summary>
        public string IpAddress { get; set; }
        #endregion

        public static Config Instance { get; private set; }

        private const string fileName = "config.json";

        private static StorageFile file;

        public static async Task CreateInstance()
        {
            file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

            var content = await FileIO.ReadTextAsync(file);

            Instance = JsonConvert.DeserializeObject<Config>(content);
        }

        public static async Task SaveInstance()
        {
            var content = JsonConvert.SerializeObject(Instance, Formatting.Indented);

            await FileIO.WriteTextAsync(file, content);
        }
    }
}
