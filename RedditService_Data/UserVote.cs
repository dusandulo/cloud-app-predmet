using Microsoft.WindowsAzure.Storage.Table;

namespace RedditService_Data
{
    public class UserVote : TableEntity
    {
        public string UserId { get; set; }
        public string TopicId { get; set; }

        public UserVote(string userId, string topicId)
        {
            PartitionKey = userId;
            RowKey = topicId;
        }

        public UserVote() { }
    }
}