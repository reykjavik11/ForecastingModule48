using System;
using System.Windows.Forms;

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

            log.LogInfo("Application has been started.");
        }
    }
}
