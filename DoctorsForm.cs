using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class DoctorsForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        public DoctorsForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadStaffComboBoxes();
            WireEvents();
            LoadDoctorsGrid();
        }

        private void LoadStaffComboBoxes()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                string query = "SELECT StaffId, StaffName, StaffCode FROM Staff WHERE Status = 'Active'"; // Assuming matching schema
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                // Setup ID Combo
                cmbStaffId.DataSource = dt.Copy();
                cmbStaffId.DisplayMember = "StaffCode"; // Display S001 etc.
                cmbStaffId.ValueMember = "StaffId";  // Internal ID
                cmbStaffId.SelectedIndex = -1;

                // Setup Name Combo
                cmbStaffName.DataSource = dt.Copy();
                cmbStaffName.DisplayMember = "StaffName";
                cmbStaffName.ValueMember = "StaffId"; // Same ValueMember for sync
                cmbStaffName.SelectedIndex = -1;
            }
            catch (Exception ex) 
            {
                // Soft fail safely if table issues
            }
        }

        private void WireEvents()
        {
            btnSave.Click += (s, e) => SaveDoctor();
            btnEdit.Click += (s, e) => EditDoctor();
            btnDelete.Click += (s, e) => DeleteDoctor();
            btnNew.Click += (s, e) => ClearForm();
            btnRefresh.Click += (s, e) => LoadDoctorsGrid();
            dgvDoctors.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) LoadSelectedDoctor(); };
            txtSearch.TextChanged += (s, e) => SearchDoctors();

            // SYNC LOGIC
            cmbStaffId.SelectionChangeCommitted += (s, e) => {
                if (cmbStaffId.SelectedValue != null)
                    cmbStaffName.SelectedValue = cmbStaffId.SelectedValue;
            };

            cmbStaffName.SelectionChangeCommitted += (s, e) => {
                if (cmbStaffName.SelectedValue != null)
                    cmbStaffId.SelectedValue = cmbStaffName.SelectedValue;
            };
        }

        private void SearchDoctors()
        {
            if (dgvDoctors.DataSource == null) return;
            string filter = txtSearch.Text.Trim().Replace("'", "''");
            if (filter == "Search doctors..." || string.IsNullOrEmpty(filter)) {
                (dgvDoctors.DataSource as DataTable).DefaultView.RowFilter = "";
            } else {
                (dgvDoctors.DataSource as DataTable).DefaultView.RowFilter = 
                    $"DoctorName LIKE '%{filter}%' OR Specialization LIKE '%{filter}%' OR DoctorCode LIKE '%{filter}%' OR Qualification LIKE '%{filter}%' OR Department LIKE '%{filter}%' OR Status LIKE '%{filter}%' OR RoomNo LIKE '%{filter}%' OR StaffName LIKE '%{filter}%'";
            }
        }


        private void SetupGrid()
        {
            if (dgvDoctors == null) return;

            dgvDoctors.AutoGenerateColumns = false;
            dgvDoctors.Columns.Clear();

            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoctorCode", DataPropertyName = "DoctorCode", HeaderText = "ID", Width = 80 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoctorName", DataPropertyName = "DoctorName", HeaderText = "Doctor Name", Width = 180 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Specialization", DataPropertyName = "Specialization", HeaderText = "Specialization", Width = 150 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoomNo", DataPropertyName = "RoomNo", HeaderText = "Room/OPD", Width = 100 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Department", DataPropertyName = "Department", HeaderText = "Department", Width = 150 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "Status", Width = 100 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Experience", DataPropertyName = "Experience", HeaderText = "Exp", Width = 80 });

            // Hidden columns
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoctorId", DataPropertyName = "DoctorId", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qualification", DataPropertyName = "Qualification", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkingDays", DataPropertyName = "WorkingDays", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "WorkingHours", DataPropertyName = "WorkingHours", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Salary", DataPropertyName = "Salary", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "JoiningDate", DataPropertyName = "JoiningDate", HeaderText = "Joining Date", Width = 100 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remarks", DataPropertyName = "Remarks", HeaderText = "Remarks", Width = 150 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "ConsultationFee", DataPropertyName = "ConsultationFee", HeaderText = "Fee", Width = 80 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffId", DataPropertyName = "StaffId", Visible = false });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffCode", DataPropertyName = "StaffCode", HeaderText = "Staff Code", Width = 80 });
            dgvDoctors.Columns.Add(new DataGridViewTextBoxColumn { Name = "StaffName", DataPropertyName = "StaffName", HeaderText = "Staff Name", Width = 150 });
        }

        private void LoadDoctorsGrid()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                var dt = new DataTable();
                using var da = new SqlDataAdapter(@"
                    SELECT d.*, s.StaffName as StaffName, s.StaffCode 
                    FROM Doctors d 
                    LEFT JOIN Staff s ON d.StaffId = s.StaffId", con);
                da.Fill(dt);

                dgvDoctors.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading doctors: {ex.Message}");
            }
        }


        private void SaveDoctor()
        {
            if (!ValidateInputs()) return;

            try
            {
                using var con = new SqlConnection(connectionString);
                string query = @"INSERT INTO Doctors 
                    (DoctorName, Specialization, Qualification, Experience, WorkingDays, WorkingHours, Department, Status, StaffId, JoiningDate, Salary, ConsultationFee, Remarks, RoomNo) 
                    VALUES (@DoctorName, @Specialization, @Qualification, @Experience, @WorkingDays, @WorkingHours, @Department, @Status, @StaffId, @JoiningDate, @Salary, @ConsultationFee, @Remarks, @RoomNo)";

                using var cmd = new SqlCommand(query, con);
                AddParameters(cmd);

                con.Open();
                cmd.ExecuteNonQuery();

                MessageBox.Show("Doctor added successfully!");
                LoadDoctorsGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving doctor: {ex.Message}");
            }
        }

        private void EditDoctor()
        {
            if (dgvDoctors.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a doctor to edit.");
                return;
            }

            if (!ValidateInputs()) return;

            try
            {
                var row = dgvDoctors.SelectedRows[0];
                string doctorId = row.Cells["DoctorId"].Value.ToString();

                using var con = new SqlConnection(connectionString);

                string query = @"UPDATE Doctors SET 
                    DoctorName=@DoctorName, Specialization=@Specialization, Qualification=@Qualification, 
                    Experience=@Experience, WorkingDays=@WorkingDays, WorkingHours=@WorkingHours, 
                    Department=@Department, Status=@Status, StaffId=@StaffId, JoiningDate=@JoiningDate, Salary=@Salary, 
                    ConsultationFee=@ConsultationFee, Remarks=@Remarks, RoomNo=@RoomNo 
                    WHERE DoctorId=@Id"; // Use @Id for consistency or @DoctorId

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", doctorId);
                AddParameters(cmd);

                con.Open();
                cmd.ExecuteNonQuery();

                MessageBox.Show("Doctor updated successfully!");
                LoadDoctorsGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating doctor: {ex.Message}");
            }
        }


        private void DeleteDoctor()
        {
            if (dgvDoctors.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a doctor to delete.");
                return;
            }

            try
            {
                var doctorId = dgvDoctors.SelectedRows[0].Cells["DoctorId"].Value.ToString();
                using var con = new SqlConnection(connectionString);
                string query = "UPDATE Doctors SET Status='Inactive' WHERE DoctorId=@Id"; 
                
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", doctorId);

                con.Open();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Doctor marked as Inactive/Retired.");
                LoadDoctorsGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting doctor: {ex.Message}");
            }
        }


        #region Helper Methods

        private void AddParameters(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@DoctorName", txtName.Text.Trim());
            cmd.Parameters.AddWithValue("@Specialization", cmbSpecialization.SelectedItem?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@Qualification", txtQualification.Text.Trim());
            cmd.Parameters.AddWithValue("@Experience", txtExperience.Text.Trim());
            string workingDays = string.Join(",", clbDays.CheckedItems.Cast<string>());
            cmd.Parameters.AddWithValue("@WorkingDays", workingDays);
            cmd.Parameters.AddWithValue("@WorkingHours", txtWorkingHours.Text.Trim());
            cmd.Parameters.AddWithValue("@Department", cmbDepartment.SelectedItem?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@Status", cmbStatus.SelectedItem?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@Salary", decimal.TryParse(txtSalary.Text, out decimal sal) ? sal : 0);
            cmd.Parameters.AddWithValue("@ConsultationFee", decimal.TryParse(txtConsultationFee.Text, out decimal fee) ? fee : 0);
            cmd.Parameters.AddWithValue("@JoiningDate", dtpJoining.Value);
            cmd.Parameters.AddWithValue("@Remarks", txtRemarks.Text.Trim());
            cmd.Parameters.AddWithValue("@RoomNo", txtRoomNo.Text.Trim());
            
            if (cmbStaffId.SelectedValue != null && int.TryParse(cmbStaffId.SelectedValue.ToString(), out int sid))
                cmd.Parameters.AddWithValue("@StaffId", sid);
            else
                cmd.Parameters.AddWithValue("@StaffId", DBNull.Value);
        }



        private void ClearForm()
        {
            txtDoctorID.Text = FetchNextDoctorCode();
            txtName.Clear();
            cmbSpecialization.SelectedIndex = -1;
            txtQualification.Clear();
            txtExperience.Clear();
            txtWorkingHours.Clear();
            txtSalary.Clear();
            txtConsultationFee.Clear();
            txtRemarks.Clear();
            txtRoomNo.Clear();
            cmbStaffId.SelectedIndex = -1;
            cmbStaffName.SelectedIndex = -1;
            for (int i = 0; i < clbDays.Items.Count; i++) clbDays.SetItemChecked(i, false);
            cmbDepartment.SelectedIndex = -1;
            cmbStatus.SelectedIndex = -1;
            dtpJoining.Value = DateTime.Now;
        }

        private void LoadSelectedDoctor()
        {
            if (dgvDoctors.SelectedRows.Count == 0) return;

            var row = dgvDoctors.SelectedRows[0];
            txtDoctorID.Text = row.Cells["DoctorCode"].Value?.ToString() ?? "";
            txtName.Text = row.Cells["DoctorName"].Value?.ToString() ?? "";
            cmbSpecialization.SelectedItem = row.Cells["Specialization"].Value?.ToString() ?? "";
            txtQualification.Text = row.Cells["Qualification"]?.Value?.ToString() ?? "";
            txtExperience.Text = row.Cells["Experience"]?.Value?.ToString() ?? "";
            txtWorkingHours.Text = row.Cells["WorkingHours"]?.Value?.ToString() ?? "";
            txtSalary.Text = row.Cells["Salary"]?.Value?.ToString() ?? "";
            txtConsultationFee.Text = row.Cells["ConsultationFee"]?.Value?.ToString() ?? "";
            txtRemarks.Text = row.Cells["Remarks"]?.Value?.ToString() ?? "";
            txtRoomNo.Text = (row.Cells["RoomNo"]?.Value != DBNull.Value) ? row.Cells["RoomNo"].Value.ToString() : "";
            
            for (int i = 0; i < clbDays.Items.Count; i++) clbDays.SetItemChecked(i, false);
            var daysStr = row.Cells["WorkingDays"]?.Value?.ToString();
            if (!string.IsNullOrEmpty(daysStr))
            {
                var days = daysStr.Split(',');
                for (int i = 0; i < clbDays.Items.Count; i++)
                {
                    if (Array.Exists(days, d => d.Trim() == clbDays.Items[i].ToString()))
                        clbDays.SetItemChecked(i, true);
                }
            }
            cmbDepartment.SelectedItem = row.Cells["Department"].Value?.ToString() ?? "";
            cmbStatus.SelectedItem = row.Cells["Status"].Value?.ToString() ?? "";
            if (row.Cells["JoiningDate"].Value != DBNull.Value && DateTime.TryParse(row.Cells["JoiningDate"].Value.ToString(), out DateTime joiningDate) && joiningDate >= dtpJoining.MinDate)
                dtpJoining.Value = joiningDate;
            else 
                dtpJoining.Value = DateTime.Now;

            if (row.Cells["StaffId"].Value != DBNull.Value)
            {
                int staffId = Convert.ToInt32(row.Cells["StaffId"].Value);
                cmbStaffId.SelectedValue = staffId;
                cmbStaffName.SelectedValue = staffId;
            }
            else
            {
                cmbStaffId.SelectedIndex = -1;
                cmbStaffName.SelectedIndex = -1;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Doctor Name is required.");
                return false;
            }
            return true;
        }

        private string FetchNextDoctorCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ISNULL(IDENT_CURRENT('Doctors'), 0) + ISNULL(IDENT_INCR('Doctors'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        return "D" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "D---";
        }

        #endregion
    }
}
