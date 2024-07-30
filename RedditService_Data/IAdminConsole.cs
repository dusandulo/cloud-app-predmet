using RedditService_Data;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

[ServiceContract]
public interface IAdminConsole
{
    [OperationContract]
    string Authenticate(string username, string password);
    [OperationContract]
    Task<IEnumerable<User>> ListUsersAsync(string adminKey);
    [OperationContract]
    Task<IEnumerable<Topic>> ListAllTopicsAsync(string adminKey);
    [OperationContract]
    Task<IEnumerable<Subscription>> ListAllSubscriptionsAsync(string adminKey);
    [OperationContract]
    Task<string> DeleteByIdAsync(string adminKey, string userEmail);
}