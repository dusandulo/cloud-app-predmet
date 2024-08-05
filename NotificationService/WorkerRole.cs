using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using RedditService_Data;
using Microsoft.Azure;
using HealthMonitoringService;

namespace NotificationService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static HealthMonitoringServer healthMonitoringServer;

        public override void Run()
        {
            Trace.TraceInformation("NotificationService is running");

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
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            healthMonitoringServer = new HealthMonitoringServer();
            healthMonitoringServer.Open();

            Trace.TraceInformation("NotificationService has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("NotificationService is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("NotificationService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var repository = new RedditDataRepository();

            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("notificationsqueue");

            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Checking queue for new messages.");
                CloudQueueMessage message = await queue.GetMessageAsync();
                if (message != null)
                {
                    string[] messageParts = message.AsString.Split('|');
                    if (messageParts[0] == "Deleted")
                    {
                        // Handle deletion notification
                        string commentId = messageParts[1];
                        string userId = messageParts[2];
                        string topicId = messageParts[3];
                        NotifyAuthorOfDeletion(userId, commentId, topicId);
                    }
                    else if (messageParts[0] == "New")
                    {
                        // Handle new comment notification
                        string commentId = messageParts[1];
                        ProcessNewCommentMessage(repository, commentId);
                    }

                    await queue.DeleteMessageAsync(message);
                }

                await Task.Delay(15000); // Check every 15 seconds
            }
        }
        private void NotifyAuthorOfDeletion(string userId, string commentId, string topicId)
        {
            var repository = new RedditDataRepository();

            var user = repository.GetUserByEmail(userId);
            if (user != null)
            {
                string email = user.Email;
                string body = $"Your comment on topic {topicId} with ID: {commentId} has been deleted.";
                SendEmail(email, body, "Comment Deletion Notification");
            }
        }

        private void ProcessNewCommentMessage(RedditDataRepository repository, string commentId)
        {
            var comment = repository.GetCommentById(commentId);
            if (comment != null)
            {
                var topicId = comment.TopicId;
                var subscribers = repository.GetSubscribersByTopicId(topicId);
                foreach (var subscriber in subscribers)
                {
                    string body = $"New comment on topic: {comment.TopicId}\n\n" +
                                  $"Comment: {comment.Content}\n" +
                                  $"Posted by: {comment.UserId}\n" +
                                  $"At: {comment.CreatedAt}";
                    SendEmail(subscriber.Email, body, "New Comment Notification");
                }
            }
        }

        private void SendEmail(string email, string body, string subject)
        {
            var smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io")
            {
                Port = 2525,
                Credentials = new NetworkCredential("c94feac755e586", "46f367e560adf7"),
                EnableSsl = false,
            };

            try
            {
                smtpClient.Send("from@example.com", email, subject, body);
                Trace.TraceInformation($"{email} got message: {subject}");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to send email. Error: {ex.Message}");
            }
        }
    }
}
