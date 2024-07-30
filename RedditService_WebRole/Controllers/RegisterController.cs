using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using RedditService_Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RedditService_WebRole.Controllers
{
    public class RegisterController : Controller
    {
        private readonly RedditDataRepository _repository;
        public RegisterController()
        {
            _repository = new RedditDataRepository();
        }
        public ActionResult ShowRegister()
        {
            return View();
        }
        public ActionResult ShowListUsers()
        {
            var users = _repository.RetrieveAllUsers().ToList();
            return View("ListUsers", users);
        }
        public ActionResult Index()
        {
                var users = _repository.RetrieveAllUsers().ToList();
                return View(users);
        }
        public ActionResult Register()
        {
            return View("RegisterUser");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterUser(string FirstName, string LastName, string Address, string City, string Country, string PhoneNumber, string Email, string Password, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string uniqueBlobName = $"image_{Email}";
                    var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference("roland");
                    container.CreateIfNotExists();
                    var blob = container.GetBlockBlobReference(uniqueBlobName);
                    blob.Properties.ContentType = file.ContentType;

                    blob.UploadFromStream(file.InputStream);

                    var newUser = new User(Email)
                    {
                        FirstName = FirstName,
                        LastName = LastName,
                        Address = Address,
                        City = City,
                        Country = Country,
                        PhoneNumber = PhoneNumber,
                        Email = Email,
                        Password = Password,
                        ProfilePictureUrl = blob.Uri.ToString(),
                    };

                    _repository.AddUser(newUser);

                    var queue = storageAccount.CreateCloudQueueClient().GetQueueReference("roland");
                    queue.CreateIfNotExists();
                    queue.AddMessage(new CloudQueueMessage(Email));

                    return RedirectToAction("Index", "Topics");
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

            return View("RegisterUser");



        }
    }
}