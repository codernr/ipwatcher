using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LightBuzz.SMTP;
using Polly;
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
            var ip = await Policy
                .HandleResult<string>(s => s == null)
                .WaitAndRetryAsync<string>(Config.Instance.RetryCount, this.RetryExponential, this.LogIPRetry)
                .ExecuteAsync(this.GetExternalIP);

            // if the ip is still null after the retries, wait for the next cycle
            if (ip == null)
            {
                Debug.WriteLine("All IP check attempt failed, wait for next cycle");
                return;
            }

            // no IP address change, no notification
            if (ip == Config.Instance.IpAddress)
            {
                Debug.WriteLine("IP address hasn't changed, returning");
                return;
            }

            var policyResult = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(Config.Instance.RetryCount, this.RetryExponential, this.LogMailRetry)
                .ExecuteAndCaptureAsync(() => this.SendMail(ip));

            // email send fail, wait for next cycle
            if (policyResult.Outcome == OutcomeType.Failure)
            {
                Debug.WriteLine("All mail send attempt failed, wait for next cycle");
                return;
            }

            // success, save new IP to config
            Config.Instance.IpAddress = ip;

            await Config.SaveInstance();
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

        private async Task SendMail(string ip)
        {
            throw new Exception();
            using (SmtpClient client = new SmtpClient(Config.Instance.SmtpServer, Config.Instance.Port, Config.Instance.Ssl, Config.Instance.Username, Config.Instance.Password))
            {
                EmailMessage message = new EmailMessage();

                message.To.Add(new EmailRecipient(Config.Instance.Recipient));
                message.Subject = Config.Instance.EmailSubject;
                message.Body = string.Format(Config.Instance.EmailBodyFormat, Config.Instance.DeviceName, ip);

                await client.SendMail(message);
            }
        }

        private TimeSpan RetryExponential(int retryAttempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(Config.Instance.RetryBaseSeconds, retryAttempt));
        }

        private async Task LogIPRetry(DelegateResult<string> result, TimeSpan time, int retryCount, Context onRetry)
        {
            Debug.WriteLine("{0}. Failed IP check attempt, retrying in {1}", retryCount, time);
        }

        private async Task LogMailRetry(Exception ex, TimeSpan time)
        {
            Debug.WriteLine("Failed email send attempt, retrying in {0}", time);
        }
    }
}
