using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using ForecastingModule.Helper;

namespace ForecastingModule.OtherForm
{
    partial class SettingsView
    {
        private SqlDataAdapter adapter;
        private DataTable dataTable;

        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private String table = "OperationsSettings";

        // UI elements
        private DataGridView dataGridView;
        private Button insertButton;
        private Button deleteButton;
        private Button saveButton;
        private Button cancelButton;
        private Button updateButton;


        private DataRow newRow; // To track new row
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private const int BUTTON_HEIGHT = 40;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            //this.ClientSize = new System.Drawing.Size(800, 450);
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Text = "Settings";//Model should contain setting's name 

            // Initialize UI elements
            this.dataGridView = new DataGridView();
            this.insertButton = new Button();
            this.deleteButton = new Button();
            this.saveButton = new Button();
            this.cancelButton = new Button();

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

            // Set up Cancel Button
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Dock = DockStyle.Top;
            this.cancelButton.Click += cancelButton_Click;
            this.cancelButton.Visible = false;

            // Add controls to the form
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.insertButton);
            this.Controls.Add(this.dataGridView);

            // Set form properties
            this.Text = $"{this.table}";
            //this.Size = new System.Drawing.Size(800, 600);
            this.Size = new System.Drawing.Size(1200, 800);
            this.Load += MainForm_Load;
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            DataGridViewRow dataGridViewRow = dataGridView.Rows[19];
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Initialize SQL adapter and data table
            string query = $"SELECT * FROM {this.table} order by OPS_DisplayOrder";

            adapter = new SqlDataAdapter(query, connectionString);
            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

            // Fill DataTable
            dataTable = new DataTable();

            adapter.Fill(dataTable);
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
            // Add a new row to the DataTable
            newRow = dataTable.NewRow();//TODO should be get dynamic names and types - !!SQL SELECT COLUMN_NAME, DATA_TYPE  FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OperationsSettings'
            newRow["OPS_RecordID"] = Guid.NewGuid(); // Assuming Id is auto-incremented
            newRow["OPS_Tab"] = ""; // Empty default value for Name
            newRow["OPS_NbrMonths"] = 18; // Default value for IsActive
            newRow["OPS_NbrDays"] = 60; // Default value for IsActive
            newRow["OPS_ActiveFlag"] = 1; // Default value for IsActive
            newRow["OPS_DisplayOrder"] = 0; // Default value for IsActive
            newRow["OPS_Comments"] = ""; // Default value for IsActive

            // Add the new row to the DataTable
            dataTable.Rows.Add(newRow);

            dataGridView.CurrentCell = null;//deselect focus                
            dataGridView.Rows[dataGridView.Rows.Count - 1].Selected = true;//selected last row 
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;//scroll to last row
            // Enable Save button, Cancel button and change Insert button text to Save
            //insertButton.Text = "Save";
            //insertButton.Click -= insertButton_Click;
            //insertButton.Click += saveButton_Click;

            //cancelButton.Visible = true; // Show cancel button (disable in case of buggy (deleted row still exist while insertion))
        }

        // Save Button: Save changes to the database (insert, update, delete)
        private void saveButton_Click(object sender, EventArgs e)
        {
            dataGridView.EndEdit();
            // Create SqlCommand for Insert, Update, Delete
            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

            // Update the database with changes from the DataTable
            adapter.Update(dataTable);

            // Change the button back to Insert and reset click event
            //insertButton.Text = "Insert";
            //insertButton.Click -= saveButton_Click;
            //insertButton.Click += insertButton_Click;

            //cancelButton.Visible = false; // Hide cancel button after save
        }

        // Delete Button: Delete the selected row from the DataGridView
        private void deleteButton_Click(object sender, EventArgs e)
        {
            // Check if a row is selected
            if (dataGridView.SelectedRows.Count > 0)
            {
                // Get the selected row
                DataGridViewRow row = dataGridView.SelectedRows[0];

                // Mark the row for deletion
                dataTable.Rows[row.Index].Delete();
            }
        }

        // Cancel Button: Cancel the insertion of a new row
        private void cancelButton_Click(object sender, EventArgs e)//disable in case of buggy (deleted row still exist while insertion)
        {
            // Remove the newly added row
            if (newRow != null)
            {
                newRow.Delete(); // Remove the new row from the DataTable
            }

            // Reset the button states
            insertButton.Text = "Insert";
            insertButton.Click -= saveButton_Click;
            insertButton.Click += insertButton_Click;

            cancelButton.Visible = false; // Hide cancel button
        }

        #endregion
    }
}