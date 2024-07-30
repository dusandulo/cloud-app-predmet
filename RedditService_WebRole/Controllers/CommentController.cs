using System;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.WindowsAzure.Storage.Queue;
using RedditService_Data;

namespace RedditService.Controllers
{
    public class CommentController : Controller
    {
        private readonly RedditDataRepository _repository;

        public CommentController()
        {
            _repository = new RedditDataRepository();
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

        [HttpGet]
        public ActionResult Add(string id)
        {
            string userName = GetUserNameFromCookie();
            // Provera da li je korisnik ulogovan
            if (!String.IsNullOrEmpty(userName))
            {
                ViewBag.TopicId = id;
                return View("AddComment");
            }
            else
            {
                // Ako korisnik nije ulogovan, preusmerimo ga na stranicu za prijavljivanje
                return RedirectToAction("ShowLogin", "Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(string topicId, string content)
        {
            string userName = GetUserNameFromCookie();
            CloudQueue _notificationsQueue = QueueHelper.GetQueueReference("notificationsqueue");

            if (!String.IsNullOrEmpty(userName))
            {
                var comment = new Comment(Guid.NewGuid().ToString())
                {
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userName,
                    TopicId = topicId
                };

                _repository.AddComment(comment);

                // Add comment ID to the cloud queue
                CloudQueueMessage message = new CloudQueueMessage(comment.RowKey);
                _notificationsQueue.AddMessage(message);

                return RedirectToAction("Index", "Topics");
            }
            else
            {
                return RedirectToAction("ShowLogin", "Login");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComment(string id)
        {
            string userName = GetUserNameFromCookie();
            // Provera da li je korisnik ulogovan
            if (!String.IsNullOrEmpty(userName))
            {
                // Pronalaženje komentara koji treba da se obriše
                var commentToDelete = _repository.GetCommentById(id);

                // Provera da li je komentar pronađen i da li pripada trenutnom korisniku
                if (commentToDelete != null && commentToDelete.UserId == userName)
                {
                    // Brisanje komentara iz baze podataka
                    _repository.DeleteComment(id);
                }
                else
                {
                    // Ako komentar nije pronađen ili ne pripada trenutnom korisniku, možete preusmeriti korisnika na odgovarajuću stranicu ili prikazati odgovarajuću poruku.
                    // Na primer:
                    TempData["ErrorMessage"] = "You don't have permission to delete this comment.";
                }

                // Preusmeravanje nazad na stranicu sa topic-om nakon brisanja komentara
                return RedirectToAction("Index", "Topics");
            }
            else
            {
                // Ako korisnik nije ulogovan, preusmerimo ga na stranicu za prijavljivanje
                return RedirectToAction("ShowLogin", "Login");
            }
        }


    }
}