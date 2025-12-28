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
    public partial class DoctorsPatientHistoryForm : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";
        private string _doctorName;
        private string _doctorFullName = "";
        private int _doctorId = -1;
        private List<PatientHistoryViewModel> _allHistory = new List<PatientHistoryViewModel>();
        private ToolTip _toolTip = new ToolTip();

        private class PatientHistoryViewModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Gender { get; set; }
            public string Age { get; set; }
            public string Disease { get; set; }
            public string Status { get; set; }
            public string Phone { get; set; }
        }

        public DoctorsPatientHistoryForm(string doctorName)
        {
            _doctorName = doctorName;
            InitializeComponent();
            
            ResolveDoctorIdentity();

            txtSearch.TextChanged += (s, e) => RefreshUI();
            cmbFilterStatus.SelectedIndexChanged += (s, e) => RefreshUI();
            btnResetFilters.Click += (s, e) => {
                txtSearch.Clear();
                cmbFilterStatus.SelectedIndex = 0;
                RefreshUI();
            };

            // Refresh data when form becomes visible
            this.VisibleChanged += (s, e) => {
                if (this.Visible) FetchData();
            };

            FetchData();
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

                    // 2. Resolve DoctorId
                    string simpleName = _doctorFullName.Replace("Dr.", "").Replace("Dr", "").Trim();
                    string docQuery = @"SELECT TOP 1 DoctorId, DoctorName FROM Doctors 
                                       WHERE DoctorName = @FullName 
                                          OR DoctorName LIKE '%' + @SimpleName + '%'
                                          OR DoctorName = @User";
                    using (var cmd = new SqlCommand(docQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@FullName", _doctorFullName);
                        cmd.Parameters.AddWithValue("@SimpleName", simpleName);
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
            _allHistory.Clear();
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Fetch unique patients that this doctor has seen (through Visits)
                    // OR are assigned to them (if DoctorName is in Patients table)
                    
                    string query = @"
                        SELECT DISTINCT P.PatientID, P.PatientName, P.Gender, P.Age, 
                               (SELECT TOP 1 Diagnosis FROM Visits WHERE PatientID = P.PatientID ORDER BY VisitDate DESC) as Disease, 
                               P.Status, P.Phone 
                        FROM Patients P
                        LEFT JOIN Visits V ON P.PatientID = V.PatientID
                        WHERE (V.DoctorId = @DocId 
                           OR V.DoctorName LIKE '%' + @DocFullName + '%'
                           OR P.DoctorName LIKE '%' + @DocFullName + '%')
                           AND V.Status = 'COMPLETED'";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocId", _doctorId);
                        cmd.Parameters.AddWithValue("@DocFullName", _doctorFullName);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _allHistory.Add(new PatientHistoryViewModel
                                {
                                    Id = reader["PatientID"].ToString(),
                                    Name = reader["PatientName"].ToString(),
                                    Gender = reader["Gender"].ToString(),
                                    Age = reader["Age"].ToString(),
                                    Disease = reader["Disease"] != DBNull.Value ? reader["Disease"].ToString() : "N/A",
                                    Status = reader["Status"].ToString(),
                                    Phone = reader["Phone"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}\n\nDoctor ID: {_doctorId}\nDoctor Name: {_doctorFullName}", "Debug Info");
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            string search = txtSearch.Text.ToLower().Trim();
            string selectedStatus = cmbFilterStatus.SelectedItem?.ToString() ?? "All Records";

            var filtered = _allHistory.Where(p => 
                (string.IsNullOrEmpty(search) || p.Name.ToLower().Contains(search) || p.Phone.Contains(search)) &&
                (selectedStatus == "All Records" || p.Status == selectedStatus)
            ).ToList();

            flowHistory.SuspendLayout();
            flowHistory.Controls.Clear();

            if (filtered.Any())
            {
                foreach (var item in filtered)
                {
                    flowHistory.Controls.Add(CreateHistoryCard(item));
                }
            }
            else
            {
                Label empty = new Label {
                    Text = "No historical records found.",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                flowHistory.Controls.Add(empty);
            }
            flowHistory.ResumeLayout();
        }

        private Control CreateHistoryCard(PatientHistoryViewModel patient)
        {
            Panel card = new Panel {
                Width = flowHistory.Width - 90,
                Height = 110,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 15)
            };

            Color statusColor = patient.Status switch {
                "Active" => Color.FromArgb(59, 130, 246),
                "Discharged" => Color.FromArgb(16, 185, 129),
                "Follow-up" => Color.FromArgb(245, 158, 11),
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

            Label lblID = new Label { Text = $"ID: {patient.Id}", Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(25, 20), AutoSize = true };
            Label lblPatient = new Label { Text = patient.Name, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(25, 45), AutoSize = true };
            
            Label lblDetails = new Label { 
                Text = $"{patient.Gender} • {patient.Age} yrs • {patient.Phone}", 
                Font = new Font("Segoe UI", 10), 
                ForeColor = Color.FromArgb(75, 85, 99), 
                Location = new Point(25, 78), 
                AutoSize = true 
            };

            Label lblDiagnosis = new Label {
                Text = $"Latest Diagnosis: {patient.Disease}",
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(350, 45),
                AutoSize = false,
                AutoEllipsis = true,
                Height = 30
            };

            Label lblBadge = new Label {
                Text = patient.Status.ToUpper(),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = statusColor,
                BackColor = Color.FromArgb(20, statusColor),
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            IconButton btnAction = new IconButton {
                IconChar = IconChar.FileInvoice,
                IconSize = 20,
                IconColor = Color.White,
                BackColor = Color.FromArgb(107, 114, 128),
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAction.FlatAppearance.BorderSize = 0;

            EventHandler openHistory = (s, e) => {
                if (int.TryParse(patient.Id, out int pid)) {
                    var detailForm = new PatientHistoryViewForm(pid);
                    detailForm.Show();
                }
            };

            card.Click += openHistory;
            card.Cursor = Cursors.Hand;
            lblID.Click += openHistory; lblID.Cursor = Cursors.Hand;
            lblPatient.Click += openHistory; lblPatient.Cursor = Cursors.Hand;
            lblDetails.Click += openHistory; lblDetails.Cursor = Cursors.Hand;
            lblDiagnosis.Click += openHistory; lblDiagnosis.Cursor = Cursors.Hand;
            lblBadge.Click += openHistory; lblBadge.Cursor = Cursors.Hand;

            btnAction.Click += openHistory;
            _toolTip.SetToolTip(btnAction, "View Full Patient Record");

            card.Controls.AddRange(new Control[] { lblID, lblPatient, lblDetails, lblDiagnosis, lblBadge, btnAction });
            
            card.Resize += (s, e) => {
                btnAction.Location = new Point(card.Width - 60, (card.Height - btnAction.Height) / 2);
                lblBadge.Location = new Point(btnAction.Left - lblBadge.Width - 25, (card.Height - lblBadge.Height) / 2);
                
                // Set diagnosis width to occupy space between patient name and badge
                int diagWidth = lblBadge.Left - lblDiagnosis.Left - 20;
                lblDiagnosis.Width = Math.Max(100, diagWidth);
            };

            return card;
        }
    }
}
