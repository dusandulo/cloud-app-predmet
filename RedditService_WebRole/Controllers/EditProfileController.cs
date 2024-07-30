using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using RedditService_Data;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace RedditService_WebRole.Controllers
{
    public class EditProfileController : Controller
    {
        private readonly RedditDataRepository _repository;

        public EditProfileController()
        {
            _repository = new RedditDataRepository();
        }

        public ActionResult ShowEdit()
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                var userEmail = authTicket?.Name;

                var user = _repository.RetrieveAllUsers().FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    return View("Edit", user);
                }
            }

            return RedirectToAction("ShowLogin", "Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User user, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = _repository.RetrieveAllUsers().FirstOrDefault(u => u.Email == user.Email);
                    if (existingUser != null)
                    {
                        existingUser.FirstName = user.FirstName;
                        existingUser.LastName = user.LastName;
                        existingUser.Address = user.Address;
                        existingUser.City = user.City;
                        existingUser.Country = user.Country;
                        existingUser.PhoneNumber = user.PhoneNumber;
                        existingUser.Password = user.Password;

                        if (file != null && file.ContentLength > 0)
                        {
                            string uniqueBlobName = $"image_{user.Email}";
                            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                            var blobClient = storageAccount.CreateCloudBlobClient();
                            var container = blobClient.GetContainerReference("roland");
                            container.CreateIfNotExists();
                            var blob = container.GetBlockBlobReference(uniqueBlobName);
                            blob.Properties.ContentType = file.ContentType;

                            blob.UploadFromStream(file.InputStream);
                            existingUser.ProfilePictureUrl = blob.Uri.ToString();
                        }

                        _repository.UpdateUser(existingUser);

                        return RedirectToAction("Index", "Topics");
                    }
                }
                catch (StorageException ex)
                {
                    System.Diagnostics.Debug.WriteLine("StorageException: " + ex.Message);
                    ModelState.AddModelError("", "A storage error occurred while updating the profile: " + ex.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                    ModelState.AddModelError("", "An error occurred while updating the profile: " + ex.Message);
                }
            }

            return View(user);
        }
    }
}