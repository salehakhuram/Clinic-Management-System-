using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class AddWalkInForm : Form
    {
        private string connectionString;
        public int SelectedPatientID { get; private set; }
        public int SelectedDoctorID { get; private set; }
        public string SelectedPatientName { get; private set; }
        public string SelectedDoctorName { get; private set; }
        public string GeneratedToken { get; private set; }
        public bool IsSuccess { get; private set; }

        private class ComboItem
        {
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString() => Text;
        }

        public AddWalkInForm(string connectionString, int nextToken)
        {
            this.connectionString = connectionString;
            this.GeneratedToken = nextToken.ToString("D3");
            this.IsSuccess = false;

            InitializeComponent();
            LoadDoctors();
            lblTokenValue.Text = "#" + GeneratedToken;
        }

        private void LoadDoctors()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                // Reverting to DoctorName as Name/LastName columns don't exist. 
                // Removed status filter to debug empty list issues.
                string query = "SELECT DoctorID, DoctorName, DoctorCode FROM Doctors WHERE Status NOT IN ('Inactive', 'Suspended')";
                using var cmd = new SqlCommand(query, con);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string code = reader["DoctorCode"]?.ToString() ?? "";
                    string name = reader["DoctorName"].ToString() ?? "";
                    cmbDoctor.Items.Add(new ComboItem { 
                        Text = string.IsNullOrEmpty(code) ? name : $"{name} ({code})",
                        Value = reader["DoctorID"] 
                    });
                }
                if (cmbDoctor.Items.Count > 0) cmbDoctor.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                // Fallback trial if concatenation fails (unlikely but safe)
                MessageBox.Show("Error loading doctors: " + ex.Message);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string search = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(search)) return;

            lstResults.Items.Clear();
            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                string query = "SELECT PatientName, Phone, PatientID, PatientCode FROM Patients WHERE PatientName LIKE @s OR Phone LIKE @s OR PatientCode LIKE @s";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@s", "%" + search + "%");
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string code = reader["PatientCode"]?.ToString() ?? reader["PatientID"].ToString();
                    lstResults.Items.Add(new ComboItem {
                        Text = $"{reader["PatientName"]} ({reader["Phone"]}) - {code}",
                        Value = reader["PatientID"]
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching patients: " + ex.Message);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null)
            {
                MessageBox.Show("Please select a patient from the results.");
                return;
            }

            if (cmbDoctor.SelectedItem == null)
            {
                MessageBox.Show("Please select a doctor.");
                return;
            }

            try
            {
                var selectedPatient = (ComboItem)lstResults.SelectedItem;
                var selectedDoctor = (ComboItem)cmbDoctor.SelectedItem;

                SelectedPatientID = Convert.ToInt32(selectedPatient.Value);
                SelectedDoctorID = Convert.ToInt32(selectedDoctor.Value);
                SelectedPatientName = selectedPatient.Text.Split('(')[0].Trim();
                // Strip code from doctor name if present "Name (Code)"
                string rawDocName = selectedDoctor.Text;
                if (rawDocName.Contains("(") && rawDocName.Contains(")"))
                    SelectedDoctorName = rawDocName.Substring(0, rawDocName.LastIndexOf("(")).Trim();
                else
                    SelectedDoctorName = rawDocName;

                RegisterVisit(SelectedPatientID, SelectedPatientName, SelectedDoctorID, SelectedDoctorName, GeneratedToken);
                
                IsSuccess = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error registering walk-in: " + ex.Message);
            }
        }

        private void RegisterVisit(int pid, string patientName, int docId, string doctorName, string token)
        {
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();
                using (var tran = con.BeginTransaction())
                {
                    try
                    {
                        // 1. Create Appointment Record (Walk-In)
                        string apptQ = @"
                            INSERT INTO Appointments (AppointmentCode, PatientId, PatientName, DoctorID, DoctorName, AppointmentDate, AppointmentTime, Status, AppointmentType, CreatedAt) 
                            OUTPUT INSERTED.AppointmentIntId
                            VALUES (@token, @pid, @pname, @docId, @doc, GETDATE(), CONVERT(TIME, GETDATE()), 'Checked-In', 'Walk-In', GETDATE())";
                        
                        int appointmentId = 0;
                        using (var cmdAppt = new SqlCommand(apptQ, con, tran))
                        {
                            cmdAppt.Parameters.AddWithValue("@pid", pid);
                            cmdAppt.Parameters.AddWithValue("@pname", patientName);
                            cmdAppt.Parameters.AddWithValue("@docId", docId);
                            cmdAppt.Parameters.AddWithValue("@doc", doctorName);
                            cmdAppt.Parameters.AddWithValue("@token", token);
                            appointmentId = (int)cmdAppt.ExecuteScalar();
                        }

                        // 2. Create Visit Record (Linked to Appointment)
                        string visitQ = @"
                            INSERT INTO Visits (PatientID, AppointmentIntId, DoctorId, DoctorName, TokenNumber, Status, VisitDate) 
                            VALUES (@pid, @appId, @docId, @doc, @token, 'WAITING', GETDATE())";
                        
                        using (var cmdVisit = new SqlCommand(visitQ, con, tran))
                        {
                            cmdVisit.Parameters.AddWithValue("@pid", pid);
                            cmdVisit.Parameters.AddWithValue("@appId", appointmentId);
                            cmdVisit.Parameters.AddWithValue("@docId", docId);
                            cmdVisit.Parameters.AddWithValue("@doc", doctorName);
                            cmdVisit.Parameters.AddWithValue("@token", token);
                            cmdVisit.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
