using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditService_Data
{
    public class User : TableEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ProfilePictureUrl { get; set; }


        [IgnoreProperty]
        public List<string> VotedTopics
        {
            get { return JsonConvert.DeserializeObject<List<string>>(VotedTopicsSerialized); }
            set { VotedTopicsSerialized = JsonConvert.SerializeObject(value); }
        }

        public string VotedTopicsSerialized { get; set; }

        public User(string email)
        {
            PartitionKey = "User";
            RowKey = email;
            FirstName = string.Empty;
            LastName = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            PhoneNumber = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ProfilePictureUrl = string.Empty;
            VotedTopics = new List<string>();
        }

        public User()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            PhoneNumber = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ProfilePictureUrl = string.Empty;
            VotedTopics = new List<string>();
        }
    }
}

