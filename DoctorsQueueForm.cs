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
    public partial class DoctorsQueueForm : Form
    {
        private string _doctorName;
        private string _doctorFullName = "";
        private int _doctorId = -1;
        private List<VisitItem> _queue = new List<VisitItem>();
        private ToolTip _toolTip = new ToolTip();
        private readonly string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";

        private class VisitItem
        {
            public int VisitID { get; set; }
            public string Token { get; set; }
            public string PatientName { get; set; }
            public DateTime InTime { get; set; }
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public string AppointmentCode { get; set; }
        }

        public DoctorsQueueForm(string doctorName)
        {
            _doctorName = doctorName;
            InitializeComponent();
            
            ResolveDoctorIdentity();

            cmbFilterStatus.SelectedIndexChanged += (s, e) => RefreshUI();
            cmbSort.SelectedIndexChanged += (s, e) => RefreshUI();
            btnResetFilters.Click += (s, e) => {
                cmbFilterStatus.SelectedIndex = 0;
                cmbSort.SelectedIndex = 0;
                RefreshUI();
            };

            RefreshUI();
            
            // Add Polling Timer for Real-time Sync
            System.Windows.Forms.Timer syncTimer = new System.Windows.Forms.Timer { Interval = 10000 }; // 10 seconds
            syncTimer.Tick += (s, e) => { if (this.Visible) FetchData(); RefreshUI(); };
            syncTimer.Start();
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

        private void FetchData()
        {
            _queue.Clear();
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = @"
                        SELECT V.VisitID, V.TokenNumber, V.VisitDate, V.Status as VisitStatus,
                               P.PatientName,
                               A.AppointmentIntId, A.AppointmentCode, A.Status as ApptStatus,
                               ISNULL(B.PaymentStatus, 'Unpaid') as PaymentStatus
                        FROM Visits V
                        JOIN Patients P ON V.PatientID = P.PatientID
                        LEFT JOIN Appointments A ON V.AppointmentIntId = A.AppointmentIntId
                        LEFT JOIN Bills B ON (V.AppointmentIntId = B.AppointmentIntId AND V.AppointmentIntId IS NOT NULL)
                        WHERE (V.DoctorId = @docId OR V.DoctorName = @doc)
                          AND CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)
                          AND V.Status IN ('WAITING', 'Checked-In', 'WITH_DOCTOR', 'COMPLETED')
                        ORDER BY 
                            CASE 
                                WHEN V.Status = 'WITH_DOCTOR' THEN 1 
                                WHEN V.Status IN ('WAITING', 'Checked-In') THEN 2
                                ELSE 3 
                            END,
                            CASE WHEN ISNUMERIC(V.TokenNumber) = 1 THEN CAST(V.TokenNumber AS INT) ELSE 999999 END,
                            V.TokenNumber";
                            
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@docId", _doctorId);
                    cmd.Parameters.AddWithValue("@doc", _doctorFullName);
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            _queue.Add(new VisitItem {
                                VisitID = (int)dr["VisitID"],
                                Token = dr["TokenNumber"]?.ToString() ?? "000",
                                PatientName = dr["PatientName"].ToString(),
                                InTime = Convert.ToDateTime(dr["VisitDate"]),
                                Status = dr["VisitStatus"]?.ToString()?.ToUpper().Replace(" ", "-").Replace("_", "-") ?? "WAITING",
                                PaymentStatus = dr["PaymentStatus"]?.ToString() ?? "Unpaid",
                                AppointmentCode = dr["AppointmentCode"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            } catch { }
        }

        private void RefreshUI()
        {
            FetchData();
            string selectedStatus = cmbFilterStatus.SelectedItem?.ToString() ?? "All Statuses";
            int sortIndex = cmbSort.SelectedIndex;

            var filtered = _queue.Where(q => 
                (selectedStatus == "All Statuses" || q.Status == selectedStatus.ToUpper().Replace(" ", "-").Replace("_", "-"))
            ).ToList();

            // Sort strictly by TokenNo as requested (Numerical sort)
            filtered = sortIndex switch {
                0 => filtered.OrderBy(q => { int.TryParse(q.Token, out int t); return t; }).ThenBy(q => q.Token).ToList(), // Default: Token Rank
                1 => filtered.OrderBy(q => q.InTime).ToList(),
                2 => filtered.OrderByDescending(q => q.InTime).ToList(),
                _ => filtered
            };

            flowQueue.SuspendLayout();
            flowQueue.Controls.Clear();

            if (filtered.Any())
            {
                foreach (var item in filtered)
                {
                    flowQueue.Controls.Add(CreateQueueCard(item));
                }
            }
            else
            {
                Label empty = new Label {
                    Text = "No active patients in your queue.",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                flowQueue.Controls.Add(empty);
            }
            flowQueue.ResumeLayout();
        }

        private Control CreateQueueCard(VisitItem item)
        {
            Panel card = new Panel {
                Width = flowQueue.Width - 90,
                Height = 110,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 15)
            };

            Color statusColor = item.Status switch {
                "WAITING" => Color.FromArgb(249, 115, 22),       // accentOrange
                "CHECKED-IN" => Color.FromArgb(59, 130, 246),    // Blue
                "WITH-DOCTOR" => Color.FromArgb(139, 92, 246),   // Purple
                "COMPLETED" => Color.FromArgb(16, 185, 129),     // accentGreen
                _ => Color.Gray
            };

            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width-1, card.Height-1, 12))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
                using (SolidBrush b = new SolidBrush(statusColor))
                    e.Graphics.FillRectangle(b, 0, 20, 6, card.Height - 40);
            };

            Label lblToken = new Label { 
                Text = !string.IsNullOrEmpty(item.AppointmentCode) ? $"Appt: {item.AppointmentCode}" : $"Token: #{item.Token}", 
                Font = new Font("Segoe UI", 18, FontStyle.Bold), 
                ForeColor = Color.FromArgb(31, 41, 55), 
                Location = new Point(25, 28), 
                AutoSize = true 
            };

            // Calculate dynamic spacing to avoid overlap
            int tokenWidth = TextRenderer.MeasureText(lblToken.Text, lblToken.Font).Width;
            int infoX = Math.Max(240, tokenWidth + 60);

            Label lblPatient = new Label { Text = item.PatientName, Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(infoX, 28), AutoSize = true };
            Label lblTime = new Label { Text = $"In: {item.InTime:hh:mm tt}", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(infoX, 58), AutoSize = true };

            Label lblBadge = new Label {
                Text = item.Status.Replace("_", " ").Replace("-", " "),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = statusColor,
                BackColor = Color.FromArgb(20, statusColor),
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Payment Badge
            bool isPaid = item.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
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

            FlowLayoutPanel pnlActions = new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight,
                Width = 150, // Fixed width for 3 buttons
                Height = 45,
                BackColor = Color.Transparent
            };

            // 1. Play (Consultation)
            IconButton btnOpen = CreateActionButton(IconChar.Play, Color.FromArgb(139, 92, 246), "Open Consultation");
            btnOpen.Click += (s, e) => {
                if (item.Status == "WAITING" || item.Status == "CHECKED-IN") {
                    if (IsDoctorOccupied()) {
                        MessageBox.Show("You already have an active patient in consultation. Please complete the current session before starting a new one.", "Doctor Occupied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    UpdateVisitStatus(item.VisitID, "WITH_DOCTOR");
                }
                OpenConsultation(item.VisitID);
            };

            // 2. Check (Complete)
            IconButton btnFinish = CreateActionButton(IconChar.Check, Color.FromArgb(16, 185, 129), "Quick Complete");
            btnFinish.Click += (s, e) => { 
                UpdateVisitStatus(item.VisitID, "COMPLETED"); 
                RefreshUI(); 
            };

            // 3. Times (Refer Back)
            IconButton btnCancel = CreateActionButton(IconChar.Times, Color.FromArgb(225, 29, 72), "Refer back to reception");
            btnCancel.Click += (s, e) => {
                if (MessageBox.Show("Refer this patient back to reception?", "Refer Back", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    UpdateVisitStatus(item.VisitID, "WAITING");
                    RefreshUI();
                }
            };

            // Only show buttons based on status for cleaner UI
            btnOpen.Visible = (item.Status == "WAITING" || item.Status == "CHECKED-IN" || item.Status == "WITH-DOCTOR");
            btnFinish.Visible = (item.Status == "WITH-DOCTOR");
            btnCancel.Visible = (item.Status == "WAITING" || item.Status == "CHECKED-IN");

            pnlActions.Controls.AddRange(new IconButton[] { btnOpen, btnFinish, btnCancel });

            card.Controls.AddRange(new Control[] { lblToken, lblPatient, lblTime, lblBadge, lblPayment, pnlActions });
            
            card.Resize += (s, e) => {
                pnlActions.Location = new Point(card.Width - pnlActions.Width - 20, (card.Height - pnlActions.Height) / 2);
                lblBadge.Location = new Point(infoX, 80);
                lblPayment.Location = new Point(lblBadge.Right + 10, 80);
            };

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

                            // 2. Update linked Appointment
                            string qSync = @"UPDATE Appointments SET Status = @apptStat 
                                            WHERE AppointmentIntId = (SELECT AppointmentIntId FROM Visits WHERE VisitID = @vid)";
                            string apptStat = status == "WITH_DOCTOR" ? "With Doctor" : 
                                              status == "COMPLETED" ? "Completed" : "Checked-In";
                            var cmdSync = new SqlCommand(qSync, con, trans);
                            cmdSync.Parameters.AddWithValue("@apptStat", apptStat);
                            cmdSync.Parameters.AddWithValue("@vid", vid);
                            cmdSync.ExecuteNonQuery();

                            // 3. Update Doctor Readiness
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
            } catch { }
        }

        private void OpenConsultation(int vid)
        {
            if (vid <= 0) {
                MessageBox.Show("Could not find a visit record. Please ensure the patient is Checked-In correctly.", "Missing Visit Record");
                return;
            }
            using (var form = new PatientConsultationForm(vid))
            {
                form.ShowDialog();
                RefreshUI();
            }
        }

        private IconButton CreateActionButton(IconChar icon, Color color, string tooltip)
        {
            IconButton btn = new IconButton {
                IconChar = icon,
                IconSize = 20,
                IconColor = Color.White,
                BackColor = color,
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            _toolTip.SetToolTip(btn, tooltip);
            return btn;
        }

        private bool IsDoctorOccupied()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string q = @"SELECT COUNT(*) FROM Visits 
                                WHERE (DoctorId = @did OR DoctorName = @dname) 
                                AND Status = 'WITH_DOCTOR' 
                                AND CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)";
                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@did", _doctorId);
                        cmd.Parameters.AddWithValue("@dname", _doctorFullName);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch { return false; }
        }

    }
}
