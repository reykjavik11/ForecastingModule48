using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule.OtherForm
{
    partial class SettingsView
    {
        private String table;

        // UI elements
        private DataGridView dataGridView;
        private Button insertButton;
        private Button deleteButton;
        private Button saveButton;
        private Button updateButton;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private const int BUTTON_HEIGHT = 40;

        private Tuple<String, List<Tuple<String, String>>>  model;
        /// Handlers <summary>
        /// Handlers//define delegate for event
        /// </summary>
        /// <param name="disposing"></param>
        public delegate object LoadHandler();
        // Define the event
        public event LoadHandler OnLoad;

        public delegate void InsertHandler();
        public event InsertHandler OnInsert; 

        public delegate void DeleteHandler(int rowIndex);
        public event DeleteHandler OnDelete;  

        public delegate void SaveHandler();
        public event SaveHandler OnSave;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        /// 
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Icon = SystemInfo.GetAppIcon();

            // Initialize UI elements
            this.dataGridView = new DataGridView();
            this.insertButton = new Button();
            this.deleteButton = new Button();
            this.saveButton = new Button();
            this.updateButton = new Button();

            // Set up DataGridView
            this.dataGridView.Dock = DockStyle.Top;
            this.dataGridView.Height = 600;
            this.dataGridView.AllowUserToAddRows = false;
            // Set up Insert Button
            this.insertButton.Text = "Insert";
            this.insertButton.Dock = DockStyle.Top;
            this.insertButton.Height = BUTTON_HEIGHT;
            this.insertButton.Width = 100;
            this.insertButton.Click += insertButton_Click;

            // Set up Delete Button
            this.deleteButton.Text = "Delete";
            this.deleteButton.Dock = DockStyle.Top;
            this.deleteButton.Height = BUTTON_HEIGHT;
            this.deleteButton.Click += deleteButton_Click;   
            
            // Set up Update Button
            this.updateButton.Text = "Update";
            this.updateButton.Dock = DockStyle.Top;
            this.updateButton.Height = BUTTON_HEIGHT;
            this.updateButton.Click += updateButton_Click;

            // Set up Save Button
            this.saveButton.Text = "Save";
            this.saveButton.Dock = DockStyle.Top;
            this.saveButton.Height = BUTTON_HEIGHT;
            this.saveButton.Click += saveButton_Click;

            // Add controls to the form
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.insertButton);
            this.Controls.Add(this.dataGridView);

            // Set form properties
            this.Text = $"{model.Item1}";
            this.Size = new System.Drawing.Size(1200, 800);
            this.Load += MainForm_Load;
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = dataGridView.Rows[19];
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DataTable dataTable = (DataTable)OnLoad.Invoke();

            dataGridView.DataSource = dataTable;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.Name.Contains("Active"))
                {
                    column.CellTemplate = new DataGridViewCheckBoxCell();
                    column.HeaderText = column.Name; //"Active";
                } else if(column.Name.Contains("RecordID"))
                {
                    column.Visible = false;
                }
            }
        }

        // Insert Button: Add a new row to the DataGridView
        private void insertButton_Click(object sender, EventArgs e)
        {
            OnInsert.Invoke();

            dataGridView.CurrentCell = null;//deselect focus                
            dataGridView.Rows[dataGridView.Rows.Count - 1].Selected = true;//select last row 
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;//scroll to last row
        }

        // Save Button: Save changes to the database (insert, update, delete)
        private void saveButton_Click(object sender, EventArgs e)
        {
            dataGridView.EndEdit();

            OnSave.Invoke();

            string message = $"Updated data has been saved into '{model.Item1}' table.";
            Logger.Instance.LogInfo(message);
            MessageBox.Show(message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Delete Button: Delete the selected row from the DataGridView
        private void deleteButton_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dataGridView.SelectedRows.Count > 0)
            {
                // Get the selected row
                DataGridViewRow row = dataGridView.SelectedRows[0];

                OnDelete.Invoke(row.Index);
            }
        }

        public void setModel(object model)
        {
            if (model is Tuple<String, List<Tuple<String, String>>> tuple)
            {
                this.model = tuple;
            }
            else
            {
                throw new ArgumentException("SettingsView -> setModel illlegal model type.");
            }
        }

        #endregion
    }
}