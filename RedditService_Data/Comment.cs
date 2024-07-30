using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditService_Data
{
    public class Comment : TableEntity
    {
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public string TopicId { get; set; }

        public Comment(string commentId)
        {
            PartitionKey = "Comment";
            RowKey = commentId;
            Content = string.Empty;
            CreatedAt = DateTime.Now;
            UserId = string.Empty;
            TopicId = string.Empty;
        }

        public Comment()
        {
            Content = string.Empty;
            CreatedAt = DateTime.Now;
            UserId = string.Empty;
            TopicId = string.Empty;
        }
    }
}
