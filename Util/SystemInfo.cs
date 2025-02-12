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

        public static string GetAssemblyVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            return version;
        }

        public static System.Drawing.Icon GetAppIcon() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            return ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        }
    }
}
