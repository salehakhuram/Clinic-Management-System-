using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class AppointmentsForm : Form
    {
        private string connectionString =
            @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        private string currentUser;
        private string loggedInUser; // Added this field based on the instruction's constructor change

        public AppointmentsForm(string loggedInUser)
        {
            this.loggedInUser = loggedInUser;
            InitializeComponent();
            SetupGrid();
            LoadAppointmentsGrid();
            LoadPatientComboBoxes();
            LoadDoctorComboBoxes();
            WireEvents();
            
            // Standardizing form setup

        }

        private void LoadPatientComboBoxes()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                // Allow Active or Under Treatment status for patients
                string query = "SELECT PatientId, PatientCode, PatientName FROM Patients WHERE Status != 'Inactive'"; 
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cmbPatientID.DataSource = dt.Copy();
                cmbPatientID.DisplayMember = "PatientCode"; 
                cmbPatientID.ValueMember = "PatientId";
                cmbPatientID.SelectedIndex = -1;

                cmbPatientName.DataSource = dt.Copy();
                cmbPatientName.DisplayMember = "PatientName";
                cmbPatientName.ValueMember = "PatientId";
                cmbPatientName.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients: " + ex.Message);
            }
        }

        private void LoadDoctorComboBoxes()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                // Allow all non-inactive doctors (Busy, On Duty, etc.)
                string query = "SELECT DoctorId, DoctorCode, DoctorName FROM Doctors WHERE Status != 'Inactive'"; 
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    // Fallback to all doctors if filtering leaves none (better for scheduling future appointments)
                    da.SelectCommand.CommandText = "SELECT DoctorId, DoctorCode, DoctorName FROM Doctors";
                    dt.Clear();
                    da.Fill(dt);
                }

                cmbDoctorID.DataSource = dt.Copy();
                cmbDoctorID.DisplayMember = "DoctorCode";
                cmbDoctorID.ValueMember = "DoctorId"; 
                cmbDoctorID.SelectedIndex = -1;

                cmbDoctorName.DataSource = dt.Copy();
                cmbDoctorName.DisplayMember = "DoctorName";
                cmbDoctorName.ValueMember = "DoctorId";
                cmbDoctorName.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading doctors: " + ex.Message);
            }
        }

        // ================= EVENTS =================
        private void WireEvents()
        {
            btnSave.Click += (s, e) => SaveAppointment();
            btnEdit.Click += (s, e) => EditAppointment();
            btnDelete.Click += (s, e) => DeleteAppointment();
            btnNew.Click += (s, e) => ClearForm();
            btnView.Click += (s, e) => new ReceptionistAppointmentsForm().ShowDialog();
            dgvAppointments.SelectionChanged += (s, e) => LoadSelectedAppointment();

            // SYNC PATIENT
            cmbPatientID.SelectionChangeCommitted += (s, e) => {
                if (cmbPatientID.SelectedValue != null) cmbPatientName.SelectedValue = cmbPatientID.SelectedValue;
            };
            cmbPatientName.SelectionChangeCommitted += (s, e) => {
                if (cmbPatientName.SelectedValue != null) cmbPatientID.SelectedValue = cmbPatientName.SelectedValue;
            };

            // SYNC DOCTOR
            cmbDoctorID.SelectionChangeCommitted += (s, e) => {
                if (cmbDoctorID.SelectedValue != null) cmbDoctorName.SelectedValue = cmbDoctorID.SelectedValue;
            };
            cmbDoctorName.SelectionChangeCommitted += (s, e) => {
                if (cmbDoctorName.SelectedValue != null) cmbDoctorID.SelectedValue = cmbDoctorName.SelectedValue;
            };

            if (txtSearch != null) txtSearch.TextChanged += (s, e) => SearchAppointments();
        }
        private void DgvAppointments_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // ignore header
            dgvAppointments.Rows[e.RowIndex].Selected = true;
            LoadSelectedAppointment(); // only load values into textboxes
        }

        private void SetupGrid()
        {
            if (dgvAppointments == null) return;

            dgvAppointments.AutoGenerateColumns = true;
            dgvAppointments.Columns.Clear();

        }

        // ================= LOAD GRID =================
        private void LoadAppointmentsGrid()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                // Joined with Patients to get PatientCode
                string query = @"
                    SELECT a.AppointmentCode, a.PatientId, p.PatientCode, a.PatientName, a.DoctorId, a.DoctorName, 
                           a.AppointmentDate, a.AppointmentTime, a.Status, a.Reason, 
                           a.CreatedBy, a.CreatedAt, a.UpdatedBy, a.UpdatedAt,
                           a.FollowUpRequired, a.PaymentStatus
                    FROM Appointments a
                    LEFT JOIN Patients p ON a.PatientId = p.PatientId
                    ORDER BY a.AppointmentDate DESC";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvAppointments.DataSource = dt;
                
                // Hide ID columns if they exist
                if (dgvAppointments.Columns["PatientId"] != null) dgvAppointments.Columns["PatientId"].Visible = false;
                if (dgvAppointments.Columns["DoctorId"] != null) dgvAppointments.Columns["DoctorId"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message);
            }
        }

        private void SearchAppointments()
        {
            if (dgvAppointments.DataSource == null) return;
            string filter = txtSearch.Text.Trim().Replace("'", "''");
            if (filter == "Search appointments..." || string.IsNullOrEmpty(filter)) {
                (dgvAppointments.DataSource as DataTable).DefaultView.RowFilter = "";
            } else {
                (dgvAppointments.DataSource as DataTable).DefaultView.RowFilter = 
                    string.Format("AppointmentCode LIKE '%{0}%' OR PatientName LIKE '%{0}%' OR PatientCode LIKE '%{0}%' OR DoctorName LIKE '%{0}%' OR Status LIKE '%{0}%' OR PaymentStatus LIKE '%{0}%' OR Reason LIKE '%{0}%'", filter);
            }
        }

        // ================= INSERT =================
        private void SaveAppointment()
        {
            if (!ValidateInputs()) return;

            try
            {
                using var con = new SqlConnection(connectionString);
                string query = @"
                INSERT INTO Appointments
                (
                    AppointmentCode,
                    PatientID,
                    PatientName,
                    DoctorID,
                    DoctorName,
                    AppointmentDate,
                    AppointmentTime,
                    Status,
                    Reason,
                    CreatedBy,
                    CreatedAt,
                    FollowUpRequired,
                    PaymentStatus
                )
                VALUES
                (
                    @AppointmentCode,
                    @PatientID,
                    @PatientName,
                    @DoctorID,
                    @DoctorName,
                    @AppointmentDate,
                    @AppointmentTime,
                    @Status,
                    @Reason,
                    @CreatedBy,
                    GETDATE(),
                    @FollowUpRequired,
                    @PaymentStatus
                )";

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentCode", FetchNextAppointmentCode());
                cmd.Parameters.AddWithValue("@PatientID", cmbPatientID.SelectedValue);
                cmd.Parameters.AddWithValue("@PatientName", cmbPatientName.Text);
                cmd.Parameters.AddWithValue("@DoctorID", cmbDoctorID.SelectedValue);
                cmd.Parameters.AddWithValue("@DoctorName", cmbDoctorName.Text);
                cmd.Parameters.AddWithValue("@AppointmentDate", dtpDate.Value.Date);
                cmd.Parameters.AddWithValue("@AppointmentTime", dtpTime.Value.TimeOfDay);
                cmd.Parameters.AddWithValue("@Status", cmbStatus.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Reason", txtReason.Text.Trim());
                cmd.Parameters.AddWithValue("@CreatedBy", Session.CurrentUser);
                cmd.Parameters.AddWithValue("@FollowUpRequired", cmbFollowUp.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@PaymentStatus", cmbPayment.SelectedItem.ToString());

                con.Open();
                cmd.ExecuteNonQuery();

                MessageBox.Show("Appointment saved successfully");
                LoadAppointmentsGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving appointment: " + ex.Message);
            }
        }

        // ================= UPDATE =================
        private void EditAppointment()
        {
            if (dgvAppointments.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select an appointment first");
                return;
            }

            if (!ValidateInputs()) return;

            // Update UI for feedback
            // Update UI for feedback
            txtUpdatedBy.Text = Session.CurrentUser;
            dtpUpdatedAt.Value = DateTime.Now;

            try
            {
                using var con = new SqlConnection(connectionString);
                string query = @"
            UPDATE Appointments SET
                PatientID=@PatientID,
                PatientName=@PatientName,
                DoctorID=@DoctorID,
                DoctorName=@DoctorName,
                AppointmentDate=@AppointmentDate,
                AppointmentTime=@AppointmentTime,
                Status=@Status,
                Reason=@Reason,
                UpdatedBy=@UpdatedBy,
                UpdatedAt=@UpdatedAt,
                FollowUpRequired=@FollowUpRequired,
                PaymentStatus=@PaymentStatus
            WHERE AppointmentCode=@AppointmentCode";

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentCode", txtAppointmentIntId.Text.Trim());
                cmd.Parameters.AddWithValue("@PatientID", cmbPatientID.SelectedValue); 
                cmd.Parameters.AddWithValue("@PatientName", cmbPatientName.Text);
                cmd.Parameters.AddWithValue("@DoctorID", cmbDoctorID.SelectedValue);
                cmd.Parameters.AddWithValue("@DoctorName", cmbDoctorName.Text);
                cmd.Parameters.AddWithValue("@AppointmentDate", dtpDate.Value.Date);
                cmd.Parameters.AddWithValue("@AppointmentTime", dtpTime.Value.TimeOfDay);
                cmd.Parameters.AddWithValue("@Status", cmbStatus.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Reason", txtReason.Text.Trim());
                cmd.Parameters.AddWithValue("@UpdatedBy", Session.CurrentUser);
                cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@FollowUpRequired", cmbFollowUp.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@PaymentStatus", cmbPayment.SelectedItem.ToString());
                
                con.Open();
                cmd.ExecuteNonQuery();
                
                MessageBox.Show("Appointment updated successfully");
                LoadAppointmentsGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating appointment: " + ex.Message);
            }
        }

        // ================= DELETE =================
        private void DeleteAppointment()
        {
            if (dgvAppointments.SelectedRows.Count == 0) return;

            string apptCode = txtAppointmentIntId.Text.Trim();
            if(string.IsNullOrEmpty(apptCode)) return;

            if (MessageBox.Show("Are you sure you want to delete this appointment? This will delete all related Prescriptions, Bills, and Visit records.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return;

            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                using var trans = con.BeginTransaction();

                try
                {
                    // 1. Get AppointmentIntId (PK) from Code
                    int apptId = 0;
                    using (var cmdGetId = new SqlCommand("SELECT AppointmentIntId FROM Appointments WHERE AppointmentCode = @Code", con, trans))
                    {
                        cmdGetId.Parameters.AddWithValue("@Code", apptCode);
                        var res = cmdGetId.ExecuteScalar();
                        if (res != null) apptId = Convert.ToInt32(res);
                    }

                    if (apptId > 0)
                    {
                        // 2. Delete Bill Services & Bills
                        using (var cmdBills = new SqlCommand(@"
                            DELETE FROM BillItems WHERE BillId IN (SELECT BillId FROM Bills WHERE AppointmentIntId = @ApptID);
                            DELETE FROM Bills WHERE AppointmentIntId = @ApptID;", con, trans))
                        {
                            cmdBills.Parameters.AddWithValue("@ApptID", apptId);
                            cmdBills.ExecuteNonQuery();
                        }

                        // 3. Delete Prescription Details & Prescriptions
                        using (var cmdPresc = new SqlCommand(@"
                            DELETE FROM PrescriptionDetails WHERE PrescriptionId IN (SELECT PrescriptionID FROM Prescriptions WHERE AppointmentIntId = @ApptID);
                            DELETE FROM Prescriptions WHERE AppointmentIntId = @ApptID;", con, trans))
                        {
                            cmdPresc.Parameters.AddWithValue("@ApptID", apptId);
                            cmdPresc.ExecuteNonQuery();
                        }

                        // 4. Delete Visits
                        using (var cmdVisit = new SqlCommand("DELETE FROM Visits WHERE AppointmentIntId = @ApptID", con, trans))
                        {
                            cmdVisit.Parameters.AddWithValue("@ApptID", apptId);
                            cmdVisit.ExecuteNonQuery();
                        }

                        // 5. Delete Appointment
                        using (var cmd = new SqlCommand("DELETE FROM Appointments WHERE AppointmentIntId = @ApptID", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@ApptID", apptId);
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show("Appointment and all related records deleted.");
                        LoadAppointmentsGrid();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Appointment not found.");
                    }
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting appointment: " + ex.Message);
            }
        }

        // ================= LOAD SELECTED =================
        private void LoadSelectedAppointment()
        {
            if (dgvAppointments.SelectedRows.Count == 0) return;

            var row = dgvAppointments.SelectedRows[0];
            DateTime date;

            if (row.Cells["AppointmentDate"].Value != DBNull.Value &&
                DateTime.TryParse(row.Cells["AppointmentDate"].Value.ToString(), out date) &&
                date >= dtpDate.MinDate)
            {
                dtpDate.Value = date;
            }
            else
            {
                dtpDate.Value = DateTime.Today; // fallback
            }

            txtAppointmentIntId.Text = row.Cells["AppointmentCode"].Value?.ToString() ?? "";
            
            string pid = row.Cells["PatientId"].Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(pid)) {
                 cmbPatientID.Text = pid; 
                 cmbPatientID.SelectedValue = pid; 
                 cmbPatientName.SelectedValue = pid;
            } else { cmbPatientID.SelectedIndex = -1; cmbPatientName.SelectedIndex = -1; }

            string did = row.Cells["DoctorId"].Value?.ToString() ?? "";
             if (!string.IsNullOrEmpty(did)) {
                 cmbDoctorID.Text = did;
                 cmbDoctorID.SelectedValue = did; 
                 cmbDoctorName.SelectedValue = did;
            } else { cmbDoctorID.SelectedIndex = -1; cmbDoctorName.SelectedIndex = -1; }
            dtpDate.Value = row.Cells["AppointmentDate"].Value != DBNull.Value
                ? Convert.ToDateTime(row.Cells["AppointmentDate"].Value)
                : DateTime.Today;
            dtpTime.Value = row.Cells["AppointmentTime"].Value != DBNull.Value
                ? DateTime.Today.Add((TimeSpan)row.Cells["AppointmentTime"].Value)
                : DateTime.Now;

            cmbStatus.SelectedItem = row.Cells["Status"].Value?.ToString() ?? cmbStatus.Items[0].ToString();
            txtReason.Text = row.Cells["Reason"].Value?.ToString() ?? "";
            cmbFollowUp.SelectedItem = row.Cells["FollowUpRequired"].Value?.ToString() ?? cmbFollowUp.Items[0].ToString();
            cmbPayment.SelectedItem = row.Cells["PaymentStatus"].Value?.ToString() ?? cmbPayment.Items[0].ToString();

            // Populate CreatedBy/UpdatedBy from Grid
            if (row.Cells["CreatedBy"].Value != DBNull.Value && txtCreatedBy != null)
                txtCreatedBy.Text = row.Cells["CreatedBy"].Value.ToString();
            if (row.Cells["CreatedAt"].Value != DBNull.Value && dtpCreatedAt != null)
                dtpCreatedAt.Value = Convert.ToDateTime(row.Cells["CreatedAt"].Value);
            
            if (row.Cells["UpdatedBy"].Value != DBNull.Value && txtUpdatedBy != null)
                txtUpdatedBy.Text = row.Cells["UpdatedBy"].Value.ToString();
            if (row.Cells["UpdatedAt"].Value != DBNull.Value && dtpUpdatedAt != null)
                dtpUpdatedAt.Value = Convert.ToDateTime(row.Cells["UpdatedAt"].Value);
            else if (txtUpdatedBy != null) txtUpdatedBy.Clear(); // Clear if null

        }


        // ================= VALIDATION =================
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(cmbPatientID.Text) || string.IsNullOrWhiteSpace(cmbDoctorID.Text))
            {
                MessageBox.Show("Patient ID and Doctor ID are required");
                return false;
            }

            // Verify Schedule
            if (!ValidateDoctorSchedule()) return false;

            return true;
        }

        private bool ValidateDoctorSchedule()
        {
            try
            {
                if (cmbDoctorID.SelectedValue == null) return true;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT WorkingDays, WorkingHours FROM Doctors WHERE DoctorID = @docId";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@docId", cmbDoctorID.SelectedValue);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string daysStr = reader["WorkingDays"]?.ToString() ?? "";
                                string hoursStr = reader["WorkingHours"]?.ToString() ?? "";

                                // 1. Check Day
                                if (!string.IsNullOrWhiteSpace(daysStr))
                                {
                                    string currentDay = dtpDate.Value.DayOfWeek.ToString();
                                    bool isWorkingDay = daysStr.Split(',').Any(d => d.Trim().Equals(currentDay, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (!isWorkingDay)
                                    {
                                        MessageBox.Show($"Doctor is not available on {currentDay}s.\nWorking Days: {daysStr}", "Schedule Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        return false;
                                    }
                                }

                                // 2. Check Time
                                if (!string.IsNullOrWhiteSpace(hoursStr) && hoursStr.Contains("-"))
                                {
                                    var parts = hoursStr.Split('-');
                                    if (parts.Length == 2)
                                    {
                                        if (TimeSpan.TryParse(parts[0].Trim(), out TimeSpan start) && 
                                            TimeSpan.TryParse(parts[1].Trim(), out TimeSpan end))
                                        {
                                            TimeSpan apptTime = dtpTime.Value.TimeOfDay;
                                            if (apptTime < start || apptTime > end)
                                            {
                                                MessageBox.Show($"Selected time is outside working hours.\nWorking Hours: {hoursStr}", "Schedule Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                return false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // specific parse errors shouldn't block, but let's log or show warning
                System.Diagnostics.Debug.WriteLine("Schedule Validation Error: " + ex.Message);
            }
            return true;
        }


        // ================= CLEAR =================
        private void ClearForm()
        {
            txtAppointmentIntId.Text = FetchNextAppointmentCode();
            cmbPatientID.SelectedIndex = -1;
            cmbPatientName.SelectedIndex = -1;
            cmbDoctorID.SelectedIndex = -1;
            cmbDoctorName.SelectedIndex = -1;
            txtReason.Clear();

            cmbStatus.SelectedIndex = 0;
            cmbPayment.SelectedIndex = 0;
            cmbFollowUp.SelectedIndex = 0;

            dtpDate.Value = DateTime.Today;
            dtpTime.Value = DateTime.Now;
            
            // Set CreatedBy to current user for new records
            if (txtCreatedBy != null) txtCreatedBy.Text = Session.CurrentUser;
            if (txtUpdatedBy != null) txtUpdatedBy.Clear();

        }

        private string FetchNextAppointmentCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ISNULL(IDENT_CURRENT('Appointments'), 0) + ISNULL(IDENT_INCR('Appointments'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        return "A" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "A---";
        }
    }
}
