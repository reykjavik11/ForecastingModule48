using System.Net;

namespace ForecastingModule.Util
{
    internal sealed class SystemInfo
    {
        private SystemInfo()
        {
        }

        public static string getIPAdress()
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            // Manually convert to string
            string[] addressStrings = new string[hostAddresses.Length];
            for (int i = 0; i < hostAddresses.Length; i++)
            {
                addressStrings[i] = hostAddresses[i].ToString();
            }

            return string.Join(", ", addressStrings);
        }
        public static string getMachineName()
        {
            return System.Environment.MachineName;
        }
    }
}
