using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class PrescriptionsForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";
        private DataTable dtItems;

        public PrescriptionsForm()
        {
            InitializeComponent();
            InitializeItemsTable();
            SetupEvents();
            LoadMedicines();
            LoadDoctors();
            LoadAppointments();
            LoadPrescriptions();
            InitializeStatusCombo();
        }

        private void InitializeStatusCombo()
        {
            cmbItemStatus.Items.AddRange(new string[] { "Pending", "Issued" });
            cmbItemStatus.SelectedIndex = 0;
        }

        private void InitializeItemsTable()
        {
            dtItems = new DataTable();
            dtItems.Columns.Add("Medicine");
            dtItems.Columns.Add("MedicineId", typeof(int));
            dtItems.Columns.Add("Quantity", typeof(int));
            dtItems.Columns.Add("Dosage");
            dtItems.Columns.Add("Duration");
            dtItems.Columns.Add("Status");
            dgvItems.DataSource = dtItems;
            if (dgvItems.Columns["MedicineId"] != null) dgvItems.Columns["MedicineId"].Visible = false;
        }

        private void SetupEvents()
        {
            btnAddItem.Click += (s, e) => AddItemToGrid();
            btnSave.Click += (s, e) => SavePrescription();
            btnNew.Click += (s, e) => ResetForm();
            btnRefresh.Click += (s, e) => LoadPrescriptions();
            btnDelete.Click += (s, e) => DeletePrescription();
            dgvMaster.SelectionChanged += DgvMaster_SelectionChanged;
            btnPrint.Click += (s, e) => PrintPrescription();
            // New Events
            if (txtSearch != null) txtSearch.TextChanged += (s, e) => PerformSearch(txtSearch.Text);
            if (btnPrintAudit != null) btnPrintAudit.Click += (s, e) => PrintAuditReport();

            // Automatic Qty Calculation
            txtDosage.TextChanged += (s, e) => CalculateQuantity();
            txtDuration.TextChanged += (s, e) => CalculateQuantity();
            
            cmbAppointmentIntId.SelectedIndexChanged += (s, e) => {
                if (cmbAppointmentIntId.SelectedValue != null && int.TryParse(cmbAppointmentIntId.SelectedValue.ToString(), out int appId)) {
                    AutoSelectDoctor(appId);
                }
            };
        }

        private void LoadDoctors()
        {
            try {
                using (SqlConnection conn = new SqlConnection(connectionString)) {
                    string query = "SELECT DoctorId, DoctorName, Specialization FROM Doctors WHERE Status = 'Active' ORDER BY DoctorName ASC";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cmbDoctor.DisplayMember = "DoctorName";
                    cmbDoctor.ValueMember = "DoctorId";
                    cmbDoctor.DataSource = dt;
                }
            } catch (Exception ex) { MessageBox.Show("Error loading doctors: " + ex.Message); }
        }

        private void AutoSelectDoctor(int appointmentId)
        {
            try {
                using (SqlConnection conn = new SqlConnection(connectionString)) {
                    conn.Open();
                    string query = "SELECT DoctorId FROM Appointments WHERE AppointmentIntId = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", appointmentId);
                    object res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value) cmbDoctor.SelectedValue = res;
                }
            } catch { }
        }

        private void CalculateQuantity()
        {
            try {
                string dosage = txtDosage.Text.Trim();
                string durationStr = txtDuration.Text.Replace("days", "").Replace("day", "").Trim();
                
                if (string.IsNullOrEmpty(dosage) || string.IsNullOrEmpty(durationStr)) return;

                int dosesPerDay = 0;
                // Parse dosage like 1-0-1 or 1-1-1
                var parts = dosage.Split('-');
                foreach (var part in parts) {
                    if (int.TryParse(part.Trim(), out int d)) dosesPerDay += d;
                }

                if (dosesPerDay == 0 && int.TryParse(dosage, out int dSingle)) dosesPerDay = dSingle;

                if (int.TryParse(durationStr, out int days)) {
                    txtQty.Text = (dosesPerDay * days).ToString();
                }
            } catch { }
        }

        private void LoadMedicines()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT MedIntId, TradeName FROM Medicines WHERE Status = 'Available' ORDER BY TradeName ASC";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cmbMedicines.DisplayMember = "TradeName";
                    cmbMedicines.ValueMember = "MedIntId";
                    cmbMedicines.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading medicines: " + ex.Message); }
        }

        private void LoadAppointments()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT AppointmentIntId, AppointmentCode + ' - ' + PatientName as DisplayText FROM Appointments ORDER BY AppointmentIntId DESC";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cmbAppointmentIntId.DisplayMember = "DisplayText";
                    cmbAppointmentIntId.ValueMember = "AppointmentIntId";
                    cmbAppointmentIntId.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading appointments: " + ex.Message); }
        }

        private void LoadPrescriptions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            P.PrescriptionID, 
                            P.PrescriptionCode, 
                            P.AppointmentIntId, 
                            Pt.PatientName,
                            A.AppointmentCode,
                            D.DoctorName as PrescribedBy,
                            P.Diagnosis, 
                            P.Symptoms, 
                            P.Notes, 
                            P.DateCreated 
                        FROM Prescriptions P
                        LEFT JOIN Appointments A ON P.AppointmentIntId = A.AppointmentIntId
                        LEFT JOIN Patients Pt ON A.PatientID = Pt.PatientID
                        LEFT JOIN Doctors D ON P.PrescribedByDoctorId = D.DoctorId
                        ORDER BY P.DateCreated DESC";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvMaster.DataSource = dt;
                    dgvMaster.Columns["PrescriptionID"].Visible = false;
                    dgvMaster.Columns["AppointmentIntId"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading prescriptions: " + ex.Message); }
        }

        private void PerformSearch(string query)
        {
            if (dgvMaster.DataSource is DataTable dt)
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    dt.DefaultView.RowFilter = "";
                }
                else
                {
                   dt.DefaultView.RowFilter = string.Format("PrescriptionCode LIKE '%{0}%' OR PatientName LIKE '%{0}%' OR Diagnosis LIKE '%{0}%'", query);
                }
            }
        }

        private void PrintAuditReport()
        {
             try
            {
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                pd.DefaultPageSettings.Landscape = true;
                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    Font fontHeader = new Font("Arial", 12, FontStyle.Bold);
                    Font fontBody = new Font("Arial", 9);
                    int y = 50;
                    int x = 50;

                    g.DrawString("PRESCRIPTION AUDIT REPORT", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, x, y);
                    y += 40;
                    g.DrawString($"Generated on: {DateTime.Now}", fontBody, Brushes.Black, x, y);
                    y += 30;

                    // Headers
                    g.DrawString("Code", fontHeader, Brushes.Black, x, y);
                    g.DrawString("Date", fontHeader, Brushes.Black, x + 100, y);
                    g.DrawString("Patient", fontHeader, Brushes.Black, x + 250, y);
                    g.DrawString("Diagnosis", fontHeader, Brushes.Black, x + 450, y);
                    g.DrawString("Notes", fontHeader, Brushes.Black, x + 700, y);
                    y += 25;
                    g.DrawLine(Pens.Black, x, y, x + 1000, y);
                    y += 10;

                    if (dgvMaster.DataSource is DataTable dt)
                    {
                        DataView dv = dt.DefaultView; // Use filtered view
                        foreach (DataRowView row in dv)
                        {
                            g.DrawString(row["PrescriptionCode"].ToString(), fontBody, Brushes.Black, x, y);
                            g.DrawString(Convert.ToDateTime(row["DateCreated"]).ToShortDateString(), fontBody, Brushes.Black, x + 100, y);
                            string pat = row["PatientName"] != DBNull.Value ? row["PatientName"].ToString() : "N/A";
                            if (pat.Length > 20) pat = pat.Substring(0, 17) + "...";
                            g.DrawString(pat, fontBody, Brushes.Black, x + 250, y);

                            string diag = row["Diagnosis"].ToString();
                            if (diag.Length > 30) diag = diag.Substring(0, 27) + "...";
                            g.DrawString(diag, fontBody, Brushes.Black, x + 450, y);

                            string notes = row["Notes"].ToString();
                            if (notes.Length > 40) notes = notes.Substring(0, 37) + "...";
                            g.DrawString(notes, fontBody, Brushes.Black, x + 700, y);

                            y += 20;
                        }
                    }
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Audit Print Error: " + ex.Message); }
        }

        private void AddItemToGrid()
        {
            if (cmbMedicines.SelectedItem == null || string.IsNullOrEmpty(txtQty.Text))
            {
                MessageBox.Show("Please select a medicine and enter quantity.");
                return;
            }

            DataRow row = dtItems.NewRow();
            row["Medicine"] = cmbMedicines.Text;
            row["MedicineId"] = cmbMedicines.SelectedValue;
            row["Quantity"] = int.Parse(txtQty.Text);
            row["Dosage"] = txtDosage.Text;
            row["Duration"] = txtDuration.Text;
            row["Status"] = cmbItemStatus.SelectedItem?.ToString() ?? "Pending";
            dtItems.Rows.Add(row);

            txtQty.Text = "";
            txtDosage.Text = "";
            txtDuration.Text = "";
        }

        private void DgvMaster_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMaster.SelectedRows.Count == 0) return;
            
            var row = dgvMaster.SelectedRows[0];
            txtPrescriptionID.Tag = row.Cells["PrescriptionID"].Value;
            txtPrescriptionID.Text = row.Cells["PrescriptionCode"].Value.ToString();
            cmbAppointmentIntId.SelectedValue = row.Cells["AppointmentIntId"].Value;
            txtDiagnosis.Text = row.Cells["Diagnosis"].Value.ToString();
            txtSymptoms.Text = row.Cells["Symptoms"].Value.ToString();
            txtNotes.Text = row.Cells["Notes"].Value.ToString();
            if (row.Cells["DateCreated"].Value != DBNull.Value)
                dtpCreatedDate.Value = Convert.ToDateTime(row.Cells["DateCreated"].Value);
            else
                dtpCreatedDate.Value = DateTime.Now;

            LoadPrescriptionDetails(txtPrescriptionID.Tag.ToString());
        }

        private void LoadPrescriptionDetails(string prescriptionID)
        {
            try
            {
                dtItems.Clear();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT pd.Dosage, pd.Quantity, pd.Status, pd.MedicineId, pd.Duration, m.TradeName 
                                   FROM PrescriptionDetails pd
                                   JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                   WHERE pd.PrescriptionId = @ID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ID", prescriptionID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable detailsDt = new DataTable();
                    adapter.Fill(detailsDt);

                    foreach (DataRow dRow in detailsDt.Rows)
                    {
                        DataRow newRow = dtItems.NewRow();
                        newRow["Medicine"] = dRow["TradeName"];
                        newRow["MedicineId"] = dRow["MedicineId"];
                        newRow["Quantity"] = dRow["Quantity"];
                        newRow["Dosage"] = dRow["Dosage"];
                        newRow["Duration"] = dRow["Duration"];
                        newRow["Status"] = dRow["Status"];
                        dtItems.Rows.Add(newRow);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading details: " + ex.Message); }
        }

        private void SavePrescription()
        {
            if (cmbAppointmentIntId.SelectedValue == null || dtItems.Rows.Count == 0)
            {
                MessageBox.Show("Please enter Appointment ID and add at least one medicine.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // Fetch PatientId first
                    int patientId = 0;
                    string qPat = "SELECT PatientId FROM Appointments WHERE AppointmentIntId = @id";
                    using (SqlCommand cmdPat = new SqlCommand(qPat, conn, trans)) {
                        cmdPat.Parameters.AddWithValue("@id", cmbAppointmentIntId.SelectedValue);
                        object res = cmdPat.ExecuteScalar();
                        if (res != null && res != DBNull.Value) patientId = Convert.ToInt32(res);
                    }

                    string masterQuery = @"INSERT INTO Prescriptions (AppointmentIntId, Diagnosis, Symptoms, Notes, DateCreated, PrescribedByDoctorId) 
                                         VALUES (@AppID, @Diag, @Symp, @Notes, @Date, @DocID); 
                                         SELECT SCOPE_IDENTITY();";
                    SqlCommand cmdMst = new SqlCommand(masterQuery, conn, trans);
                    cmdMst.Parameters.AddWithValue("@AppID", cmbAppointmentIntId.SelectedValue.ToString());
                    cmdMst.Parameters.AddWithValue("@Diag", txtDiagnosis.Text);
                    cmdMst.Parameters.AddWithValue("@Symp", txtSymptoms.Text);
                    cmdMst.Parameters.AddWithValue("@Notes", txtNotes.Text);
                    cmdMst.Parameters.AddWithValue("@Date", dtpCreatedDate.Value);
                    cmdMst.Parameters.AddWithValue("@DocID", cmbDoctor.SelectedValue ?? DBNull.Value);

                    int newID = Convert.ToInt32(cmdMst.ExecuteScalar());

                    foreach (DataRow row in dtItems.Rows)
                    {
                        string detailQuery = @"INSERT INTO PrescriptionDetails (PrescriptionId, MedicineId, Quantity, Dosage, Duration, Status, PatientId) 
                                             VALUES (@PID, @MID, @Qty, @Dos, @Dur, @Status, @PatID)";
                        SqlCommand cmdDet = new SqlCommand(detailQuery, conn, trans);
                        cmdDet.Parameters.AddWithValue("@PID", newID);
                        cmdDet.Parameters.AddWithValue("@MID", row["MedicineId"]);
                        cmdDet.Parameters.AddWithValue("@Qty", row["Quantity"]);
                        cmdDet.Parameters.AddWithValue("@Dos", row["Dosage"]); 
                        cmdDet.Parameters.AddWithValue("@Dur", row["Duration"]);
                        cmdDet.Parameters.AddWithValue("@Status", row["Status"] ?? "Pending");
                        cmdDet.Parameters.AddWithValue("@PatID", patientId);
                        cmdDet.ExecuteNonQuery();
                    }

                    trans.Commit();
                    MessageBox.Show("Prescription saved successfully! ID: " + newID);
                    LoadPrescriptions();
                    ResetForm();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("Save error: " + ex.Message);
                }
            }
        }

        private void DeletePrescription()
        {
            if (dgvMaster.SelectedRows.Count == 0) return;
            string id = dgvMaster.SelectedRows[0].Cells["PrescriptionID"].Value.ToString();

            if (MessageBox.Show("Delete Prescription " + id + "?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        // 1. Delete Details
                        SqlCommand cmdDet = new SqlCommand("DELETE FROM PrescriptionDetails WHERE PrescriptionID = @ID", conn);
                        cmdDet.Parameters.AddWithValue("@ID", id);
                        cmdDet.ExecuteNonQuery();

                        // 2. Delete Master
                        SqlCommand cmdMst = new SqlCommand("DELETE FROM Prescriptions WHERE PrescriptionID = @ID", conn);
                        cmdMst.Parameters.AddWithValue("@ID", id);
                        cmdMst.ExecuteNonQuery();

                        MessageBox.Show("Prescription deleted.");
                        LoadPrescriptions();
                        ResetForm();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Delete error: " + ex.Message); }
            }
        }

        private void ResetForm()
        {
            txtPrescriptionID.Tag = null;
            txtPrescriptionID.Text = FetchNextPrescriptionCode();
            if (cmbAppointmentIntId.Items.Count > 0) cmbAppointmentIntId.SelectedIndex = 0;
            if (cmbDoctor.Items.Count > 0) cmbDoctor.SelectedIndex = 0;
            txtDiagnosis.Text = "";
            txtSymptoms.Text = "";
            txtNotes.Text = "";
            txtDosage.Text = "";
            txtDuration.Text = "";
            txtQty.Text = "";
            dtItems.Clear();
            dtpCreatedDate.Value = DateTime.Now;
        }

        private void PrintPrescription()
        {
            if (dgvMaster.SelectedRows.Count == 0 && txtPrescriptionID.Tag == null)
            {
                MessageBox.Show("Please select a prescription to print.");
                return;
            }

            try
            {
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    Font fontTitle = new Font("Arial", 18, FontStyle.Bold);
                    Font fontHeader = new Font("Arial", 12, FontStyle.Bold);
                    Font fontBody = new Font("Arial", 10);
                    int y = 50;

                    g.DrawString("AL REHMAN CLINIC", fontTitle, Brushes.Navy, new RectangleF(0, y, ev.PageBounds.Width, 40), new StringFormat { Alignment = StringAlignment.Center });
                    y += 40;
                    g.DrawString("Near Civil Hospital, Shujabad, Multan | Tel: +92-300-1234567", fontBody, Brushes.DimGray, new RectangleF(0, y, ev.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
                    y += 35;

                    // Doctor Info
                    string docName = cmbDoctor.Text;
                    string docSpec = "";
                    if (cmbDoctor.SelectedItem is DataRowView drv) docSpec = drv["Specialization"]?.ToString() ?? "";
                    
                    g.DrawString($"Dr. {docName}", fontHeader, Brushes.Black, 100, y);
                    g.DrawString(docSpec, fontBody, Brushes.DimGray, 100, y + 20);
                    
                    y += 45;
                    g.DrawLine(new Pen(Color.Navy, 2), 100, y, 700, y);
                    y += 30;

                    g.DrawString($"Prescription No: {txtPrescriptionID.Text}", fontHeader, Brushes.Black, 100, y);
                    g.DrawString($"Date: {dtpCreatedDate.Value.ToShortDateString()}", fontHeader, Brushes.Black, 500, y);
                    y += 30;

                    g.DrawString($"Diagnosis: {txtDiagnosis.Text}", fontBody, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawString($"Symptoms: {txtSymptoms.Text}", fontBody, Brushes.Black, 100, y);
                    y += 40;

                    g.DrawString("MEDICATIONS:", fontHeader, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawLine(Pens.Black, 100, y, 700, y);
                    y += 10;

                    foreach (DataRow row in dtItems.Rows)
                    {
                        string medicine = row["Medicine"].ToString();
                        int qty = row["Quantity"] != DBNull.Value ? Convert.ToInt32(row["Quantity"]) : 0;
                        string dosage = row["Dosage"].ToString();
                        string duration = row["Duration"]?.ToString() ?? "";

                        // Fallback calculation for legacy items or missing qty
                        if (qty == 0 && !string.IsNullOrEmpty(dosage) && !string.IsNullOrEmpty(duration))
                        {
                            try {
                                int dosesPerDay = 0;
                                var parts = dosage.Split('-');
                                foreach (var part in parts) if (int.TryParse(part.Trim(), out int d)) dosesPerDay += d;
                                if (dosesPerDay == 0 && int.TryParse(dosage, out int dSingle)) dosesPerDay = dSingle;

                                string durClean = duration.Replace("days", "").Replace("day", "").Trim();
                                if (int.TryParse(durClean, out int days)) qty = dosesPerDay * days;
                            } catch { }
                        }

                        string qtyStr = qty > 0 ? qty.ToString() : "";
                        g.DrawString($"{medicine} ({qtyStr}) - {dosage}", fontBody, Brushes.Black, 120, y);
                        y += 20;
                    }

                    y += 50;
                    string doctorName = cmbDoctor.Text;
                    g.DrawString($"Dr. {doctorName}", fontBody, Brushes.Black, 100, y);
                    y += 20;
                    g.DrawString("Doctor's Signature: ____________________", fontBody, Brushes.Black, 100, y);
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Print error: " + ex.Message); }
        }

        private string FetchNextPrescriptionCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ISNULL(IDENT_CURRENT('Prescriptions'), 0) + ISNULL(IDENT_INCR('Prescriptions'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        return "PR" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "PR---";
        }
    }
}
