using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class CheckInQueueForm : Form
    {
        private string currentUser;
        private class VisitItem
        {
            public int VisitID { get; set; }
            public string Token { get; set; }
            public string PatientName { get; set; }
            public string DoctorName { get; set; }
            public string VisitTime { get; set; }
            public string Status { get; set; }
            public string AppointmentCode { get; set; }
        }

        private readonly string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";

        public CheckInQueueForm(string username = "Receptionist")
        {
            this.currentUser = username;
            InitializeComponent();
            EnsureDatabaseSchema();
            InitializeData();
            LoadDoctors();
            LoadAppointments();
            WireEvents();
            RenderQueue();
        }

        private void EnsureDatabaseSchema()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string schemaSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Visits')
                        BEGIN
                            CREATE TABLE Visits (
                                VisitID INT PRIMARY KEY IDENTITY(1,1),
                                AppointmentIntId INT NULL,
                                DoctorId INT NULL,
                                PatientId INT NOT NULL,
                                DoctorName NVARCHAR(100),
                                TokenNumber NVARCHAR(20),
                                VisitDate DATETIME DEFAULT GETDATE(),
                                Status NVARCHAR(50) DEFAULT 'WAITING',
                                ChiefComplaint NVARCHAR(MAX),
                                Diagnosis NVARCHAR(MAX),
                                DoctorNotes NVARCHAR(MAX)
                            );
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Visits') AND name = 'DoctorName')
                                ALTER TABLE Visits ADD DoctorName NVARCHAR(100);
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Visits') AND name = 'TokenNumber')
                                ALTER TABLE Visits ADD TokenNumber NVARCHAR(20);
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Visits') AND name = 'ChiefComplaint')
                                ALTER TABLE Visits ADD ChiefComplaint NVARCHAR(MAX);
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Visits') AND name = 'DoctorNotes')
                                ALTER TABLE Visits ADD DoctorNotes NVARCHAR(MAX);
                            
                            -- Fix Constraints
                            ALTER TABLE Visits ALTER COLUMN AppointmentIntId INT NULL;
                            ALTER TABLE Visits ALTER COLUMN DoctorId INT NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppointmentAuditLogs')
                        BEGIN
                            CREATE TABLE AppointmentAuditLogs (
                                LogID INT PRIMARY KEY IDENTITY(1,1),
                                AppointmentIntId INT,
                                Action NVARCHAR(50),
                                PreviousDate DATETIME NULL,
                                PreviousTime TIME NULL,
                                NewDate DATETIME NULL,
                                NewTime TIME NULL,
                                Reason NVARCHAR(MAX),
                                PerformedBy NVARCHAR(100),
                                LogDate DATETIME DEFAULT GETDATE()
                            );
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Prescriptions')
                        BEGIN
                            CREATE TABLE Prescriptions (
                                PrescriptionID INT PRIMARY KEY IDENTITY(1,1),
                                VisitID INT FOREIGN KEY REFERENCES Visits(VisitID),
                                MedicineName NVARCHAR(200),
                                Dosage NVARCHAR(100),
                                Frequency NVARCHAR(100),
                                Duration NVARCHAR(100),
                                Instructions NVARCHAR(MAX),
                                Quantity INT DEFAULT 1,
                                UnitPrice DECIMAL(18,2) DEFAULT 0.0
                            );
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Prescriptions') AND name = 'Quantity')
                                ALTER TABLE Prescriptions ADD Quantity INT DEFAULT 1;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Prescriptions') AND name = 'UnitPrice')
                                ALTER TABLE Prescriptions ADD UnitPrice DECIMAL(18,2) DEFAULT 0.0;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrescriptionDetails')
                        BEGIN
                            CREATE TABLE PrescriptionDetails (
                                PrescriptionDetailId INT PRIMARY KEY IDENTITY(1,1),
                                PrescriptionId INT NOT NULL,
                                MedicineId INT,
                                Quantity NVARCHAR(50),
                                Dosage NVARCHAR(MAX),
                                Status NVARCHAR(50) DEFAULT 'Pending',
                                Duration NVARCHAR(100),
                                Instructions NVARCHAR(MAX)
                            );
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrescriptionDetails') AND name = 'Quantity')
                                ALTER TABLE PrescriptionDetails ADD Quantity NVARCHAR(50);
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrescriptionDetails') AND name = 'Status')
                                ALTER TABLE PrescriptionDetails ADD Status NVARCHAR(50) DEFAULT 'Pending';

                            -- Ensure MedicineId is nullable to prevent save errors
                            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrescriptionDetails') AND name = 'MedicineId')
                                ALTER TABLE PrescriptionDetails ALTER COLUMN MedicineId INT NULL;
                        END

                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'AppointmentType')
                                ALTER TABLE Appointments ADD AppointmentType NVARCHAR(50) DEFAULT 'Scheduled';
                        END
                    ";
                    var cmd = new SqlCommand(schemaSql, con);
                    cmd.ExecuteNonQuery();

                    SeedDummyData(con);
                }
            }
            catch (Exception ex) { MessageBox.Show("Database Init Error: " + ex.Message); }
        }

        private void SeedDummyData(SqlConnection con)
        {
            // Only seed if no patients exist
            string checkQ = "SELECT COUNT(*) FROM Patients";
            var checkCmd = new SqlCommand(checkQ, con);
            int count = (int)checkCmd.ExecuteScalar();

            if (count == 0)
            {
                string seedSql = @"
                    INSERT INTO Patients (PatientName, Phone, Age, Gender, Status) 
                    VALUES ('Test Patient (Male)', '0300-1122334', 45, 'Male', 'Active');
                    DECLARE @pid1 INT = SCOPE_IDENTITY();
                    
                    INSERT INTO Patients (PatientName, Phone, Age, Gender, Status) 
                    VALUES ('Test Patient (Female)', '0300-5566778', 29, 'Female', 'Active');
                    DECLARE @pid2 INT = SCOPE_IDENTITY();
                    
                    -- Visit for 'Dr. Sarah'
                    INSERT INTO Visits (PatientID, DoctorName, TokenNumber, Status, ChiefComplaint, VisitDate)
                    VALUES (@pid1, 'Dr. Sarah', 'T-001', 'WAITING', 'Lower back pain and stiffness.', GETDATE());
                    
                    -- Visit for 'Doctor' (Global test account)
                    INSERT INTO Visits (PatientID, DoctorName, TokenNumber, Status, ChiefComplaint, VisitDate)
                    VALUES (@pid2, 'Doctor', 'T-002', 'WITH_DOCTOR', 'Sore throat and persistent cough.', GETDATE());
                    
                    DECLARE @vid2 INT = SCOPE_IDENTITY();
                    INSERT INTO Prescriptions (VisitID, MedicineName, Dosage, Frequency, Duration, Quantity, UnitPrice)
                    VALUES (@vid2, 'Cough Syrup', '10ml', 'TDS', '5 Days', 1, 150.00);
                ";
                var seedCmd = new SqlCommand(seedSql, con);
                seedCmd.ExecuteNonQuery();
            }
        }

        private void InitializeData()
        {
            // Initial placeholders, will be filled by LoadDoctors/LoadAppointments
            cmbFilterStatus.Items.AddRange(new string[] { "All Statuses", "Waiting", "With Doctor", "Completed" });
            cmbFilterStatus.SelectedIndex = 0;

            cmbSort.Items.AddRange(new string[] { "Sort by Token", "Sort by Time" });
            cmbSort.SelectedIndex = 0;
        }

        private void LoadDoctors()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string q = "SELECT DoctorID, DoctorName FROM Doctors WHERE Status NOT IN ('Inactive', 'Suspended') ORDER BY DoctorName";
                    using (var cmd = new SqlCommand(q, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var doctors = new List<ComboItem>();
                        while (reader.Read())
                        {
                            doctors.Add(new ComboItem { 
                                Text = reader["DoctorName"].ToString(), 
                                Value = reader["DoctorID"] 
                            });
                        }

                        cmbDoctor.DisplayMember = "Text";
                        cmbDoctor.ValueMember = "Value";
                        cmbDoctor.DataSource = new BindingSource(doctors, null);

                        var filterDoctors = new List<ComboItem>();
                        filterDoctors.Add(new ComboItem { Text = "All Doctors", Value = 0 });
                        filterDoctors.AddRange(doctors);
                        cmbFilterDoctor.DisplayMember = "Text";
                        cmbFilterDoctor.ValueMember = "Value";
                        cmbFilterDoctor.DataSource = new BindingSource(filterDoctors, null);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading doctors: " + ex.Message); }
        }

        private void LoadAppointments(string searchTerm = "", int? filterDoctorId = null)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string q = @"SELECT A.AppointmentIntId, A.AppointmentCode, A.PatientId, P.PatientName, D.DoctorName, D.DoctorID, A.AppointmentTime 
                        FROM Appointments A
                        JOIN Patients P ON A.PatientId = P.PatientID
                        JOIN Doctors D ON A.DoctorID = D.DoctorID
                        WHERE A.Status IN ('Scheduled', 'Rescheduled', 'Waiting')";

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        q += " AND (P.PatientName LIKE @search OR P.Phone LIKE @search OR A.AppointmentCode LIKE @search)";
                    }
                    
                    if (filterDoctorId.HasValue && filterDoctorId.Value > 0)
                    {
                        q += " AND A.DoctorID = @docId";
                    }
                    else
                    {
                        // If no doctor selected, default to today's appointments to avoid overwhelming the combo
                        q += " AND CAST(A.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)";
                    }

                    q += " ORDER BY A.AppointmentTime";

                    using (var cmd = new SqlCommand(q, con))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm)) cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");
                        if (filterDoctorId.HasValue && filterDoctorId.Value > 0) cmd.Parameters.AddWithValue("@docId", filterDoctorId.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                var appts = new List<ComboItem>();
                appts.Add(new ComboItem { Text = "-- Select Appointment --", Value = null });
                while (reader.Read())
                {
                    string time = reader["AppointmentTime"] != DBNull.Value ? ((TimeSpan)reader["AppointmentTime"]).ToString(@"hh\:mm") : "N/A";
                    string code = reader["AppointmentCode"]?.ToString() ?? "";
                    appts.Add(new ComboItem { 
                        Text = $"{reader["PatientName"]} - {time} ({reader["DoctorName"]})", 
                        Value = new { 
                            Id = reader["AppointmentIntId"], 
                            PatientId = reader["PatientId"],
                            PatientName = reader["PatientName"],
                            DoctorName = reader["DoctorName"],
                            DoctorId = reader["DoctorID"]
                        } 
                    });
                        }

                            cmbAppointment.DisplayMember = "Text";
                            cmbAppointment.DataSource = new BindingSource(appts, null);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading appointments: " + ex.Message); }
        }

        private class ComboItem
        {
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString() => Text;
        }

        private void WireEvents()
        {
            btnCompleteCheckIn.Click += (s, e) => CheckInCurrent();
            cmbFilterDoctor.SelectedIndexChanged += (s, e) => RenderQueue();
            cmbFilterStatus.SelectedIndexChanged += (s, e) => RenderQueue();
            cmbSort.SelectedIndexChanged += (s, e) => RenderQueue();
            
            // Reload appointments when doctor is selected
            cmbDoctor.SelectedIndexChanged += (s, e) => {
                int? docId = null;
                if (cmbDoctor.SelectedItem is ComboItem item && item.Value is int id && id > 0) docId = id;
                LoadAppointments(txtPatientSearch.Text.Trim(), docId);
            };

            // Search Logic
            txtPatientSearch.TextChanged += (s, e) => {
                int? docId = null;
                if (cmbDoctor.SelectedItem is ComboItem item && item.Value is int id && id > 0) docId = id;
                LoadAppointments(txtPatientSearch.Text.Trim(), docId);
            };

            cmbAppointment.SelectedIndexChanged += (s, e) => {
                if (cmbAppointment.SelectedItem is ComboItem item && item.Value != null)
                {
                    dynamic val = item.Value;
                    // Auto-fill patient name if empty
                    if (string.IsNullOrEmpty(txtPatientSearch.Text)) txtPatientSearch.Text = val.PatientName;
                    
                    // Highlight in Queue if exists
                    foreach (Control c in flowQueue.Controls)
                    {
                        var card = c as Panel;
                        if (card != null && card.Tag != null && card.Tag.ToString() == val.PatientName)
                        {
                            card.BackColor = Color.FromArgb(254, 249, 195); 
                            flowQueue.ScrollControlIntoView(card);
                            break;
                        }
                    }
                }
            };

            btnAddWalkIn.Click += (s, e) => {
                using (var modal = new AddWalkInForm(connectionString, GetNextTokenCount() + 1))
                {
                    if (modal.ShowDialog() == DialogResult.OK && modal.IsSuccess)
                    {
                        RenderQueue();
                    }
                }
            };

            btnRegisterNew.Click += (s, e) => {
                var patientsForm = new PatientsForm();
                patientsForm.Show(); 
            };
            
            // Removed btnClearQueue click handler
            
            // Clear Filters Button (The single Clear button)
             if (btnResetFilters != null)
             {
                btnResetFilters.Click += (s, e) => {
                    txtPatientSearch.Clear();
                    cmbFilterDoctor.SelectedIndex = 0;
                    cmbFilterStatus.SelectedIndex = 0;
                    cmbSort.SelectedIndex = 0;
                    if(cmbDoctor.Items.Count > 0) cmbDoctor.SelectedIndex = 0; // Reset selection doctor too
                    LoadAppointments(); 
                    RenderQueue();
                };
             }
        }


        private void ClearQueueInDB()
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    var cmd = new SqlCommand("DELETE FROM Visits WHERE CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)", con);
                    cmd.ExecuteNonQuery();
                }
            } catch { }
        }

        private void CheckInCurrent()
        {
            string name = txtPatientSearch.Text.Trim();
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("Please enter patient name"); return; }

            int? apptId = null;
            int? docId = null;
            int? patientId = null;
            string docName = cmbDoctor.Text;

            if (cmbAppointment.SelectedItem is ComboItem item && item.Value != null)
            {
                dynamic val = item.Value;
                apptId = val.Id;
                patientId = val.PatientId;
                docId = val.DoctorId;
                docName = val.DoctorName;
            }
            else if (cmbDoctor.SelectedItem is ComboItem dItem)
            {
                if (dItem.Value is int id) docId = id;
                else if (int.TryParse(dItem.Value.ToString(), out int parsedId)) docId = parsedId;
            }

            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    using (var trans = con.BeginTransaction()) {
                        try {
                            // If we don't have a patientId from appointment, resolve/create it
                            if (!patientId.HasValue) {
                                patientId = GetOrCreatePatientId(name, con, trans);
                                
                                // AUTO-LINK: Check if this patient already has an appointment today for this doctor
                                // Using UPPER(Status) and filtering for today's date
                                string qCheckAppt = @"SELECT TOP 1 AppointmentIntId, DoctorID, DoctorName 
                                                     FROM Appointments 
                                                     WHERE (PatientId = @pid OR PatientName = @pname) 
                                                     AND UPPER(Status) IN ('SCHEDULED', 'RESCHEDULED', 'WAITING')
                                                     AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)";
                                using (var cmdCheck = new SqlCommand(qCheckAppt, con, trans))
                                {
                                    cmdCheck.Parameters.AddWithValue("@pid", patientId.Value);
                                    cmdCheck.Parameters.AddWithValue("@pname", name);
                                    using (var reader = cmdCheck.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            apptId = Convert.ToInt32(reader["AppointmentIntId"]);
                                            docId = Convert.ToInt32(reader["DoctorID"]);
                                            docName = reader["DoctorName"].ToString();
                                        }
                                    }
                                }
                            }

                            string token = (GetNextTokenCount(con, trans) + 1).ToString("D3");
                            
                            // Set status based on appointment existence
                            string initialStatus = apptId.HasValue ? "Checked-In" : "WAITING";

                            string q = "INSERT INTO Visits (PatientID, AppointmentIntId, DoctorId, DoctorName, TokenNumber, Status) VALUES (@pid, @aid, @did, @doc, @token, @status)";
                            using (var cmd = new SqlCommand(q, con, trans)) {
                                cmd.Parameters.AddWithValue("@pid", patientId);
                                cmd.Parameters.AddWithValue("@aid", (object)apptId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@did", (object)docId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@doc", docName);
                                cmd.Parameters.AddWithValue("@token", token);
                                cmd.Parameters.AddWithValue("@status", initialStatus);
                                cmd.ExecuteNonQuery();
                            }

                            if (apptId.HasValue) {
                                string upAppt = "UPDATE Appointments SET Status = 'Checked-In' WHERE AppointmentIntId = @aid";
                                using (var upCmd = new SqlCommand(upAppt, con, trans)) {
                                    upCmd.Parameters.AddWithValue("@aid", apptId.Value);
                                    upCmd.ExecuteNonQuery();
                                }

                                // 4. Audit Log
                                string qLog = "INSERT INTO AppointmentAuditLogs (AppointmentIntId, Action, PerformedBy) VALUES (@aid, 'Check-In', @user)";
                                using (var logCmd = new SqlCommand(qLog, con, trans)) {
                                    logCmd.Parameters.AddWithValue("@aid", apptId.Value);
                                    logCmd.Parameters.AddWithValue("@user", currentUser);
                                    logCmd.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                            txtPatientSearch.Clear();
                            LoadAppointments(); // Refresh appt list
                            RenderQueue();
                        } catch (Exception ex) {
                            trans.Rollback();
                            throw ex;
                        }
                    }
                }
            } catch (Exception ex) { MessageBox.Show("Check-in error: " + ex.Message); }
        }

        private int GetOrCreatePatientId(string name, SqlConnection con = null, SqlTransaction trans = null)
        {
            bool localCon = false;
            if (con == null) {
                con = new SqlConnection(connectionString);
                con.Open();
                localCon = true;
            }

            try {
                var cmd = new SqlCommand("SELECT PatientID FROM Patients WHERE PatientName = @name", con, trans);
                cmd.Parameters.AddWithValue("@name", name);
                object result = cmd.ExecuteScalar();
                if (result != null) return (int)result;

                var insCmd = new SqlCommand("INSERT INTO Patients (PatientName, Status) OUTPUT INSERTED.PatientID VALUES (@name, 'Active')", con, trans);
                insCmd.Parameters.AddWithValue("@name", name);
                return (int)insCmd.ExecuteScalar();
            } finally {
                if (localCon) con.Dispose();
            }
        }

        private int GetNextTokenCount(SqlConnection con = null, SqlTransaction trans = null)
        {
            bool localCon = false;
            if (con == null) {
                con = new SqlConnection(connectionString);
                con.Open();
                localCon = true;
            }

            try {
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Visits WHERE CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)", con, trans);
                return (int)cmd.ExecuteScalar();
            } finally {
                if (localCon) con.Dispose();
            }
        }

        private void RenderQueue()
        {
            flowQueue.SuspendLayout();
            flowQueue.Controls.Clear();

            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string filDoc = cmbFilterDoctor.Text ?? "All Doctors";
                    string filStat = cmbFilterStatus.SelectedItem?.ToString() ?? "All Statuses";

                    string q = "SELECT V.*, P.PatientName, A.AppointmentCode FROM Visits V JOIN Patients P ON V.PatientID = P.PatientID LEFT JOIN Appointments A ON V.AppointmentIntId = A.AppointmentIntId WHERE CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)";
                    if (filDoc != "All Doctors") q += " AND V.DoctorName = @doc";
                    if (filStat != "All Statuses") q += " AND V.Status = @stat";
                    q += " ORDER BY V.VisitID DESC";

                    var cmd = new SqlCommand(q, con);
                    if (filDoc != "All Doctors") cmd.Parameters.AddWithValue("@doc", filDoc);
                    if (filStat != "All Statuses")
                    {
                        string dbStat = filStat.ToUpper().Replace(" ", "_");
                        if (dbStat == "WITH_DOCTOR")
                            cmd.Parameters.AddWithValue("@stat", "WITH_DOCTOR"); // Try find by normalized underscore
                        else
                            cmd.Parameters.AddWithValue("@stat", dbStat);
                    }

                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            var item = new VisitItem {
                                VisitID = (int)dr["VisitID"],
                                Token = dr["TokenNumber"].ToString(),
                                PatientName = dr["PatientName"].ToString(),
                                DoctorName = dr["DoctorName"].ToString(),
                                VisitTime = Convert.ToDateTime(dr["VisitDate"]).ToString("hh:mm tt"),
                                Status = dr["Status"].ToString(),
                                AppointmentCode = dr["AppointmentCode"]?.ToString() ?? ""
                            };
                            flowQueue.Controls.Add(CreateQueueCard(item));
                        }
                    }
                }
            } catch (Exception ex) { MessageBox.Show("Load error: " + ex.Message); }

            if (flowQueue.Controls.Count == 0) ShowEmptyState();
            flowQueue.ResumeLayout();
        }

        private void ShowEmptyState()
        {
            Panel p = new Panel { Width = flowQueue.Width - 50, Height = 200 };
            Label l = new Label {
                Text = "No patients in queue matching the filters.",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(107, 114, 128),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            p.Controls.Add(l);
            flowQueue.Controls.Add(p);
        }

        private Panel CreateQueueCard(VisitItem item)
        {
            Panel card = new Panel {
                Width = 980,
                Height = 120, 
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 15),
                Cursor = Cursors.Default,
                Tag = item.PatientName // For Search Highlight
            };

            Color accent = item.Status.ToUpper().Replace(" ", "_") switch {
                "WAITING" => Color.FromArgb(249, 115, 22),    // Orange for Walk-in
                "CHECKED-IN" => Color.FromArgb(59, 130, 246), // Blue for Appointment
                "WITH_DOCTOR" => Color.FromArgb(139, 92, 246), 
                "COMPLETED" => Color.FromArgb(22, 163, 74), 
                _ => Color.Gray
            };

            card.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (Pen pShort = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.DrawPath(pShort, path);
                }
                using (SolidBrush b = new SolidBrush(accent)) {
                    e.Graphics.FillRectangle(b, 0, 10, 6, card.Height - 20);
                }
            };

            // Display appointment code if available, otherwise show token number
            string displayCode = !string.IsNullOrEmpty(item.AppointmentCode) 
                ? $"Appt: {item.AppointmentCode}" 
                : $"Token: #{item.Token}";
            
            Label lblToken = new Label { Text = displayCode, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55), AutoSize = true };
            Label lblName = new Label { Text = item.PatientName, Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(17, 24, 39), AutoSize = true };
            Label lblDetails = new Label { Text = $"{item.DoctorName} • Time: {item.VisitTime}", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(107, 114, 128), AutoSize = true };

            Label lblBadge = new Label {
                Text = item.Status.Replace("_", " "),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = accent,
                BackColor = Color.FromArgb(20, accent),
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true
            };

            // Disable Send if already With Doctor or Completed
            bool canSend = item.Status.ToUpper().Replace(" ", "_") == "WAITING";
            IconButton btnToDoc = new IconButton { 
                IconChar = IconChar.Stethoscope, 
                IconColor = canSend ? Color.FromArgb(139, 92, 246) : Color.LightGray, 
                Width = 40, Height = 40, 
                FlatStyle = FlatStyle.Flat, 
                Cursor = canSend ? Cursors.Hand : Cursors.No,
                Enabled = canSend
            };
            btnToDoc.FlatAppearance.BorderSize = 0;
            if (canSend) btnToDoc.Click += (s, e) => UpdateVisitStatus(item.VisitID, "WITH_DOCTOR");

            IconButton btnFinish = new IconButton { IconChar = IconChar.Check, IconColor = Color.FromArgb(22, 163, 74), Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnFinish.FlatAppearance.BorderSize = 0;
            btnFinish.Click += (s, e) => UpdateVisitStatus(item.VisitID, "COMPLETED");

            IconButton btnDelete = new IconButton { IconChar = IconChar.Times, IconColor = Color.FromArgb(225, 29, 72), Width = 40, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) => {
                if (MessageBox.Show("Remove patient from queue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    DeleteVisit(item.VisitID);
                }
            };

            Panel pnlInfo = new Panel { Dock = DockStyle.Left, Width = 600, Padding = new Padding(15) };
            pnlInfo.Controls.AddRange(new Control[] { lblName, lblDetails, lblToken });
            lblName.Location = new Point(220, 30);
            lblDetails.Location = new Point(220, 65);
            lblToken.Location = new Point(25, 33);

            Panel pnlActions = new Panel { Dock = DockStyle.Right, Width = 400, Padding = new Padding(10) }; // Widened slightly
            pnlActions.Controls.AddRange(new Control[] { lblBadge, btnToDoc, btnFinish, btnDelete });
            
            // Align actions more to the right using Flow logic simulation or absolute positioning
            int badgeX = 20;
            int btnStart = 200; 
            int spacing = 50;

            lblBadge.Location = new Point(badgeX, 45); 
            btnToDoc.Location = new Point(btnStart, 40); 
            btnFinish.Location = new Point(btnStart + spacing, 40); 
            btnDelete.Location = new Point(btnStart + spacing * 2, 40); 

            card.Controls.Add(pnlInfo);
            card.Controls.Add(pnlActions);
            return card;
        }

        private void UpdateVisitStatus(int vid, string status)
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    using (var trans = con.BeginTransaction())
                    {
                        try {
                            // 1. Update Visits
                            var cmd = new SqlCommand("UPDATE Visits SET Status = @stat WHERE VisitID = @vid", con, trans);
                            cmd.Parameters.AddWithValue("@stat", status);
                            cmd.Parameters.AddWithValue("@vid", vid);
                            cmd.ExecuteNonQuery();

                            // 2. Update linked Appointment if exists
                            string qSync = @"UPDATE Appointments SET Status = @apptStat 
                                            WHERE AppointmentIntId = (SELECT AppointmentIntId FROM Visits WHERE VisitID = @vid)";
                            string apptStat = status == "WITH_DOCTOR" ? "With Doctor" : 
                                              status == "COMPLETED" ? "Completed" : "Checked-In";
                            
                            var cmdSync = new SqlCommand(qSync, con, trans);
                            cmdSync.Parameters.AddWithValue("@apptStat", apptStat);
                            cmdSync.Parameters.AddWithValue("@vid", vid);
                            cmdSync.ExecuteNonQuery();

                            // 3. Update Doctor Readiness Status automatically
                            string qDoc = @"UPDATE Doctors SET Status = @docStat 
                                           WHERE DoctorName = (SELECT DoctorName FROM Visits WHERE VisitID = @vid)";
                            string docStat = (status == "WITH_DOCTOR") ? "BUSY" : "ON DUTY";
                            
                            var cmdDoc = new SqlCommand(qDoc, con, trans);
                            cmdDoc.Parameters.AddWithValue("@docStat", docStat);
                            cmdDoc.Parameters.AddWithValue("@vid", vid);
                            cmdDoc.ExecuteNonQuery();

                            trans.Commit();
                        } catch { trans.Rollback(); throw; }
                    }
                }
                RenderQueue();
            } catch (Exception ex) { MessageBox.Show("Status Sync Failed: " + ex.Message); }
        }

        private void DeleteVisit(int vid)
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    var cmd = new SqlCommand("DELETE FROM Visits WHERE VisitID = @vid", con);
                    cmd.Parameters.AddWithValue("@vid", vid);
                    cmd.ExecuteNonQuery();
                }
                RenderQueue();
            } catch { }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();
            if (radius == 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
