using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public class CancelModal : Form
    {
        private string appointmentId;
        private string connectionString;
        private string currentUser;
        
        private TextBox txtReason = null!;
        private CheckBox chkConfirm = null!;
        private Button btnConfirm = null!, btnCancel = null!;

        public CancelModal(string appointmentId, string connectionString, string currentUser)
        {
            this.appointmentId = appointmentId;
            this.connectionString = connectionString;
            this.currentUser = currentUser;

            this.Text = "Cancel Appointment";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblTitle = new Label { 
                Text = $"Cancel Appointment: {appointmentId}", 
                Font = new Font("Segoe UI Bold", 12), 
                Location = new Point(20, 20), 
                Size = new Size(350, 30) 
            };

            Label lblReason = new Label { 
                Text = "Reason for Cancellation (Required):", 
                Font = new Font("Segoe UI", 10), 
                Location = new Point(20, 60), 
                Size = new Size(350, 25) 
            };

            txtReason = new TextBox { 
                Multiline = true, 
                Location = new Point(20, 90), 
                Size = new Size(345, 60), 
                Font = new Font("Segoe UI", 10) 
            };

            chkConfirm = new CheckBox { 
                Text = "I confirm that I want to cancel this appointment.", 
                Location = new Point(20, 160), 
                Size = new Size(350, 25), 
                Font = new Font("Segoe UI", 9) 
            };

            btnConfirm = new Button { 
                Text = "Confirm Cancellation", 
                Location = new Point(180, 210), 
                Size = new Size(185, 35), 
                BackColor = Color.FromArgb(244, 63, 94), 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button { 
                Text = "Go Back", 
                Location = new Point(20, 210), 
                Size = new Size(150, 35), 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblTitle, lblReason, txtReason, chkConfirm, btnConfirm, btnCancel });
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Please provide a reason for cancellation.");
                return;
            }

            if (!chkConfirm.Checked)
            {
                MessageBox.Show("Please check the confirmation box.");
                return;
            }

            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                using var trans = con.BeginTransaction();
                
                try
                {
                    // 1. Update Appointment Status
                    string updateQuery = "UPDATE Appointments SET Status = 'Cancelled', UpdatedAt = GETDATE(), UpdatedBy = @User WHERE AppointmentIntId = @ID";
                    using var cmdUpdate = new SqlCommand(updateQuery, con, trans);
                    cmdUpdate.Parameters.AddWithValue("@User", currentUser);
                    cmdUpdate.Parameters.AddWithValue("@ID", appointmentId);
                    cmdUpdate.ExecuteNonQuery();

                    // 2. Add Audit Log
                    string logQuery = @"INSERT INTO AppointmentAuditLogs (AppointmentIntId, Action, Reason, PerformedBy) 
                                       VALUES (@ID, 'Cancel', @Reason, @User)";
                    using var cmdLog = new SqlCommand(logQuery, con, trans);
                    cmdLog.Parameters.AddWithValue("@ID", appointmentId);
                    cmdLog.Parameters.AddWithValue("@Reason", txtReason.Text.Trim());
                    cmdLog.Parameters.AddWithValue("@User", currentUser);
                    cmdLog.ExecuteNonQuery();

                    trans.Commit();
                    MessageBox.Show("Appointment cancelled successfully.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cancelling appointment: " + ex.Message);
            }
        }
    }
}
