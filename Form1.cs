using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ForecastingModule.Util;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ForecastingModule
{
    public partial class Form1 : Form
    {
        private void mainFormLoad(object sender, EventArgs e)
        {
            //Maximaze the form to fill the screen on the primary monitor
            this.WindowState = FormWindowState.Maximized;

            //explicitly adjust it based on the screen size:
            var screen = Screen.PrimaryScreen;
            this.Size = screen.WorkingArea.Size;
            this.Location = screen.WorkingArea.Location;
        }

        public Form1()
        {
            InitializeComponent();
            this.Load += mainFormLoad;

            string user = !string.IsNullOrEmpty(config.Read(ConfigFileManager.KEY_USER) as string)
                ? config.Read(ConfigFileManager.KEY_USER) as string : "is not defined.";
            log.LogInfo("Application has been started - user: " + user);
        }
    }
}
