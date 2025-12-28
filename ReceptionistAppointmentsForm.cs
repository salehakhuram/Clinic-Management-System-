using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class ReceptionistAppointmentsForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        // Data model
        private class AppointmentViewModel
        {
            public string Id { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public string PatientName { get; set; }
            public string PatientCode { get; set; } // Added
            public string DoctorName { get; set; }
            public string DoctorCode { get; set; } // Added
            public string AppointmentCode { get; set; } // Added
            public string PatientId { get; set; }
            public string DoctorId { get; set; }
            public string Type { get; set; } // Map to Reason
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
        }

        private List<AppointmentViewModel> _allAppointments = new List<AppointmentViewModel>();
        private ToolTip _toolTip = new ToolTip();

        public ReceptionistAppointmentsForm()
        {
            InitializeComponent();
            
            // Initial selection
            monthCalendar.SetDate(DateTime.Today);
            
            // Wire events
            monthCalendar.DateSelected += (s, e) => HandleCalendarSelection(monthCalendar.SelectionStart);
            cmbFilterDoctor.SelectedIndexChanged += (s, e) => LoadAppointmentsFromList();
            cmbFilterStatus.SelectedIndexChanged += (s, e) => LoadAppointmentsFromList();
            if (txtSearch != null) txtSearch.TextChanged += (s, e) => LoadAppointmentsFromList();
            btnResetFilters.Click += ResetFilters;
            
            if (btnNewAppointment != null)
            {
               btnNewAppointment.Click += (s, e) => {
                   using (var form = new AppointmentsForm("admin")) { 
                       if(form.ShowDialog() == DialogResult.OK) RefreshData();
                   }
               };
            }

            // Initial load
            RefreshData();

            // Periodic Refresh Timer (15 seconds)
            System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 15000;
            refreshTimer.Tick += (s, e) => {
                if (this.Visible) RefreshData();
            };
            refreshTimer.Start();

            // Seed if empty (for demonstration)
            if (_allAppointments.Count == 0) SeedDemoData();
        }

        private void SeedDemoData()
        {
            try {
                using var con = new SqlConnection(connectionString);
                con.Open();
                string query = @"
                    INSERT INTO Appointments (AppointmentIntId, PatientID, PatientName, DoctorID, DoctorName, AppointmentDate, AppointmentTime, Status, Reason, CreatedBy)
                    VALUES 
                    ('D1', 'P1', 'Sarah Connor', 'D1', 'Dr. Sarah', CAST(GETDATE() AS DATE), '09:00', 'Scheduled', 'General Checkup', 'System'),
                    ('D2', 'P2', 'John Wick', 'D2', 'Dr. Smith', CAST(GETDATE() AS DATE), '10:30', 'Checked-In', 'Dental Exam', 'System'),
                    ('D3', 'P3', 'Tony Stark', 'D1', 'Dr. Sarah', CAST(GETDATE() AS DATE), '11:45', 'With Doctor', 'Consultation', 'System'),
                    ('D4', 'P4', 'Peter Parker', 'D2', 'Dr. Smith', CAST(GETDATE() AS DATE), '13:00', 'Scheduled', 'Follow Up', 'System'),
                    ('D5', 'P5', 'Wanda Maximoff', 'D3', 'Dr. Brown', CAST(GETDATE() AS DATE), '14:30', 'Scheduled', 'Consultation', 'System')";
                using var cmd = new SqlCommand(query, con);
                cmd.ExecuteNonQuery();
                RefreshData();
            } catch { /* Silent fail if already exists or DB error */ }
        }

        private void RefreshData()
        {
            FetchAppointmentsFromDB();
            HandleCalendarSelection(monthCalendar.SelectionStart);
            PopulateDoctorFilter();
        }

        private void FetchAppointmentsFromDB()
        {
            _allAppointments.Clear();
            try
            {
                using var con = new SqlConnection(connectionString);
                string query = @"SELECT A.AppointmentIntId, A.AppointmentCode, A.AppointmentDate, A.AppointmentTime, 
                                       P.PatientName, P.PatientCode, A.PatientId, D.DoctorName, D.DoctorCode, A.DoctorId, A.Status, A.Reason,
                                       ISNULL(B.PaymentStatus, 'Unpaid') as PaymentStatus
                                FROM Appointments A
                                LEFT JOIN Patients P ON A.PatientId = P.PatientId
                                LEFT JOIN Doctors D ON A.DoctorId = D.DoctorId
                                LEFT JOIN Bills B ON A.AppointmentIntId = B.AppointmentIntId
                                WHERE A.AppointmentDate >= DATEADD(year, -1, GETDATE()) ORDER BY A.AppointmentDate DESC";
                using var cmd = new SqlCommand(query, con);
                con.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    _allAppointments.Add(new AppointmentViewModel
                    {
                        Id = reader["AppointmentIntId"].ToString(),
                        AppointmentCode = reader["AppointmentCode"]?.ToString() ?? "",
                        Date = reader["AppointmentDate"] != DBNull.Value ? (DateTime)reader["AppointmentDate"] : DateTime.Today,
                        Time = reader["AppointmentTime"] != DBNull.Value ? (TimeSpan)reader["AppointmentTime"] : TimeSpan.Zero,
                        PatientName = reader["PatientName"]?.ToString() ?? "",
                        PatientCode = reader["PatientCode"]?.ToString() ?? "",
                        PatientId = reader["PatientId"]?.ToString() ?? "",
                        DoctorName = reader["DoctorName"]?.ToString() ?? "",
                        DoctorCode = reader["DoctorCode"]?.ToString() ?? "",
                        DoctorId = reader["DoctorId"]?.ToString() ?? "",
                        Type = reader["Reason"]?.ToString() ?? "",
                        Status = reader["Status"]?.ToString() ?? "",
                        PaymentStatus = reader["PaymentStatus"]?.ToString() ?? "Unpaid"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading from database: " + ex.Message);
            }
        }

        private void PopulateDoctorFilter()
        {
            var doctors = _allAppointments.Select(a => a.DoctorName).Distinct().OrderBy(d => d).ToList();
            doctors.Insert(0, "All Doctors");
            
            string current = cmbFilterDoctor.SelectedItem?.ToString();
            cmbFilterDoctor.DataSource = doctors;
            if (current != null && doctors.Contains(current)) cmbFilterDoctor.SelectedItem = current;
            else cmbFilterDoctor.SelectedIndex = 0;
        }

        private void HandleCalendarSelection(DateTime date)
        {
            lblSidebarSubtitle.Text = $"Viewing appointments for: {date:MMM dd}";
            LoadAppointmentsFromList();
        }

        private void LoadAppointmentsFromList()
        {
            DateTime date = monthCalendar.SelectionStart;
            lblPageSubtitle.Text = $"Viewing appointments for {date:dddd, MMMM dd, yyyy}";

            // Filter Logic
            string selectedDoc = cmbFilterDoctor.SelectedItem?.ToString() ?? "All Doctors";
            string selectedStatus = cmbFilterStatus.SelectedItem?.ToString() ?? "All Statuses";
            string search = txtSearch != null ? txtSearch.Text.Trim().ToLower() : "";

            var filtered = _allAppointments.Where(a => 
                a.Date.Date == date.Date &&
                (selectedDoc == "All Doctors" || a.DoctorName == selectedDoc) &&
                (selectedStatus == "All Statuses" || a.Status == selectedStatus) &&
                (string.IsNullOrEmpty(search) || 
                 a.PatientName.ToLower().Contains(search) || 
                 a.PatientCode.ToLower().Contains(search) ||
                 a.AppointmentCode.ToLower().Contains(search) ||
                 a.DoctorName.ToLower().Contains(search))
            ).OrderBy(a => a.Time).ToList();

            // Render
            flowAppts.SuspendLayout();
            flowAppts.Controls.Clear();

            if (filtered.Any())
            {
                foreach (var appt in filtered)
                {
                    flowAppts.Controls.Add(CreateAppointmentCard(appt));
                }
            }
            else
            {
                Label empty = new Label {
                    Text = "No appointments found for this date.",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Padding = new Padding(10)
                };
                flowAppts.Controls.Add(empty);
            }
            flowAppts.SizeChanged += (s, e) => {
                foreach (Control c in flowAppts.Controls) c.Width = flowAppts.ClientSize.Width - 90;
            };
            flowAppts.ResumeLayout();
        }

        private void ResetFilters(object sender, EventArgs e)
        {
            cmbFilterDoctor.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndex = 0;
            monthCalendar.SetDate(DateTime.Today);
            RefreshData();
        }

        private Control CreateAppointmentCard(AppointmentViewModel appt)
        {
            Panel card = new Panel {
                Width = flowAppts.ClientSize.Width - 90,
                Height = 125, // Standardized height
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 15)
            };

            bool isTerminal = appt.Status == "Completed" || appt.Status == "Cancelled";

            // Status Colors
            Color statusColor = appt.Status switch {
                "Scheduled" => Color.FromArgb(37, 99, 235), // Blue
                "Checked-In" => Color.FromArgb(249, 115, 22), // Orange
                "With Doctor" => Color.FromArgb(139, 92, 246), // Purple
                "Completed" => Color.FromArgb(22, 163, 74), // Green
                "Cancelled" => Color.FromArgb(225, 29, 72), // Red
                _ => Color.Gray
            };

            card.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
                
                // Left strip
                using (SolidBrush b = new SolidBrush(statusColor)) {
                    e.Graphics.FillRectangle(b, 0, 10, 6, card.Height - 20);
                }
            };

            Panel content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15) };
            
            // Time
            DateTime displayTime = DateTime.Today.Add(appt.Time);
            Label lblTime = new Label {
                Text = displayTime.ToString("hh:mm tt"),
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true
            };

            // Patient & Doctor
            Label lblPatient = new Label {
                Text = $"{appt.PatientName} ({appt.PatientCode})",
                Font = new Font("Segoe UI Semibold", 13),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true
            };
            
            // Appt Code
            Label lblCode = new Label {
                Text = $"#{appt.AppointmentCode}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(180, 10)
            };

            Label lblDetails = new Label {
                Text = $"{appt.DoctorName} ({appt.DoctorCode}) • {appt.Type}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true
            };

            // Status Badge
            Label lblBadge = new Label {
                Text = appt.Status.ToUpper(),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = isTerminal ? Color.Gray : statusColor,
                BackColor = Color.FromArgb(20, statusColor),
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            if (isTerminal) { lblBadge.BackColor = Color.FromArgb(243, 244, 246); }

            // Info Panel (Fill)
            Panel pnlInfo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15) };
            pnlInfo.Controls.AddRange(new Control[] { lblTime, lblPatient, lblDetails, lblCode });
            lblTime.Location = new Point(25, 35);
            lblPatient.Location = new Point(180, 30);
            lblDetails.Location = new Point(180, 65);
            lblDetails.MaximumSize = new Size(400, 0); // Allow wrapping

            // Badge & Actions Panel (Right side)
            Panel pnlSide = new Panel { Dock = DockStyle.Right, Width = 250, Padding = new Padding(0, 15, 10, 0) };
            
            FlowLayoutPanel pnlActions = new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Location = new Point(0, 35),
                BackColor = Color.Transparent
            };

            // 1. Status Action
            IconButton btnNextStatus = null;
            if (appt.Status == "Scheduled" || appt.Status == "Rescheduled") {
                btnNextStatus = CreateActionButton(IconChar.UserCheck, Color.FromArgb(249, 115, 22), "Mark as Checked-In");
                btnNextStatus.Click += (s, e) => UpdateApptStatus(appt, "Checked-In");
            } else if (appt.Status == "Checked-In") {
                btnNextStatus = CreateActionButton(IconChar.Stethoscope, Color.FromArgb(139, 92, 246), "Send to Doctor");
                btnNextStatus.Click += (s, e) => UpdateApptStatus(appt, "With Doctor");
            } else {
                btnNextStatus = CreateActionButton(IconChar.ChevronRight, Color.FromArgb(229, 231, 235), appt.Status == "With Doctor" ? "Doctor controlling" : "Finished");
                btnNextStatus.Enabled = false;
                btnNextStatus.Cursor = Cursors.No;
            }

            // 2. Reschedule
            IconButton btnResched = CreateActionButton(IconChar.CalendarAlt, Color.FromArgb(107, 114, 128), "Reschedule Appointment");
            if (isTerminal) { btnResched.Enabled = false; btnResched.IconColor = Color.FromArgb(229, 231, 235); btnResched.Cursor = Cursors.No; }
            btnResched.Click += (s, e) => {
                using (var modal = new RescheduleModal(appt.Id, connectionString, "Receptionist"))
                {
                    if (modal.ShowDialog() == DialogResult.OK) RefreshData();
                }
            };

            // 3. Cancel
            IconButton btnCancel = CreateActionButton(IconChar.Times, Color.FromArgb(225, 29, 72), "Cancel Appointment");
            if (isTerminal) { btnCancel.Enabled = false; btnCancel.IconColor = Color.FromArgb(229, 231, 235); btnCancel.Cursor = Cursors.No; }
            btnCancel.Click += (s, e) => {
                if (MessageBox.Show($"Are you sure you want to cancel the appointment for {appt.PatientName}?", "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    UpdateApptStatus(appt, "Cancelled");
                }
            };

            pnlActions.Controls.AddRange(new Control[] { btnNextStatus, btnResched, btnCancel });
            // Payment Badge
            Label lblPaymentBadge = new Label {
                Text = (appt.PaymentStatus ?? "Unpaid").ToUpper(),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = (appt.PaymentStatus == "Paid") ? Color.FromArgb(22, 163, 74) : Color.FromArgb(225, 29, 72),
                BackColor = (appt.PaymentStatus == "Paid") ? Color.FromArgb(20, 22, 163, 74) : Color.FromArgb(20, 225, 29, 72),
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Badges Panel (Right Aligned)
            FlowLayoutPanel pnlBadges = new FlowLayoutPanel {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 10, 0) // 10px gap from right
            };
            pnlBadges.Controls.Add(lblBadge);
            pnlBadges.Controls.Add(lblPaymentBadge);

            // Actions Panel (Right Aligned)
            pnlActions.Dock = DockStyle.Top;
            pnlActions.Height = 50;
            pnlActions.Padding = new Padding(0, 0, 5, 0);
            pnlActions.FlowDirection = FlowDirection.RightToLeft; // Align buttons from right

            pnlSide.Controls.Add(pnlBadges);
            pnlSide.Controls.Add(pnlActions);
 // Added second to appear above actions (due to DockStyle.Top stacking)

            content.Controls.Add(pnlInfo); // Dock Fill
            content.Controls.Add(pnlSide); // Dock Right
            card.Controls.Add(content);

            return card;
        }

        private IconButton CreateActionButton(IconChar icon, Color color, string tooltip)
        {
            IconButton btn = new IconButton {
                IconChar = icon,
                IconSize = 22,
                IconColor = color,
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 244, 246);
            _toolTip.SetToolTip(btn, tooltip);
            return btn;
        }

        private void UpdateApptStatus(AppointmentViewModel appt, string newStatus)
        {
            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();
                using var trans = con.BeginTransaction();
                try
                {
                    if (newStatus == "Checked-In")
                    {
                        // 1. Generate Token
                        var cmdToken = new SqlCommand("SELECT COUNT(*) FROM Visits WHERE CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)", con, trans);
                        int count = (int)cmdToken.ExecuteScalar();
                        string token = (count + 1).ToString("D3");

                        // 2. Create Visit Record
                        string qVisit = "INSERT INTO Visits (PatientID, AppointmentIntId, DoctorId, DoctorName, TokenNumber, Status) VALUES (@pid, @aid, @did, @doc, @token, 'WAITING')";
                        using (var cmdVisit = new SqlCommand(qVisit, con, trans))
                        {
                            cmdVisit.Parameters.AddWithValue("@pid", appt.PatientId);
                            cmdVisit.Parameters.AddWithValue("@aid", appt.Id);
                            cmdVisit.Parameters.AddWithValue("@did", appt.DoctorId);
                            cmdVisit.Parameters.AddWithValue("@doc", appt.DoctorName);
                            cmdVisit.Parameters.AddWithValue("@token", token);
                            cmdVisit.ExecuteNonQuery();
                        }

                        // 3. Update Appointment with Token as Code
                        string qAppt = "UPDATE Appointments SET Status = @Status, AppointmentCode = @token, UpdatedAt = GETDATE(), UpdatedBy = 'Receptionist' WHERE AppointmentIntId = @ID";
                        using (var cmdAppt = new SqlCommand(qAppt, con, trans))
                        {
                            cmdAppt.Parameters.AddWithValue("@Status", newStatus);
                            cmdAppt.Parameters.AddWithValue("@token", token);
                            cmdAppt.Parameters.AddWithValue("@ID", appt.Id);
                            cmdAppt.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string query = "UPDATE Appointments SET Status = @Status, UpdatedAt = GETDATE(), UpdatedBy = 'Receptionist' WHERE AppointmentIntId = @ID";
                        using var cmd = new SqlCommand(query, con, trans);
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@ID", appt.Id);
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    appt.Status = newStatus;
                    RefreshData(); // Full refresh to ensure list is in sync
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message);
            }
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
