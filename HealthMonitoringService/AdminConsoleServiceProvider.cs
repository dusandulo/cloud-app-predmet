using RedditService_Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HealthMonitoringService
{
    public class AdminConsoleServiceProvider : IAdminConsole
    {
        private readonly RedditDataRepository _repository;

        private List<string> _activeAdmins = new List<string>();
        private Dictionary<string, string> _adminAccounts = new Dictionary<string, string>();
        public static List<string> adminEmails = new List<string>
        {
            "admin@admin.com"
        };
        private readonly Regex reg = new Regex(@"^[a-zA-z0-9]+[\-\.]?[a-zA-z0-9]+@[a-zA-Z0-9]+\.[a-zA-Z0-9]+$");

        // Constructor to inject the repository dependency
        public AdminConsoleServiceProvider()
        {
            _repository = _repository ?? new RedditDataRepository();
            _adminAccounts.Add("admin", "admin");
        }

        public string Authenticate(string username, string password)
        {
            if (_adminAccounts.ContainsKey(username))
            {
                if (_adminAccounts[username] == password)
                {
                    Guid adminKey = Guid.NewGuid();
                    _activeAdmins.Add(adminKey.ToString());
                    return adminKey.ToString();
                }
            }
            return string.Empty;
        }

        public async Task<IEnumerable<User>> ListUsersAsync(string adminKey)
        {
            if (_activeAdmins.Contains(adminKey))
            {
                return await _repository.RetrieveAllUsersAsync();
            }
            return Enumerable.Empty<User>();
        }

        public async Task<string> DeleteByIdAsync(string adminKey, string email)
        {
            if (_activeAdmins.Contains(adminKey))
            {
                await _repository.DeleteUserAsync(email);
                return "User deleted successfully";
            }
            else
            {
                return "Unauthorized";
            }
        }

        public async Task<IEnumerable<Topic>> ListAllTopicsAsync(string adminKey)
        {
            if (_activeAdmins.Contains(adminKey))
            {
                return await _repository.RetrieveAllTopicsAsync();
            }
            return Enumerable.Empty<Topic>();
        }

        public async Task<IEnumerable<Subscription>> ListAllSubscriptionsAsync(string adminKey)
        {
            if (_activeAdmins.Contains(adminKey))
            {
                return await _repository.RetrieveAllSubscriptionsAsync();
            }
            return Enumerable.Empty<Subscription>();
        }
    }
}