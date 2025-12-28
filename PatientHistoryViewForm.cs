using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PatientHistoryViewForm : Form
    {
        private readonly string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";
        private int _patientId;

        public PatientHistoryViewForm(int patientId)
        {
            _patientId = patientId;
            InitializeComponent();
            LoadPatientData();
            LoadTimeline();
            
            btnClose.Click += (s, e) => this.Close();
        }

        private void LoadPatientData()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    string query = "SELECT PatientName, PatientCode, Gender, Age, Phone, Email FROM Patients WHERE PatientID = @pid";
                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@pid", _patientId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            lblPatientName.Text = dr["PatientName"].ToString();
                            lblPatientID.Text = "PATIENT ID: " + dr["PatientCode"].ToString();
                            lblPatientDetails.Text = $"{dr["Gender"]}, {dr["Age"]} yrs";
                            lblContactInfo.Text = $"📞 {dr["Phone"]} | ✉ {dr["Email"]}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patient data: " + ex.Message);
            }
        }

        private void LoadTimeline()
        {
            try
            {
                flowTimeline.SuspendLayout();
                flowTimeline.Controls.Clear();

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Join Visits with Appointments to get Reason and Status
                    string query = @"SELECT V.*, A.Reason, A.Status as ApptStatus, A.DoctorName as ApptDoctor, A.PaymentStatus
                                   FROM Visits V 
                                   LEFT JOIN Appointments A ON V.AppointmentIntId = A.AppointmentIntId
                                   WHERE V.PatientID = @pid
                                   ORDER BY V.VisitDate DESC";
                    
                    var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@pid", _patientId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        bool hasRecords = false;
                        while (dr.Read())
                        {
                            hasRecords = true;
                            int visitId = (int)dr["VisitID"];
                            DateTime date = Convert.ToDateTime(dr["VisitDate"]);
                            string doctor = dr["ApptDoctor"]?.ToString() ?? dr["DoctorName"]?.ToString() ?? "Unknown Doctor";
                            string status = dr["ApptStatus"]?.ToString() ?? dr["Status"]?.ToString() ?? "Completed";
                            string payment = dr["PaymentStatus"]?.ToString() ?? "N/A";
                            string reason = dr["Reason"]?.ToString() ?? dr["ChiefComplaint"]?.ToString() ?? "Routine Checkup";
                            string complaint = dr["ChiefComplaint"]?.ToString() ?? "N/A";
                            string diagnosis = dr["Diagnosis"]?.ToString() ?? "N/A";
                            string notes = dr["DoctorNotes"]?.ToString() ?? "No notes provided.";

                            Panel card = CreateTimelineCard(visitId, date, doctor, status, reason, payment, complaint, diagnosis, notes);
                            flowTimeline.Controls.Add(card);
                        }

                        if (!hasRecords)
                        {
                            Label empty = new Label {
                                Text = "No medical history recorded for this patient.",
                                Font = new Font("Segoe UI", 12),
                                ForeColor = Color.Gray,
                                TextAlign = ContentAlignment.MiddleCenter,
                                Size = new Size(flowTimeline.Width - 80, 200)
                            };
                            flowTimeline.Controls.Add(empty);
                        }
                    }
                }
                flowTimeline.ResumeLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading timeline: " + ex.Message);
            }
        }

        private Panel CreateTimelineCard(int visitId, DateTime date, string doctor, string status, string reason, string payment, string complaint, string diagnosis, string notes)
        {
            Panel card = new Panel {
                Width = flowTimeline.Width - 60,
                AutoSize = true,
                MinimumSize = new Size(flowTimeline.Width - 60, 160),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 25),
                Cursor = Cursors.Default
            };

            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.DrawPath(pen, path);
                }
                // Date accent
                using (SolidBrush b = new SolidBrush(Color.FromArgb(59, 130, 246)))
                    e.Graphics.FillRectangle(b, 0, 20, 5, 40);
            };

            // Basic Info row
            Label lblDate = new Label { Text = date.ToString("dd MMM yyyy"), Font = new Font("Segoe UI Bold", 11), ForeColor = Color.FromArgb(59, 130, 246), Location = new Point(25, 20), AutoSize = true };
            Label lblTime = new Label { Text = date.ToString("hh:mm tt"), Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(25, 42), AutoSize = true };
            
            Label lblDoctor = new Label { Text = "With " + doctor, Font = new Font("Segoe UI Bold", 12), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(160, 20), AutoSize = true };
            Label lblReason = new Label { Text = "Reason: " + reason, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(75, 85, 99), Location = new Point(162, 45), AutoSize = true };

            // Status Badge
            Label lblBadge = new Label {
                Text = status.ToUpper(),
                Font = new Font("Segoe UI Bold", 7.5F),
                ForeColor = Color.White,
                BackColor = status.Contains("Completed") ? Color.FromArgb(16, 185, 129) : Color.FromArgb(245, 158, 11),
                Padding = new Padding(8, 4, 8, 4),
                Location = new Point(card.Width - 150, 22),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Payment Badge
            Label lblPayment = new Label {
                Text = payment == "Paid" ? "● PAID" : (payment == "Unpaid" ? "● UNPAID" : "● NO BILL"),
                Font = new Font("Segoe UI Bold", 7.5F),
                ForeColor = payment == "Paid" ? Color.FromArgb(16, 185, 129) : (payment == "Unpaid" ? Color.FromArgb(239, 68, 68) : Color.Gray),
                Location = new Point(card.Width - 150, 50),
                AutoSize = true
            };

            // Clinical Details (Symptoms, Diagnosis)
            Label lblSymptTitle = new Label { Text = "SYMPTOMS", Font = new Font("Segoe UI Black", 7), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(25, 85), AutoSize = true };
            Label lblSymptoms = new Label { Text = complaint, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(75, 85, 99), Location = new Point(25, 100), Width = 300, AutoSize = false, Height = 40 };

            Label lblDiagTitle = new Label { Text = "DIAGNOSIS", Font = new Font("Segoe UI Black", 7), ForeColor = Color.FromArgb(156, 163, 175), Location = new Point(350, 85), AutoSize = true };
            Label lblDiagnosis = new Label { Text = diagnosis, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(350, 100), Width = 300, AutoSize = false, Height = 40 };

            // Expand Button
            IconButton btnExpand = new IconButton {
                IconChar = IconChar.ChevronDown,
                IconSize = 16,
                IconColor = Color.FromArgb(107, 114, 128),
                BackColor = Color.FromArgb(249, 250, 251),
                Size = new Size(30, 30),
                Location = new Point(card.Width - 45, 125),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExpand.FlatAppearance.BorderSize = 0;

            card.Controls.AddRange(new Control[] { lblDate, lblTime, lblDoctor, lblReason, lblBadge, lblPayment, lblSymptTitle, lblSymptoms, lblDiagTitle, lblDiagnosis, btnExpand });

            bool expanded = false;
            Panel pnlExpanded = null;

            btnExpand.Click += (s, e) => {
                expanded = !expanded;
                btnExpand.IconChar = expanded ? IconChar.ChevronUp : IconChar.ChevronDown;
                
                if (expanded) {
                    pnlExpanded = CreateExpandedSection(visitId, notes);
                    pnlExpanded.Location = new Point(25, 160);
                    pnlExpanded.Width = card.Width - 50;
                    card.Controls.Add(pnlExpanded);
                    card.Height = 160 + pnlExpanded.Height + 20;
                } else {
                    if (pnlExpanded != null) card.Controls.Remove(pnlExpanded);
                    card.Height = 160;
                }
            };

            return card;
        }

        private Panel CreateExpandedSection(int visitId, string notes)
        {
            Panel p = new Panel { AutoSize = true, Padding = new Padding(0, 10, 0, 10) };
            
            Label lblNotesTitle = new Label { Text = "DOCTOR NOTES & ADVICE", Font = new Font("Segoe UI Black", 7), ForeColor = Color.FromArgb(156, 163, 175), Dock = DockStyle.Top, AutoSize = true };
            Label lblNotes = new Label { Text = notes, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(55, 65, 81), Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 5, 0, 20) };
            
            p.Controls.Add(lblNotes);
            p.Controls.Add(lblNotesTitle);

            // Prescriptions
            Label lblPrescTitle = new Label { Text = "PRESCRIPTIONS", Font = new Font("Segoe UI Black", 7), ForeColor = Color.FromArgb(59, 130, 246), Dock = DockStyle.Top, AutoSize = true };
            p.Controls.Add(lblPrescTitle);

            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = @"SELECT m.TradeName, pd.Dosage, pd.Duration 
                                    FROM PrescriptionDetails pd
                                    JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                    JOIN Prescriptions p ON pd.PrescriptionId = p.PrescriptionID
                                    WHERE p.AppointmentIntId = (SELECT AppointmentIntId FROM Visits WHERE VisitID = @vid)";
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@vid", visitId);
                    
                    using (var reader = cmd.ExecuteReader()) {
                        bool any = false;
                        while (reader.Read()) {
                            any = true;
                            Label lblMed = new Label { 
                                Text = $"💊 {reader["TradeName"]} — {reader["Dosage"]} ({reader["Duration"]})", 
                                Font = new Font("Segoe UI Semibold", 9.5F), 
                                ForeColor = Color.FromArgb(31, 41, 55), 
                                Dock = DockStyle.Top, 
                                AutoSize = true, 
                                Padding = new Padding(10, 5, 0, 5) 
                            };
                            p.Controls.Add(lblMed);
                        }
                        if (!any) {
                            Label lblNoPresc = new Label { Text = "No medications prescribed.", Font = new Font("Segoe UI Italic", 9), ForeColor = Color.Gray, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 5, 0, 5) };
                            p.Controls.Add(lblNoPresc);
                        }
                    }
                }
            } catch { }

            return p;
        }
    }
}
