using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class SelectMedicineDialog : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";
        
        public int SelectedId { get; private set; }
        public string SelectedMedicineName { get; private set; }
        public string SelectedCategory { get; private set; }
        public double SelectedPrice { get; private set; }
        public int SelectedQuantity { get; private set; }

        private TextBox txtSearch;
        private DataGridView dgvMedicines;
        private NumericUpDown numQuantity;
        private Button btnAdd, btnCancel;

        public SelectMedicineDialog()
        {
            InitializeComponentManual();
            LoadMedicines();
        }

        private void InitializeComponentManual()
        {
            this.Text = "Select Medicine";
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblSearch = new Label { Text = "Search Medicine:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            txtSearch = new TextBox { Location = new Point(20, 45), Size = new Size(440, 25), Font = new Font("Segoe UI", 10) };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            dgvMedicines = new DataGridView
            {
                Location = new Point(20, 80),
                Size = new Size(440, 280),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            SetupGrid();

            Label lblQty = new Label { Text = "Quantity:", Location = new Point(20, 380), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            numQuantity = new NumericUpDown { Location = new Point(110, 378), Size = new Size(80, 25), Minimum = 1, Maximum = 1000, Value = 1, Font = new Font("Segoe UI", 10) };

            btnAdd = new Button { Text = "Add Item", Location = new Point(260, 410), Size = new Size(100, 35), BackColor = Color.FromArgb(45, 137, 239), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnAdd.Click += BtnAdd_Click;

            btnCancel = new Button { Text = "Cancel", Location = new Point(370, 410), Size = new Size(90, 35), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblSearch, txtSearch, dgvMedicines, lblQty, numQuantity, btnAdd, btnCancel });
        }

        private void LoadMedicines()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT MedIntId, TradeName, GenericName, Category, UnitPrice, StockQuantity FROM Medicines WHERE Status = 'Available'";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvMedicines.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading medicines: " + ex.Message);
            }
        }

        private void SetupGrid()
        {
            dgvMedicines.AutoGenerateColumns = false;
            dgvMedicines.Columns.Clear();
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedIntId", DataPropertyName = "MedIntId", HeaderText = "ID", Width = 50 });
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "TradeName", DataPropertyName = "TradeName", HeaderText = "Medicine", Width = 150 });
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", DataPropertyName = "Category", HeaderText = "Category", Width = 100 });
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", DataPropertyName = "UnitPrice", HeaderText = "Price", Width = 80 });
            dgvMedicines.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockQuantity", DataPropertyName = "StockQuantity", HeaderText = "Stock", Width = 80 });
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (dgvMedicines.DataSource is DataTable dt)
            {
                dt.DefaultView.RowFilter = string.Format("TradeName LIKE '%{0}%' OR GenericName LIKE '%{0}%'", txtSearch.Text.Trim().Replace("'", "''"));
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (dgvMedicines.CurrentRow != null)
            {
                int stock = Convert.ToInt32(dgvMedicines.CurrentRow.Cells["StockQuantity"].Value);
                int requested = (int)numQuantity.Value;

                if (requested > stock)
                {
                    MessageBox.Show($"Not enough stock! Available: {stock}", "Insufficient Stock", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SelectedId = Convert.ToInt32(dgvMedicines.CurrentRow.Cells["MedIntId"].Value);
                SelectedMedicineName = dgvMedicines.CurrentRow.Cells["TradeName"].Value.ToString();
                SelectedCategory = dgvMedicines.CurrentRow.Cells["Category"].Value.ToString();
                SelectedPrice = Convert.ToDouble(dgvMedicines.CurrentRow.Cells["UnitPrice"].Value);
                SelectedQuantity = requested;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a medicine.");
            }
        }
    }
}
