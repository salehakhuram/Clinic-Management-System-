using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class PharmacistInventoryForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        public PharmacistInventoryForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadData();
            btnPrint.Click += (s, e) => PrintInventory();
        }

        private void PrintInventory()
        {
            try
            {
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                pd.DefaultPageSettings.Landscape = true;
                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    Font fontTitle = new Font("Arial", 18, FontStyle.Bold);
                    Font fontHeader = new Font("Arial", 10, FontStyle.Bold);
                    Font fontBody = new Font("Arial", 9);
                    
                    int y = 40;
                    int x = 40;
                    int rowHeight = 30;

                    // Header
                    g.DrawString("AL REHMAN CLINIC", fontTitle, Brushes.Navy, new RectangleF(0, y, ev.PageBounds.Width, 40), new StringFormat { Alignment = StringAlignment.Center });
                    y += 40;
                    g.DrawString("Near Civil Hospital, Shujabad, Multan | Tel: +92-300-1234567", fontBody, Brushes.DimGray, new RectangleF(0, y, ev.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
                    y += 35;
                    g.DrawLine(new Pen(Color.Navy, 2), x, y, ev.PageBounds.Width - x, y);
                    y += 20;

                    g.DrawString("PHARMACY INVENTORY REPORT", fontHeader, Brushes.Black, x, y);
                    g.DrawString($"Date: {DateTime.Now}", fontBody, Brushes.Black, ev.PageBounds.Width - x - 200, y);
                    y += 40;

                    // Columns
                    int[] colWidths = { 60, 200, 150, 120, 80, 80, 100 };
                    string[] headers = { "ID", "Trade Name", "Generic Name", "Category", "Price", "Stock", "Status" };
                    
                    int currentX = x;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        g.FillRectangle(Brushes.LightGray, currentX, y, colWidths[i], rowHeight);
                        g.DrawRectangle(Pens.Black, currentX, y, colWidths[i], rowHeight);
                        g.DrawString(headers[i], fontHeader, Brushes.Black, currentX + 5, y + 8);
                        currentX += colWidths[i];
                    }
                    y += rowHeight;

                    // Rows
                    foreach (DataGridViewRow row in dgvInventory.Rows)
                    {
                        if (row.IsNewRow) continue;
                        
                        currentX = x;
                        string[] values = {
                            row.Cells["MedIntId"].Value?.ToString(),
                            row.Cells["TradeName"].Value?.ToString(),
                            row.Cells["GenericName"].Value?.ToString(),
                            row.Cells["Category"].Value?.ToString(),
                            row.Cells["UnitPrice"].Value?.ToString(),
                            row.Cells["Quantity"].Value?.ToString(),
                            row.Cells["Status"].Value?.ToString()
                        };

                        for (int i = 0; i < values.Length; i++)
                        {
                            g.DrawRectangle(Pens.Black, currentX, y, colWidths[i], rowHeight);
                            g.DrawString(values[i], fontBody, Brushes.Black, currentX + 5, y + 8);
                            currentX += colWidths[i];
                        }
                        y += rowHeight;

                        if (y > ev.MarginBounds.Bottom) // Simple pagination check (just stops for now)
                            break;
                    }
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Print error: " + ex.Message); }
        }
        private void SetupGrid()
        {
            if (dgvInventory == null) return;

            dgvInventory.AutoGenerateColumns = false;
            dgvInventory.Columns.Clear();

            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "MedIntId", DataPropertyName = "MedIntId", HeaderText = "ID", Width = 80 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "TradeName", DataPropertyName = "TradeName", HeaderText = "Trade Name", Width = 180 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "GenericName", DataPropertyName = "GenericName", HeaderText = "Generic Name", Width = 150 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", DataPropertyName = "Category", HeaderText = "Category", Width = 120 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", DataPropertyName = "UnitPrice", HeaderText = "Price", Width = 100 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Stock", Width = 80 });
            dgvInventory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "Status", Width = 100 });
        }

        private void LoadData()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string q = "SELECT MedIntId, TradeName, GenericName, Category, UnitPrice, Quantity, Status FROM Medicines";
                    SqlDataAdapter da = new SqlDataAdapter(q, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvInventory.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading inventory: " + ex.Message); }
        }
    }
}

