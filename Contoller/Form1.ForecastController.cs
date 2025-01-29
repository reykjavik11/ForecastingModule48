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
        private List<DateTime> forcastDateTimes = new List<DateTime>();
        private DataGridView forecastDataGridView;
        private void generateForecastDataGrid(TabPage selectedTab, string selectedTabName, string selectedSubTabName)
        {
            Dictionary<string, object> operationSettings = operationService.getOperationsSetting(selectedTabName);

            this.selectedSubTab = selectedSubTabName;
            log.LogInfo($"Generating {ITEM_OPERATION_PLANNING} DataGridView for '{selectedSubTabName}' moodel");

            selectedTabModel = forecastService.retrieveForecastData(selectedSubTabName);
            if (this.selectedTabModel != null && this.selectedTabModel.Keys.Count() == 0)
            {
                log.LogWarning($"Empty result set by selected tab {selectedTabName} with sub-tab {selectedSubTabName}");
                return;
            }

            selectedTab.Controls.Clear();

            populateForecastDates();

            if (this.forcastDateTimes.Count() > 0)
            {
                int numCoLumns = forcastDateTimes.Count;
                if (numCoLumns == 0)
                {
                    log.LogWarning($"generateDataGrid: Number of retrieved columns: {numCoLumns}");
                    return;
                }

                forecastDataGridView = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = numCoLumns + 5,//plus sale code, Item Name, FC %...Total, Comments
                    ColumnHeadersVisible = false,
                };
                forecastDataGridView.AllowUserToAddRows = false;

                forecastDataGridView.CellValidating += OnDataGridView_ForecastValidating;
                forecastDataGridView.CellEndEdit += OnDataGridView_CellForecastEndEdit;
                forecastDataGridView.CellBeginEdit += OnDataGridView_CellBeginEdit;

                selectedTab.Controls.Add(forecastDataGridView);

                bool readOnlyMode = UserSession.GetInstance().User != null && !UserSession.GetInstance().User.accessForecast;
                if (readOnlyMode)
                {
                    forecastDataGridView.ReadOnly = true;

                    string message = $"{selectedSubTabName} data is read only.";
                    log.LogInfo(message);
                    statusLabel.Text = message;
                }
                else
                {   //set up preventing to edit cells where abse_flag = 1 or there is not numeric cells
                    forecastDataGridView.CellBeginEdit += OnDataGridView_PreventForecastCellBeginEdit;
                }

                populateForecastGrid(forcastDateTimes, forecastDataGridView);
                adjsutForecastGridToCenter(forecastDataGridView);
            }
            else
            {
                log.LogError("Forecast Module: OperationSetting seted up not correctly.");
            }
        }

        private void populateForecastDates()
        {
            this.forcastDateTimes.Clear();
            List<string> modelKeys = this.selectedTabModel.Keys.ToList();
            foreach (var key in modelKeys)//collect only forecast dates
            {
                SyncLinkedDictionary<object, object> syncLinkedDictionary = this.selectedTabModel.Get(key);
                if (syncLinkedDictionary != null)
                {
                    foreach (var paramKey in syncLinkedDictionary.Keys.ToList())
                    {
                        if (paramKey is DateTime forecastDate)
                        {
                            DateTime operPlanDate = DateUtil.toOperationPlanningDay(forecastDate);
                            if (!this.forcastDateTimes.Contains(operPlanDate))
                            {
                                this.forcastDateTimes.Add(operPlanDate);
                            }
                        }
                    }
                }
            }
            this.forcastDateTimes.Sort();
        }

        private void populateForecastGrid(List<DateTime> forecastDates, DataGridView dataGridView, bool refreshFromOperationPlannig = false)
        {
            if (forecastDates == null || forecastDates.Count == 0 || this.selectedTabModel == null)
            {
                return;
            }

            dataGridView.Rows.Clear();

            List<string> plannigDates = new List<string> { "", "", "" };
            List<string> secondRow = new List<string> { "", "", "FC %" };
            foreach (DateTime forecastDate in forecastDates)
            {
                plannigDates.Add(forecastDate.ToString(OPERATION_DATE_FORMAT));
                secondRow.Add(DateUtil.toForecastDay(forecastDate).ToString(FORECAST_DATE_FORMAT));
            }
            secondRow.Add(Calculation.TOTAL);
            secondRow.Add("COMMENTS");

            dataGridView.Rows.Add(plannigDates.ToArray());
            dataGridView.Rows.Add(secondRow.ToArray());

            List<string> saleCodes = this.selectedTabModel.Keys.ToList();
            List<Tuple<int, int>> diffrenceForecastWithOPCellCoordinates =
                populateBodyForecasting(forecastDates, dataGridView, saleCodes, refreshFromOperationPlannig);

            colorToRedNotMatchedCells(diffrenceForecastWithOPCellCoordinates, dataGridView);
            //colorAllNotCorrectTotals(dataGridView);//disable TOTAL color
        }


        private void colorAllNotCorrectTotals(DataGridView dataGridView)
        {
            int lastColumn = dataGridView.ColumnCount - 2;
            int startRow = findAfterTotalRowIndex(dataGridView, lastColumn);
            if (startRow > 0)
            {
                Tuple<int, float> percentageBaseTuple = findBaseTotalAndPercentage(this.selectedTabModel);
                for (int row = startRow; row < dataGridView.Rows.Count; ++row)
                {
                    colorCellIfWrongTotal(dataGridView, row, percentageBaseTuple, false);
                }
            }
            else
            {
                log.LogError($"colorAllNotCorrectTotals - Can not find {Calculation.TOTAL} column");
            }
        }

        private static int findAfterTotalRowIndex(DataGridView dataGridView, int lastColumn)
        {
            int startRow = 0;
            for (int row = 0; row < dataGridView.Rows.Count && lastColumn > 0; ++row)
            {
                if (dataGridView.Rows[row].Cells[lastColumn].Value is string value && Calculation.TOTAL == value)
                {
                    startRow = row + 1;
                    break;
                }
            }

            return startRow;
        }

        private List<Tuple<int, int>> populateBodyForecasting(List<DateTime> forecastDates, DataGridView dataGridView, List<string> saleCodes, bool refreshFromOperationPlannig = false)
        {
            int rowIndex = dataGridView.Rows.Count;//headers (2) rows have already added, so start from 3

            List<Tuple<int, int>> higlightRowColumnList = new List<Tuple<int, int>>();
            SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operationModel = operationService.retrieveOperationsByModel(this.selectedSubTab);

            populateForecastingWhileRefreshing(refreshFromOperationPlannig, operationModel);

            foreach (var saleCode in saleCodes)
            {
                SyncLinkedDictionary<object, object> saleParams = this.selectedTabModel.Get(saleCode);
                if (saleParams != null)
                {
                    List<object> row = new List<object>(forecastDates.Count + 4);

                    object objectflag = saleParams.Get("SC_BaseFlag");
                    object percentageObject = saleParams.Get("SC_FCPercent");
                    int currentPercentage = percentageObject != null ? (int)percentageObject : 0;

                    row.Add(saleCode);
                    row.Add(saleParams.Get("SC_ItemName"));
                    row.Add(currentPercentage);
                    int columnIndex = row.Count;//starting after SC_FCPercent column (column with +1 because it won't add yet)

                    foreach (DateTime operationDate in forecastDates)
                    {
                        DateTime forecastDate = DateUtil.toForecastDay(operationDate);
                        object count = (int)saleParams.GetOrDefault(forecastDate, 0);

                        bool baseFlag = objectflag is bool ? (bool)objectflag : false;

                        SyncLinkedDictionary<object, object> operationParams = operationModel.Get(saleCode);

                        if (!refreshFromOperationPlannig && baseFlag)//compare base Opeeration Planning value with Forecast value- add higlight cell coordinates
                        {

                            if (operationParams != null)
                            {
                                object operationCount = operationParams.GetOrDefault(operationDate, 0);
                                if (!count.Equals(operationCount))
                                {
                                    log.LogInfo($"{selectedTab} - {selectedSubTab}. OperPlanning date: {operationDate} Base sale code: {saleCode} with Forecast [row: {rowIndex}, coulumn: {columnIndex}] value: {count} <> Operation planning {operationCount}");
                                    higlightRowColumnList.Add(Tuple.Create(rowIndex, columnIndex));
                                }
                            }
                            row.Add(count != null ? count : 0);//added cell position and add forecast count anyway
                        }
                        else
                        {
                            row.Add(count != null ? count : 0);
                        }
                        ++columnIndex;
                    }
                    row.Add(saleParams.Get(Calculation.TOTAL));
                    row.Add(saleParams.GetOrDefault("SC_Comments", string.Empty));


                    dataGridView.Rows.Add(row.ToArray());

                    ++rowIndex;
                }
            }
            operationModel = null;

            return higlightRowColumnList;
        }

        private void populateForecastingWhileRefreshing(bool refreshFromOperationPlannig, SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operationModel)
        {
            if (refreshFromOperationPlannig)
            {
                this.isModelUpdated = Calculation.populateForecasting(operationModel, this.selectedTabModel);
            }
        }

        private object refreshBase0Value(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operationModel, int currentPercentage, DateTime operationDate, bool baseFlag, object operationCount)
        {
            if (!baseFlag)//if base flag == false do round and update cells
            {
                Tuple<int, float> percentageBaseTuple = findBaseValueAndPercentage(operationModel, operationDate);

                int base100Percentage = percentageBaseTuple.Item1;
                float base100Total = percentageBaseTuple.Item2;

                double refreshedNotBasevalue = 0.0;
                if (base100Percentage == 0)
                {
                    log.LogWarning($"Attemp to devide be zero. Model {selectedTab} - selected sub tab {selectedSubTab}. Base code percentage < 100, that's why base100Percentage is 0.");
                }
                else
                {
                    refreshedNotBasevalue = base100Total * ((float)currentPercentage / (float)base100Percentage);
                }
                //if (refreshedNotBasevalue > 0.0 && refreshedNotBasevalue < 1.0)//looks like when value from percentage > 0 and < 1 - it expected total should be rounded to 1
                //{
                //    operationCount = 1;
                //}
                //else
                //{
                    operationCount = (int)Math.Round(refreshedNotBasevalue, MidpointRounding.AwayFromZero);
                //}
            }

            return operationCount;
        }

        private void addTotalToRefreshedRow(string saleCode, SyncLinkedDictionary<object, object> saleParams)
        {
            object prevTotal = saleParams.Get(Calculation.TOTAL);
            int newTotal = Calculation.getSumBySalesCode(saleCode, this.selectedTabModel);
            saleParams.Update(Calculation.TOTAL, newTotal, prevTotal);
        }

        private void colorToRedNotMatchedCells(List<Tuple<int, int>> diffrenceForecastWithOPCellCoordinates, DataGridView dataGridView)
        {
            foreach (Tuple<int, int> coordinate in diffrenceForecastWithOPCellCoordinates)
            {
                int rowIndex = coordinate.Item1;
                int columnIndex = coordinate.Item2;
                DataGridViewCell cellToColor = dataGridView.Rows[rowIndex].Cells[columnIndex];
                cellToColor.Style.ForeColor = Color.Red;
            }
        }
        private void OnDataGridView_PreventForecastCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is DataGridView dataGrid)
            {

                if ((e.ColumnIndex >= 0 && e.ColumnIndex <= 2) || e.RowIndex == 0 || e.RowIndex == 1 
                    || e.ColumnIndex == dataGrid.ColumnCount - 1 || (dataGrid.ColumnCount > 1 && e.ColumnIndex == dataGrid.ColumnCount - 2)) //disable - 1..3 columns, 1-2 Rows, column (TOTAL), last column: COMMENTS
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
                    clearAfterCellEdited(updateInfo.RowIndex, dataGridView);
                    // Update the cell value
                    dataGridView.Rows[updateInfo.RowIndex].Cells[updateInfo.ColumnIndex].Value = updateInfo.NewValue;

                    //recalculate the total
                    updateForecastTotalCell(updateInfo.ColumnIndex, updateInfo.RowIndex, dataGridView, (string)updateInfo.NewValue);


                    // Clear the tag
                    dataGridView.Tag = null;
                    isModelUpdated = true;
                }
            }
        }

        private void clearAfterCellEdited(int rowIndex, DataGridView dataGridView)
        {
            dataGridView.Rows[rowIndex].Cells[dataGridView.ColumnCount - 2].Style.ForeColor = Color.Black;
            this.statusLabel.Text = string.Empty;
            this.statusLabel.ForeColor = STATUS_LABEL_COLOR;
        }

        private void colorCellIfWrongTotal(DataGridView dataGridView, int rowIndex, Tuple<int, float> baseTuple, bool showInStatus = true)
        {
            if (this.selectedTabModel == null)
            {
                log.LogWarning("Method ->  colorCellIfWrongTotal - Got Illegal model state is null. In Forecast module when try to color the TOTAL");
                return;
            }

            int base100Percentage = baseTuple.Item1;
            float base100Total = baseTuple.Item2;

            if (base100Percentage > 0)//TODO Does forecasting percentage potentialy can be greater that 100%?
            {
                string editedSaleCodeRow = (string)dataGridView[0, rowIndex].Value;
                SyncLinkedDictionary<object, object> saleParams = this.selectedTabModel.Get(editedSaleCodeRow);
                if (saleParams != null)
                {
                    object selectedTempPercentage = saleParams.Get("SC_FCPercent");
                    object editedBaseFlag = saleParams.Get(ForecastRepositoryImpl.BASE_FLAG);
                    if (selectedTempPercentage != null && selectedTempPercentage is int selectedPercentage
                        && editedBaseFlag != null && editedBaseFlag is bool editFlag && !editFlag)
                    {
                        int expectedTotal = 0;
                        double value = base100Total * ((float)selectedPercentage / (float)base100Percentage);
                        //if (value > 0.0 && value < 1.0)//looks like when value from percentage > 0 and < 1 - it expected total should be rounded to 1
                        //{
                        //    expectedTotal = 1;
                        //}
                        //else
                        //{
                            expectedTotal = (int)Math.Round(value, MidpointRounding.AwayFromZero);
                        //}
                        object actualTempTotal = saleParams.Get(Calculation.TOTAL);
                        if (actualTempTotal != null && actualTempTotal is int actualTotalValue)
                        {
                            if (actualTotalValue != expectedTotal && actualTotalValue > 0)
                            {
                                float actualPercent = 0;
                                if (expectedTotal > 0)
                                {
                                    //actualPercent = ((float)actualTotalValue / (float)expectedTotal) * base100Percentage;
                                    actualPercent = ((float)actualTotalValue / (float)expectedTotal) * selectedPercentage;
                                }
                                string warnMessage = $"Warning: Row {rowIndex + 1} - Actual TOTAL: {actualTotalValue} ({actualPercent.ToString("0.00")}%) <> Expected TOTAL: {expectedTotal} ({selectedPercentage}%)";
                                dataGridView.Rows[rowIndex].Cells[dataGridView.ColumnCount - 2].Style.ForeColor = Color.Red;
                                if (showInStatus)
                                {
                                    this.statusLabel.Text = warnMessage;
                                    this.statusLabel.ForeColor = Color.Red;
                                }
                                log.LogInfo(warnMessage);
                            }
                        }
                    }
                }
            }
        }

        private Tuple<int, float> findBaseValueAndPercentage(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> model, DateTime operationsDate)
        {
            List<string> saleCodes = model.Keys.ToList();

            int base100Total = 0;
            int base100Percentage = 0;
            float base100Value = 0;
            foreach (var code in saleCodes)
            {
                SyncLinkedDictionary<object, object> saleParams = model.Get(code);
                if (saleParams != null)
                {
                    object baseFlag = saleParams.Get(ForecastRepositoryImpl.BASE_FLAG);

                    if (baseFlag != null && baseFlag is bool flag && flag)
                    {
                        object objectValue = saleParams.Get(operationsDate);
                        object baseTempPercentage = saleParams.Get("SC_FCPercent");
                        object objectTotal = saleParams.Get(Calculation.TOTAL);

                        object scModel = saleParams.Get(OperationsPlanningRepositoryImpl.SC_MODEL);

                        if (baseTempPercentage != null && baseTempPercentage is int basePercentage && basePercentage >= 100
                           && objectValue != null && objectValue is int baseCount
                           && objectTotal != null && objectTotal is int baseTotal
                           && scModel != null && scModel.Equals(this.selectedSubTab))
                        {

                            if (/*((float)baseCount) > base100Value && */baseTotal > base100Total)
                            {
                                base100Percentage = basePercentage;
                                base100Value = (float)baseCount;

                                base100Total = baseTotal;
                            }
                        }
                    }
                }
            }
            checkForZeroBasePercetage(base100Percentage);
            return Tuple.Create(base100Percentage, base100Value);
        }

        private Tuple<int, float> findBaseTotalAndPercentage(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> model)
        {
            List<string> saleCodes = model.Keys.ToList();

            int base100Percentage = 0;
            float base100Total = 0;
            foreach (var code in saleCodes)
            {
                SyncLinkedDictionary<object, object> saleParams = model.Get(code);
                if (saleParams != null)
                {
                    object baseFlag = saleParams.Get(ForecastRepositoryImpl.BASE_FLAG);
                    if (baseFlag != null && baseFlag is bool flag && flag)
                    {
                        object baseTempPercentage = saleParams.Get("SC_FCPercent");
                        object baseTempTotal = saleParams.Get(Calculation.TOTAL);
                        if (baseTempPercentage != null && baseTempPercentage is int basePercentage && basePercentage >= 100
                            && baseTempTotal != null && baseTempTotal is int baseTotal)
                        {
                            if (((float)baseTotal) > base100Total)
                            {
                                base100Percentage = basePercentage;
                                base100Total = (float)baseTotal;
                            }
                        }
                    }
                }
            }
            checkForZeroBasePercetage(base100Percentage);
            return Tuple.Create(base100Percentage, base100Total);
        }

        private void checkForZeroBasePercetage(int base100Percentage)
        {
            if (base100Percentage == 0)
            {
                string warning = $"Base sales code with model - {selectedSubTab}, does not have any FC % = 100. [Selected tab {selectedTab}]";
                this.statusLabel.Text = warning;
                log.LogWarning(warning);
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

                object previousValue = saleCodeContent.Get(Calculation.TOTAL);
                saleCodeContent.Update(Calculation.TOTAL, total, previousValue);//update the model

                dataGridView[dataGridView.ColumnCount - 2, rowIndex].Value = total;

                Tuple<int, float> percentageBaseTuple = findBaseTotalAndPercentage(this.selectedTabModel);
                //colorCellIfWrongTotal(dataGridView, rowIndex, percentageBaseTuple);//Disable worng TOTAL
            }
        }
        private void OnDataGridView_ForecastValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                string newValue = e.FormattedValue?.ToString() ?? "";

                string cleanValue = Validator.RemoveNonNumericCharacters(newValue);

                bool dataRow = !(dataGridView.ColumnCount - 1 == e.ColumnIndex || (e.RowIndex >= 0 && e.RowIndex <= 1) /*|| e.RowIndex == 0 || e.RowIndex == 1*/);
                if (cleanValue != newValue)
                {
                    if (string.IsNullOrEmpty(cleanValue))
                    {
                        cleanValue = "0";
                    }
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = cleanValue };
                }
                else if (dataRow && cleanValue != previousEditedValue)
                {
                    if (string.IsNullOrEmpty(newValue))
                    {
                        newValue = "0";
                    }
                    dataGridView.Tag = new CellUpdateInfo { RowIndex = e.RowIndex, ColumnIndex = e.ColumnIndex, NewValue = newValue };
                }
                else
                {
                    clearStatusLabel();
                }
                previousEditedValue = null;
            }
        }

        private Panel createForecastButtonsPanel()
        {
            Panel operationButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
            };


            if (UserSession.GetInstance().User.accessForecast)
            {
                Button refreshForecast = null;
                Button saveOperations = null;
                refreshForecast = new Button
                {
                    Text = "Refresh",
                    Anchor = AnchorStyles.None,
                    Size = new Size(100, 40)
                };

                refreshForecast.Click += OnForecastRefreshButtons_Click;
                operationButtonsPanel.Controls.Add(refreshForecast);

                saveOperations = new Button
                {
                    Text = "Save",
                    Anchor = AnchorStyles.None,
                    Size = new Size(100, 40)
                };
                saveOperations.Click += OnSaveForecastButtons_ClickAsync;
                operationButtonsPanel.Controls.Add(saveOperations);

                // Center the buttons horizontally in the panel and reduce the spacing
                operationButtonsPanel.Paint += (sender, e) =>//Can be refactore to privaaet two buttons for both view
                {
                    if (refreshForecast != null && saveOperations != null)
                    {
                        int panelWidth = operationButtonsPanel.ClientSize.Width;
                        int buttonSpacing = 20; // Smaller spacing for closer buttons

                        int totalWidth = refreshForecast.Width + saveOperations.Width + buttonSpacing;
                        int startX = (panelWidth - totalWidth) / 2;

                        refreshForecast.Location = new Point(startX, (operationButtonsPanel.Height - refreshForecast.Height) / 2);
                        saveOperations.Location = new Point(startX + refreshForecast.Width + buttonSpacing, (operationButtonsPanel.Height - saveOperations.Height) / 2);
                    }
                };

            }


            return operationButtonsPanel;
        }

        private async void OnSaveForecastButtons_ClickAsync(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    this.log.LogInfo($"Saving Foforecast data from {selectedTab} -> {selectedSubTab}");
                    int insertedRows = forecastService.save(selectedTabModel);
                    Invoke((Action)(() =>
                    {
                        clearStatusLabel();
                        this.statusLabel.Text = $"Foforecast data from {selectedTab} -> {selectedSubTab} has been saved successfully.";
                        this.isModelUpdated = false;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        this.log.LogError($"Error saving Foforecast data: {selectedTab} -> {selectedSubTab}: " + ex.Message);
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.statusLabel.Text = $"Error saving Foforecast data: {selectedTab} -> {selectedSubTab}.";
                        this.statusLabel.ForeColor = Color.Red;
                        return;
                    }));
                }
            });
        }

        private async void OnForecastRefreshButtons_Click(object sender, EventArgs e)
        {
            if(!refreshClickValidation())
            {
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    this.log.LogInfo($"Start refreshing process: {selectedTab} -> View {selectedSubTab}");
                    Invoke((Action)(() =>
                    {
                        clearStatusLabel();

                        this.selectedTabModel = forecastService.retrieveForecastData(this.selectedSubTab);//refresh model

                        populateForecastGrid(forcastDateTimes, forecastDataGridView, true);

                        printRefreshStatus();
                        this.log.LogInfo($"Refreshing process has been end: {selectedTab} -> View {selectedSubTab}");
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        this.log.LogError($"Error Refreshing Forecast data: {selectedTab} -> {selectedSubTab}: " + ex.Message);
                        MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.statusLabel.Text = $"Error Refreshing Forecast data: {selectedTab} -> {selectedSubTab}.";
                        this.statusLabel.ForeColor = Color.Red;
                        return;
                    }));
                }
            });
        }

        private bool refreshClickValidation()
        {
            bool canNotProcess = !(forcastDateTimes.Count > 0 || forecastDataGridView != null);
            if (canNotProcess)
            {
                log.LogError($"OnForecastRefreshButtons_Click -> forcastDateTimes {forcastDateTimes.Count} OR forecastDataGridView is null.");
                return false;
            }
            return true;
        }

        private void printRefreshStatus()
        {
            if (this.isModelUpdated)
            {
                this.statusLabel.Text = "Data has been refreshed. Hit the 'Save' button for persist the changes.";
            }
            else if (!validateRefreshData(this.selectedTabModel))
            {
                this.statusLabel.Text = "No Data to refresh.";
            } else
            {
                this.statusLabel.Text = "No changes after refreshing.";
            }
        }

        private static bool validateRefreshData(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> model)
        {
            List<string> keyList = model.Keys.ToList();

            foreach (string key in keyList)
            {
                SyncLinkedDictionary<object, object> saleParams = model.Get(key);
                if (saleParams != null)
                {
                    foreach (Object paramKey in saleParams.Keys.ToList())
                    {
                        if (paramKey is DateTime)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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
