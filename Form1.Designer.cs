using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Service;
using ForecastingModule.Utilities;
using OfficeOpenXml;

namespace ForecastingModule
{
    partial class Form1
    {
        private const string ITEM_OPERATION_PLANNING = "OPERATIONS PLANNING";
        private const string ITEM_MANANGE = "MANAGE";
        private SplitContainer splitContainer;
        private TabControl tabControl;
        private DataGridView testDataGridView;
        private ToolStripStatusLabel statusLabel;

        public readonly Font TEXT_FONT = new Font("Arial", 9, FontStyle.Bold);

        private readonly Logger log = Logger.Instance;
        private readonly ConfigFileManager config = ConfigFileManager.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;

        ///  Required designer variable.
        private System.ComponentModel.IContainer components = null;
        private List<string> tabList = new List<string>();
        private List<string> subTabNameList = new List<string> { "E1050", "E1250C", "E1650A", "E2850C", "E2860C" };
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>

        private string selectedTab;
        private string selectedSubTab;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                log.LogError("Application has been closed.");
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Name = "Form1";
            Text = $"Forecasting Module - [ {UserSession.GetInstance().User.userName} ]";
            ResumeLayout(false);

            populateTabList();

            InitializeInnerComponent();
            this.FormClosing += Form_Closing;


        }

        private void populateTabList()
        {
            try
            {
                log.LogInfo($"Loaded dymanic Tabs from [TAB_DisplayOrder]: {string.Join(", ", tabList)}");

                tabList.Add(ITEM_OPERATION_PLANNING);
                tabList.AddRange(TabRepositoryImpl.Instance.getActiveTabs());
                tabList.Add(ITEM_MANANGE);

                tabList = new HashSet<string>(tabList).ToList();//cover the case with duplication - avoid duplication

                tabList.Reverse();
            }
            catch (Exception ex)
            {
                log.LogError(ex.StackTrace);
            }
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            log.LogInfo("Program has been closed.");
        }

        private void InitializeInnerComponent()
        {
            //create the SplitContainer
            splitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical, // Divide left and right
                SplitterDistance = (int)(this.ClientSize.Width * 0.006) //6% for left panel
            };

            // Left Panel - Dynamicly Generated Buttons
            GenerateMenuButtons(tabList);

