using System;
using System.Windows.Forms;
using ForecastingModule.OtherForm;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;
using ForecastingModule.Util;

namespace ForecastingModule
{
    static class Program
    {
        private static readonly Logger log = Logger.Instance;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using (LoginForm loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        Form1 mainForm = new Form1();

                        logSuccessLogIn(loginForm);

                        writeToConfigIfUserNotInConfigFile(loginForm);

                        Application.Run(mainForm);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
            }
        }

        private static void logSuccessLogIn(LoginForm loginForm)
        {
            log.LogInfo($"User: {loginForm.userName} has been loged successfully.");
            log.LogInfo($"Machine Net Address: {SystemInfo.getIPAdress()} and computer name {SystemInfo.getMachineName()}");
        }

        private static void writeToConfigIfUserNotInConfigFile(LoginForm loginForm)
        {
            ConfigFileManager config = ConfigFileManager.Instance;
            string userFromConfig = config.Read(ConfigFileManager.KEY_USER) as string;

            bool noUserInConfigFile = string.IsNullOrEmpty(userFromConfig) || string.IsNullOrWhiteSpace(userFromConfig.Trim());
            if (noUserInConfigFile)
            {
                config.Write(ConfigFileManager.KEY_USER, loginForm.userName);
            }
        }
    }
}
