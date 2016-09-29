using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LightBuzz.SMTP;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.System.Threading;
using Windows.Web.Http;

namespace IPWatcher
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            this.deferral = taskInstance.GetDeferral();

            this.Initialize();
        }

        private async Task Initialize()
        {
            await Config.CreateInstance();

            // running check for the first time
            this.Check();

            // and then start the timer to call it periodically
            ThreadPoolTimer.CreatePeriodicTimer(this.Check, TimeSpan.FromHours(Config.Instance.UpdateHours));
        }

        private async void Check(ThreadPoolTimer timer = null)
        {
            var ip = await this.GetExternalIP();

            if (ip != Config.Instance.IpAddress)
            {
                Config.Instance.IpAddress = ip;

                await Config.SaveInstance();

                await this.SendMail();
            }
        }

        private async Task<string> GetExternalIP()
        {
            HttpClient client = new HttpClient();

            string body;

            try
            {
                var response = await client.GetAsync(new Uri(Config.Instance.ExternalIPCheckAddress));
                response.EnsureSuccessStatusCode();
                body = await response.Content.ReadAsStringAsync();
                body = body.TrimEnd('\n');
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                body = null;
            }

            return body;
        }

        private async Task SendMail()
        {
            using (SmtpClient client = new SmtpClient(Config.Instance.SmtpServer, Config.Instance.Port, Config.Instance.Ssl, Config.Instance.Username, Config.Instance.Password))
            {
                EmailMessage message = new EmailMessage();

                message.To.Add(new EmailRecipient(Config.Instance.Recipient));
                message.Subject = Config.Instance.EmailSubject;
                message.Body = string.Format(Config.Instance.EmailBodyFormat, Config.Instance.DeviceName, Config.Instance.IpAddress);

                await client.SendMail(message);
            }
        }
    }
}
