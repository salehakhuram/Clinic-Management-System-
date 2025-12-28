using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Printing;
using Microsoft.Reporting.WinForms;

namespace ClinicManagement
{
    public partial class Reports : Form
    {
        private string connectionString = "Server=DESKTOP-5NPQD72\\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True;";
        private DataTable currentReportDataTable = null;
        private string currentRdlcPath = "";
        private string currentDataSetName = "";
        public Reports()
        {
            InitializeComponent();
            dgvReports.AutoGenerateColumns = true;

            // Populate Dropdowns
            PopulateDropdowns();

            // Button events
            btnGenerate.Click += BtnGenerate_Click;
            btnExport.Click += BtnExport_Click;
            btnPrint.Click += BtnPrint_Click;
            btnClearFilters.Click += BtnClearFilters_Click;
            
            // Default dates
            dtpFrom.Value = DateTime.Now.AddDays(-7);
            dtpTo.Value = DateTime.Now;

            if (txtSearch != null) txtSearch.TextChanged += (s, e) => SearchReports();
        }

        private void PopulateDropdowns()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Doctors
                    cmbDoctor.Items.Clear();
                    cmbDoctor.Items.Add(new { Text = "All Doctors", Value = 0 });
                    string docQuery = "SELECT DoctorId, DoctorName FROM Doctors WHERE Status NOT IN ('Inactive', 'Suspended') ORDER BY DoctorName";
                    using (var dr = new SqlCommand(docQuery, con).ExecuteReader())
                    {
                        while (dr.Read()) cmbDoctor.Items.Add(new { Text = dr["DoctorName"].ToString(), Value = dr["DoctorId"] });
                    }
                    cmbDoctor.DisplayMember = "Text";
                    cmbDoctor.ValueMember = "Value";
                    cmbDoctor.SelectedIndex = 0;

