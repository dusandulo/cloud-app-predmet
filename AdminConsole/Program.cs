using StudentServiceClient.UniversalConnector;
using System;

namespace AdminConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Admin Console";
            try
            {
                ServiceConnector<IAdminConsole> serviceConnector = new ServiceConnector<IAdminConsole>();
                serviceConnector.Connect("net.tcp://localhost:10106/admin-console");
                IAdminConsole proxy = serviceConnector.GetProxy();

                Console.WriteLine("Authentication Required");
                Console.Write("Enter username: ");
                string username = Console.ReadLine().Trim();
                Console.Write("Enter password: ");
                string password = Console.ReadLine().Trim();
                string res = proxy.Authenticate(username, password);
                if (res == string.Empty)
                {
                    Console.WriteLine("Invalid username or password.");
                    return;
                }

                Console.Clear();
                string input = string.Empty;
                do
                {
                    Console.WriteLine("Admin Console Menu:");
                    Console.WriteLine("[1] - List all users");
                    Console.WriteLine("[2] - List all topics");
                    Console.WriteLine("[3] - List all subscriptions");
                    Console.WriteLine("[4] - Delete user by email");
                    Console.WriteLine("[q] - Exit");
                    Console.WriteLine();
                    Console.Write("Select an option: ");

                    input = Console.ReadLine();
                    switch (input)
                    {
                        case "1":
                            ListAllUsers(res, proxy);
                            break;
                        case "2":
                            ListAllTopics(res, proxy);
                            break;
                        case "3":
                            ListAllSubscriptions(res, proxy);
                            break;
                        case "4":
                            DeleteUserUsers(res, proxy);
                            break;
                        case "q":
                            Console.WriteLine("Exiting the Admin Console...");
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                while (input != "q");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error encountered: " + ex.ToString());
            }
            finally
            {
                Console.WriteLine("Press any key to close the console...");
                Console.ReadLine();
            }
        }

        private static async void ListAllUsers(string adminKey, IAdminConsole proxy)
        {
            var users = await proxy.ListUsersAsync(adminKey);
            // Define column widths
            int emailWidth = 30;
            int nameWidth = 15;
            int lastNameWidth = 15;
            int phoneWidth = 15;

            // Header
            Console.WriteLine("\nList of Users:");
            Console.WriteLine(new String('=', emailWidth + nameWidth + lastNameWidth + phoneWidth + 9));
            Console.WriteLine(
                $"{FormatColumn("Email", emailWidth)}|{FormatColumn("Name", nameWidth)}|" +
                $"{FormatColumn("Last name", lastNameWidth)}|{FormatColumn("Phone number", phoneWidth)}"
            );
            Console.WriteLine(new String('-', emailWidth + nameWidth + lastNameWidth + phoneWidth + 9));

            // Data rows
            foreach (var user in users)
            {
                Console.WriteLine(
                    $"{FormatColumn(user.Email, emailWidth)}|{FormatColumn(user.FirstName, nameWidth)}|" +
                    $"{FormatColumn(user.LastName, lastNameWidth)}|{FormatColumn(user.PhoneNumber, phoneWidth)}"
                );
            }

            // Footer
            Console.WriteLine(new String('=', emailWidth + nameWidth + lastNameWidth + phoneWidth + 9));
            Console.WriteLine();  // Space after the table
        }

        private static async void ListAllTopics(string adminKey, IAdminConsole proxy)
        {
            var topics = await proxy.ListAllTopicsAsync(adminKey);
            // Define column widths
            int titleWidth = 30;
            int contentWidth = 40; // Adjust width according to your needs
            int votesWidth = 10;
            int dateWidth = 20;
            int userWidth = 15;

            // Header
            Console.WriteLine("\nList of Topics:");
            Console.WriteLine(new String('=', titleWidth + contentWidth + 2 * votesWidth + dateWidth + userWidth + 13));
            Console.WriteLine(
                $"{FormatColumn("Title", titleWidth)}|{FormatColumn("Content", contentWidth)}|" +
                $"{FormatColumn("Upvotes", votesWidth)}|{FormatColumn("Downvotes", votesWidth)}|" +
                $"{FormatColumn("Date", dateWidth)}|{FormatColumn("User ID", userWidth)}"
            );
            Console.WriteLine(new String('-', titleWidth + contentWidth + 2 * votesWidth + dateWidth + userWidth + 13));

            // Data rows
            foreach (var topic in topics)
            {
                Console.WriteLine(
                    $"{FormatColumn(topic.Title, titleWidth)}|{FormatColumn(topic.Content, contentWidth)}|" +
                    $"{FormatColumn(topic.Upvotes.ToString(), votesWidth)}|{FormatColumn(topic.Downvotes.ToString(), votesWidth)}|" +
                    $"{FormatColumn(topic.CreatedAt.ToString("yyyy-MM-dd"), dateWidth)}|{FormatColumn(topic.UserId, userWidth)}"
                );
            }

            // Footer
            Console.WriteLine(new String('=', titleWidth + contentWidth + 2 * votesWidth + dateWidth + userWidth + 13));
            Console.WriteLine();  // Space after the table
        }

        private static async void ListAllSubscriptions(string adminKey, IAdminConsole proxy)
        {
            var subscriptions = await proxy.ListAllSubscriptionsAsync(adminKey);
            // Define column widths
            int emailWidth = 30;
            int topicIdWidth = 30;

            // Header
            Console.WriteLine("\nList of Subscriptions:");
            Console.WriteLine(new String('=', emailWidth + topicIdWidth + 3));
            Console.WriteLine(
                $"{FormatColumn("Email", emailWidth)}|{FormatColumn("Topic ID", topicIdWidth)}"
            );
            Console.WriteLine(new String('-', emailWidth + topicIdWidth + 3));

            // Data rows
            foreach (var subscription in subscriptions)
            {
                Console.WriteLine(
                    $"{FormatColumn(subscription.Email, emailWidth)}|{FormatColumn(subscription.TopicId, topicIdWidth)}"
                );
            }

            // Footer
            Console.WriteLine(new String('=', emailWidth + topicIdWidth + 3));
            Console.WriteLine();  // Space after the table
        }

        // Helper method to format column data
        private static string FormatColumn(string data, int width)
        {
            return data.PadRight(width).Substring(0, width);
        }

        private static async void DeleteUserUsers(string adminKey, IAdminConsole proxy)
        {
            Console.Write("\nEnter user email to delete: ");
            string emailToDelete = Console.ReadLine();
            var res = await proxy.DeleteByIdAsync(adminKey, emailToDelete);
            Console.WriteLine(res == "Success" ? "User successfully deleted." : "Failed to delete user.");
            Console.WriteLine();
        }
    }
}