            // Right Panel - Tabs and other elements
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                //Alignment = System.Windows.Forms.TabAlignment.Bottom //Uncoment when need same desiign like in task, but Top aligment (as is) is user friendly
            };
            splitContainer.Panel2.Controls.Add(tabControl);
            splitContainer.Panel2.Controls.Add(createStatusStrip());


            // Add SplitContainer to the Form
            this.Controls.Add(splitContainer);

            //Form Settings
            this.WindowState = FormWindowState.Maximized;
        }

        private StatusStrip createStatusStrip()
        {
            var statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom // Dock it to the bottom
            };

            this.statusLabel = new ToolStripStatusLabel
            {
                Text = "",
                Spring = true, // Ensures it takes up available space
                TextAlign = ContentAlignment.MiddleRight, // Align to bottom-right
                ForeColor = Color.LimeGreen,
                //BackColor = Color.White
            };

            statusStrip.Items.Add(statusLabel);
            return statusStrip;
        }

        private void GenerateMenuButtons(List<string> list)
        {
            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            int buttonHeight = 50;
            foreach (string label in list)
            {
                var button = new Button
                {
                    Text = label,
                    Dock = DockStyle.Top,
                    Height = buttonHeight,
                    Tag = label, //Store label or identifier in Tag
                    Font = TEXT_FONT,
                };
                button.FlatAppearance.BorderColor = Color.DarkSeaGreen;
                button.FlatAppearance.BorderSize = 1;

                button.TabStop = false;//Prevent highlight (looks selection) on last button
                                       //
                button.Click += OnMenuButtonClick;
                scrollPanel.Controls.Add(button);
            }
            splitContainer.Panel1.Controls.Add(scrollPanel);
        }

        private void OnMenuButtonClick(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                this.statusLabel.Text = string.Empty;
                this.statusLabel.ForeColor = Color.LimeGreen;

                ((Control)button).Focus();

                string label = button.Text;
                AddTab(label);
            }
        }

        private async void AddTab(string menuName)
        {
            clearTabsAndContents();

            if (menuName == ITEM_OPERATION_PLANNING)
            {
                await populateSubTabs();
            }
            else
            {
                await populateSubTabs(menuName);
            }
        }

        private async Task populateSubTabs(string menuName = null)
        {
            await Task.Run(() =>
            {
                try
                {
                    List<string> forecastSubTabs = menuName != null ? SubTabRepositoryForecastImpl.Instance.getActiveSubTabs(menuName)
                    : SubTabRepositoryOperationsImpl.Instance.getActiveSubTabs();

                    Invoke((Action)(() =>
                    {
                        List<TabPage> tabList = new List<TabPage>(forecastSubTabs.Count);
                        foreach (var tabName in forecastSubTabs)
                        {
                            var mtvTab = new TabPage(tabName);
                            tabControl.TabPages.Add(mtvTab);

                            tabList.Add(mtvTab);

                            //mtvTab.GotFocus += OnSelectOperationSubTab;
                            //tabControl.Selecting += OnSelectingOperationPlaningModelTab;
                        }
                        
                        tabControl.Selected += OnSelectedOperationPlaningModelTab;

                        var firstSubTab = tabList.Count > 0 ? tabList[0] : null;
                        if (firstSubTab != null)
                        {
                            tabControl.SelectedTab = firstSubTab; // Focus the new tab

                            TabControlEventArgs args = new TabControlEventArgs(firstSubTab, 0, TabControlAction.Selected);
                            OnSelectedOperationPlaningModelTab(tabControl, args);
                        }

                    }));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        this.log.LogError(ex.Message);
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.statusLabel.Text = "Population of SubTabs failed.";
                        this.statusLabel.ForeColor = Color.Red;
                        return;
                    }));
                }
            });
        }

        private void OnSelectedOperationPlaningModelTab(object sender, TabControlEventArgs e)
        {
            if (sender is TabControl tabControll && tabControl.SelectedTab != null)
            {
                MessageBox.Show($"Cliked subTab {tabControl.SelectedTab.Text}");
            }
        }

        //private void OnSelectingOperationPlaningModelTab(object sender, TabControlCancelEventArgs e)
        //{
        //    if (sender is TabControl tabControll)
        //    {
        //        MessageBox.Show($"Cliked subTab {tabControl.SelectedTab.Text}");
        //    }
        //}

        //private async void AddTab(string menuName)
        //{
        //    //check if if the tab already exist
        //    foreach (TabPage existingTab in tabControl.TabPages)
        //    {
        //        if (existingTab.Text == menuName)
        //        {
        //            tabControl.SelectedTab = existingTab; // Focus the existing tab
        //            return;
        //        }
        //    }

        //    // Create a new tab page
        //    var tabPage = new TabPage(menuName);

        //    clearTabsAndContents();

        //    if (menuName == "MTV" /*&& tabControl.TabPages.Count == 0*/)
        //    {
        //        List<TabPage> tabList = new List<TabPage>(subTabNameList.Count);

        //        foreach (var tabName in subTabNameList)
        //        {
        //            var mtvTab = new TabPage(tabName);
        //            var dataGridView = new DataGridView
        //            {
        //                Dock = DockStyle.Fill,
        //                ColumnCount = 3
        //            };
        //            dataGridView.Rows.Add("Row 1", "Data 1", "Info 1");
        //            dataGridView.Rows.Add("Row 2", "Data 2", "Info 2");
        //            mtvTab.Controls.Add(dataGridView);

        //            tabControl.TabPages.Add(mtvTab);

        //            tabList.Add(mtvTab);

        //            //tabControl.SelectedTab = mtvTab; // Focus the new tab
        //        }

        //        var firstSubTab = tabList.Count > 0? tabList[0] : null;
        //        if (firstSubTab != null)
        //        {
        //            tabControl.SelectedTab = firstSubTab; // Focus the new tab
        //        }

        //        //TODO only for test DB connection
        //        this.statusLabel.Text = "Trying to run query.";

        //        await Task.Run(() =>
        //        {
        //            try
        //            {
        //                //db.ExecuteMySqlQuery("SELECT U.user_name, U.device_id FROM user U");
        //                db.ExecuteQuery("select USR_UserName, USR_Access_OperationsPlanning FROM [WeilerForecasting].[dbo].[Users]");
        //                Invoke((Action)(() =>
        //                {
        //                    this.statusLabel.Text = "Query been runed.";
        //                }));
        //            }
        //            catch (Exception ex)
        //            {
        //                Invoke((Action)(() =>
        //                {
        //                    this.log.LogError(ex.Message);
        //                    MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //                    this.statusLabel.Text = "Query has been failed.";
        //                    this.statusLabel.ForeColor = Color.Red;
        //                    return;
        //                }));
        //            }
        //        });

        //        //End TODO only for test DB connection
        //        return;
        //    }
        //    else if (menuName == "BROOM")
        //    {
        //        var testTab = new TabPage(menuName);

        //        testDataGridView = new DataGridView
        //        {
        //            Dock = DockStyle.Fill,
        //        };
        //        testDataGridView.AllowUserToAddRows = false;

        //        testDataGridView.DataSource = LoadExcelToDataTable("D:\\PROJECTS\\C#\\ForecastingModule48\\bin\\Debug\\mtv_example.xlsx");

        //        testDataGridView.CellValueChanged += dataGridTestView_CellValueChanged;
        //        testTab.Controls.Add(testDataGridView);

        //        tabControl.TabPages.Add(testTab);
        //        tabControl.SelectedTab = testTab; // Focus the new tab
        //        return;
        //    }
        //    else if (menuName == "STABILIZER")
        //    {
        //        var label = new Label
        //        {
        //            Text = "Content for STABILIZER",
        //            Dock = DockStyle.Fill,
        //            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        //        };
        //        tabPage.Controls.Add(label);
        //    }

        //    tabControl.TabPages.Add(tabPage);
        //    tabControl.SelectedTab = tabPage; // Focus the new tab
        //}

        private void clearTabsAndContents()
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                tab.Controls.Clear();
            }
            tabControl.TabPages.Clear();
            tabControl.Selected -= OnSelectedOperationPlaningModelTab;
        }

        #endregion
        /*
            Test excell loading
        */
        private DataTable LoadExcelToDataTable(string filePath)
        {
            try
            {
                log.LogInfo($"Trying to read data from {filePath} file.");
                using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        string message = $"Can not find path {filePath} or file is empty.";
                        MessageBox.Show(message);
                        log.LogError(message);

                        return new DataTable();
                    }
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // First worksheet
                    DataTable dataTable = new DataTable();

                    // Get column headers
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        dataTable.Columns.Add(worksheet.Cells[1, col].Text);
                    }

                    // Get rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        DataRow dataRow = dataTable.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text;
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                    log.LogInfo($"File {filePath} was read sucessfully.");

                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                string message = $"Exception while rading data file {filePath} - {ex.Message}.";
                log.LogError(message);
                MessageBox.Show(message);
                return new DataTable();
            }
        }


        //Calculation
        private void dataGridTestView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //TODO If percentage is present -
            //EvaluateAndRecalculate(testDataGridView);
        }
    }

}

