using Microsoft.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using RedditService_Data;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace RedditService.Controllers
{
    public class TopicsController : Controller
    {
        private readonly RedditDataRepository _repository;

        public TopicsController()
        {
            _repository = new RedditDataRepository();
        }

        public ActionResult Index(string sortOrder, string searchString)
        {
            try
            {
                // Get user name from cookie
                var userName = GetUserNameFromCookie();
                ViewBag.UserName = userName;

                var topics = _repository.RetrieveAllTopics();

                // Separate topics into owned by user and others
                var userTopics = topics.Where(t => t.UserId == userName).ToList();
                var otherTopics = topics.Where(t => t.UserId != userName).ToList();

                if (!String.IsNullOrEmpty(searchString))
                {
                    userTopics = userTopics.Where(t => t.Title.ToLower().Contains(searchString.ToLower())).ToList();
                    otherTopics = otherTopics.Where(t => t.Title.ToLower().Contains(searchString.ToLower())).ToList();
                }

                switch (sortOrder)
                {
                    case "asc":
                        userTopics = userTopics.OrderBy(t => t.Title).ToList();
                        otherTopics = otherTopics.OrderBy(t => t.Title).ToList();
                        break;
                    case "desc":
                        userTopics = userTopics.OrderByDescending(t => t.Title).ToList();
                        otherTopics = otherTopics.OrderByDescending(t => t.Title).ToList();
                        break;
                    default:
                        userTopics = userTopics.ToList();
                        otherTopics = otherTopics.ToList();
                        break;
                }

                // Combine the lists
                topics = userTopics.Concat(otherTopics).ToList();

                // Log the topics
                System.Diagnostics.Debug.WriteLine("Retrieved topics count: " + topics.Count);
                foreach (var topic in topics)
                {
                    System.Diagnostics.Debug.WriteLine($"Topic - RowKey: {topic.RowKey}, Title: {topic.Title}, Content: {topic.Content}, ImageUrl: {topic.ImageUrl}");
                }

                return View(topics);
            }
            catch (StorageException ex)
            {
                // Log the exception details
                System.Diagnostics.Debug.WriteLine("StorageException: " + ex.Message);
                // Handle the exception as needed, possibly return an error view
                return View("Error", new HandleErrorInfo(ex, "Topics", "Index"));
            }
        }

        public ActionResult Create()
        {
            return View("AddTopic");
        }

        private string GetUserNameFromCookie()
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                return authTicket?.Name; // This will be the email in this case
            }
            return null;
        }

        [HttpPost]
        public ActionResult AddTopic(string Title, string Content, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string RowKey = Guid.NewGuid().ToString();
                    string uniqueBlobName = $"image_{RowKey}";
                    var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference("roland");
                    container.CreateIfNotExists();
                    var blob = container.GetBlockBlobReference(uniqueBlobName);
                    blob.Properties.ContentType = file.ContentType;

                    blob.UploadFromStream(file.InputStream);

                    var entry = new Topic(RowKey)
                    {
                        Title = Title,
                        Content = Content,
                        ImageUrl = blob.Uri.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Downvotes = 0,
                        Upvotes = 0,
                        UserId = GetUserNameFromCookie()
                };

                    _repository.AddTopic(entry);

                    var queue = storageAccount.CreateCloudQueueClient().GetQueueReference("roland");
                    queue.CreateIfNotExists();
                    queue.AddMessage(new CloudQueueMessage(RowKey));

                    return RedirectToAction("Index");
                }
                catch (StorageException ex)
                {
                    System.Diagnostics.Debug.WriteLine("StorageException: " + ex.Message);
                    ModelState.AddModelError("", "A storage error occurred while creating the topic: " + ex.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                    ModelState.AddModelError("", "An error occurred while creating the topic: " + ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please upload a valid image file.");
            }

            return View("AddTopic");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            try
            {
                var topic = _repository.RetrieveAllTopics().FirstOrDefault(t => t.RowKey == id);
                if (topic != null)
                {
                    var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference("roland");
                    var blob = container.GetBlockBlobReference($"image_{id}");

                    if (blob.Exists())
                    {
                        blob.Delete();
                    }

                    _repository.DeleteTopic(id);

                    var queue = storageAccount.CreateCloudQueueClient().GetQueueReference("roland");
                    if (queue.Exists())
                    {
                        var messages = queue.GetMessages(32);
                        foreach (var message in messages)
                        {
                            if (message.AsString == id)
                            {
                                queue.DeleteMessage(message);
                                break;
                            }
                        }
                    }
                }

                return RedirectToAction("Index");
            }
            catch (StorageException ex)
            {
                System.Diagnostics.Debug.WriteLine("StorageException: " + ex.Message);
                return View("Error", new HandleErrorInfo(ex, "Topics", "Delete"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return View("Error", new HandleErrorInfo(ex, "Topics", "Delete"));
            }
        }

        //upvote downvote


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upvote(string id)
        {
            var user = GetUser();
            if (user == null || _repository.HasUserVoted(user.RowKey, id))
            {
                return RedirectToAction("Index");
            }

            _repository.UpvoteTopic(id);
            _repository.RecordUserVote(user.RowKey, id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Downvote(string id)
        {
            var user = GetUser();
            if (user == null || _repository.HasUserVoted(user.RowKey, id))
            {
                return RedirectToAction("Index");
            }

            _repository.DownvoteTopic(id);
            _repository.RecordUserVote(user.RowKey, id);

            return RedirectToAction("Index");
        }

        private User GetUser()
        {
            var username = GetUserNameFromCookie();
            if (username == null) return null;
            return _repository.GetUserByEmail(username);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Subscribe(string topicId)
        {
            var authCookie = Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = System.Web.Security.FormsAuthentication.Decrypt(authCookie.Value);
                var userName = authTicket?.Name;

                if (!string.IsNullOrEmpty(userName))
                {
                    var user = _repository.GetUserByEmail(userName);
                    var subscription = new Subscription
                    {
                        PartitionKey = "Subscription",
                        RowKey = Guid.NewGuid().ToString(),
                        Email = user.Email,
                        TopicId = topicId
                    };


                    _repository.AddSubscription(subscription);

                    TempData["Message"] = "Subscribed successfully!";
                }
            }

            return RedirectToAction("Index");
        }

    }
}