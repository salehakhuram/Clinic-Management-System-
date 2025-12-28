using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class MedicinesForm : Form
    {
        private readonly string connectionString =
            @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True;";

        public MedicinesForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadMedicinesGrid();
            this.Resize += (s, e) => LayoutContent();
            WireEvents();
        }

        private void WireEvents()
        {
            btnSave.Click += BtnSave_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnNew.Click += (s, e) => ClearForm();
            dgvMedicines.CellClick += DgvMedicines_CellClick;
            if (txtSearch != null) txtSearch.TextChanged += TxtSearch_TextChanged;
        }

        private void SetupGrid()
        {
            if (dgvMedicines == null) return;

            dgvMedicines.AutoGenerateColumns = true;
            dgvMedicines.Columns.Clear();
        }

        private void LoadMedicinesGrid()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    string query = "SELECT MedIntId, MedCode, TradeName, GenericName, Category, UnitPrice, Quantity, ExpiryDate, Status, Manufacturer, Source, Supplier, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt FROM Medicines";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvMedicines.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading medicines: " + ex.Message);
                }
            }
        }

        private void DgvMedicines_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvMedicines.Rows[e.RowIndex];
            txtMedIntId.Tag = row.Cells["MedIntId"].Value;
            txtMedIntId.Text = row.Cells["MedCode"].Value?.ToString();
            txtTradeName.Text = row.Cells["TradeName"].Value?.ToString();
            txtGenericName.Text = row.Cells["GenericName"].Value?.ToString();
            cmbCategory.Text = row.Cells["Category"].Value?.ToString();
            txtUnitPrice.Text = row.Cells["UnitPrice"].Value?.ToString();
            txtQuantity.Text = row.Cells["Quantity"].Value?.ToString();
            cmbStatus.Text = row.Cells["Status"].Value?.ToString();
            
            if (row.Cells["ExpiryDate"].Value != DBNull.Value) dtpExpiry.Value = Convert.ToDateTime(row.Cells["ExpiryDate"].Value);
            txtManufacturer.Text = row.Cells["Manufacturer"].Value?.ToString();
            txtSource.Text = row.Cells["Source"].Value?.ToString();
            txtSupplier.Text = row.Cells["Supplier"].Value?.ToString();
            
            // Audit fields
            txtCreatedBy.Text = row.Cells["CreatedBy"].Value?.ToString();
            if (row.Cells["CreatedAt"].Value != DBNull.Value) dtpCreatedAt.Value = Convert.ToDateTime(row.Cells["CreatedAt"].Value);
            
            // Update "Updated By" fields for display (current user doing the edit)
            txtUpdatedBy.Text = Session.CurrentUser;
            dtpUpdatedAt.Value = DateTime.Now;
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (dgvMedicines.DataSource == null) return;
            string filter = txtSearch.Text.Trim().Replace("'", "''");
            (dgvMedicines.DataSource as DataTable).DefaultView.RowFilter = 
                string.Format("TradeName LIKE '%{0}%' OR GenericName LIKE '%{0}%' OR Category LIKE '%{0}%' OR Manufacturer LIKE '%{0}%' OR Source LIKE '%{0}%' OR Supplier LIKE '%{0}%' OR MedCode LIKE '%{0}%'", filter);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTradeName.Text)) return;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"INSERT INTO Medicines 
                 (TradeName, GenericName, Category, UnitPrice, Quantity, StockQuantity, ExpiryDate, Status, Manufacturer, Source, Supplier, CreatedBy, CreatedAt) 
                 VALUES (@TradeName, @GenericName, @Category, @UnitPrice, @Quantity, @StockQuantity, @ExpiryDate, @Status, @Manufacturer, @Source, @Supplier, @CreatedBy, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@TradeName", txtTradeName.Text);
                cmd.Parameters.AddWithValue("@GenericName", txtGenericName.Text);
                cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                cmd.Parameters.AddWithValue("@UnitPrice", decimal.TryParse(txtUnitPrice.Text, out decimal price) ? price : 0);
                int quantity = int.TryParse(txtQuantity.Text, out int qty) ? qty : 0;
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@StockQuantity", quantity); // Same as Quantity initially
                cmd.Parameters.AddWithValue("@ExpiryDate", dtpExpiry.Value);
                cmd.Parameters.AddWithValue("@Status", cmbStatus.Text);
                cmd.Parameters.AddWithValue("@Manufacturer", txtManufacturer.Text);
                cmd.Parameters.AddWithValue("@Source", txtSource.Text);
                cmd.Parameters.AddWithValue("@Supplier", txtSupplier.Text);
                cmd.Parameters.AddWithValue("@CreatedBy", Session.CurrentUser);

                try {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Medicine saved successfully.");
                    LoadMedicinesGrid();
                    ClearForm();
                } catch (Exception ex) { MessageBox.Show("Save error: " + ex.Message); }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (txtMedIntId.Tag == null) return;

            // Update UI feedback
            txtUpdatedBy.Text = Session.CurrentUser;
            dtpUpdatedAt.Value = DateTime.Now;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"UPDATE Medicines SET 
                 TradeName=@TradeName, GenericName=@GenericName, Category=@Category, 
                 UnitPrice=@UnitPrice, Quantity=@Quantity, ExpiryDate=@ExpiryDate, 
                 Status=@Status, Manufacturer=@Manufacturer, Source=@Source, Supplier=@Supplier,
                 UpdatedBy=@UpdatedBy, UpdatedAt=GETDATE()
                 WHERE MedIntId=@MedIntId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MedIntId", txtMedIntId.Tag);
                cmd.Parameters.AddWithValue("@TradeName", txtTradeName.Text);
                cmd.Parameters.AddWithValue("@GenericName", txtGenericName.Text);
                cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
                cmd.Parameters.AddWithValue("@UnitPrice", decimal.TryParse(txtUnitPrice.Text, out decimal price) ? price : 0);
                cmd.Parameters.AddWithValue("@Quantity", int.TryParse(txtQuantity.Text, out int qty) ? qty : 0);
                cmd.Parameters.AddWithValue("@ExpiryDate", dtpExpiry.Value);
                cmd.Parameters.AddWithValue("@Status", cmbStatus.Text);
                cmd.Parameters.AddWithValue("@Manufacturer", txtManufacturer.Text);
                cmd.Parameters.AddWithValue("@Source", txtSource.Text);
                cmd.Parameters.AddWithValue("@Supplier", txtSupplier.Text);
                cmd.Parameters.AddWithValue("@UpdatedBy", Session.CurrentUser);

                try {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Medicine updated successfully.");
                    LoadMedicinesGrid();
                    ClearForm();
                } catch (Exception ex) { MessageBox.Show("Update error: " + ex.Message); }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (txtMedIntId.Tag == null) return;

            if (MessageBox.Show("Delete this record?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Medicines WHERE MedIntId=@MedIntId";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@MedIntId", txtMedIntId.Tag);
                    try {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Medicine deleted.");
                        LoadMedicinesGrid();
                        ClearForm();
                    } catch (Exception ex) { MessageBox.Show("Delete error: " + ex.Message); }
                }
            }
        }

        private void ClearForm()
        {
            txtMedIntId.Tag = null;
            txtMedIntId.Text = FetchNextMedicineCode();
            txtTradeName.Clear();
            txtGenericName.Clear();
            txtUnitPrice.Clear();
            txtQuantity.Clear();
            cmbCategory.SelectedIndex = -1;
            cmbStatus.SelectedIndex = -1;
            
            // Auto-fill CreatedBy
            if (txtCreatedBy != null) txtCreatedBy.Text = Session.CurrentUser;
            if (txtUpdatedBy != null) txtUpdatedBy.Clear();
            dtpCreatedAt.Value = DateTime.Now;
            dtpUpdatedAt.Value = DateTime.Now;
        }

        private string FetchNextMedicineCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ISNULL(IDENT_CURRENT('Medicines'), 0) + ISNULL(IDENT_INCR('Medicines'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        return "M" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "M---";
        }
    }
}
