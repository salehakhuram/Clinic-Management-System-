using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public class RescheduleModal : Form
    {
        private string appointmentId;
        private string connectionString;
        private string currentUser;
        
        private DateTimePicker dtpDate = null!, dtpTime = null!;
        private ComboBox cmbDoctor = null!;
        private TextBox txtReason = null!;
        private Button btnConfirm = null!, btnCancel = null!;

        public RescheduleModal(string appointmentId, string connectionString, string currentUser)
        {
            this.appointmentId = appointmentId;
            this.connectionString = connectionString;
            this.currentUser = currentUser;

            this.Text = "Reschedule Appointment";
            this.Size = new Size(450, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            InitializeControls();
            LoadDoctors();
            LoadExistingData();
        }

        private void InitializeControls()
        {
            int y = 20;

            Label lblTitle = new Label { Text = $"Reschedule Appointment: {appointmentId}", Font = new Font("Segoe UI Bold", 12), Location = new Point(25, y), Size = new Size(400, 30) };
            y += 50;

            Label lblDate = new Label { Text = "New Date:", Font = new Font("Segoe UI", 10), Location = new Point(25, y), AutoSize = true };
            dtpDate = new DateTimePicker { Location = new Point(25, y + 25), Size = new Size(380, 25), Format = DateTimePickerFormat.Short, MinDate = DateTime.Today };
            y += 65;

            Label lblTime = new Label { Text = "New Time:", Font = new Font("Segoe UI", 10), Location = new Point(25, y), AutoSize = true };
            dtpTime = new DateTimePicker { Location = new Point(25, y + 25), Size = new Size(380, 25), Format = DateTimePickerFormat.Time, ShowUpDown = true };
            y += 65;

            Label lblDoctor = new Label { Text = "Change Doctor (Optional):", Font = new Font("Segoe UI", 10), Location = new Point(25, y), AutoSize = true };
            cmbDoctor = new ComboBox { Location = new Point(25, y + 25), Size = new Size(380, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            y += 65;

            Label lblReason = new Label { Text = "Reason for Rescheduling (Required):", Font = new Font("Segoe UI", 10), Location = new Point(25, y), AutoSize = true };
            txtReason = new TextBox { Multiline = true, Location = new Point(25, y + 25), Size = new Size(380, 60), Font = new Font("Segoe UI", 10) };
            y += 100;

            btnConfirm = new Button { 
                Text = "Confirm Reschedule", 
                Location = new Point(225, y), 
                Size = new Size(180, 35), 
                BackColor = Color.FromArgb(59, 130, 246), 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button { 
                Text = "Cancel", 
                Location = new Point(25, y), 
                Size = new Size(180, 35), 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblTitle, lblDate, dtpDate, lblTime, dtpTime, lblDoctor, cmbDoctor, lblReason, txtReason, btnConfirm, btnCancel });
        }

        private void LoadDoctors()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                string query = "SELECT DoctorID, DoctorName FROM Doctors";
                using var cmd = new SqlCommand(query, con);
                using var reader = cmd.ExecuteReader();
                
                cmbDoctor.Items.Add(new { ID = "", Name = "-- Keep Current --" });
                while (reader.Read())
                {
                    cmbDoctor.Items.Add(new { ID = reader["DoctorID"].ToString(), Name = reader["DoctorName"].ToString() });
                }
                cmbDoctor.DisplayMember = "Name";
                cmbDoctor.SelectedIndex = 0;
            }
            catch { /* Ignore if fails */ }
        }

        private void LoadExistingData()
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                string query = "SELECT AppointmentDate, AppointmentTime FROM Appointments WHERE AppointmentIntId = @ID";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", appointmentId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Pre-fill if possible
                }
            }
            catch { }
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text)) { MessageBox.Show("Please provide a reason."); return; }

            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                using var trans = con.BeginTransaction();
                
                try
                {
                    // Get current data for audit
                    DateTime oldDate = DateTime.Today;
                    TimeSpan oldTime = TimeSpan.Zero;
                    using (var cmdOld = new SqlCommand("SELECT AppointmentDate, AppointmentTime FROM Appointments WHERE AppointmentIntId = @ID", con, trans))
                    {
                        cmdOld.Parameters.AddWithValue("@ID", appointmentId);
                        using var reader = cmdOld.ExecuteReader();
                        if (reader.Read()) { 
                            oldDate = Convert.ToDateTime(reader["AppointmentDate"]); 
                            oldTime = (TimeSpan)reader["AppointmentTime"]; 
                        }
                    }

                    // Update
                    string updateQuery = @"UPDATE Appointments SET 
                                          AppointmentDate = @Date, 
                                          AppointmentTime = @Time, 
                                          Status = 'Rescheduled', 
                                          UpdatedAt = GETDATE(), 
                                          UpdatedBy = @User";
                    
                    object? selectedItem = cmbDoctor.SelectedItem;
                    string? selectedDocID = null;
                    string? selectedDocName = null;
                    
                    if (selectedItem != null)
                    {
                        // Using reflection or dynamic safely with null checks
                        var props = selectedItem.GetType().GetProperties();
                        selectedDocID = selectedItem.GetType().GetProperty("ID")?.GetValue(selectedItem)?.ToString();
                        selectedDocName = selectedItem.GetType().GetProperty("Name")?.GetValue(selectedItem)?.ToString();
                    }

                    if (!string.IsNullOrEmpty(selectedDocID))
                    {
                        updateQuery += ", DoctorID = @DocID, DoctorName = @DocName";
                    }
                    updateQuery += " WHERE AppointmentIntId = @ID";

                    using var cmdUpdate = new SqlCommand(updateQuery, con, trans);
                    cmdUpdate.Parameters.AddWithValue("@Date", dtpDate.Value.Date);
                    cmdUpdate.Parameters.AddWithValue("@Time", dtpTime.Value.TimeOfDay);
                    cmdUpdate.Parameters.AddWithValue("@User", currentUser);
                    cmdUpdate.Parameters.AddWithValue("@ID", appointmentId);
                    
                    if (!string.IsNullOrEmpty(selectedDocID))
                    {
                        cmdUpdate.Parameters.AddWithValue("@DocID", selectedDocID);
                        cmdUpdate.Parameters.AddWithValue("@DocName", selectedDocName ?? string.Empty);
                    }
                    cmdUpdate.ExecuteNonQuery();

                    // Log
                    string logQuery = @"INSERT INTO AppointmentAuditLogs (AppointmentIntId, Action, PreviousDate, PreviousTime, NewDate, NewTime, Reason, PerformedBy) 
                                       VALUES (@ID, 'Reschedule', @OldDate, @OldTime, @NewDate, @NewTime, @Reason, @User)";
                    using var cmdLog = new SqlCommand(logQuery, con, trans);
                    cmdLog.Parameters.AddWithValue("@ID", appointmentId);
                    cmdLog.Parameters.AddWithValue("@OldDate", oldDate);
                    cmdLog.Parameters.AddWithValue("@OldTime", oldTime);
                    cmdLog.Parameters.AddWithValue("@NewDate", dtpDate.Value.Date);
                    cmdLog.Parameters.AddWithValue("@NewTime", dtpTime.Value.TimeOfDay);
                    cmdLog.Parameters.AddWithValue("@Reason", txtReason.Text.Trim());
                    cmdLog.Parameters.AddWithValue("@User", currentUser);
                    cmdLog.ExecuteNonQuery();

                    trans.Commit();
                    MessageBox.Show("Appointment rescheduled successfully.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception) { trans.Rollback(); throw; }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}
