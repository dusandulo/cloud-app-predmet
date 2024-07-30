using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditService_Data
{
    public class Subscription : TableEntity
    {
        public string Email { get; set; }
        public string TopicId { get; set; }

        public Subscription(string subscriptionId)
        {
            RowKey = subscriptionId;
            Email = string.Empty;
            TopicId = string.Empty;
        }

        public Subscription()
        {
            Email = string.Empty;
            TopicId = string.Empty;
        }
    }
}
