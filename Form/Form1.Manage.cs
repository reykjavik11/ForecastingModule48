using System;
using System.Windows.Forms;
using ForecastingModule.OtherForm;
using ForecastingModule.Service;

namespace ForecastingModule
{
    public partial class Form1
    {
        private Button manageForecastTabsButton;
        private Button manageSubTabsButton;
        private Button manageUsersButton;
        private Button manageSalesCodesButton;

        private void populateManageTab(string menuName)
        {
            var tabPage = new TabPage(menuName);

            //Create FlowLayoutPanel
            FlowLayoutPanel flowLayoutPanel = createFlowLayoutMenegerPanel();

            if (UserSession.GetInstance().User.accessManage)
            {
                createMenegeButtons(flowLayoutPanel);
            }
            else
            {
                statusLabel.Text = MESSAGE_NO_PERMISSION;
            }

            tabPage.Controls.Add(flowLayoutPanel);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
        }

        private FlowLayoutPanel createFlowLayoutMenegerPanel()
        {
            FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.Dock = DockStyle.Fill;
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown; // Or LeftToRight
            flowLayoutPanel.WrapContents = false;  // Prevent wrapping

            // Set the FlowLayoutPanel's alignment to center its contents
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown; // For vertical layout
            flowLayoutPanel.AutoSize = true;
            flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel.Anchor = AnchorStyles.Top;
            flowLayoutPanel.Padding = new Padding(0, (this.Height / 2) - 140, 0, 0);
            return flowLayoutPanel;
        }

        private void createMenegeButtons(FlowLayoutPanel flowLayoutPanel)
        {
            this.manageForecastTabsButton = new Button();
            this.manageForecastTabsButton.Text = "Manage Forecast Tabs";
            this.manageForecastTabsButton.Height = SettingsView.BUTTON_HEIGHT;
            this.manageForecastTabsButton.Anchor = AnchorStyles.None;
            this.manageForecastTabsButton.Width = 300;
            this.manageForecastTabsButton.Font = TEXT_FONT;
            this.manageForecastTabsButton.Click += OnManageForecast_Click;
            flowLayoutPanel.Controls.Add(this.manageForecastTabsButton);

            this.manageSubTabsButton = new Button();
            this.manageSubTabsButton.Text = "Manage Sub Tabs";
            this.manageSubTabsButton.Height = SettingsView.BUTTON_HEIGHT;
            this.manageSubTabsButton.Anchor = AnchorStyles.None;
            this.manageSubTabsButton.Width = 300;
            this.manageSubTabsButton.Font = TEXT_FONT;
            this.manageSubTabsButton.Click += OnManageSubTabs_Click;
            flowLayoutPanel.Controls.Add(this.manageSubTabsButton);

            this.manageSalesCodesButton = new Button();
            this.manageSalesCodesButton.Text = "Manage Sales Codes";
            this.manageSalesCodesButton.Height = SettingsView.BUTTON_HEIGHT;
            this.manageSalesCodesButton.Anchor = AnchorStyles.None;
            this.manageSalesCodesButton.Width = 300;
            this.manageSalesCodesButton.Font = TEXT_FONT;
            this.manageSalesCodesButton.Click += OnManageSalesCodes_Click;
            flowLayoutPanel.Controls.Add(this.manageSalesCodesButton);

            this.manageUsersButton = new Button();
            this.manageUsersButton.Text = "Manage Users";
            this.manageUsersButton.Height = SettingsView.BUTTON_HEIGHT;
            this.manageUsersButton.Anchor = AnchorStyles.None;
            this.manageUsersButton.Width = 300;
            this.manageUsersButton.Font = TEXT_FONT;
            this.manageUsersButton.Click += OnManageUsers_Click;
            flowLayoutPanel.Controls.Add(this.manageUsersButton);
        }

        private void OnManageSalesCodes_Click(object sender, EventArgs e)
        {
            MessageBox.Show("OnManageSalesCodes_Click");
        }

        private void OnManageUsers_Click(object sender, EventArgs e)
        {
            MessageBox.Show("OnManageUsers_Click");
        }

        private void OnManageSubTabs_Click(object sender, EventArgs e)
        {
            MessageBox.Show("OnManageSubTabs_Click");
        }

        private void OnManageForecast_Click(object sender, EventArgs e)
        {
            MessageBox.Show("OnManageForecast_Click");
        }
    }
}
