using System;
using System.Drawing;
using System.Windows.Forms;
using ForecastingModule.Helper;
using ForecastingModule.Model;
using ForecastingModule.Service;
using ForecastingModule.Service.Impl;
using ForecastingModule.Util;

namespace ForecastingModule.OtherForm
{
    public partial class LoginForm : Form
    {
        private readonly string LOG_USER_STRING;
        private readonly TextBox txtUserName;
        private readonly Button btnLogin;
        private readonly Button btnCancel;


        private const int WIDTH = 150;
        public string userName {  get; private set; }
        public LoginForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            LOG_USER_STRING = $"Machine Net Address: {SystemInfo.getIPAdress()} and computer name {SystemInfo.getMachineName()}";
            //Set form properties
            this.Text = "Login";
            this.Size = new Size(300, WIDTH);
            this.StartPosition = FormStartPosition.CenterScreen;

            var labelUserName = new Label
            {
                Text = "User Name:",
                Location = new Point(10, 10),
                Width = 80
            };
            this.Controls.Add(labelUserName);

            this.txtUserName = new TextBox
            {
                Location = new Point(100, 10),
                Width = WIDTH
            };
            this.Controls.Add(txtUserName);
            this.txtUserName.TextChanged += userTextChanged;
            //Add user name if it already exist
            addConfigUserToTxtUserName();

            this.btnLogin = new Button
            {
                Text = "Login",
                Location = new Point(100, 50),
                Width = 70
            };
            this.Controls.Add(btnLogin);
            this.btnLogin.Click += Click_Login;

            this.btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(180, 50),
                Width = 70
            };
            this.Controls.Add(btnCancel);
            this.btnCancel.Click += Click_Cancel;

        }

        private void userTextChanged(object sender, EventArgs e)
        {
            userName = txtUserName.Text;
        }

        private void addConfigUserToTxtUserName()
        {
            string user = config.Read(ConfigFileManager.KEY_USER) as string;
            if (!string.IsNullOrEmpty(user))
            {
                this.txtUserName.Text = user;
            }
        }

        private void Click_Login(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.txtUserName.Text))
                {
                    MessageBox.Show("Please enter a valid user name.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (UserServiceImpl.Instance.findUserByName(this.txtUserName.Text))
                    {
                        this.DialogResult = DialogResult.OK;
                        UserDto user = UserSession.GetInstance().User;
                        log.LogInfo($"User {user.userName} permission: {user}");
                    } else
                    {
                        MessageBox.Show($"User '{this.txtUserName.Text}' does not exist.", "Invalid Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        log.LogWarning($"Attempt to login with user: {this.txtUserName.Text}. User: {this.txtUserName.Text} is not in the system.\n{LOG_USER_STRING}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.StackTrace);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        private void Click_Cancel(object sender, EventArgs e)
        {
            this.log.LogInfo("Close the LoginForm " + LOG_USER_STRING);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

       
    }
}
