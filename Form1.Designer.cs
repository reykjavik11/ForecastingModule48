using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Service;
using ForecastingModule.Service.Impl;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule
{
    partial class Form1
    {
        private const string ITEM_OPERATION_PLANNING = "OPERATIONS PLANNING";
        private const string ITEM_MANANGE = "MANAGE";
        private SplitContainer splitContainer;
        private TabControl tabControl;
        private ToolStripStatusLabel statusLabel;

        public readonly Font TEXT_FONT = new Font("Arial", 9, FontStyle.Bold);

        private readonly Logger log = Logger.Instance;
        private readonly ConfigFileManager config = ConfigFileManager.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private readonly OperationsPlanningServiceImpl operationService = OperationsPlanningServiceImpl.Instance;

        ///  Required designer variable.
        private System.ComponentModel.IContainer components = null;
        private List<string> tabList = new List<string>();
        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>

        private string selectedTab;
        private string selectedSubTab;
        private SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> selectedTabModel;
        private bool isModelUpdated = false;
        private readonly static string OPERATION_DATE_FORMAT = "MMM-yy";
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
                clear();
                //this.statusLabel.ForeColor = Color.LimeGreen;
                this.statusLabel.ForeColor = Color.DarkBlue;

                ((Control)button).Focus();

                string label = button.Text;
                AddTab(label);
            }
        }

        private void clear()
        {
            this.statusLabel.Text = string.Empty;
            selectedSubTab = string.Empty;
            selectedTabModel = null;
            isModelUpdated = false;
        }

        private async void AddTab(string menuName)
        {
            log.LogInfo($"Menu {menuName} was clicked.");
            clearTabsAndContents();

            selectedTab = menuName;

            if (menuName == ITEM_OPERATION_PLANNING)
            {
                await populateSubTabs();
            }
            else if (menuName == "MANAGE")
            {
                populateManageTab(menuName);
            }
            else
            {
                await populateSubTabs(menuName);
            }
        }

        private void populateManageTab(string menuName)
        {
            var tabPage = new TabPage(menuName);
            var label = new Label
            {
                Text = "Content for MANAGE",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            tabPage.Controls.Add(label);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
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
                        }

                        tabControl.Selected += OnSelectedOperationPlaningModelTab;

                        tabControl.Selecting += OnChangeTheTab;

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

        private void OnChangeTheTab(object sender, TabControlCancelEventArgs e)
        {
            // If preventTabSwitch is true, cancel the tab switch
            if (isModelUpdated)
            {
                DialogResult result = MessageBox.Show("There are unsaved changes - all new changes will be lost. Do you really want to switch the tab?", "Unsaved Changes",
                                                        MessageBoxButtons.YesNo,
                                                        MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;  // Prevent switching to the selected tab
                }
                else
                {
                    isModelUpdated = false;  // Reset to false if the user confirms
                }
            }
        }

        private void OnSelectedOperationPlaningModelTab(object sender, TabControlEventArgs e)
        {
            if (sender is TabControl tabControll && tabControl.SelectedTab != null)
            {
                this.selectedSubTab = e.TabPage.Text;
                log.LogInfo($"Sub Menu {tabControl.SelectedTab.Text} was clicked.");
                clear();//clear when selected on tab and subTab
                generateDataGrid(e.TabPage, selectedTab, e.TabPage.Text);
            }
        }

        private void generateDataGrid(TabPage selectedTab, string selectedTabName, string selectedSubTabName)
        {
            if (ITEM_OPERATION_PLANNING == selectedTabName)
            {
                this.selectedSubTab = selectedSubTabName;
                log.LogInfo($"Generating {ITEM_OPERATION_PLANNING} DataGridView for '{selectedSubTabName}' moodel");

                selectedTabModel = operationService.retrieveExistedOperationsPlanning(selectedSubTabName);

                List<string> headerSaleCodes = selectedTabModel.Keys.ToList();
                int numCoLumns = headerSaleCodes.Count;
                if (numCoLumns == 0)
                {
                    log.LogWarning($"generateDataGrid: Number of retrieved columns: {numCoLumns}");
                }
                selectedTab.Controls.Clear();
                
                var dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = numCoLumns + 1,
                    ColumnHeadersVisible = false,
                    //RowHeadersVisible = false
                };
                dataGridView.AllowUserToAddRows = false;

                dataGridView.CellValidating += OnDataGridView_DataGridValidating;
                dataGridView.CellEndEdit += OnDataGridView_CellOperationEndEdit;

                selectedTab.Controls.Add(dataGridView);

                bool readOnlyMode = UserSession.GetInstance().User != null && !UserSession.GetInstance().User.accessOperationsPlanning;
                if (readOnlyMode)
                {
                    dataGridView.ReadOnly = true;

                    string message = $"{ITEM_OPERATION_PLANNING} DataGridView is read only.";
                    log.LogInfo(message);
                    statusLabel.Text = message;
                }

                if (!readOnlyMode)
                {
                    // set first row and first column to read only and last TOTAL row
                    dataGridView.CellBeginEdit += OnDataGridView_PreventCellBeginEdit;
                }

                populateOperationPlanningGrid(selectedSubTabName, headerSaleCodes, dataGridView);
                adjsutGridToCenter(dataGridView);
            }
            else if (ITEM_MANANGE != selectedSubTabName)
            {
                this.selectedSubTab = selectedSubTabName;
                log.LogInfo($"Generating Forecast DataGridView for '{selectedSubTabName}' moodel");
            }
            else if (ITEM_MANANGE == selectedSubTabName)
            {
                log.LogInfo($"Grip on Manage will be there.");
            }
        }

        private void populateOperationPlanningGrid(string selectedSubTabName, List<string> headerSaleCodes, DataGridView dataGridView)
        {
            Dictionary<string, object> operationSettings = operationService.getOperationsSetting(selectedSubTabName);

            object objDays;
            object objMonths;
            if (operationSettings.TryGetValue("OPS_NbrDays", out objDays) && operationSettings.TryGetValue("OPS_NbrMonths", out objMonths))
            {
                List<DateTime> dateTimes = DataGridHelper.GenerateDateList(DateTime.Now, (int)objDays, (int)objMonths);

                headerSaleCodes.Insert(0, "");
                dataGridView.Rows.Add(headerSaleCodes.ToArray());//add header line

                List<object> totalRow = new List<object> { OperationsPlanningServiceImpl.TOTAL };

                foreach (var date in dateTimes)//add other lines with values
                {
                    List<object> contentRow = new List<object>();
                    contentRow.Add(date.ToString(OPERATION_DATE_FORMAT));

                    foreach (var code in headerSaleCodes)
                    {
                        SyncLinkedDictionary<object, object> valuesByCode = selectedTabModel.Get(code);
                        if (valuesByCode != null)
                        {
                            object number = valuesByCode.Get(date);
                            contentRow.Add(number);

                            object total = valuesByCode.Get(OperationsPlanningServiceImpl.TOTAL);
                            if (total != null)
                            {
                                totalRow.Add(total);
                            }
                        }
                    }
                    dataGridView.Rows.Add(contentRow.ToArray());
                }
                //Add TOTAl Footer
                dataGridView.Rows.Add(totalRow.ToArray());

            }
            else
            {//IF operattion setting did not set up correctly
                log.LogWarning("Operattion setting did not set up correctly.");
            }
        }

        private static void adjsutGridToCenter(DataGridView dataGridView)
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.Index != 0) // Exclude the first column
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            //Center align cells in all rows except the last row
            //foreach (DataGridViewRow row in dataGridView.Rows)
            //{
            //    if (row.Index != dataGridView.Rows.Count - 1) // Exclude the last row
            //    {
            //        foreach (DataGridViewCell cell in row.Cells)
            //        {
            //            if (cell.ColumnIndex != 0) // Exclude the first column
            //            {
            //                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //            }
            //        }
            //    }
            //}
        }

        private void OnDataGridView_PreventCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Block editing for the first row(sale codes HEADER) or first column (period column) or last row (TOTAL)
            if (sender is DataGridView dataGrid)
            {
                int lastRowIndex = dataGrid.RowCount - 1;

                if (e.RowIndex == 0 || e.ColumnIndex == 0 || e.RowIndex == lastRowIndex)
                {
                    e.Cancel = true;
                }
            }
        }

        private void OnDataGridView_DataGridValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                string newValue = e.FormattedValue?.ToString() ?? "";

                string cleanValue = Validator.RemoveNonNumericCharacters(newValue);

                bool dateRow = !(dataGridView.RowCount - 1 == e.RowIndex || e.RowIndex == 0); //last dataGridView.RowCount - 1 - TOTAL first row, or first empty row 
                if (cleanValue != newValue)
                {
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = cleanValue }; // Default value
                }
                else if (dateRow && !string.IsNullOrEmpty(newValue))
                {
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = newValue }; // Default value
                    //updateOperationsTotalCell(e.ColumnIndex, e.RowIndex, dataGridView, newValue);
                }
            }
        }

        private void updateOperationsTotalCell(int columnIndex, int rowIndex, DataGridView dataGridView, string newValue)
        {
            if(string.IsNullOrEmpty(newValue))
            {
                return;
            }
            string saleCode = (string)dataGridView[columnIndex, 0].Value;

            SyncLinkedDictionary<object, object> saleCodeContent = selectedTabModel.Get(saleCode);
            if (saleCodeContent != null)
            {
                string date = (string)dataGridView[0, rowIndex].Value;
                DateTime convertedDateTime = DateTime.ParseExact(date, OPERATION_DATE_FORMAT, System.Globalization.CultureInfo.InvariantCulture);

                object oldVallue = saleCodeContent.Get(convertedDateTime);

                if (oldVallue != null)
                {
                    saleCodeContent.Update(convertedDateTime, int.Parse(newValue), oldVallue);
                }
                else
                {
                    saleCodeContent.Add(convertedDateTime, int.Parse(newValue));
                }

                int total = OperationsPlanningServiceImpl.getTotal(saleCode, selectedTabModel);

                saleCodeContent.Add(OperationsPlanningServiceImpl.TOTAL, int.Parse(newValue));

                dataGridView[columnIndex, dataGridView.RowCount - 1].Value = total;
                isModelUpdated = true;
            }
        }

        private void OnDataGridView_CellOperationEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                // Check if there's a pending update from validation
                var updateInfo = dataGridView.Tag as CellUpdateInfo;
                if (updateInfo != null && updateInfo.RowIndex == e.RowIndex && updateInfo.ColumnIndex == e.ColumnIndex)
                {
                    // Update the cell value
                    dataGridView.Rows[updateInfo.RowIndex].Cells[updateInfo.ColumnIndex].Value = updateInfo.NewValue;

                    //recalculate the total
                    updateOperationsTotalCell(updateInfo.ColumnIndex, updateInfo.RowIndex, dataGridView, (string)updateInfo.NewValue);

                    // Clear the tag
                    dataGridView.Tag = null;
                }
            }
        }

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
    }
}

