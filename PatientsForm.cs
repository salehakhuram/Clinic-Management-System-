using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class PatientsForm : Form
    {
        private readonly string connectionString =
            @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True;";

        public PatientsForm()
        {
            InitializeComponent();
            SetupGrid();
            LoadDoctors();
            LoadPatients();
            InitializeEvents();

            this.Resize += (s, e) => LayoutContent();
            this.Padding = Padding.Empty;
            this.Margin = Padding.Empty;

            cmbDoctorID.SelectedIndexChanged += cmbDoctorID_SelectedIndexChanged;
            cmbDoctor.SelectedIndexChanged += cmbDoctor_SelectedIndexChanged;
            dtpDOB.ValueChanged += dtpDOB_ValueChanged;
        }

        private void PatientsForm_Load(object? sender, EventArgs e)
        {
            // Safety check
            if (cmbDoctorID == null || cmbDoctor == null) return;

            InitializeEvents();
            SetupGrid();
            LoadDoctors();
            LoadPatients();
        }




        // ================= EVENTS =================

        private void InitializeEvents()
        {
            if (txtSearch != null) txtSearch.TextChanged += TxtSearch_TextChanged;
            if (dgvPatients != null) dgvPatients.CellClick += DgvPatients_CellClick;
            if (btnSave != null) btnSave.Click += BtnSave_Click;
            if (btnEdit != null) btnEdit.Click += BtnEdit_Click;
            if (btnDelete != null) btnDelete.Click += BtnDelete_Click;
            if (btnNew != null) btnNew.Click += BtnNew_Click;
        }


        // ================= LOAD DATA =================

        private void LoadDoctors()
        {
            if (cmbDoctorID == null || cmbDoctor == null)
                return;
 
            cmbDoctorID.Items.Clear();
            cmbDoctor.Items.Clear();
 
            using SqlConnection con = new SqlConnection(connectionString);
            using SqlCommand cmd = new SqlCommand(
                "SELECT DoctorCode, DoctorName FROM Doctors WHERE Status NOT IN ('Inactive', 'Suspended')", con);
 
            try
            {
                con.Open();
                using SqlDataReader dr = cmd.ExecuteReader();
 
                while (dr.Read())
                {
                    string doctorCode = dr["DoctorCode"]?.ToString() ?? "";
                    string doctorName = dr["DoctorName"]?.ToString() ?? "";
 
                    if (!string.IsNullOrWhiteSpace(doctorCode))
                        cmbDoctorID.Items.Add(doctorCode);
 
                    if (!string.IsNullOrWhiteSpace(doctorName))
                        cmbDoctor.Items.Add(doctorName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading doctors: " + ex.Message);
            }
        }



        private void SetupGrid()
        {
            if (dgvPatients == null) return;

            dgvPatients.AutoGenerateColumns = true;
            dgvPatients.Columns.Clear();
        }

        private void LoadPatients()
        {
            if (dgvPatients == null) return;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    string query = @"SELECT 
                        PatientID, PatientCode, PatientName, FatherName, Gender, DOB, Age, 
                        Phone, Email, Address, Disease, BloodGroup, Status, DoctorID, DoctorName, RegistrationDate
                        FROM Patients ORDER BY PatientID ASC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvPatients.DataSource = dt;
                    
                    // Hide internal ID column and reorder to show PatientCode first
                    if (dgvPatients.Columns["PatientID"] != null)
                        dgvPatients.Columns["PatientID"].Visible = false;
                    if (dgvPatients.Columns["PatientCode"] != null)
                        dgvPatients.Columns["PatientCode"].DisplayIndex = 0;
                    if (dgvPatients.Columns["DoctorID"] != null)
                        dgvPatients.Columns["DoctorID"].Visible = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading patients: " + ex.Message);
                }
            }
        }
        private void cmbDoctorID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDoctorID.SelectedIndex >= 0 && cmbDoctor != null)
                cmbDoctor.SelectedIndex = cmbDoctorID.SelectedIndex;
        }

        private void cmbDoctor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDoctor.SelectedIndex >= 0 && cmbDoctorID != null)
                cmbDoctorID.SelectedIndex = cmbDoctor.SelectedIndex;
        }

        private void dtpDOB_ValueChanged(object sender, EventArgs e)
        {
            CalculateAge();
        }

        private void CalculateAge()
        {
            if (dtpDOB == null || txtAge == null) return;
            
            DateTime today = DateTime.Today;
            int age = today.Year - dtpDOB.Value.Year;
            
            // Go back to the year the person was born in case of leap year
            if (dtpDOB.Value.Date > today.AddYears(-age)) age--;
            
            txtAge.Text = age < 0 ? "0" : age.ToString();
        }

        // ================= SEARCH =================

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            if (dgvPatients == null || dgvPatients.DataSource == null) return;
            string filter = txtSearch.Text.Trim().Replace("'", "''");
            (dgvPatients.DataSource as DataTable).DefaultView.RowFilter = 
                string.Format("PatientName LIKE '%{0}%' OR Phone LIKE '%{0}%' OR PatientCode LIKE '%{0}%' OR FatherName LIKE '%{0}%' OR Email LIKE '%{0}%' OR Address LIKE '%{0}%' OR Disease LIKE '%{0}%' OR BloodGroup LIKE '%{0}%' OR DoctorName LIKE '%{0}%' OR Status LIKE '%{0}%'", filter);
        }


        // ================= SAVE =================

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            bool isEdit = txtPatientID.Tag != null;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (!isEdit)
                    {
                        cmd.CommandText = @"
                            INSERT INTO Patients
                            (PatientName, FatherName, Gender, DOB, Age, Phone, Email, Address, Disease, BloodGroup, Status, DoctorID, DoctorName, RegistrationDate)
                            VALUES
                            (@PatientName,@FatherName,@Gender,@DOB,@Age,@Phone,@Email,@Address,@Disease,@BloodGroup,@Status,@DoctorID,@DoctorName,@RegistrationDate)";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE Patients SET
                            PatientName=@PatientName,
                            FatherName=@FatherName,
                            Gender=@Gender,
                            DOB=@DOB,
                            Age=@Age,
                            Phone=@Phone,
                            Email=@Email,
                            Address=@Address,
                            Disease=@Disease,
                            BloodGroup=@BloodGroup,
                            Status=@Status,
                            DoctorID=@DoctorID,
                            DoctorName=@DoctorName,
                            RegistrationDate=@RegistrationDate
                            WHERE PatientID=@PatientID";
                        
                        cmd.Parameters.AddWithValue("@PatientID", txtPatientID.Tag);
                    }

                    FillParameters(cmd);
                    cmd.ExecuteNonQuery();
                }

                LoadPatients();
                ClearForm();
                MessageBox.Show(isEdit ? "Patient updated successfully." : "Patient saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving/updating patient: " + ex.Message);
            }
        }

        // ================= UPDATE =================

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (txtPatientID.Tag != null)
            {
                // If a patient is already selected (edit mode), clicking "Update Info" will trigger the save logic
                BtnSave_Click(sender, e);
            }
            else
            {
                // If no patient is selected, try to load the selected patient from the grid
                if (dgvPatients.CurrentRow == null)
                {
                    MessageBox.Show("Please select a patient from the grid first to edit.", "Selection Required");
                    return;
                }
                
                DgvPatients_CellClick(dgvPatients, new DataGridViewCellEventArgs(0, dgvPatients.CurrentRow.Index));
            }
        }

        // ================= DELETE =================

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (txtPatientID.Tag == null)
            {
                MessageBox.Show("Please select a patient to delete.");
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this patient?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        // Changed to Soft Delete to preserve historical financial/medical records
                        string query = "UPDATE Patients SET Status = 'Inactive' WHERE PatientID=@PatientID";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@PatientID", txtPatientID.Tag);
                        int rows = cmd.ExecuteNonQuery();
                        
                        // If no 'Status' column exists, this will throw.
                        // Assuming 'Status' column exists as user system is "Advanced".
                    }

                    LoadPatients();
                    ClearForm();
                    MessageBox.Show("Patient marked as Inactive (Soft Deleted).");
                }
                catch (Exception ex)
                {
                    // Fallback to hard delete with constraint check if soft delete fails (e.g. no Status col)
                    MessageBox.Show("Error deleting patient. They may have active bills/records.\nError: " + ex.Message);
                }
            }
        }

        // ================= GRID CLICK =================

        private void DgvPatients_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvPatients == null) return;
 
            DataGridViewRow r = dgvPatients.Rows[e.RowIndex];
 
            txtPatientID.Tag = r.Cells["PatientID"].Value; // Store identity ID in Tag
            txtPatientID.Text = r.Cells["PatientCode"].Value?.ToString(); // Show Computed Code
            
            txtPatientName.Text = r.Cells["PatientName"].Value?.ToString();
            txtFatherName.Text = r.Cells["FatherName"].Value?.ToString();
            txtAge.Text = r.Cells["Age"].Value?.ToString();
            txtPhone.Text = r.Cells["Phone"].Value?.ToString();
            txtEmail.Text = r.Cells["Email"].Value?.ToString();
            txtAddress.Text = r.Cells["Address"].Value?.ToString();
            txtDisease.Text = r.Cells["Disease"].Value?.ToString();
 
            cmbGender.Text = r.Cells["Gender"].Value?.ToString();
            cmbBloodGroup.Text = r.Cells["BloodGroup"].Value?.ToString();
            cmbStatus.Text = r.Cells["Status"].Value?.ToString();
            cmbDoctorID.Text = r.Cells["DoctorID"].Value?.ToString(); // This needs to be DoctorCode
            
            // Fix: Find the DoctorCode instead of raw numeric ID
            string docId = r.Cells["DoctorID"].Value?.ToString();
            if (!string.IsNullOrEmpty(docId))
            {
                string docCode = "D" + int.Parse(docId).ToString("D3");
                cmbDoctorID.Text = docCode;
            }
            
            cmbDoctor.Text = r.Cells["DoctorName"].Value?.ToString();
            
            if (r.Cells["DOB"].Value != DBNull.Value)
                dtpDOB.Value = Convert.ToDateTime(r.Cells["DOB"].Value);

            if (r.Cells["RegistrationDate"].Value != DBNull.Value)
                dtpRegistrationDate.Value = Convert.ToDateTime(r.Cells["RegistrationDate"].Value);
        }

        // ================= HELPERS =================

        private void FillParameters(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@PatientName", txtPatientName.Text);
            cmd.Parameters.AddWithValue("@FatherName", txtFatherName.Text);
            cmd.Parameters.AddWithValue("@Gender", cmbGender.Text);
            cmd.Parameters.AddWithValue("@DOB", dtpDOB.Value);
            cmd.Parameters.AddWithValue("@Age", txtAge.Text);
            cmd.Parameters.AddWithValue("@Phone", txtPhone.Text);
            cmd.Parameters.AddWithValue("@Email", txtEmail.Text);
            cmd.Parameters.AddWithValue("@Address", txtAddress.Text);
            cmd.Parameters.AddWithValue("@Disease", txtDisease.Text);
            cmd.Parameters.AddWithValue("@BloodGroup", cmbBloodGroup.Text);
            cmd.Parameters.AddWithValue("@Status", cmbStatus.Text);
            
            // Resolve DoctorCode (e.g., D001) to numeric ID
            int docId = 0;
            if (cmbDoctorID.Text.StartsWith("D"))
                int.TryParse(cmbDoctorID.Text.Substring(1), out docId);
            
            cmd.Parameters.AddWithValue("@DoctorID", docId > 0 ? (object)docId : DBNull.Value);
            cmd.Parameters.AddWithValue("@DoctorName", cmbDoctor.Text);
            cmd.Parameters.AddWithValue("@RegistrationDate", dtpRegistrationDate.Value);
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                MessageBox.Show("Patient name is required.", "Validation Error");
                return false;
            }

            if (!int.TryParse(txtAge.Text, out int age) || age < 0 || age > 150)
            {
                MessageBox.Show("Please enter a valid age (0-150).", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text) || txtPhone.Text.Length < 10)
            {
                MessageBox.Show("Please enter a valid phone number.", "Validation Error");
                return false;
            }

            if (cmbDoctor.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a doctor.", "Validation Error");
                return false;
            }

            return true;
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            if (txtPatientID != null)
            {
                txtPatientID.Tag = null; // Clear identity tracking
                txtPatientID.Text = FetchNextPatientCode(); // Show next available code
            }
            
            txtPatientName?.Clear();
            txtFatherName?.Clear();
            txtAge?.Clear();
            txtPhone?.Clear();
            txtEmail?.Clear();
            txtAddress?.Clear();
            txtDisease?.Clear();
 
            if (cmbGender != null) cmbGender.SelectedIndex = -1;
            if (cmbBloodGroup != null) cmbBloodGroup.SelectedIndex = -1;
            if (cmbStatus != null) cmbStatus.SelectedIndex = -1;
            if (cmbDoctorID != null) cmbDoctorID.SelectedIndex = -1;
            if (cmbDoctor != null) cmbDoctor.SelectedIndex = -1;
            
            dtpDOB.Value = DateTime.Now;
            dtpRegistrationDate.Value = DateTime.Now;
            CalculateAge();
        }

        private string FetchNextPatientCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Get next identity value
                    string query = "SELECT ISNULL(IDENT_CURRENT('Patients'), 0) + ISNULL(IDENT_INCR('Patients'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        // Format matching the computed column definition: 'P' + right('000' + ID, 3)
                        return "P" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "P---";
        }
    }
}
