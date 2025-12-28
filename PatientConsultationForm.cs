using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PatientConsultationForm : Form
    {
        private readonly string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";
        private int _visitId;
        private int _patientId;
        private int _appointmentId;
        private string _apptStatus = "";
        private DataTable _dtPrescriptions;
        private List<MedicineItem> _medicineList = new List<MedicineItem>();

        private class MedicineItem
        {
            public int MedIntId { get; set; }
            public string TradeName { get; set; }
            public override string ToString() => TradeName;
        }

        public PatientConsultationForm(int visitId)
        {
            _visitId = visitId;
            InitializeComponent();
            SetupPrescriptionGrid();
            LoadVisitData();
            LoadMedicines();
            
            btnAddPrescription.Click += (s, e) => AddPrescription();
            btnSaveProgress.Click += (s, e) => SaveConsultation(false);
            btnCompleteAppointment.Click += (s, e) => SaveConsultation(true);
            btnPrint.Click += (s, e) => PrintPrescription();
            btnClose.Click += (s, e) => this.Close();

            txtDosage.TextChanged += (s, e) => UpdateCalculatedQty();
            txtDuration.TextChanged += (s, e) => UpdateCalculatedQty();
            
            CheckStatusRestrictions();
        }

        private void CheckStatusRestrictions()
        {
            string status = _apptStatus.Replace("_", " ").Trim();
            bool canEdit = status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || 
                           status.Equals("Checked-In", StringComparison.OrdinalIgnoreCase) || 
                           status.Equals("Checked In", StringComparison.OrdinalIgnoreCase) || 
                           status.Equals("Waiting", StringComparison.OrdinalIgnoreCase) || 
                           status.Equals("With Doctor", StringComparison.OrdinalIgnoreCase);
            
            txtSymptoms.ReadOnly = !canEdit;
            txtDiagnosis.ReadOnly = !canEdit;
            txtNotes.ReadOnly = !canEdit;
            cmbMedicines.Enabled = canEdit;
            txtDosage.Enabled = canEdit;
            txtDuration.Enabled = canEdit;
            btnAddPrescription.Enabled = canEdit;
            btnSaveProgress.Enabled = canEdit;
            btnCompleteAppointment.Enabled = canEdit;

            if (!canEdit)
            {
                lblStatus.Text = $"Status: {_apptStatus} (View Only)";
                lblStatus.ForeColor = Color.IndianRed;
            }
        }

        private void SetupPrescriptionGrid()
        {
            _dtPrescriptions = new DataTable();
            _dtPrescriptions.Columns.Add("MedicineId", typeof(int));
            _dtPrescriptions.Columns.Add("Medicine");
            _dtPrescriptions.Columns.Add("Dosage");
            _dtPrescriptions.Columns.Add("Duration");
            _dtPrescriptions.Columns.Add("Quantity", typeof(int));
            _dtPrescriptions.Columns.Add("Status");
            
            dgvPrescriptions.DataSource = _dtPrescriptions;
            
            // Add Delete Button Column
            DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn {
                HeaderText = "Action",
                Text = "Remove",
                UseColumnTextForButtonValue = true,
                Name = "colDelete",
                FlatStyle = FlatStyle.Flat,
                Width = 80
            };
            dgvPrescriptions.Columns.Add(btnDelete);
            
            dgvPrescriptions.CellContentClick += (s, e) => {
                if (e.ColumnIndex == dgvPrescriptions.Columns["colDelete"].Index && e.RowIndex >= 0) {
                    _dtPrescriptions.Rows.RemoveAt(e.RowIndex);
                }
            };
        }

        private void LoadMedicines()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    string query = "SELECT MedIntId, TradeName FROM Medicines WHERE Status = 'Available' ORDER BY TradeName";
                    var cmd = new SqlCommand(query, con);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        cmbMedicines.Items.Clear();
                        _medicineList.Clear();
                        int count = 0;
                        while (dr.Read())
                        {
                            var item = new MedicineItem {
                                MedIntId = (int)dr["MedIntId"],
                                TradeName = dr["TradeName"].ToString()
                            };
                            _medicineList.Add(item);
                            cmbMedicines.Items.Add(item);
                            count++;
                        }
                        
                        if (count == 0)
                        {
                            MessageBox.Show("No active medicines found in database. Please add medicines first.", "No Medicines", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading medicines: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadVisitData()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    string query = @"SELECT V.*, P.PatientName, P.Age, P.Gender, P.PatientCode, 
                                          A.AppointmentIntId, A.AppointmentCode, A.Status as ApptStatus
                                   FROM Visits V 
                                   JOIN Patients P ON V.PatientID = P.PatientID 
                                   JOIN Appointments A ON V.AppointmentIntId = A.AppointmentIntId 
                                   WHERE V.VisitID = @vid";
                    
                    // Note: If the link above is different, we might need a left join or different mapping.
                    // Checking if Appointments has a VisitId column. 
                    // Based on previous knowledge, Appointments often links to Visits.
                    
                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@vid", _visitId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            _patientId = (int)dr["PatientID"];
                            _appointmentId = (int)dr["AppointmentIntId"];
                            _apptStatus = dr["ApptStatus"]?.ToString() ?? "Unknown";
                            
                            lblPatientName.Text = dr["PatientName"].ToString();
                            lblPatientDetails.Text = $"{dr["Gender"]}, {dr["Age"]} yrs | ID: {dr["PatientCode"]}";
                            lblAppointmentInfo.Text = $"Appt ID: {dr["AppointmentCode"]} | Date: {Convert.ToDateTime(dr["VisitDate"]):dd MMM yyyy hh:mm tt}";
                            
                            txtSymptoms.Text = dr["ChiefComplaint"]?.ToString();
                            txtDiagnosis.Text = dr["Diagnosis"]?.ToString();
                            txtNotes.Text = dr["DoctorNotes"]?.ToString();
                        }
                        else
                        {
                            // Try fallback without Appointment join if it fails
                            dr.Close();
                            string fallbackQuery = @"SELECT V.*, P.PatientName, P.Age, P.Gender, P.PatientCode
                                                   FROM Visits V 
                                                   JOIN Patients P ON V.PatientID = P.PatientID 
                                                   WHERE V.VisitID = @vid";
                            var cmd2 = new SqlCommand(fallbackQuery, con);
                            cmd2.Parameters.AddWithValue("@vid", _visitId);
                            using (SqlDataReader dr2 = cmd2.ExecuteReader())
                            {
                                if (dr2.Read())
                                {
                                    _patientId = (int)dr2["PatientID"];
                                    lblPatientName.Text = dr2["PatientName"].ToString();
                                    lblPatientDetails.Text = $"{dr2["Gender"]}, {dr2["Age"]} yrs | ID: {dr2["PatientCode"]}";
                                    lblAppointmentInfo.Text = $"Visit Date: {Convert.ToDateTime(dr2["VisitDate"]):dd MMM yyyy hh:mm tt}";
                                    
                                    txtSymptoms.Text = dr2["ChiefComplaint"]?.ToString();
                                    txtDiagnosis.Text = dr2["Diagnosis"]?.ToString();
                                    txtNotes.Text = dr2["DoctorNotes"]?.ToString();
                                }
                            }
                        }
                    }
                }
                LoadExistingPrescriptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading visit: " + ex.Message);
            }
        }

        private void LoadExistingPrescriptions()
        {
             try {
                using (var con = new SqlConnection(connectionString)) {
                    string query = @"SELECT pd.MedicineId, m.TradeName, pd.Dosage, pd.Duration, pd.Quantity 
                                    FROM PrescriptionDetails pd
                                    JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                    JOIN Prescriptions p ON pd.PrescriptionId = p.PrescriptionID
                                    WHERE p.AppointmentIntId = @aid";
                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@aid", _appointmentId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            int q = 1;
                            if (dr["Quantity"] != DBNull.Value) {
                                q = Convert.ToInt32(dr["Quantity"]);
                            }
                            _dtPrescriptions.Rows.Add(dr["MedicineId"], dr["TradeName"], dr["Dosage"], dr["Duration"], q, "Pending");
                        }
                    }
                }
            } catch { }
        }

        private void AddPrescription()
        {
            if (cmbMedicines.SelectedItem == null)
            {
                MessageBox.Show("Please select a medicine.");
                return;
            }
            
            var med = (MedicineItem)cmbMedicines.SelectedItem;
            int qty = CalculateQuantity(txtDosage.Text, txtDuration.Text);
            
            _dtPrescriptions.Rows.Add(med.MedIntId, med.TradeName, txtDosage.Text, txtDuration.Text, qty, "Pending");
            
            // Clear inputs
            cmbMedicines.SelectedIndex = -1;
            txtDosage.Clear();
            txtDuration.Clear();
            txtQty.Text = "0";
        }

        private void UpdateCalculatedQty()
        {
            txtQty.Text = CalculateQuantity(txtDosage.Text, txtDuration.Text).ToString();
        }

        private int CalculateQuantity(string dosage, string duration)
        {
            try
            {
                // 1. Calculate Dosage Sum (e.g., "1-0-1" -> 2)
                double dailyTotal = 0;
                string[] dosageParts = dosage.Split(new char[] { '-', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in dosageParts)
                {
                    if (double.TryParse(part, out double val)) dailyTotal += val;
                }
                if (dailyTotal <= 0) dailyTotal = 1; // Fallback

                // 2. Calculate Duration (e.g., "5 days" -> 5)
                int days = 1;
                string durationClean = duration.ToLower().Trim();
                string numPart = new string(durationClean.TakeWhile(c => char.IsDigit(c)).ToArray());
                
                if (int.TryParse(numPart, out int dVal))
                {
                    days = dVal;
                    if (durationClean.Contains("week")) days *= 7;
                    else if (durationClean.Contains("month")) days *= 30;
                }

                return (int)Math.Ceiling(dailyTotal * days);
            }
            catch { return 1; }
        }

        private void SaveConsultation(bool complete)
        {
            if (complete && _dtPrescriptions.Rows.Count == 0)
            {
                if (MessageBox.Show("No medicines prescribed. Are you sure you want to complete without a prescription?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }

            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var trans = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. Update Visit
                            string vQuery = @"UPDATE Visits SET 
                                            ChiefComplaint = @cc, 
                                            Diagnosis = @diag, 
                                            DoctorNotes = @notes,
                                            Status = @status 
                                            WHERE VisitID = @vid";
                            var vCmd = new SqlCommand(vQuery, con, trans);
                            vCmd.Parameters.AddWithValue("@cc", txtSymptoms.Text);
                            vCmd.Parameters.AddWithValue("@diag", txtDiagnosis.Text);
                            vCmd.Parameters.AddWithValue("@notes", txtNotes.Text);
                            vCmd.Parameters.AddWithValue("@status", complete ? "COMPLETED" : "WITH_DOCTOR");
                            vCmd.Parameters.AddWithValue("@vid", _visitId);
                            vCmd.ExecuteNonQuery();

                            // 2. Update Appointment if available
                            if (_appointmentId > 0)
                            {
                                string apptQuery = "UPDATE Appointments SET Status = @status WHERE AppointmentIntId = @aid";
                                var apptCmd = new SqlCommand(apptQuery, con, trans);
                                apptCmd.Parameters.AddWithValue("@status", complete ? "Completed" : "With Doctor");
                                apptCmd.Parameters.AddWithValue("@aid", _appointmentId);
                                apptCmd.ExecuteNonQuery();
                            }

                            // 3. Update Doctor Readiness
                            string qDoc = @"UPDATE Doctors SET Status = @docStat 
                                           WHERE DoctorName = (SELECT DoctorName FROM Visits WHERE VisitID = @vid)";
                            string docStat = (complete) ? "ON DUTY" : "BUSY";
                            var cmdDoc = new SqlCommand(qDoc, con, trans);
                            cmdDoc.Parameters.AddWithValue("@docStat", docStat);
                            cmdDoc.Parameters.AddWithValue("@vid", _visitId);
                            cmdDoc.ExecuteNonQuery();

                            // 4. Handle Prescription Header
                            int prescriptionId = -1;
                            
                            // Fetch DoctorId linked to this visit
                            int doctorId = 0;
                            string qGetDoc = "SELECT DoctorId FROM Doctors WHERE DoctorName = (SELECT DoctorName FROM Visits WHERE VisitID = @vid)";
                            using (var cmdD = new SqlCommand(qGetDoc, con, trans)) {
                                cmdD.Parameters.AddWithValue("@vid", _visitId);
                                object r = cmdD.ExecuteScalar();
                                if (r != null && r != DBNull.Value) doctorId = Convert.ToInt32(r);
                            }

                            string checkPresQuery = "SELECT PrescriptionID FROM Prescriptions WHERE AppointmentIntId = @aid";
                            var checkCmd = new SqlCommand(checkPresQuery, con, trans);
                            checkCmd.Parameters.AddWithValue("@aid", _appointmentId);
                            object existingId = checkCmd.ExecuteScalar();

                            if (existingId != null)
                            {
                                prescriptionId = (int)existingId;
                                string updatePresQuery = @"UPDATE Prescriptions SET 
                                                          Diagnosis = @diag, 
                                                          Symptoms = @sym, 
                                                          Notes = @notes,
                                                          PrescribedByDoctorId = @did
                                                          WHERE PrescriptionID = @pid";
                                var upCmd = new SqlCommand(updatePresQuery, con, trans);
                                upCmd.Parameters.AddWithValue("@diag", txtDiagnosis.Text);
                                upCmd.Parameters.AddWithValue("@sym", txtSymptoms.Text);
                                upCmd.Parameters.AddWithValue("@notes", txtNotes.Text);
                                upCmd.Parameters.AddWithValue("@did", doctorId == 0 ? (object)DBNull.Value : doctorId);
                                upCmd.Parameters.AddWithValue("@pid", prescriptionId);
                                upCmd.ExecuteNonQuery();
                            }
                            else if (_dtPrescriptions.Rows.Count > 0 || complete)
                            {
                                string insertPresQuery = @"INSERT INTO Prescriptions 
                                                          (AppointmentIntId, Diagnosis, Symptoms, Notes, DateCreated, PrescribedByDoctorId) 
                                                          OUTPUT INSERTED.PrescriptionID
                                                          VALUES (@aid, @diag, @sym, @notes, GETDATE(), @did)";
                                var inCmd = new SqlCommand(insertPresQuery, con, trans);
                                inCmd.Parameters.AddWithValue("@aid", _appointmentId);
                                inCmd.Parameters.AddWithValue("@diag", txtDiagnosis.Text);
                                inCmd.Parameters.AddWithValue("@sym", txtSymptoms.Text);
                                inCmd.Parameters.AddWithValue("@notes", txtNotes.Text);
                                inCmd.Parameters.AddWithValue("@did", doctorId == 0 ? (object)DBNull.Value : doctorId);
                                prescriptionId = (int)inCmd.ExecuteScalar();
                            }

                            // 5. Handle Prescription Details
                            if (prescriptionId != -1)
                            {
                                // Clean existing details for this prescription
                                var delDetails = new SqlCommand("DELETE FROM PrescriptionDetails WHERE PrescriptionId = @pid", con, trans);
                                delDetails.Parameters.AddWithValue("@pid", prescriptionId);
                                delDetails.ExecuteNonQuery();

                                foreach (DataRow row in _dtPrescriptions.Rows)
                                {
                                    string insDetailQuery = @"INSERT INTO PrescriptionDetails 
                                                             (PrescriptionId, PatientId, MedicineId, Dosage, Duration, Quantity, DispensedQty, Status) 
                                                             VALUES (@pid, @patid, @mid, @dos, @dur, @qty, 0, 'Pending')";
                                    var detailCmd = new SqlCommand(insDetailQuery, con, trans);
                                    detailCmd.Parameters.AddWithValue("@pid", prescriptionId);
                                    detailCmd.Parameters.AddWithValue("@patid", _patientId);
                                    detailCmd.Parameters.AddWithValue("@mid", row["MedicineId"]);
                                    detailCmd.Parameters.AddWithValue("@dos", row["Dosage"].ToString());
                                    detailCmd.Parameters.AddWithValue("@dur", row["Duration"].ToString());
                                    detailCmd.Parameters.AddWithValue("@qty", row["Quantity"]);
                                    detailCmd.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                            MessageBox.Show(complete ? "Consultation Finalized and Appointment Completed!" : "Progress Saved.");
                            if (complete) this.Close();
                        }
                        catch (Exception innerEx)
                        {
                            trans.Rollback();
                            throw innerEx;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Error: " + ex.Message);
            }
        }

        private void PrintPrescription()
        {
            if (_dtPrescriptions.Rows.Count == 0)
            {
                MessageBox.Show("No medicines to print.");
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

                    // Header
                    g.DrawString("AL REHMAN CLINIC", fontTitle, Brushes.Navy, new RectangleF(0, y, ev.PageBounds.Width, 40), new StringFormat { Alignment = StringAlignment.Center });
                    y += 40;
                    g.DrawString("Near Civil Hospital, Shujabad, Multan | Tel: +92-300-1234567", fontBody, Brushes.DimGray, new RectangleF(0, y, ev.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
                    y += 35;

                    // Fetch Doctor Info
                    string docName = "";
                    string docSpec = "";
                    try {
                        using (var con = new SqlConnection(connectionString)) {
                            con.Open();
                            string query = "SELECT DoctorName, Specialization FROM Doctors WHERE DoctorName = (SELECT DoctorName FROM Visits WHERE VisitID = @vid)";
                            var cmd = new SqlCommand(query, con);
                            cmd.Parameters.AddWithValue("@vid", _visitId);
                            using (var dr = cmd.ExecuteReader()) {
                                if (dr.Read()) {
                                    docName = dr["DoctorName"].ToString();
                                    docSpec = dr["Specialization"].ToString();
                                }
                            }
                        }
                    } catch { }

                    if (!string.IsNullOrEmpty(docName)) {
                        g.DrawString($"Dr. {docName}", fontHeader, Brushes.Black, 100, y);
                        g.DrawString(docSpec, fontBody, Brushes.DimGray, 100, y + 20);
                        y += 45;
                    }

                    g.DrawLine(new Pen(Color.Navy, 2), 100, y, 700, y);
                    y += 30;

                    // Patient & Appt Info
                    string apptCode = lblAppointmentInfo.Text.Split('|')[0].Replace("Appt ID:", "").Trim();
                    g.DrawString($"Prescription Ref: {apptCode}", fontHeader, Brushes.Black, 100, y);
                    g.DrawString($"Date: {DateTime.Now:dd MMM yyyy}", fontHeader, Brushes.Black, 500, y);
                    y += 30;

                    g.DrawString($"Patient: {lblPatientName.Text} ({lblPatientDetails.Text.Split('|')[0].Trim()})", fontBody, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawString($"Diagnosis: {txtDiagnosis.Text}", fontBody, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawString($"Symptoms: {txtSymptoms.Text}", fontBody, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawString($"Advice/Notes: {txtNotes.Text}", fontBody, Brushes.Black, 100, y);
                    y += 40;

                    // Medications
                    g.DrawString("MEDICATIONS:", fontHeader, Brushes.Black, 100, y);
                    y += 25;
                    g.DrawLine(Pens.Black, 100, y, 700, y);
                    y += 10;

                    foreach (DataRow row in _dtPrescriptions.Rows)
                    {
                        string medicine = row["Medicine"].ToString();
                        string qty = row["Quantity"].ToString();
                        string dosage = row["Dosage"].ToString();
                        string duration = row["Duration"].ToString();
                        g.DrawString($"{medicine} ({qty}) — {dosage} ({duration})", fontBody, Brushes.Black, 120, y);
                        y += 20;
                    }

                    y += 60;
                    if (!string.IsNullOrEmpty(docName)) {
                        g.DrawString($"Dr. {docName}", fontBody, Brushes.Black, 100, y);
                        y += 20;
                    }
                    g.DrawString("Doctor's Signature: ____________________", fontBody, Brushes.Black, 100, y);
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                ppd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing Error: " + ex.Message);
            }
        }

        private string GeneratePrescriptionCode()
        {
            return "RX-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
        }
    }
}
