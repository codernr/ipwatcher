using System;
using System.Threading.Tasks;
using Windows.Storage;

using Newtonsoft.Json;

namespace IPWatcher
{
    internal class Config
    {
        #region Config items
        public string DeviceName { get; set; }

        public int UpdateHours { get; set; }

        public string IpAddress { get; set; }

        public string SmtpServer { get; set; }

        public int Port { get; set; }

        public bool Ssl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
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
            var content = JsonConvert.SerializeObject(Instance);

            await FileIO.WriteTextAsync(file, content);
        }
    }
}
