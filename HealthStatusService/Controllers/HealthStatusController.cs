using RedditService_Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HealthStatusService.Controllers
{
    public class HealthStatusController : Controller
    {
        private readonly RedditDataRepository _repository;

        public HealthStatusController()
        {
            _repository = _repository ?? new RedditDataRepository();
        }

        // GET: HealthStatus
        public async Task<ActionResult> Index()
        {
            var statuses = await _repository.RetrieveAllHealthCheckInfoAsync();

            var last24Hours = DateTime.UtcNow.AddHours(-24);
            statuses = statuses.Where(s => ExtractTimestamp(s.Message) >= last24Hours);

            var redditMessages = statuses.Where(s => s.Message.Contains("REDDIT"));
            var notificationMessages = statuses.Where(s => s.Message.Contains("NOTIFICATION"));

            int redditOkCount = redditMessages.Count(s => s.Message.Contains("REDDIT_OK"));
            int redditNotOkCount = redditMessages.Count(s => s.Message.Contains("REDDIT_NOT_OK"));

            int notificationOkCount = notificationMessages.Count(s => s.Message.Contains("NOTIFICATION_OK"));
            int notificationNotOkCount = notificationMessages.Count(s => s.Message.Contains("NOTIFICATION_NOT_OK"));

            double redditUptimePercentage = CalculateUptimePercentage(redditOkCount, redditNotOkCount);
            double notificationUptimePercentage = CalculateUptimePercentage(notificationOkCount, notificationNotOkCount);

            ViewBag.RedditUptime = redditUptimePercentage;
            ViewBag.NotificationUptime = notificationUptimePercentage;

            ViewBag.RedditOkCount = redditOkCount;
            ViewBag.RedditNotOkCount = redditNotOkCount;

            ViewBag.NotificationOkCount = notificationOkCount;
            ViewBag.NotificationNotOkCount = notificationNotOkCount;

            return View();
        }

        private DateTime ExtractTimestamp(string message)
        {
            var timestampString = message.Split(' ')[1].Replace("_REDDIT_OK", "").Replace("_REDDIT_NOT_OK", "").Replace("_NOTIFICATION_OK", "").Replace("_NOTIFICATION_NOT_OK", "");
            return DateTime.Parse(timestampString);
        }

        private double CalculateUptimePercentage(int okCount, int notOkCount)
        {
            int totalCount = okCount + notOkCount;
            if (totalCount == 0) return 0;
            return (double)okCount / totalCount * 100;
        }
    }
}