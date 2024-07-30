using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Diagnostics;
using System.ServiceModel;

namespace HealthMonitoringService
{
    public class AdminConsoleService
    {
        private ServiceHost serviceHost;
        private string endPointName = "admin-console";

        public AdminConsoleService()
        {
            RoleInstanceEndpoint inputEndPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endPointName];
            string endpoint = string.Format("net.tcp://{0}/{1}", inputEndPoint.IPEndpoint, endPointName);
            serviceHost = new ServiceHost(typeof(AdminConsoleServiceProvider));
            NetTcpBinding binding = new NetTcpBinding();
            serviceHost.AddServiceEndpoint(typeof(IAdminConsole), binding, endpoint);
        }

        public void Open()
        {
            try
            {
                serviceHost.Open();
                Trace.TraceInformation($"Host for {endPointName} endpoint type opened successfully at {DateTime.Now}");
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Host open error for {endPointName} endpoint type. Error message is: {e.Message}. ");
            }
        }

        public void Close()
        {
            try
            {
                serviceHost.Close();
                Trace.TraceInformation($"Host for {endPointName} endpoint type closed successfully at {DateTime.Now}");
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Host close error for {endPointName} endpoint type. Error message is: {e.Message}. ");
            }
        }
    }
}