                    // Patients
                    cmbPatient.Items.Clear();
                    cmbPatient.Items.Add(new { Text = "All Patients", Value = 0 });
                    string patQuery = "SELECT PatientID, PatientName FROM Patients ORDER BY PatientName";
                    using (var dr = new SqlCommand(patQuery, con).ExecuteReader())
                    {
                        while (dr.Read()) cmbPatient.Items.Add(new { Text = dr["PatientName"].ToString(), Value = dr["PatientID"] });
                    }
                    cmbPatient.DisplayMember = "Text";
                    cmbPatient.ValueMember = "Value";
                    cmbPatient.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error populating dropdowns: " + ex.Message);
            }
        }

        private void BtnClearFilters_Click(object sender, EventArgs e)
        {
            cmbReportType.SelectedIndex = 0;
            cmbDoctor.SelectedIndex = 0;
            cmbPatient.SelectedIndex = 0;
            dtpFrom.Value = DateTime.Now.AddDays(-7);
            dtpTo.Value = DateTime.Now;
            
            dgvReports.DataSource = null;
            dgvReports.Rows.Clear();
            dgvReports.Columns.Clear();

            reportViewer1.Visible = false;
            dgvReports.Visible = true;
        }

        // ================= VALIDATION =================
        private bool ValidateFilters()
        {
            if (dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                MessageBox.Show("From date cannot be greater than To date.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbReportType.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a report type.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        // ================= GENERATE REPORT =================
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (!ValidateFilters()) return;

            dgvReports.DataSource = null;
            dgvReports.Columns.Clear();
            dgvReports.Rows.Clear();
            currentReportDataTable = null;
            currentRdlcPath = "";
            currentDataSetName = "";

            string reportType = cmbReportType.Text;

            switch (reportType)
            {
                case "Patients":
                    LoadPatientsReport();
                    break;
                case "Medicine Stock":
                    LoadMedicineStockReport();
                    break;
                case "Billing":
                    LoadBillingReport();
                    break;
                case "Consultation":
                    LoadConsultationReport();
                    break;
            }
        }

        // ================= REPORT LOADERS (RDLC) =================

        private void LoadReport(string rdlcPath, string dsName, DataTable dt)
        {
            try
            {
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.ReportPath = rdlcPath;
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource(dsName, dt));
                
                // Keep for export/print
                currentReportDataTable = dt;
                currentRdlcPath = rdlcPath;
                currentDataSetName = dsName;

                // Show in Grid for user
                dgvReports.DataSource = dt;
                dgvReports.Visible = true;
                reportViewer1.Visible = false;
                
                // Silent refresh in case user switches view
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error rendering RDLC report: " + ex.Message);
            }
        }

        private void LoadPatientsReport()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientCode, PatientName, FatherName, Gender, DOB, Age, Phone, Email, Address, Disease, BloodGroup, Status, DoctorName, RegistrationDate FROM Patients ORDER BY PatientName";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    
                    string path = Path.Combine(Application.StartupPath, "Reports", "Layouts", "PatientsReport.rdlc");
                    if (!File.Exists(path)) path = @"C:\Users\saleh\source\repos\ClinicManagement\Reports\Layouts\PatientsReport.rdlc";
                    
                    LoadReport(path, "dsPatients", dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients report: " + ex.Message);
            }
        }

        private void LoadMedicineStockReport()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT MedCode AS MedicineCode, TradeName, GenericName, Category, Manufacturer, UnitPrice, StockQuantity, ExpiryDate, Status FROM Medicines ORDER BY TradeName";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    
                    string path = Path.Combine(Application.StartupPath, "Reports", "Layouts", "MedicineStockReport.rdlc");
                    if (!File.Exists(path)) path = @"C:\Users\saleh\source\repos\ClinicManagement\Reports\Layouts\MedicineStockReport.rdlc";
                    
                    LoadReport(path, "dsMedicines", dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading medicine stock report: " + ex.Message);
            }
        }

        private void LoadBillingReport()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT CAST(B.BillId AS VARCHAR(50)) AS BillCode, P.PatientCode, P.PatientName, D.DoctorCode, D.DoctorName, B.TotalAmount, B.PaymentType AS PaymentMethod, B.BillDate
                        FROM Bills B 
                        JOIN Patients P ON B.PatientId = P.PatientId
                        JOIN Doctors D ON B.DoctorId = D.DoctorId
                        WHERE CAST(B.BillDate AS DATE) >= @from AND CAST(B.BillDate AS DATE) <= @to";

                    if (cmbDoctor.SelectedIndex > 0) query += " AND B.DoctorId = @doctorId";
                    if (cmbPatient.SelectedIndex > 0) query += " AND B.PatientId = @patientId";

                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@from", dtpFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@to", dtpTo.Value.Date.AddDays(1).AddSeconds(-1));
                    
                    if (cmbDoctor.SelectedIndex > 0) {
                        dynamic sel = cmbDoctor.SelectedItem;
                        cmd.Parameters.AddWithValue("@doctorId", sel.Value);
                    }
                    if (cmbPatient.SelectedIndex > 0) {
                        dynamic sel = cmbPatient.SelectedItem;
                        cmd.Parameters.AddWithValue("@patientId", sel.Value);
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    string path = Path.Combine(Application.StartupPath, "Reports", "Layouts", "BillsReport.rdlc");
                    if (!File.Exists(path)) path = @"C:\Users\saleh\source\repos\ClinicManagement\Reports\Layouts\BillsReport.rdlc";

                    LoadReport(path, "dsBills", dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading billing report: " + ex.Message);
            }
        }

        private void LoadConsultationReport()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT A.AppointmentCode, P.PatientCode, P.PatientName, D.DoctorCode, D.DoctorName, A.AppointmentDate, A.AppointmentTime, A.Status, A.PaymentStatus
                        FROM Appointments A
                        JOIN Patients P ON A.PatientId = P.PatientId
                        JOIN Doctors D ON A.DoctorId = D.DoctorId
                        WHERE CAST(A.AppointmentDate AS DATE) >= @from AND CAST(A.AppointmentDate AS DATE) <= @to";

                    if (cmbDoctor.SelectedIndex > 0) query += " AND A.DoctorId = @doctorId";
                    if (cmbPatient.SelectedIndex > 0) query += " AND A.PatientId = @patientId";

                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@from", dtpFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@to", dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

                    if (cmbDoctor.SelectedIndex > 0) {
                        dynamic sel = cmbDoctor.SelectedItem;
                        cmd.Parameters.AddWithValue("@doctorId", sel.Value);
                    }
                    if (cmbPatient.SelectedIndex > 0) {
                        dynamic sel = cmbPatient.SelectedItem;
                        cmd.Parameters.AddWithValue("@patientId", sel.Value);
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    string path = Path.Combine(Application.StartupPath, "Reports", "Layouts", "AppointmentsReport.rdlc");
                    if (!File.Exists(path)) path = @"C:\Users\saleh\source\repos\ClinicManagement\Reports\Layouts\AppointmentsReport.rdlc";

                    LoadReport(path, "dsAppointments", dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading consultation report: " + ex.Message);
            }
        }

        // ================= EXPORT =================
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (currentReportDataTable == null || string.IsNullOrEmpty(currentRdlcPath))
            {
                MessageBox.Show("Please generate a report first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"{cmbReportType.Text}_Report_{DateTime.Now:yyyyMMdd}.pdf" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] pdfContent = reportViewer1.LocalReport.Render("PDF");
                        File.WriteAllBytes(sfd.FileName, pdfContent);
                        MessageBox.Show("Professional report exported to PDF successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error exporting PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ================= UNIVERSAL PRINTING (GRAPHICS BASED) =================
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (currentReportDataTable == null || currentReportDataTable.Rows.Count == 0)
            {
                MessageBox.Show("Please generate a report with data first.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true; // Reports are usually wide
            pd.PrintPage += PrintReportPage;

            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
            ppd.ShowDialog();
        }

        private void PrintReportPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font fontTitle = new Font("Segoe UI", 18, FontStyle.Bold);
            Font fontHeader = new Font("Segoe UI", 12, FontStyle.Bold);
            Font fontBody = new Font("Segoe UI", 9);
            Font fontFooter = new Font("Segoe UI", 8, FontStyle.Italic);

            float y = 40;
            float margin = 40;
            float width = e.PageBounds.Width - (margin * 2);

            // 1. HEADER
            g.DrawString("AL REHMAN CLINIC", fontTitle, Brushes.Black, margin, y);
            y += 35;
            g.DrawString($"{cmbReportType.Text} Report", fontHeader, Brushes.Gray, margin, y);
            g.DrawString($"Generated: {DateTime.Now:f}", fontBody, Brushes.Gray, e.PageBounds.Width - margin - 250, y);
            y += 25;
            g.DrawLine(new Pen(Color.FromArgb(59, 130, 246), 2), margin, y, e.PageBounds.Width - margin, y);
            y += 30;

            // 2. TABLE HEADERS
            int colCount = currentReportDataTable.Columns.Count;
            float x = margin;

            // Define column widths based on report type and content
            float[] columnWidths = new float[colCount];
            float[] weights = new float[colCount];
            float totalWeight = 0;

            for (int i = 0; i < colCount; i++)
            {
                string colName = currentReportDataTable.Columns[i].ColumnName.ToLower();
                weights[i] = 1.0f; // Default weight

                if (colName.Contains("id") || colName.Contains("code") || colName.Contains("age") || colName.Contains("gender")) weights[i] = 0.6f;
                else if (colName.Contains("status") || colName.Contains("group")) weights[i] = 0.8f;
                else if (colName.Contains("name") || colName.Contains("phone")) weights[i] = 1.2f;
                else if (colName.Contains("address") || colName.Contains("email") || colName.Contains("disease") || colName.Contains("reason")) weights[i] = 2.0f;
                else if (colName.Contains("date")) weights[i] = 1.0f;

                totalWeight += weights[i];
            }

            for (int i = 0; i < colCount; i++) columnWidths[i] = (weights[i] / totalWeight) * width;

            // Adjust font size if we have too many columns
            Font headerFontUsed = fontHeader;
            Font bodyFontUsed = fontBody;
            if (colCount > 10) {
                headerFontUsed = new Font("Segoe UI", 9, FontStyle.Bold);
                bodyFontUsed = new Font("Segoe UI", 8);
            } else if (colCount > 7) {
                headerFontUsed = new Font("Segoe UI", 10, FontStyle.Bold);
                bodyFontUsed = new Font("Segoe UI", 8.5f);
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(243, 244, 246)), margin, y, width, 40);
            for (int i = 0; i < colCount; i++)
            {
                g.DrawString(currentReportDataTable.Columns[i].ColumnName, headerFontUsed, Brushes.Black, new RectangleF(x + 5, y + 5, columnWidths[i] - 10, 30), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                x += columnWidths[i];
            }
            y += 40;
            g.DrawLine(new Pen(Color.Black, 1.5f), margin, y, e.PageBounds.Width - margin, y);

            // 3. TABLE ROWS
            foreach (DataRow row in currentReportDataTable.Rows)
            {
                x = margin;
                float maxHeight = 0;
                
                // Measure max height for wrap in this row
                for (int i = 0; i < colCount; i++)
                {
                    string txt = row[i]?.ToString() ?? "";
                    if (row[i] is DateTime dt) txt = dt.ToShortDateString();
                    
                    var sz = g.MeasureString(txt, bodyFontUsed, (int)columnWidths[i] - 10);
                    if (sz.Height > maxHeight) maxHeight = sz.Height;
                }
                maxHeight = Math.Max(maxHeight, 28);

                if (y + maxHeight > e.PageBounds.Height - 100)
                {
                    e.HasMorePages = true;
                    return; // Re-enter PrintReportPage for next page
                }

                for (int i = 0; i < colCount; i++)
                {
                    string txt = row[i]?.ToString() ?? "";
                    if (row[i] is DateTime dt) txt = dt.ToShortDateString();
                    if (row[i] is decimal || row[i] is double) txt = string.Format("{0:N2}", row[i]);

                    g.DrawString(txt, bodyFontUsed, Brushes.Black, new RectangleF(x + 5, y + 6, columnWidths[i] - 10, maxHeight));
                    x += columnWidths[i];
                }
                
                y += maxHeight + 6;
                g.DrawLine(new Pen(Color.FromArgb(229, 231, 235), 1), margin, y, e.PageBounds.Width - margin, y);

                if (y > e.PageBounds.Height - 80) break;
            }

            // 4. FOOTER
            y = e.PageBounds.Height - 60;
            g.DrawLine(Pens.Gray, margin, y, e.PageBounds.Width - margin, y);
            y += 10;
            g.DrawString("This is a computer-generated report.", fontFooter, Brushes.Gray, margin, y);
        }

        private void SearchReports()
        {
            if (dgvReports.DataSource == null || currentReportDataTable == null) return;
            string filter = txtSearch.Text.Trim().Replace("'", "''");
            if (filter == "Search reports statistics..." || string.IsNullOrEmpty(filter)) {
                currentReportDataTable.DefaultView.RowFilter = "";
            } else {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < currentReportDataTable.Columns.Count; i++)
                {
                    string colName = currentReportDataTable.Columns[i].ColumnName;
                    sb.AppendFormat("CONVERT([{0}], 'System.String') LIKE '%{1}%'", colName, filter);
                    if (i < currentReportDataTable.Columns.Count - 1) sb.Append(" OR ");
                }
                currentReportDataTable.DefaultView.RowFilter = sb.ToString();
            }
        }
    }
}
