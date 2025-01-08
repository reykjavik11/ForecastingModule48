using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Service;
using ForecastingModule.Util;

namespace ForecastingModule
{
    public partial class Form1
    {
        private void generateForecastDataGrid(TabPage selectedTab, string selectedTabName, string selectedSubTabName)
        {
            Dictionary<string, object> operationSettings = operationService.getOperationsSetting(selectedTabName);

            this.selectedSubTab = selectedSubTabName;
            log.LogInfo($"Generating {ITEM_OPERATION_PLANNING} DataGridView for '{selectedSubTabName}' moodel");

            selectedTabModel = forecastService.retrieveForecastData(selectedSubTabName);
            if(this.selectedTabModel != null && this.selectedTabModel.Keys.Count() == 0)
            {
                log.LogWarning($"Empty result set by selected tab {selectedTabName} with sub-tab {selectedSubTabName}");
                return;
            }

            selectedTab.Controls.Clear();

            object objDays;
            object objMonths;
            if (operationSettings.TryGetValue("OPS_NbrDays", out objDays) && operationSettings.TryGetValue("OPS_NbrMonths", out objMonths))
            {
                List<DateTime> dateTimes = DataGridHelper.GenerateDateList(DateTime.Now, (int)objDays, (int)objMonths, false);
                int numCoLumns = dateTimes.Count;
                if (numCoLumns == 0)
                {
                    log.LogWarning($"generateDataGrid: Number of retrieved columns: {numCoLumns}");
                    return;
                }

                var dataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = numCoLumns + 4,//plus sale code, Item Name, FC %...Total
                    ColumnHeadersVisible = false,
                };
                dataGridView.AllowUserToAddRows = false;

                dataGridView.CellValidating += OnDataGridView_ForecastValidating;
                dataGridView.CellEndEdit += OnDataGridView_CellForecastEndEdit;

                selectedTab.Controls.Add(dataGridView);

                bool readOnlyMode = UserSession.GetInstance().User != null && !UserSession.GetInstance().User.accessOperationsPlanning;
                if (readOnlyMode)
                {
                    dataGridView.ReadOnly = true;

                    string message = $"{selectedSubTabName} DataGridView is read only.";
                    log.LogInfo(message);
                    statusLabel.Text = message;
                }
                else
                {
                    dataGridView.CellBeginEdit += OnDataGridView_PreventForecastCellBeginEdit;
                }

                populateForecastGrid(dateTimes, dataGridView);
                adjsutForecastGridToCenter(dataGridView);
            }
            else
            {
                log.LogError("Forecast Module: OperationSetting seted up not correctly.");
            }
        }

        private void populateForecastGrid(List<DateTime> forecastDates, DataGridView dataGridView)
        {
            if (forecastDates == null || forecastDates.Count == 0 || this.selectedTabModel == null)
            {
                return;
            }
            List<string> plannigDates = new List<string> { "", "", "" };
            List<string> secondRow = new List<string> { "", "", "FC %" };
            foreach (DateTime forecastDate in forecastDates)
            {
                plannigDates.Add(forecastDate.ToString(OPERATION_DATE_FORMAT));
                secondRow.Add(DateUtil.toForecastDay(forecastDate).ToString(FORECAST_DATE_FORMAT));
            }
            secondRow.Add(Calculation.TOTAL);

            dataGridView.Rows.Add(plannigDates.ToArray());
            dataGridView.Rows.Add(secondRow.ToArray());

            List<string> saleCodes = this.selectedTabModel.Keys.ToList();
            populateBodyForecasting(forecastDates, dataGridView, saleCodes);
        }

        private void populateBodyForecasting(List<DateTime> forecastDates, DataGridView dataGridView, List<string> saleCodes)
        {
            foreach (var saleCode in saleCodes)
            {
                SyncLinkedDictionary<object, object> saleParams = this.selectedTabModel.Get(saleCode);
                if (saleParams != null)
                {
                    List<object> row = new List<object>(forecastDates.Count + 4);
                    row.Add(saleCode);
                    row.Add(saleParams.Get("SC_ItemName"));
                    row.Add(saleParams.Get("SC_FCPercent"));
                    foreach (DateTime forecastDate in forecastDates)
                    {
                        object count = saleParams.Get(DateUtil.toForecastDay(forecastDate));
                        row.Add(count != null ? count : 0);
                    }
                    row.Add(saleParams.Get(Calculation.TOTAL));

                    dataGridView.Rows.Add(row.ToArray());
                }
            }
        }

        private void OnDataGridView_PreventForecastCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is DataGridView dataGrid)
            {
                int lastRowIndex = dataGrid.RowCount - 1;

                if ((e.ColumnIndex >= 0 && e.ColumnIndex <= 2) || e.RowIndex == 0 || e.RowIndex == 1 || e.ColumnIndex == dataGrid.ColumnCount - 1) //disable - 1..3 columns, 1-2 Rows, Last row (TOTAL) 
                {
                    e.Cancel = true;
                }
                else if (this.selectedTabModel != null && e.RowIndex > 1)
                {
                    string saleCode = (string)dataGrid[0, e.RowIndex].Value;
                    SyncLinkedDictionary<object, object> salesValues = this.selectedTabModel.Get(saleCode);
                    if (salesValues != null)
                    {
                        object baseFlag = salesValues.Get(ForecastRepositoryImpl.BASE_FLAG);
                        if (baseFlag != null && baseFlag is bool flag && flag)
                        {
                            e.Cancel = true;
                        }
                    }
                }
            }
        }

        private void OnDataGridView_CellForecastEndEdit(object sender, DataGridViewCellEventArgs e)
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

        private void updateForecastTotalCell(int columnIndex, int rowIndex, DataGridView dataGridView, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }
            string saleCode = (string)dataGridView[0, rowIndex].Value;

            SyncLinkedDictionary<object, object> saleCodeContent = this.selectedTabModel.Get(saleCode);
            if (saleCodeContent != null)
            {
                string date = (string)dataGridView[columnIndex, 1].Value;
                DateTime convertedDateTime = DateTime.ParseExact(date, FORECAST_DATE_FORMAT, System.Globalization.CultureInfo.InvariantCulture);

                object oldVallue = saleCodeContent.Get(convertedDateTime);

                if (oldVallue != null)
                {
                    saleCodeContent.Update(convertedDateTime, int.Parse(newValue), oldVallue);
                }
                else
                {
                    saleCodeContent.Add(convertedDateTime, int.Parse(newValue));
                }

                int total = Calculation.getSumBySalesCode(saleCode, this.selectedTabModel);

                saleCodeContent.Add(Calculation.TOTAL, int.Parse(newValue));

                dataGridView[dataGridView.ColumnCount - 1, rowIndex].Value = total;
                isModelUpdated = true;
            }
        }
        private void OnDataGridView_ForecastValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                string newValue = e.FormattedValue?.ToString() ?? "";

                string cleanValue = Validator.RemoveNonNumericCharacters(newValue);

                bool dataRow = !(dataGridView.ColumnCount - 1 == e.ColumnIndex || (e.RowIndex >= 0 && e.RowIndex <= 2) || e.RowIndex == 0 || e.RowIndex == 1);
                if (cleanValue != newValue)
                {
                    if(string.IsNullOrEmpty(cleanValue))
                    {
                        cleanValue = "0";
                    }
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = cleanValue };
                }
                else if (dataRow && !string.IsNullOrEmpty(newValue))
                {
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = newValue };
                }
            }
        }

        private static void adjsutForecastGridToCenter(DataGridView dataGridView)
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.Index > 1) // Exclude the first 2 columns
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }
        //one method for OP and Forecast
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
    }

}
