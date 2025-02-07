using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ForecastingModule.Controller;
using ForecastingModule.Helper;
using ForecastingModule.OtherForm;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Service;
using ForecastingModule.Service.Impl;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule
{
    public partial class Form1
    {
        private const string ITEM_OPERATION_PLANNING = "OPERATIONS PLANNING";
        private const string ITEM_MANANGE = "MANAGE";
        private SplitContainer splitContainer;
        private TabControl tabControl;
        private ToolStripStatusLabel statusLabel;
        private StatusStrip statusStrip;
        private Panel buttomButtonPanel;

        public readonly Font TEXT_FONT = new Font("Arial", 9, FontStyle.Bold);

        private readonly Logger log = Logger.Instance;
        private readonly ConfigFileManager config = ConfigFileManager.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private readonly OperationsPlanningServiceImpl operationService = OperationsPlanningServiceImpl.Instance;
        private readonly ForecastServiceImpl forecastService = ForecastServiceImpl.Instance;

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
        private readonly static string FORECAST_DATE_FORMAT = "MM/dd/yy";
        private Color STATUS_LABEL_COLOR = Color.DarkBlue;
        private string previousEditedValue;

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
            try
            {
                GenerateMenuButtons(tabList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.LogError(ex.StackTrace);
            }

            // Right Panel - Tabs and other elements
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                //Alignment = System.Windows.Forms.TabAlignment.Bottom //Uncoment when need same desiign like in task, but Top aligment (as is) is user friendly
            };
            splitContainer.Panel2.Controls.Add(tabControl);

            // Add SplitContainer to the Form
            this.Controls.Add(splitContainer);

            //Form Settings
            this.WindowState = FormWindowState.Maximized;
        }

        private Panel createOperationButtonsPanel()
        {
            Panel operationButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                //BackColor = Color.LightGray
            };

            Button operationSetting = null;
            Button saveOperations = null;
            if (UserSession.GetInstance().User.accessOperationsSettings)
            {
                operationSetting = new Button
                {
                    Size = new Size(80, 40)
                };
                // Set the gear icon using the system icon
                //Icon gearIcon = SystemIcons.Application; // Replace with a gear icon from your resources if needed
                Icon gearIcon = IconExtractor.Extract("imageres.dll", 109); //109 - icon - gear with checkbox.
                operationSetting.Image = gearIcon.ToBitmap();
                operationSetting.TextImageRelation = TextImageRelation.ImageBeforeText;
                operationSetting.Tag = "Operation Settings";

                operationSetting.Click += OnOperationsSettingsButtons_Click;
                operationButtonsPanel.Controls.Add(operationSetting);
            }
            if (UserSession.GetInstance().User.accessOperationsPlanning)
            {
                saveOperations = new Button
                {
                    Text = "SAVE",
                    Anchor = AnchorStyles.None,
                    Size = new Size(100, 40)
                };
                saveOperations.Click += OnSaveOperationButtons_ClickAsync;
                operationButtonsPanel.Controls.Add(saveOperations);
            }

            // Center the buttons horizontally in the panel and reduce the spacing
            operationButtonsPanel.Paint += (sender, e) =>
            {
                if (operationSetting != null && saveOperations != null)
                {
                    int panelWidth = operationButtonsPanel.ClientSize.Width;
                    int buttonSpacing = 8; // Smaller spacing for closer buttons

                    int totalWidth = operationSetting.Width + saveOperations.Width + buttonSpacing;
                    int startX = (panelWidth - totalWidth) / 2;

                    operationSetting.Location = new Point(startX, (operationButtonsPanel.Height - operationSetting.Height) / 2);
                    saveOperations.Location = new Point(startX + operationSetting.Width + buttonSpacing, (operationButtonsPanel.Height - saveOperations.Height) / 2);
                }
            };

            return operationButtonsPanel;
        }

        private async void OnOperationsSettingsButtons_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    string editedTable = "OperationsSettings";
                    Optional<Tuple<string, List<Tuple<string, string>>>> optional = InformationSchemaServiceImpl.Instance.getColumsMetaByTable(editedTable);
                    Invoke((Action)(() =>
                    {
                        if (!optional.HasValue)
                        {
                            throw new InvalidOperationException($"No meta's information by {editedTable}.");
                        }
                        Tuple<string, List<Tuple<string, string>>> model = optional.Get();
                        SettingsView settingsForm = new SettingsView(model);
                        string query = $"SELECT * FROM OperationsSettings order by OPS_DisplayOrder";

                        SettingsController settingsController = new SettingsController(model, settingsForm, query);
                        settingsForm.ShowDialog();
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        log.LogError(ex.Message);
                    }));
                }
            });
        }

        private async void OnSaveOperationButtons_ClickAsync(object sender, EventArgs e)
        {
            await saveOperationPlanning();
        }

        private async Task saveOperationPlanning()
        {
            await Task.Run(() =>
            {
                try
                {
                    this.log.LogInfo($"Saving Operations Plannig data from {selectedTab} -> {selectedSubTab}");
                    int insertedRows = operationService.save(selectedTabModel);
                    Invoke((Action)(() =>
                    {
                        this.log.LogInfo($"Operations Plannig data from {selectedTab} -> {selectedSubTab} has been saved successfully. Inserted {insertedRows} rows.");
                        this.statusLabel.Text = $"Operations Plannig data from {selectedTab} -> {selectedSubTab} has been saved successfully.";
                        this.isModelUpdated = false;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        this.log.LogError($"Error saving Operations Plannig data: {selectedTab} -> {selectedSubTab}: " + ex.Message);
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.statusLabel.Text = $"Error saving Operations Plannig data: {selectedTab} -> {selectedSubTab}.";
                        this.statusLabel.ForeColor = Color.Red;
                        return;
                    }));
                }
            });
        }


        private StatusStrip createStatusStrip()
        {
            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom // Dock it to the bottom
            };

            this.statusLabel = new ToolStripStatusLabel
            {
                Text = "",
                Spring = true, // Ensures it takes up available space
                TextAlign = ContentAlignment.MiddleRight, // Align to bottom-right
                ForeColor = STATUS_LABEL_COLOR,
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

                ((Control)button).Focus();

                string label = button.Text;
                AddTab(label);
            }
        }

        private void clear()
        {
            if (this.statusLabel != null)
            {
                this.statusLabel.Text = string.Empty;
                this.statusLabel.ForeColor = STATUS_LABEL_COLOR;
            }
            selectedSubTab = string.Empty;
            selectedTabModel = null;
            isModelUpdated = false;
        }
        private void clearStatusLabel()
        {
            if (this.statusLabel != null)
            {
                this.statusLabel.Text = string.Empty;
                this.statusLabel.ForeColor = STATUS_LABEL_COLOR;
            }
        }

        private async void AddTab(string menuName)
        {
            log.LogInfo($"Menu {menuName} was clicked.");
            clearTabsAndContents();

            selectedTab = menuName;

            setSelectedTabToWindowTextBar();

            statusStrip = createStatusStrip();
            if (menuName == ITEM_OPERATION_PLANNING)
            {
                await populateSubTabs();

                //add panal with Operation Planning buttons (if accessible)
                buttomButtonPanel = createOperationButtonsPanel();
                splitContainer.Panel2.Controls.Add(buttomButtonPanel);
            }
            else if (menuName == "MANAGE")
            {
                populateManageTab(menuName);
            }
            else
            {//Forecast
                await populateSubTabs(menuName);
                buttomButtonPanel = createForecastButtonsPanel();
                splitContainer.Panel2.Controls.Add(buttomButtonPanel);
            }
            splitContainer.Panel2.Controls.Add(statusStrip);
        }

        private void setSelectedTabToWindowTextBar()
        {
            if (selectedTab != null && !string.IsNullOrEmpty(this.Text))
            {
                int index = this.Text.LastIndexOf("]");
                if (index > 0)
                {
                    string mainHeaderText = this.Text.Substring(0, index + 1);
                    this.Text = mainHeaderText + $" -> {selectedTab}";
                }
            }
        }

        private void populateManageTab(string menuName)
        {
            var tabPage = new TabPage(menuName);
            var label = new Label
            {
                Text = "MANAGE has not implemented yet.",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            tabPage.Controls.Add(label);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
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
                log.LogInfo($"Sub Menu {tabControl.SelectedTab.Text} was clicked. [Menu is {selectedTab}].");
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
                };
                dataGridView.AllowUserToAddRows = false;

                dataGridView.CellValidating += OnDataGridView_DataGridValidating;
                dataGridView.CellEndEdit += OnDataGridView_CellOperationEndEdit;
                dataGridView.CellBeginEdit += OnDataGridView_CellBeginEdit;

                createAndPopulateComments(selectedTab, selectedSubTabName, dataGridView);

                bool readOnlyMode = UserSession.GetInstance().User != null && !UserSession.GetInstance().User.accessOperationsPlanning;
                if (readOnlyMode)
                {
                    dataGridView.ReadOnly = true;

                    string message = $"{ITEM_OPERATION_PLANNING} data is read only.";
                    log.LogInfo(message);
                    statusLabel.Text = message;
                }

                if (!readOnlyMode)
                {
                    // set first row and first column to read only and last TOTAL row
                    dataGridView.CellBeginEdit += OnDataGridView_PreventOperationCellBeginEdit;
                }

                populateOperationPlanningGrid(selectedSubTabName, headerSaleCodes, dataGridView);
                adjsutGridToCenter(dataGridView);
            }
            else if (ITEM_MANANGE != selectedSubTabName)
            {
                this.selectedSubTab = selectedSubTabName;
                log.LogInfo($"Generating Forecast DataGridView for '{selectedSubTabName}' model");
                generateForecastDataGrid(selectedTab, selectedTabName, selectedSubTabName);
            }
            else if (ITEM_MANANGE == selectedSubTabName)
            {
                log.LogInfo($"Grid on Manage will be there.");
            }
        }

        private void createAndPopulateComments(TabPage selectedTab, string selectedSubTabName, DataGridView dataGridView)
        {
            Panel commentPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                AutoScroll = true
            };
            selectedTab.Controls.Add(dataGridView);
            selectedTab.Controls.Add(commentPanel);

            Label commentLabel = new Label
            {
                Dock = DockStyle.Top,
                Text = "COMMENTS:",
            };
            commentPanel.Controls.Add(commentLabel);

            var commentDataGrid = new DataGridView
            {
                Dock = DockStyle.Bottom,
                ColumnCount = 1,
                ColumnHeadersVisible = false,
                RowHeadersVisible = false,
                ReadOnly = true,
            };
            commentDataGrid.AllowUserToAddRows = false;
            commentPanel.Controls.Add(commentDataGrid);
            selectedTab.Controls.Add(commentPanel);

            populateCommentGrid(selectedSubTabName, commentDataGrid);
            deselectFocusCommentDataGrid(commentDataGrid);
        }

        private void populateCommentGrid(string selectedSubTabName, DataGridView commentDataGrid)
        {
            Dictionary<string, object> operationSettings = operationService.getOperationsSetting(selectedSubTabName);

            object comment;
            bool v = operationSettings.TryGetValue("OPS_Comments", out comment);
            if (comment != null)
            {
                string strComment = comment as string;
                setCommentColumnLength(commentDataGrid, strComment.Length);
                commentDataGrid.Rows.Add(comment);
            }
        }

        private static void deselectFocusCommentDataGrid(DataGridView commentDataGrid)
        {
            if (commentDataGrid.RowCount > 0 && commentDataGrid.ColumnCount > 0)
            {
                commentDataGrid.CurrentCell = commentDataGrid[0, 0];
                commentDataGrid.CurrentCell.Selected = false;
            }
        }

        private static void setCommentColumnLength(DataGridView commentDataGrid, int commentLenght)
        {
            if (commentLenght > 0)
            {
                commentDataGrid.Columns[0].Width = commentLenght * 6;
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

                List<object> totalRow = new List<object> { Calculation.TOTAL };

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

                            object total = valuesByCode.Get(Calculation.TOTAL);
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
                log.LogWarning("Operations Planning: Operattion setting did not set up correctly.");
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
        }

        private void OnDataGridView_PreventOperationCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
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

        private void OnDataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is DataGridView dataGridView && dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is int prevValue)
            {
                previousEditedValue = prevValue.ToString();
            }
        }

        private void OnDataGridView_DataGridValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                string newValue = e.FormattedValue?.ToString() ?? "";
                string cleanValue = Validator.RemoveNonNumericCharacters(newValue);

                bool dataRow = !(dataGridView.RowCount - 1 == e.RowIndex || e.RowIndex == 0); //NOT last dataGridView.RowCount - 1 - TOTAL first row, or first empty row 
                if (cleanValue != newValue)
                {
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = cleanValue };
                }
                else if (dataRow && /*!string.IsNullOrEmpty(newValue) &&*/ cleanValue != previousEditedValue)
                {
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = newValue };
                }
                previousEditedValue = null;
            }
        }

        private void updateOperationsTotalCell(int columnIndex, int rowIndex, DataGridView dataGridView, string newValue)
        {
            string saleCode = (string)dataGridView[columnIndex, 0].Value;

            SyncLinkedDictionary<object, object> saleCodeContent = selectedTabModel.Get(saleCode);
            if (saleCodeContent != null)
            {
                string date = (string)dataGridView[0, rowIndex].Value;
                DateTime convertedDateTime = DateTime.ParseExact(date, OPERATION_DATE_FORMAT, System.Globalization.CultureInfo.InvariantCulture);

                object oldVallue = saleCodeContent.Get(convertedDateTime);

                int newParsedValue = string.IsNullOrEmpty(newValue) ? 0 : int.Parse(newValue);
                if (oldVallue != null)
                {
                    saleCodeContent.Update(convertedDateTime, newParsedValue, oldVallue);
                }
                else
                {
                    saleCodeContent.Add(convertedDateTime, newParsedValue);
                }

                int total = Calculation.getSumBySalesCode(saleCode, selectedTabModel);

                object previousValue = saleCodeContent.Get(Calculation.TOTAL);
                saleCodeContent.Update(Calculation.TOTAL, total, previousValue);

                dataGridView[columnIndex, dataGridView.RowCount - 1].Value = total;//update the grid
            }
        }

        private void OnDataGridView_CellOperationEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                // Check if there's a pending update from validation
                var updateInfo = dataGridView.Tag as CellUpdateInfo;
                if (updateInfo != null /*&& updateInfo.RowIndex == e.RowIndex && updateInfo.ColumnIndex == e.ColumnIndex*/)
                {
                    // Update the cell value
                    dataGridView.Rows[updateInfo.RowIndex].Cells[updateInfo.ColumnIndex].Value = updateInfo.NewValue;

                    //recalculate the total
                    updateOperationsTotalCell(updateInfo.ColumnIndex, updateInfo.RowIndex, dataGridView, (string)updateInfo.NewValue);

                    // Clear the tag and flag: isModelUpdated 
                    dataGridView.Tag = null;
                    isModelUpdated = true;
                }
            }
        }

        private void clearTabsAndContents()
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                tab.Controls.Clear();
            }
            tabControl.TabPages.Clear();
            tabControl.Selected -= OnSelectedOperationPlaningModelTab;
            tabControl.Selecting -= OnChangeTheTab;

            if (buttomButtonPanel != null)
            {
                splitContainer.Panel2.Controls.Remove(buttomButtonPanel);
                buttomButtonPanel.Dispose();
            }
            if (statusStrip != null)
            {
                splitContainer.Panel2.Controls.Remove(statusStrip);
                statusStrip.Dispose();
            }
        }

        #endregion
    }
}

