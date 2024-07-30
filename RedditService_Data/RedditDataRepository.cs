using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using System.Runtime.Remoting.Contexts;
using Common.Models;

namespace RedditService_Data
{
    public class RedditDataRepository
    {
        private CloudStorageAccount _storageAccount;
        private CloudTable _userTable;
        private CloudTable _topicTable;
        private CloudTable _commentTable;
        private CloudTable _userVoteTable;
        private CloudTable _healthCheckInfoTable;
        private CloudTable _subscriptionTable;

        public RedditDataRepository()
        {
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);

            _userTable = tableClient.GetTableReference("UserTable");
            _userTable.CreateIfNotExists();

            _topicTable = tableClient.GetTableReference("TopicTable");
            _topicTable.CreateIfNotExists();

            _commentTable = tableClient.GetTableReference("CommentTable");
            _commentTable.CreateIfNotExists();

            _userVoteTable = tableClient.GetTableReference("UserVotes");
            _userVoteTable.CreateIfNotExists();

            _healthCheckInfoTable = tableClient.GetTableReference("HealthCheckInfo");
            _healthCheckInfoTable.CreateIfNotExists();

            _subscriptionTable = tableClient.GetTableReference("Subscription");
            _subscriptionTable.CreateIfNotExists();
        }

        public List<User> RetrieveAllUsers()
        {
            var query = new TableQuery<User>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "User"));
            return _userTable.ExecuteQuery(query).ToList();
        }

        public async Task<List<User>> RetrieveAllUsersAsync()
        {
            var query = new TableQuery<User>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "User"));
            TableQuerySegment<User> resultSegment = null;
            List<User> users = new List<User>();

            while (resultSegment == null || resultSegment.ContinuationToken != null)
            {
                resultSegment = await _userTable.ExecuteQuerySegmentedAsync(query, resultSegment?.ContinuationToken);
                users.AddRange(resultSegment.Results);
            }

            return users;
        }

        public List<Subscription> RetrieveAllSubscriptions()
        {
            var query = new TableQuery<Subscription>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Subscription"));
            return _subscriptionTable.ExecuteQuery(query).ToList();
        }

        public async Task<List<Subscription>> RetrieveAllSubscriptionsAsync()
        {
            var query = new TableQuery<Subscription>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Subscription"));
            TableQuerySegment<Subscription> resultSegment = null;
            List<Subscription> subscriptions = new List<Subscription>();

            while (resultSegment == null || resultSegment.ContinuationToken != null)
            {
                resultSegment = await _subscriptionTable.ExecuteQuerySegmentedAsync(query, resultSegment?.ContinuationToken);
                subscriptions.AddRange(resultSegment.Results);
            }

            return subscriptions;
        }

        public void AddSubscription(Subscription newSub)
        {
            TableOperation insertOperation = TableOperation.Insert(newSub);
            _subscriptionTable.Execute(insertOperation);
        }

        public bool IsUserSubscribed(string email, string topicId)
        {
            var query = new TableQuery<Subscription>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Subscription"),
                    TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal, email),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("TopicId", QueryComparisons.Equal, topicId)
                    )
                )
            );

            return _subscriptionTable.ExecuteQuery(query).Any();
        }

        public void AddUser(User newUser)
        {
            TableOperation insertOperation = TableOperation.Insert(newUser);
            _userTable.Execute(insertOperation);
        }

        public void DeleteUser(string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<Comment>("UserTable", userId);
            var retrievedResult = _userTable.Execute(retrieveOperation);
            var user = (User)retrievedResult.Result;

            if (user != null)
            {
                // Delete the user
                var deleteOperation = TableOperation.Delete(user);
                _userTable.Execute(deleteOperation);
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<User>("User", userId);
            var retrievedResult = await _userTable.ExecuteAsync(retrieveOperation);
            var user = (User)retrievedResult.Result;

            if (user != null)
            {
                // Delete the user
                var deleteOperation = TableOperation.Delete(user);
                await _userTable.ExecuteAsync(deleteOperation);
            }
        }

        public List<Topic> RetrieveAllTopics()
        {
            var query = new TableQuery<Topic>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Topic"));
            return _topicTable.ExecuteQuery(query).ToList();
        }

        public async Task<List<Topic>> RetrieveAllTopicsAsync()
        {
            var query = new TableQuery<Topic>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Topic"));
            TableQuerySegment<Topic> resultSegment = null;
            List<Topic> topics = new List<Topic>();

            while (resultSegment == null || resultSegment.ContinuationToken != null)
            {
                resultSegment = await _topicTable.ExecuteQuerySegmentedAsync(query, resultSegment?.ContinuationToken);
                topics.AddRange(resultSegment.Results);
            }

            return topics;
        }

        public void AddTopic(Topic newTopic)
        {
            TableOperation insertOperation = TableOperation.Insert(newTopic);
            _topicTable.Execute(insertOperation);
        }

        public List<Comment> RetrieveAllComments()
        {
            var query = new TableQuery<Comment>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Comment"));
            return _commentTable.ExecuteQuery(query).ToList();
        }

        public void AddComment(Comment newComment)
        {
            TableOperation insertOperation = TableOperation.Insert(newComment);
            _commentTable.Execute(insertOperation);
        }

        public bool Exists(string indexNo)
        {
            return RetrieveAllTopics().Where(t => t.RowKey == indexNo).FirstOrDefault() != null;
        }

        //delete

        public void DeleteTopic(string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<Topic>("Topic", rowKey);
            var retrievedResult = _topicTable.Execute(retrieveOperation);
            var deleteEntity = (Topic)retrievedResult.Result;

            if (deleteEntity != null)
            {
                // Delete the topic
                var deleteOperation = TableOperation.Delete(deleteEntity);
                _topicTable.Execute(deleteOperation);
            }
        }

        //upvote downvote

        public void UpvoteTopic(string topicId)
        {
            var retrieveOperation = TableOperation.Retrieve<Topic>("Topic", topicId);
            var retrievedResult = _topicTable.Execute(retrieveOperation);
            var topic = (Topic)retrievedResult.Result;

            if (topic != null)
            {
                topic.Upvote();
                var updateOperation = TableOperation.Replace(topic);
                _topicTable.Execute(updateOperation);
            }
        }

        public void DownvoteTopic(string topicId)
        {
            var retrieveOperation = TableOperation.Retrieve<Topic>("Topic", topicId);
            var retrievedResult = _topicTable.Execute(retrieveOperation);
            var topic = (Topic)retrievedResult.Result;

            if (topic != null)
            {
                topic.Downvote();
                var updateOperation = TableOperation.Replace(topic);
                _topicTable.Execute(updateOperation);
            }
        }

        public void UpdateUser(User user)
        {
            var updateOperation = TableOperation.Replace(user);
            _userTable.Execute(updateOperation);
        }

        public bool HasUserVoted(string userId, string topicId)
        {
            var retrieveOperation = TableOperation.Retrieve<UserVote>(userId, topicId);
            var result = _userVoteTable.Execute(retrieveOperation);
            return result.Result != null;
        }

        public void RecordUserVote(string userId, string topicId)
        {
            var userVote = new UserVote(userId, topicId);
            var insertOperation = TableOperation.Insert(userVote);
            _userVoteTable.Execute(insertOperation);
        }

        public User GetUserByEmail(string email)
        {
            var retrieveOperation = TableOperation.Retrieve<User>("User", email);
            var result = _userTable.Execute(retrieveOperation);
            return result.Result as User;
        }

        public void DeleteComment(string commentId)
        {
            var retrieveOperation = TableOperation.Retrieve<Comment>("Comment", commentId);
            var retrievedResult = _commentTable.Execute(retrieveOperation);
            var comment = (Comment)retrievedResult.Result;

            if (comment != null)
            {
                // Delete the comment
                var deleteOperation = TableOperation.Delete(comment);
                _commentTable.Execute(deleteOperation);
            }
        }

        public Comment GetCommentById(string commentId)
        {
            var retrieveOperation = TableOperation.Retrieve<Comment>("Comment", commentId);
            var retrievedResult = _commentTable.Execute(retrieveOperation);
            return (Comment)retrievedResult.Result;
        }

        // HealthCheckInfo

        public void AddHealthCheckInfo(HealthCheckInfo newHealthCheckInfo)
        {
            TableOperation insertOperation = TableOperation.Insert(newHealthCheckInfo);
            _healthCheckInfoTable.Execute(insertOperation);
        }

        public async Task AddHealthCheckInfoAsync(HealthCheckInfo newHealthCheckInfo)
        {
            TableOperation insertOperation = TableOperation.Insert(newHealthCheckInfo);
            await _healthCheckInfoTable.ExecuteAsync(insertOperation);
        }

        public async Task<IEnumerable<HealthCheckInfo>> RetrieveAllHealthCheckInfoAsync()
        {
            var query = new TableQuery<HealthCheckInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "HealthCheckInfo"));
            TableQuerySegment<HealthCheckInfo> resultSegment = null;
            List<HealthCheckInfo> healthCheckInfos = new List<HealthCheckInfo>();

            while (resultSegment == null || resultSegment.ContinuationToken != null)
            {
                resultSegment = await _healthCheckInfoTable.ExecuteQuerySegmentedAsync(query, resultSegment?.ContinuationToken);
                healthCheckInfos.AddRange(resultSegment.Results);
            }

            return healthCheckInfos;
        }


        public IEnumerable<Subscription> GetSubscribersByTopicId(string topicId)
        {
            var query = new TableQuery<Subscription>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Subscription"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("TopicId", QueryComparisons.Equal, topicId)
                )
            );

            return _subscriptionTable.ExecuteQuery(query);
        }

    }
}
