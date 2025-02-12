using System.Windows.Forms;

namespace ForecastingModule
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void buttonClose_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}
