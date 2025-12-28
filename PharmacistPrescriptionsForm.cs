using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class PharmacistPrescriptionsForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        private string _pharmacistUsername;

        public PharmacistPrescriptionsForm(string pharmacistUsername = "")
        {
            this._pharmacistUsername = pharmacistUsername;
            InitializeComponent();
            SetupGrid();
            LoadData();
            WireEvents();
        }

        private void SetupGrid()
        {
            if (dgvPrescriptions == null) return;

            dgvPrescriptions.AutoGenerateColumns = false;
            dgvPrescriptions.Columns.Clear();

            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrescriptionDetailId", DataPropertyName = "PrescriptionDetailId", Visible = false });
            dgvPrescriptions.Columns.Add(new DataPropertyNameColumn { Name = "MedicineId", DataPropertyName = "MedicineId", Visible = false });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrescriptionCode", DataPropertyName = "PrescriptionCode", HeaderText = "RX Code", Width = 110 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientCode", DataPropertyName = "PatientCode", HeaderText = "Patient ID", Width = 100 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientName", DataPropertyName = "PatientName", HeaderText = "Patient Name", Width = 150 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoctorName", DataPropertyName = "DoctorName", HeaderText = "Prescribed By", Width = 150 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TradeName", DataPropertyName = "TradeName", HeaderText = "Medicine", Width = 150 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dosage", DataPropertyName = "Dosage", HeaderText = "Dosage", Width = 100 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", DataPropertyName = "Duration", HeaderText = "Duration", Width = 100 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrescribedQty", DataPropertyName = "PrescribedQty", HeaderText = "Req. Qty", Width = 80 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "AlreadyDispensed", DataPropertyName = "AlreadyDispensed", HeaderText = "Disp.", Width = 70 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingQty", DataPropertyName = "RemainingQty", HeaderText = "Rem.", Width = 70 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "Status", Width = 100 });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrescriptionId", DataPropertyName = "PrescriptionId", Visible = false });
            dgvPrescriptions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PatientId", DataPropertyName = "PatientId", Visible = false });
        }

        // Helper class for hidden medicine ID column if needed or just use standard
        private class DataPropertyNameColumn : DataGridViewTextBoxColumn { }

        private void LoadData()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string statusFilter = chkShowHistory.Checked ? "'Pending', 'Partial', 'Dispensed'" : "'Pending', 'Partial'";
                    string q = $@"SELECT pd.PrescriptionDetailId, pd.MedicineId, m.TradeName, pd.Status,
                                       p.PrescriptionCode, 
                                       ISNULL(pt.PatientName, pt2.PatientName) as PatientName, 
                                       ISNULL(pt.PatientCode, pt2.PatientCode) as PatientCode, 
                                       ISNULL(dr.DoctorName, dr2.DoctorName) as DoctorName, 
                                       p.PrescriptionId, 
                                       ISNULL(pd.PatientId, pt2.PatientId) as PatientId,
                                       pd.Dosage, pd.Duration,
                                       ISNULL(pd.Quantity, 0) as PrescribedQty,
                                       ISNULL(pd.DispensedQty, 0) as AlreadyDispensed,
                                       CASE 
                                            WHEN ISNULL(pd.Quantity, 0) - ISNULL(pd.DispensedQty, 0) < 0 THEN 0 
                                            ELSE ISNULL(pd.Quantity, 0) - ISNULL(pd.DispensedQty, 0) 
                                       END as RemainingQty
                                FROM PrescriptionDetails pd
                                JOIN Prescriptions p ON pd.PrescriptionId = p.PrescriptionId
                                JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                LEFT JOIN Patients pt ON pd.PatientID = pt.PatientID
                                LEFT JOIN Appointments a ON p.AppointmentIntId = a.AppointmentIntId
                                LEFT JOIN Patients pt2 ON a.PatientID = pt2.PatientID
                                LEFT JOIN Doctors dr ON p.PrescribedByDoctorId = dr.DoctorId
                                LEFT JOIN Doctors dr2 ON a.DoctorId = dr2.DoctorId
                                WHERE pd.Status IN ({statusFilter})
                                ORDER BY p.DateCreated DESC";
                    SqlDataAdapter da = new SqlDataAdapter(q, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    FixLegacyQuantities(dt);
                    dgvPrescriptions.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading prescriptions: " + ex.Message); }
        }

        private void WireEvents()
        {
            btnSearch.Click += (s, e) => Search(txtSearch.Text);
            btnRefresh.Click += (s, e) => LoadData();
            btnDispense.Click += (s, e) => DispenseSelected();
            btnExport.Click += (s, e) => ExportPrescriptions();
            btnPrint.Click += (s, e) => PrintPrescriptions();
            chkShowHistory.CheckedChanged += (s, e) => LoadData();
            
            dgvPrescriptions.SelectionChanged += (s, e) => {
                if (dgvPrescriptions.SelectedRows.Count > 0) {
                    var row = dgvPrescriptions.SelectedRows[0];
                    object remVal = row.Cells["RemainingQty"].Value;
                    int remaining = (remVal != null && remVal != DBNull.Value) ? Convert.ToInt32(remVal) : 0;
                    numQty.Maximum = Math.Max(remaining, 1000); // Allow increasing
                    numQty.Value = Math.Max(remaining, 1);
                }
            };
            
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    Search(txtSearch.Text);
                    e.SuppressKeyPress = true;
                }
            };

            dgvPrescriptions.CellFormatting += (s, e) => {
                if (e.RowIndex >= 0 && dgvPrescriptions.Columns[e.ColumnIndex].Name == "Status") {
                    var val = e.Value?.ToString();
                    if (val == "Dispensed") {
                        dgvPrescriptions.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);
                        dgvPrescriptions.Rows[e.RowIndex].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(107, 114, 128);
                    } else {
                        dgvPrescriptions.Rows[e.RowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.White;
                        dgvPrescriptions.Rows[e.RowIndex].DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                    }
                }
            };
        }

        private void ExportPrescriptions()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Prescriptions_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    
                    // Headers
                    for (int i = 0; i < dgvPrescriptions.Columns.Count; i++)
                    {
                        if (!dgvPrescriptions.Columns[i].Visible) continue;
                        sb.Append(dgvPrescriptions.Columns[i].HeaderText);
                        if (i < dgvPrescriptions.Columns.Count - 1) sb.Append(",");
                    }
                    sb.AppendLine();

                    // Data
                    foreach (DataGridViewRow row in dgvPrescriptions.Rows)
                    {
                        for (int i = 0; i < dgvPrescriptions.Columns.Count; i++)
                        {
                            if (!dgvPrescriptions.Columns[i].Visible) continue;
                            sb.Append(row.Cells[i].Value?.ToString() ?? "");
                            if (i < dgvPrescriptions.Columns.Count - 1) sb.Append(",");
                        }
                        sb.AppendLine();
                    }

                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Prescriptions exported successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintPrescriptions()
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

                    g.DrawString("PENDING PRESCRIPTION ORDERS", fontHeader, Brushes.Black, x, y);
                    g.DrawString($"Date: {DateTime.Now}", fontBody, Brushes.Black, ev.PageBounds.Width - x - 200, y);
                    y += 40;

                    // Columns
                    // Adjust widths to match visible columns
                    int[] colWidths = { 80, 120, 120, 120, 80, 80, 60, 60, 60, 90 };
                    string[] headers = { "RX Code", "Patient", "Prescribed By", "Medicine", "Dosage", "Duration", "Req.", "Disp.", "Rem.", "Status" };
                    string[] colNames = { "PrescriptionCode", "PatientName", "DoctorName", "TradeName", "Dosage", "Duration", "PrescribedQty", "AlreadyDispensed", "RemainingQty", "Status" };

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
                    foreach (DataGridViewRow row in dgvPrescriptions.Rows)
                    {
                        if (row.IsNewRow) continue;
                        
                        currentX = x;
                        for (int i = 0; i < colNames.Length; i++)
                        {
                            string val = row.Cells[colNames[i]].Value?.ToString() ?? "";
                            g.DrawRectangle(Pens.Black, currentX, y, colWidths[i], rowHeight);
                            g.DrawString(val, fontBody, Brushes.Black, currentX + 5, y + 8);
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

        private void Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) { LoadData(); return; }
            
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string statusFilter = chkShowHistory.Checked ? "'Pending', 'Partial', 'Dispensed'" : "'Pending', 'Partial'";
                    string q = $@"SELECT pd.PrescriptionDetailId, pd.MedicineId, m.TradeName, pd.Status,
                                       p.PrescriptionCode, 
                                       ISNULL(dr.DoctorName, dr2.DoctorName) as DoctorName, 
                                       p.PrescriptionId, 
                                       ISNULL(pd.PatientId, pt2.PatientId) as PatientId,
                                       pd.Dosage, pd.Duration,
                                       ISNULL(pd.Quantity, 0) as PrescribedQty,
                                       ISNULL(pd.DispensedQty, 0) as AlreadyDispensed,
                                       CASE 
                                            WHEN ISNULL(pd.Quantity, 0) - ISNULL(pd.DispensedQty, 0) < 0 THEN 0 
                                            ELSE ISNULL(pd.Quantity, 0) - ISNULL(pd.DispensedQty, 0) 
                                       END as RemainingQty
                                FROM PrescriptionDetails pd
                                JOIN Prescriptions p ON pd.PrescriptionId = p.PrescriptionId
                                JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                LEFT JOIN Patients pt ON pd.PatientID = pt.PatientID
                                LEFT JOIN Appointments a ON p.AppointmentIntId = a.AppointmentIntId
                                LEFT JOIN Patients pt2 ON a.PatientID = pt2.PatientID
                                LEFT JOIN Doctors dr ON p.PrescribedByDoctorId = dr.DoctorId
                                LEFT JOIN Doctors dr2 ON a.DoctorId = dr2.DoctorId
                                WHERE pd.Status IN ({statusFilter}) 
                                AND (ISNULL(pt.PatientName, pt2.PatientName) LIKE @t OR p.PrescriptionCode LIKE @t OR m.TradeName LIKE @t OR ISNULL(pt.PatientCode, pt2.PatientCode) LIKE @t)
                                ORDER BY p.DateCreated DESC";
                    SqlCommand cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@t", "%" + term + "%");
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    FixLegacyQuantities(dt);
                    dgvPrescriptions.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Search error: " + ex.Message); }
        }

        private void FixLegacyQuantities(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                int prescribedQty = Convert.ToInt32(row["PrescribedQty"] == DBNull.Value ? 0 : row["PrescribedQty"]);
                if (prescribedQty == 0)
                {
                    try
                    {
                        string dosage = row["Dosage"]?.ToString().Trim();
                        string durationStr = row["Duration"]?.ToString().Replace("days", "").Replace("day", "").Trim();

                        if (string.IsNullOrEmpty(dosage) || string.IsNullOrEmpty(durationStr)) continue;

                        int dosesPerDay = 0;
                        var parts = dosage.Split('-');
                        foreach (var part in parts)
                        {
                            if (int.TryParse(part.Trim(), out int d)) dosesPerDay += d;
                        }
                        if (dosesPerDay == 0 && int.TryParse(dosage, out int dSingle)) dosesPerDay = dSingle;

                        if (int.TryParse(durationStr, out int days))
                        {
                            int calculated = dosesPerDay * days;
                            row["PrescribedQty"] = calculated;

                            int disp = Convert.ToInt32(row["AlreadyDispensed"] == DBNull.Value ? 0 : row["AlreadyDispensed"]);
                            row["RemainingQty"] = Math.Max(0, calculated - disp);
                        }
                    }
                    catch { }
                }
            }
        }

        private void DispenseSelected()
        {
            if (dgvPrescriptions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a prescription item to dispense.");
                return;
            }
            
            var row = dgvPrescriptions.SelectedRows[0];
            if (row.Cells["Status"].Value?.ToString() == "Dispensed")
            {
                MessageBox.Show("This item has already been fully dispensed.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int detailId = (int)row.Cells["PrescriptionDetailId"].Value;
            int medicineId = (int)row.Cells["MedicineId"].Value;
            int prescriptionId = (int)row.Cells["PrescriptionId"].Value;
            int patientId = (int)row.Cells["PatientId"].Value;
            string patientName = row.Cells["PatientName"].Value?.ToString();
            string tradeName = row.Cells["TradeName"].Value?.ToString();
            int prescribedQty = Convert.ToInt32(row.Cells["PrescribedQty"].Value ?? 0);
            int alreadyDispensed = Convert.ToInt32(row.Cells["AlreadyDispensed"].Value ?? 0);
            int remainingQty = Convert.ToInt32(row.Cells["RemainingQty"].Value ?? 0);

            int dispenseQty = (int)numQty.Value;

            if (dispenseQty <= 0)
            {
                MessageBox.Show("Please select a quantity to dispense.");
                return;
            }

            if (dispenseQty > remainingQty)
            {
                if (MessageBox.Show($"Warning: Dispensing {dispenseQty} exceeds the remaining prescription quantity ({remainingQty}).\n\nAre you sure you want to proceed?", "Confirm Over-Dispense", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }

            // 2. Fetch Pharmacist StaffId
            int pharmacistStaffId = 0;
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = "SELECT StaffId FROM Users WHERE Username = @u";
                    using (var cmd = new SqlCommand(q, con)) {
                        cmd.Parameters.AddWithValue("@u", _pharmacistUsername);
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value) pharmacistStaffId = Convert.ToInt32(res);
                    }
                }
            } catch { }

            if (pharmacistStaffId == 0) {
                 MessageBox.Show("Pharmacist profile not found. Cannot record dispensing by.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
            }

            // 3. Perform Transaction
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var trans = con.BeginTransaction())
                    {
                        try
                        {
                            // A. Update Prescription Detail (DispensedQty and Status and DispensedBy)
                            int totalNow = alreadyDispensed + dispenseQty;
                            string newStatus = (totalNow >= prescribedQty) ? "Dispensed" : "Partial";

                            string updatePd = @"UPDATE PrescriptionDetails SET 
                                               DispensedQty = DispensedQty + @qty, 
                                               Status = @s, 
                                               DispensedBy = @by 
                                               WHERE PrescriptionDetailId = @did";
                            using (var cmd = new SqlCommand(updatePd, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@qty", dispenseQty);
                                cmd.Parameters.AddWithValue("@s", newStatus);
                                cmd.Parameters.AddWithValue("@by", pharmacistStaffId);
                                cmd.Parameters.AddWithValue("@did", detailId);
                                cmd.ExecuteNonQuery();
                            }

                            // B. Deduct Stock
                            string deductStock = "UPDATE Medicines SET Quantity = Quantity - @qty WHERE MedIntId = @mid AND Quantity >= @qty";
                            using (var cmd = new SqlCommand(deductStock, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@qty", dispenseQty);
                                cmd.Parameters.AddWithValue("@mid", medicineId);
                                if (cmd.ExecuteNonQuery() == 0) throw new Exception("Insufficient stock in pharmacy.");
                            }

                            trans.Commit();
                            MessageBox.Show($"Successfully dispensed {dispenseQty} {tradeName} for {patientName}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadData();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Dispensing error: " + ex.Message); }
        }
    }
}
