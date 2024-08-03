using Microsoft.WindowsAzure.Storage.Table;

namespace RedditService_Data
{
    public class UserVoteComment : TableEntity
    {
        public string UserId { get; set; }
        public string CommentId { get; set; }

        public UserVoteComment(string userId, string commentId)
        {
            PartitionKey = userId;
            RowKey = commentId;
        }

        public UserVoteComment() { }
    }
}