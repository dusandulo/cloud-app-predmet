using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using RedditService_Data;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace RedditService_WebRole.Controllers
{
    public class LoginController : Controller
    {
        private readonly RedditDataRepository _repository;
        public LoginController()
        {
            _repository = new RedditDataRepository();
        }

        public ActionResult ShowLogin()
        {
            return View("LoginUser");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginUser(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("", "Email and Password are required.");
                return View("LoginUser");
            }

            try
            {
                var user = _repository.RetrieveAllUsers().FirstOrDefault(u => u.Email == Email && u.Password == Password);
                if (user != null)
                {
                    FormsAuthentication.SetAuthCookie(user.Email, false);
                    return RedirectToAction("Index", "Topics", user);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                ModelState.AddModelError("", "An error occurred while logging in: " + ex.Message);
            }

            return View("LoginUser");
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("ShowLogin", "Login");
        }
    }
}
