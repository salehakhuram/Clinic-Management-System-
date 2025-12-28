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
    public partial class DoctorsAppointmentsForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";
        private string _doctorName;
        private int _doctorId = -1; // Resolved ID for robust filtering
        private List<AppointmentViewModel> _allAppointments = new List<AppointmentViewModel>();
        private ToolTip _toolTip = new ToolTip();

        private class AppointmentViewModel
        {
            public string Id { get; set; }
            public string AppointmentCode { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public string PatientName { get; set; }
            public string PatientCode { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public string DisplayCode { get; set; }
        }

        public DoctorsAppointmentsForm(string doctorName)
        {
            _doctorName = doctorName;
            InitializeComponent();
            
            // Resolve Doctor ID first to ensure reliable filtering
            ResolveDoctorId();

            // Initial selection
            monthCalendar.SetDate(DateTime.Today);
            
            // Wire events
            monthCalendar.DateSelected += (s, e) => RefreshData();
            cmbFilterStatus.SelectedIndexChanged += (s, e) => RefreshUI();
            btnResetFilters.Click += (s, e) => {
                monthCalendar.SetDate(DateTime.Today);
                cmbFilterStatus.SelectedIndex = 0;
                RefreshData();
            };

            RefreshData();
        }

        private void ResolveDoctorIdentity()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // 1. Get Full Name from Users
                    string userQuery = "SELECT FullName FROM Users WHERE Username = @User";
                    using (var cmd = new SqlCommand(userQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@User", _doctorName);
                        _doctorFullName = cmd.ExecuteScalar()?.ToString() ?? _doctorName;
                    }

                    // 2. Resolve DoctorId (Robust fuzzy match)
                    string simpleName = _doctorFullName.Replace("Dr.", "").Replace("Dr", "").Replace(".", "").Trim();
                    string firstWord = simpleName.Split(' ')[0];

                    string docQuery = @"SELECT TOP 1 DoctorId, DoctorName FROM Doctors 
                                       WHERE DoctorName = @FullName 
                                          OR DoctorName LIKE '%' + @SimpleName + '%'
                                          OR DoctorName LIKE '%' + @FirstWord + '%'
                                          OR DoctorName = @User";
                    using (var cmd = new SqlCommand(docQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@FullName", _doctorFullName);
                        cmd.Parameters.AddWithValue("@SimpleName", simpleName);
                        cmd.Parameters.AddWithValue("@FirstWord", firstWord);
                        cmd.Parameters.AddWithValue("@User", _doctorName);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _doctorId = (int)reader["DoctorId"];
                                _doctorFullName = reader["DoctorName"].ToString();
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        // Backwards compatibility for the constructor call
        private void ResolveDoctorId() => ResolveDoctorIdentity();
        
        private string _doctorFullName; // Added for robustness

        private void RefreshData()
        {
            FetchAppointments();
            RefreshUI();
        }

       private void FetchAppointments()
{
    _allAppointments.Clear();

    try
    {
        using var con = new SqlConnection(connectionString);

        string sql = @"
        SELECT A.AppointmentIntId, A.AppointmentCode, A.AppointmentDate, 
       A.AppointmentTime, P.PatientName, P.PatientCode, 
       A.Status, A.Reason, ISNULL(B.PaymentStatus, 'Unpaid') as PaymentStatus,
       V.TokenNumber
FROM Appointments A
LEFT JOIN Patients P ON A.PatientId = P.PatientId
LEFT JOIN Billing B ON A.AppointmentIntId = B.AppointmentIntId
LEFT JOIN Visits V ON A.AppointmentIntId = V.AppointmentIntId
WHERE (A.DoctorId = @DocId OR A.DoctorName = @DocName)
  AND CAST(A.AppointmentDate AS DATE) = CAST(@SelectedDate AS DATE)
";

       using var cmd = new SqlCommand(sql, con);
       cmd.Parameters.AddWithValue("@DocId", _doctorId);
       cmd.Parameters.AddWithValue("@DocName", _doctorFullName);
       cmd.Parameters.AddWithValue("@SelectedDate", monthCalendar.SelectionStart.Date);
       con.Open();

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            _allAppointments.Add(new AppointmentViewModel
            {
                Id = reader["AppointmentIntId"].ToString(),
                AppointmentCode = reader["AppointmentCode"]?.ToString() ?? "",
                Date = Convert.ToDateTime(reader["AppointmentDate"]),
          Time = reader["AppointmentTime"] != DBNull.Value
    ? (TimeSpan)reader["AppointmentTime"]
    : TimeSpan.Zero,
                PatientName = reader["PatientName"]?.ToString() ?? "Unknown",
                PatientCode = reader["PatientCode"]?.ToString() ?? "N/A",
                Type = reader["Reason"]?.ToString() ?? "Consultation",
                Status = reader["Status"]?.ToString() ?? "Scheduled",
                PaymentStatus = reader["PaymentStatus"]?.ToString() ?? "Unpaid",
                DisplayCode = GetDisplayCode(reader["AppointmentCode"]?.ToString(), reader["TokenNumber"]?.ToString())
            });
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Fetch error: " + ex.Message);
        MessageBox.Show($"Fetched: {_allAppointments.Count} appointments");

    }
}

        private string GetDisplayCode(string apptCode, string token)
        {
            // If token exists (walk-in), show token number
            if (!string.IsNullOrEmpty(token)) return token;
            // Otherwise show full appointment code
            if (!string.IsNullOrEmpty(apptCode)) return apptCode;
            return "N/A";
        }

        private void RefreshUI()
{
    DateTime selectedDate = monthCalendar.SelectionStart.Date;
    string selectedStatus = cmbFilterStatus.SelectedItem?.ToString() ?? "All Statuses";

    if (lblPageTitle != null) {
        lblPageTitle.Text = selectedDate.Date == DateTime.Today.Date 
            ? "Today's Schedule & Appointments" 
            : $"Schedule for {selectedDate:dd MMM yyyy}";
    }
    if (lblPageSubtitle != null) {
        lblPageSubtitle.Text = $"Showing {selectedStatus.ToLower()} patients for this date";
    }

    // lblPageSubtitle removed in designer for consistency
    var filtered = _allAppointments
       .Where(a =>
    (selectedStatus == "All Statuses" ||
     a.Status.Equals(selectedStatus, StringComparison.OrdinalIgnoreCase))
)

        .OrderBy(a => a.Time)
        .ToList();

    flowAppts.SuspendLayout();
    flowAppts.Controls.Clear();

    if (filtered.Any())
    {
        foreach (var appt in filtered)
            flowAppts.Controls.Add(CreateAppointmentCard(appt));
    }
    else
    {
        flowAppts.Controls.Add(new Label
        {
            Text = "No appointments found for this selection.",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.Gray,
            Dock = DockStyle.Top,
            Height = 120,
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    flowAppts.ResumeLayout();
}

        private Control CreateAppointmentCard(AppointmentViewModel appt)
        {
           Panel card = new Panel
{
    Width = flowAppts.ClientSize.Width - 40,
    Height = 125,
    Margin = new Padding(0, 0, 0, 15),
    BackColor = Color.White
};

            string rawStatus = appt.Status ?? "";
            string statusNorm = rawStatus.Replace("_", " ").ToUpper();
            bool isTerminal = statusNorm == "COMPLETED" || statusNorm == "CANCELLED";

            // Status Colors matching Receptionist view
            Color statusColor = statusNorm switch {
                "SCHEDULED" => Color.FromArgb(37, 99, 235), // Blue
                "CHECKED-IN" => Color.FromArgb(249, 115, 22), // Orange
                "CHECKED IN" => Color.FromArgb(249, 115, 22), // Orange
                "WAITING" => Color.FromArgb(249, 115, 22), // Orange
                "WITH DOCTOR" => Color.FromArgb(79, 70, 229), // Indigo
                "COMPLETED" => Color.FromArgb(22, 163, 74), // Green
                "CANCELLED" => Color.FromArgb(225, 29, 72), // Red
                _ => Color.Gray
            };

            card.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 10))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
                
                // Left strip
                using (SolidBrush b = new SolidBrush(statusColor))
                    e.Graphics.FillRectangle(b, 0, 10, 6, card.Height - 20);
            };

            Panel content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15) };
            
            // 1. Info Section
            Panel pnlInfo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 15) };
            
            // Time
           DateTime displayTime = appt.Date.Date + appt.Time;

            Label lblTime = new Label {
                Text = displayTime.ToString("hh:mm tt"),
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(25, 35)
            };

            // Patient
            Label lblPatient = new Label {
                Text = $"{appt.PatientName} ({appt.PatientCode})",
                Font = new Font("Segoe UI Semibold", 13),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(180, 30)
            };

            // Details/Type
            Label lblType = new Label {
                Text = appt.Type,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true,
                Location = new Point(180, 65)
            };
            
            // Appt Code
            Label lblCode = new Label {
                Text = $"#{appt.DisplayCode}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(180, 10)
            };

            pnlInfo.Controls.AddRange(new Control[] { lblTime, lblPatient, lblType, lblCode });

            // 2. Badge & Actions Section (Right side)
            Panel pnlSide = new Panel { Dock = DockStyle.Right, Width = 250, Padding = new Padding(0, 15, 10, 0) };
            
            // Payment Badge
            bool isPaid = appt.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
            Color payColor = isPaid ? Color.FromArgb(22, 163, 74) : Color.FromArgb(225, 29, 72);
            Label lblPayment = new Label {
                Text = isPaid ? "PAID" : "UNPAID",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = payColor,
                BackColor = Color.FromArgb(20, payColor),
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Status Badge
            Label lblBadge = new Label {
                Text = appt.Status.ToUpper(),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = isTerminal ? Color.Gray : statusColor,
                BackColor = Color.FromArgb(20, statusColor),
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0)
            };
            if (isTerminal) { lblBadge.BackColor = Color.FromArgb(243, 244, 246); }

            // Badges Panel (Right Aligned)
            FlowLayoutPanel pnlBadges = new FlowLayoutPanel {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 10, 0)
            };
            pnlBadges.Controls.Add(lblBadge);
            pnlBadges.Controls.Add(lblPayment);

            // Actions
            FlowLayoutPanel pnlActions = new FlowLayoutPanel {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 5, 0)
            };
            
            pnlSide.Controls.Add(pnlBadges);
            pnlSide.Controls.Add(pnlActions);
            
            // 1. History
            IconButton btnHistory = CreateActionButton(IconChar.History, Color.FromArgb(107, 114, 128), "View Patient History");
            pnlActions.Controls.Add(btnHistory);

            // 2. Play (Consultation) - Visible for Waiting and With Doctor
            if (statusNorm == "CHECKED-IN" || statusNorm == "CHECKED IN" || statusNorm == "WITH DOCTOR" || statusNorm == "WAITING") {
                IconButton btnPlay = CreateActionButton(IconChar.Play, Color.FromArgb(79, 70, 229), "Open Consultation");
                btnPlay.Click += (s, e) => {
                    if (statusNorm != "WITH DOCTOR") {
                         if (IsDoctorOccupied()) {
                            MessageBox.Show("You already have an active patient in consultation. Please complete the current session before starting a new one.", "Doctor Occupied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        UpdateApptStatus(appt, "With Doctor");
                    }
                    OpenConsultation(int.Parse(appt.Id));
                };
                pnlActions.Controls.Add(btnPlay);
            }

            // 3. Complete - Visible for With Doctor
            if (statusNorm == "WITH DOCTOR") {
                IconButton btnComplete = CreateActionButton(IconChar.Check, Color.FromArgb(22, 163, 74), "Complete Appointment");
                btnComplete.Click += (s, e) => UpdateApptStatus(appt, "Completed");
                pnlActions.Controls.Add(btnComplete);
            }

            // Removed redundant Adds

            content.Controls.Add(pnlInfo);
            content.Controls.Add(pnlSide);
            card.Controls.Add(content);

            card.Resize += (s, e) => {
                pnlSide.Left = card.Width - pnlSide.Width - 10;
            };

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
                try {
                    // 1. Update Appointment
                    string query = "UPDATE Appointments SET Status = @Status, UpdatedAt = GETDATE(), UpdatedBy = @DocName WHERE AppointmentIntId = @ID";
                    using var cmd = new SqlCommand(query, con, trans);
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@ID", appt.Id);
                    cmd.Parameters.AddWithValue("@DocName", _doctorName);
                    cmd.ExecuteNonQuery();

                    // 2. Sync Visits
                    string qVisit = "UPDATE Visits SET Status = @vStatus WHERE AppointmentIntId = @ID";
                    string vStatus = (newStatus == "With Doctor") ? "WITH_DOCTOR" : 
                                     (newStatus == "Completed") ? "COMPLETED" : "WAITING";
                    using var cmdV = new SqlCommand(qVisit, con, trans);
                    cmdV.Parameters.AddWithValue("@vStatus", vStatus);
                    cmdV.Parameters.AddWithValue("@ID", appt.Id);
                    cmdV.ExecuteNonQuery();

                    trans.Commit();
                    appt.Status = newStatus;
                    RefreshUI();
                } catch { trans.Rollback(); throw; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error: " + ex.Message);
            }
        }

        private bool IsDoctorOccupied()
        {
            try {
                using var con = new SqlConnection(connectionString);
                con.Open();
                string q = @"SELECT COUNT(*) FROM Appointments 
                            WHERE (DoctorID = @did OR DoctorName = @dname) 
                            AND Status = 'With Doctor'
                            AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@did", _doctorId);
                cmd.Parameters.AddWithValue("@dname", _doctorName);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            } catch { return false; }
        }

        private void OpenConsultation(int apptId)
        {
            try {
                int visitId = 0;
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = "SELECT TOP 1 VisitID FROM Visits WHERE AppointmentIntId = @aid ORDER BY VisitDate DESC";
                    using (var cmd = new SqlCommand(q, con)) {
                        cmd.Parameters.AddWithValue("@aid", apptId);
                        object res = cmd.ExecuteScalar();
                        if (res != null) visitId = Convert.ToInt32(res);
                    }
                }

                if (visitId > 0) {
                    var consult = new PatientConsultationForm(visitId);
                    consult.FormClosed += (s, e) => { RefreshData(); RefreshUI(); };
                    consult.Show();
                } else {
                    MessageBox.Show("No visit record found for this appointment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch (Exception ex) { MessageBox.Show("Error opening consultation: " + ex.Message); }
        }
    }
}
