using Common.Models;
using Common;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using RedditService_Data;
using StudentServiceClient.UniversalConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitoringService
{
    public class WorkerRole : RoleEntryPoint
    {
        private RedditDataRepository _repository;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static AdminConsoleService adminConsoleService;

        public override void Run()
        {
            Trace.TraceInformation("HealthMonitoringService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            _repository = new RedditDataRepository();

            // Use TLS 1.2 for Service Bus connections
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();
            adminConsoleService = new AdminConsoleService();
            adminConsoleService.Open();

            Trace.TraceInformation("HealthMonitoringService has been started");

            return result;
        }


        private async Task TestServicesAsync()
        {
            ServiceConnector<IHealthMonitoringService> serviceConnector = new ServiceConnector<IHealthMonitoringService>();

            Guid redditGuid = Guid.NewGuid();
            Guid notificationGuid = Guid.NewGuid();

            HealthCheckInfo redditHealthCheck = new HealthCheckInfo(redditGuid)
            {
                Id = redditGuid
            };

            HealthCheckInfo notificationHealthCheck = new HealthCheckInfo(notificationGuid)
            {
                Id = notificationGuid
            };

            /// Test Reddit service
            try
            {
                // Connect to the reddit service
                serviceConnector.Connect("net.tcp://localhost:10100/health-monitoring");
                IHealthMonitoringService healthMonitoringService = serviceConnector.GetProxy();
                healthMonitoringService.HealthCheck();

                // Log and set message for reddit service health check
                Trace.WriteLine($"[INFO] {DateTime.UtcNow}_REDDIT_OK");
                redditHealthCheck.Message = $"[INFO] {DateTime.UtcNow}_REDDIT_OK";
            }
            catch (Exception ex)
            {
                // Log and set message for reddit service health check failure
                Trace.WriteLine($"[WARNING] {DateTime.UtcNow}_REDDIT_NOT_OK. Exception: {ex.Message}");
                redditHealthCheck.Message = $"[WARNING] {DateTime.UtcNow}_REDDIT_NOT_OK";
            }
            /// Test Notification service
            try
            {
                serviceConnector.Connect("net.tcp://localhost:10101/health-monitoring");
                IHealthMonitoringService healthMonitoringService = serviceConnector.GetProxy();
                healthMonitoringService.HealthCheck();

                Trace.WriteLine($"[INFO] {DateTime.UtcNow}_NOTIFICATION_OK");
                notificationHealthCheck.Message = $"[INFO] {DateTime.UtcNow}_NOTIFICATION_OK";
            }
            catch
            {
                Trace.WriteLine($"[WARNING] {DateTime.UtcNow}_NOTIFICATION_NOT_OK");
                notificationHealthCheck.Message = $"[WARNING] {DateTime.UtcNow}_NOTIFICATION_NOT_OK";
            }

            /// Add the messages to the table

            await _repository.AddHealthCheckInfoAsync(redditHealthCheck);
            await _repository.AddHealthCheckInfoAsync(notificationHealthCheck);
            if (redditHealthCheck.Message.Contains("WARNING"))
            {
                //await MailHelper.SendServiceDown(AdminConsoleServiceProvider.adminEmails, "Reddit service");
            }
            if (notificationHealthCheck.Message.Contains("WARNING"))
            {
                //await MailHelper.SendServiceDown(AdminConsoleServiceProvider.adminEmails, "Notification service");
            }
        }


        public override void OnStop()
        {
            Trace.TraceInformation("HealthMonitoringService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();
            adminConsoleService.Close();
            base.OnStop();

            Trace.TraceInformation("HealthMonitoringService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            Random r = new Random();
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Health service working");
                await TestServicesAsync();
                await Task.Delay(1000 + r.Next(0, 4001));
            }
        }
    }
}
