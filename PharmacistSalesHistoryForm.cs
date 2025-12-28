using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class PharmacistSalesHistoryForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        public PharmacistSalesHistoryForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadData();
            btnRefresh.Click += (s, e) => LoadData();
            btnExport.Click += (s, e) => ExportToCSV();
            btnPrint.Click += (s, e) => PrintData();
        }

        private void LoadData()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // We'll show completed prescriptions as 'sales'
                    string q = @"SELECT V.VisitID, P.PatientCode, P.PatientName, V.TokenNumber, V.VisitDate, 'Prescription' as Type, 
                                       ISNULL(NULLIF(A.PaymentStatus, ''), 'Unpaid') as Payment
                                FROM Visits V
                                JOIN Patients P ON V.PatientID = P.PatientID
                                LEFT JOIN Appointments A ON V.AppointmentIntId = A.AppointmentIntId
                                WHERE V.Status = 'COMPLETED'
                                ORDER BY V.VisitDate DESC";
                    SqlDataAdapter da = new SqlDataAdapter(q, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvSales.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading history: " + ex.Message); }
        }

        private void SetupGrid()
        {
            if (dgvSales == null) return;

            dgvSales.AutoGenerateColumns = false;
            dgvSales.Columns.Clear();

            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitID", DataPropertyName = "VisitID", HeaderText = "ID", Width = 60 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientCode", DataPropertyName = "PatientCode", HeaderText = "Patient Code", Width = 120 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", DataPropertyName = "PatientName", HeaderText = "Patient Name", Width = 180 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "TokenNumber", DataPropertyName = "TokenNumber", HeaderText = "Token", Width = 80 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitDate", DataPropertyName = "VisitDate", HeaderText = "Date", Width = 150 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", DataPropertyName = "Type", HeaderText = "Type", Width = 100 });
            dgvSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "Payment", DataPropertyName = "Payment", HeaderText = "Payment", Width = 100 });
        }

        private void ExportToCSV()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"SalesHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    
                    // Headers
                    for (int i = 0; i < dgvSales.Columns.Count; i++)
                    {
                        sb.Append(dgvSales.Columns[i].HeaderText);
                        if (i < dgvSales.Columns.Count - 1) sb.Append(",");
                    }
                    sb.AppendLine();

                    // Data
                    foreach (DataGridViewRow row in dgvSales.Rows)
                    {
                        for (int i = 0; i < dgvSales.Columns.Count; i++)
                        {
                            sb.Append(row.Cells[i].Value?.ToString() ?? "");
                            if (i < dgvSales.Columns.Count - 1) sb.Append(",");
                        }
                        sb.AppendLine();
                    }

                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Data exported successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintData()
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

                    g.DrawString("SALES HISTORY REPORT", fontHeader, Brushes.Black, x, y);
                    g.DrawString($"Date: {DateTime.Now}", fontBody, Brushes.Black, ev.PageBounds.Width - x - 200, y);
                    y += 40;

                    // Columns
                    int[] colWidths = { 60, 120, 180, 80, 150, 100, 100 };
                    string[] headers = { "ID", "Patient Code", "Patient Name", "Token", "Date", "Type", "Payment" };
                    
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
                    foreach (DataGridViewRow row in dgvSales.Rows)
                    {
                        if (row.IsNewRow) continue;
                        
                        currentX = x;
                        string[] values = {
                            row.Cells["VisitID"].Value?.ToString(),
                            row.Cells["PatientCode"].Value?.ToString(),
                            row.Cells["PatientName"].Value?.ToString(),
                            row.Cells["TokenNumber"].Value?.ToString(),
                            row.Cells["VisitDate"].Value?.ToString(),
                            row.Cells["Type"].Value?.ToString(),
                            row.Cells["Payment"].Value?.ToString()
                        };

                        for (int i = 0; i < values.Length; i++)
                        {
                            g.DrawRectangle(Pens.Black, currentX, y, colWidths[i], rowHeight);
                            g.DrawString(values[i], fontBody, Brushes.Black, currentX + 5, y + 8);
                            currentX += colWidths[i];
                        }
                        y += rowHeight;

                        if (y > ev.MarginBounds.Bottom)
                            break;
                    }
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Print error: " + ex.Message); }
        }
    }
}
