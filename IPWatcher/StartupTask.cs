using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace IPWatcher
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            this.deferral = taskInstance.GetDeferral();
        }

        private async Task Initialize()
        {
            await Config.CreateInstance();
        }

        private async Task<string> GetExternalIP()
        {
            HttpClient client = new HttpClient();

            string body;

            try
            {
                var response = await client.GetAsync(new Uri("http://icanhazip.com"));
                response.EnsureSuccessStatusCode();
                body = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                body = null;
            }

            return body;
        }
    }
}
