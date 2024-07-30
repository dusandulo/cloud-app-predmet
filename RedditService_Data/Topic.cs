using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditService_Data
{
    public class Topic : TableEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }

        public Topic(string topicId)
        {
            PartitionKey = "Topic";
            RowKey = topicId;
            Title = string.Empty;
            Content = string.Empty;
            ImageUrl = string.Empty;
            Upvotes = 0;
            Downvotes = 0;
            CreatedAt = DateTime.MinValue;
            UserId = string.Empty;
        }

        public Topic()
        {
            Title = string.Empty;
            Content = string.Empty;
            ImageUrl = string.Empty;
            Upvotes = 0;
            Downvotes = 0;
            CreatedAt = DateTime.MinValue;
            UserId = string.Empty;
        }

        public void Upvote()
        {
            Upvotes++;
        }

        public void Downvote()
        {
            Downvotes++;
        }
    }
}

