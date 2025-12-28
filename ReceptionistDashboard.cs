using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.IO;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class ReceptionistDashboard : Form
    {
        private string loggedInUsername;
        private Form? activeForm = null;
        private System.Windows.Forms.Timer doctorStatusTimer;
        private System.Windows.Forms.Timer refreshTimer;
        // Connection string - match your actual database
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";


        public ReceptionistDashboard(string loggedInUsername)
        {
            try
            {
                InitializeTheme(); // ✅ Initialize theme variables first
                this.loggedInUsername = loggedInUsername;
                InitializeComponent();
                // InitializeAppointmentsView(); // ✅ Removed: Called inside InitializeComponent
                SetupCustomDrawing();
                WireSidebarEvents();
                UpdateWelcomeMessage();
                WireQuickActionEvents();
                SetupPatientSearch();
                InitializeDoctorMonitoring();


                this.Shown += (s, e) => {
                    LoadDashboardStats();
                    
                    // Setup general refresh timer (15 seconds)
                    refreshTimer = new System.Windows.Forms.Timer();
                    refreshTimer.Interval = 15000;
                    refreshTimer.Tick += (s, e) => LoadDashboardStats();
                    refreshTimer.Start();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}\n{ex.StackTrace}", "Initialization Failed");
            }
        }

        private void SetupCustomDrawing()
        {
            // Enable double buffering for smooth rendering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        private void UpdateWelcomeMessage()
        {
            if (lblWelcome != null)
            {
                lblWelcome.Text = $"Welcome, {loggedInUsername}!";
            }
            UpdateHeaderProfile();
        }

        private void UpdateHeaderProfile()
        {
            try
            {
                if (this.lblHeaderName != null) this.lblHeaderName.Text = loggedInUsername;
                if (this.lblHeaderRole != null) this.lblHeaderRole.Text = "Receptionist";

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT FullName, Username, Role, ProfilePic FROM Users WHERE Username = @Username";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", loggedInUsername.Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fullName = reader["FullName"]?.ToString();
                                string uName = reader["Username"]?.ToString();
                                string role = reader["Role"]?.ToString();
                                
                                string displayName = !string.IsNullOrWhiteSpace(fullName) ? fullName : uName;
                                if (!string.IsNullOrWhiteSpace(displayName))
                                {
                                    if (this.lblHeaderName != null) this.lblHeaderName.Text = displayName;
                                }
                                
                                if (!string.IsNullOrWhiteSpace(role))
                                {
                                    if (this.lblHeaderRole != null) this.lblHeaderRole.Text = role;
                                }
                                
                                RepositionProfileLabels();

                                if (this.pbHeaderProfile != null && reader["ProfilePic"] != DBNull.Value)
                                {
                                    byte[] img = (byte[])reader["ProfilePic"];
                                    using (var ms = new MemoryStream(img))
                                    {
                                        pbHeaderProfile.Image = Image.FromStream(ms);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void RepositionProfileLabels()
        {
            if (lblHeaderName == null || lblHeaderRole == null || pbHeaderProfile == null) return;
            
            lblHeaderName.Refresh();
            lblHeaderRole.Refresh();

            int textRight = pbHeaderProfile.Left - 15;

            lblHeaderName.Location = new Point(
                textRight - lblHeaderName.PreferredWidth,
                10
            );

            lblHeaderRole.Location = new Point(
                textRight - lblHeaderRole.PreferredWidth,
                29
            );
        }

        private void SetupPatientSearch()
        {
            if (txtPatientSearch == null) return;
            
            txtPatientSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformGlobalSearch();
                }
            };
        }

        private void PerformGlobalSearch()
        {
            if (txtPatientSearch == null) return;

            string query = txtPatientSearch.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query) || query == "Search patient (Name / Phone / ID)...")
                return;

            try
            {
                var result = GlobalSearchHelper.PerformGlobalSearch(query);

                if (result.Type == SearchResultType.None)
                {
                    MessageBox.Show(
                        $"No results found for '{query}'.\n\nTry searching by:\n• Patient name, phone, or code\n• Appointment code\n• Queue token number",
                        "No Results",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                // Clear search box
                txtPatientSearch.Text = "Search patient (Name / Phone / ID)...";
                txtPatientSearch.ForeColor = Color.Gray;

                // Navigate based on result type
                switch (result.Type)
                {
                    case SearchResultType.Patient:
                        MessageBox.Show(result.DisplayText, "Patient Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnPatients);
                        OpenChildForm(new PatientsForm());
                        break;

                    case SearchResultType.Appointment:
                        MessageBox.Show(result.DisplayText, "Appointment Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnAppointments);
                        OpenChildForm(new ReceptionistAppointmentsForm());
                        break;

                    case SearchResultType.QueueToken:
                        MessageBox.Show(result.DisplayText, "Queue Token Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnCheckIn);
                        OpenChildForm(new CheckInQueueForm(loggedInUsername));
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WireSidebarEvents()
        {
            if (panelSidebar != null)
            {
                foreach (Control ctrl in panelSidebar.Controls)
                {
                    if (ctrl is FontAwesome.Sharp.IconButton btn)
                    {
                        btn.MouseEnter += (s, e) => {
                            if (btn != previousActiveButton)
                                btn.BackColor = SidebarHoverBackColor;
                        };
                        btn.MouseLeave += (s, e) => {
                            if (btn != previousActiveButton)
                                btn.BackColor = SidebarDefaultBackColor;
                        };
                    }
                }
            }
            
            // Wire Sidebar specific buttons to functional logic
            if (btnDashboard != null) btnDashboard.Click += (s, e) => { HighlightActiveButton(btnDashboard); ShowDashboard(); };
            if (btnPatients != null) btnPatients.Click += (s, e) => { HighlightActiveButton(btnPatients); OpenChildForm(new PatientsForm()); };
            if (btnAppointments != null) btnAppointments.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new ReceptionistAppointmentsForm()); };
            if (btnCheckIn != null) btnCheckIn.Click += (s, e) => { HighlightActiveButton(btnCheckIn); OpenChildForm(new CheckInQueueForm(loggedInUsername)); };
            if (btnBilling != null) btnBilling.Click += (s, e) => { HighlightActiveButton(btnBilling); OpenChildForm(new Billing()); };

            if (btnLogout != null) btnLogout.Click += (s, e) => HandleLogout();

            // Card Specific Navigation
            if (cardPendingPayments != null)
            {
                cardPendingPayments.Cursor = Cursors.Hand;
                cardPendingPayments.Click += (s, e) => { HighlightActiveButton(btnBilling); OpenChildForm(new Billing()); };
                foreach (Control c in cardPendingPayments.Controls) { 
                    c.Cursor = Cursors.Hand; 
                    c.Click += (s, e) => { HighlightActiveButton(btnBilling); OpenChildForm(new Billing()); }; 
                }
            }

            // Default selection
            HighlightActiveButton(btnDashboard);
            ShowDashboard();
        }

        private void LoadDashboardStats()
        {
            Task.Run(() =>
            {
                try
                {
                    using (var con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        // 1. KPI Counts
                        int waiting = 0, todayAppts = 0, checkedIn = 0, pendingPayments = 0;

                        try { waiting = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Visits WHERE Status = 'WAITING' AND CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }
                        try { todayAppts = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }
                        try { checkedIn = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE Status = 'Checked-In' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }
                        try { pendingPayments = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE PaymentStatus = 'Unpaid' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }

                            // 2. Patient Queue Data
                            DataTable dtQueue = new DataTable();
                            try
                            {
                                string queueQuery = @"SELECT TOP 20 V.VisitID, P.PatientName, V.DoctorName, V.VisitDate as AppointmentTime, V.Status, V.TokenNumber, V.DoctorId 
                                                    FROM Visits V 
                                                    LEFT JOIN Patients P ON V.PatientID = P.PatientID 
                                                    WHERE CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE) 
                                                    ORDER BY V.VisitID DESC";
                                new SqlDataAdapter(queueQuery, con).Fill(dtQueue);
                            } catch { }

                        // 3. Doctors On-Duty - Dynamic Status Logic
                        DataTable dtDocs = new DataTable();
                        try
                        {
                            string docQuery = @"
                                SELECT 
                                    D.DoctorID, 
                                    S.StaffName as DoctorName, 
                                    D.Specialization, 
                                    D.RoomNo,
                                    CASE 
                                        WHEN EXISTS (
                                            SELECT 1 FROM Visits V 
                                            WHERE V.DoctorId = D.DoctorID 
                                            AND V.Status = 'WITH_DOCTOR' 
                                            AND CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)
                                        ) THEN 'Busy'
                                        ELSE 'Ready'
                                    END as CurrentStatus
                                FROM Doctors D 
                                LEFT JOIN Staff S ON D.StaffId = S.StaffId 
                                WHERE D.Status IN ('Available', 'Active', 'Busy')";
                            new SqlDataAdapter(docQuery, con).Fill(dtDocs);
                        } catch { }


                        // 4. Recent Activity
                        DataTable dtActivity = new DataTable();
                        try
                        {
                            string activityQuery = @"
                                SELECT TOP 10 Activity, ActivityDate FROM (
                                    SELECT '✅ ' + PatientName + ' checked in' as Activity, LogDate as ActivityDate 
                                    FROM AppointmentAuditLogs L
                                    INNER JOIN Appointments A ON L.AppointmentIntId = A.AppointmentIntId
                                    INNER JOIN Patients P ON A.PatientId = P.PatientID
                                    WHERE L.Action = 'Check-In'
                                    
                                    UNION ALL
                                    
                                    SELECT '➕ New patient registered: ' + PatientName as Activity, RegistrationDate as ActivityDate 
                                    FROM Patients
                                    
                                    UNION ALL
                                    
                                    SELECT '💰 Payment collected: ₨ ' + CAST(TotalAmount AS NVARCHAR) as Activity, BillDate as ActivityDate 
                                    FROM Bills
                                ) Combined
                                ORDER BY ActivityDate DESC";
                            new SqlDataAdapter(activityQuery, con).Fill(dtActivity);
                        } catch { }

                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                UpdateDashboardUI(waiting, todayAppts, checkedIn, pendingPayments, dtQueue, dtDocs, dtActivity);
                            });
                        }
                    }
                }
                catch { }
            });
        }

        private void UpdateDashboardUI(int waiting, int todayAppts, int checkedIn, int pendingPayments, DataTable dtQueue, DataTable dtDocs, DataTable dtActivity)
        {
            try
            {
                // Update KPI Cards
                UpdateCardValue(cardWaiting, waiting.ToString());
                UpdateCardValue(cardTodayAppts, todayAppts.ToString());
                UpdateCardValue(cardCheckedIn, checkedIn.ToString());
                UpdateCardValue(cardPendingPayments, pendingPayments.ToString());

                // Update Lists
                if (panelLeftColumn != null) PopulatePatientQueue(panelLeftColumn, dtQueue);
                if (panelRightColumn != null) PopulateDoctorReadyAlerts(panelRightColumn, dtDocs);

                
                if (lblSubWelcome != null)
                    lblSubWelcome.Text = $"Today's Clinic Overview: {waiting} patients waiting, {checkedIn} checked-in, and {pendingPayments} pending payments.";
            }
            catch { }
        }

        private void UpdateCardValue(Panel card, string value)
        {
            if (card == null) return;
            foreach (Control c in card.Controls)
            {
                if (c is Label lbl && lbl.Font.Size > 20) // The primary value label
                {
                    lbl.Text = value;
                    break;
                }
            }
        }

        private void WireQuickActionEvents()
        {
            // Clear existing events if any (since this is called during rebuild)
            // Note: In WinForms, we can't easily clear delegates without reflection, 
            // but we can ensure we only wire once by using a flag or checking parent logic.
            // For now, these buttons are re-created in RebuildDashboardContent, so the old references are gone.

            if (btnQuickNewPatient != null) btnQuickNewPatient.Click += (s, e) => { HighlightActiveButton(btnPatients); OpenChildForm(new PatientsForm()); };
            if (btnQuickNewAppt != null) btnQuickNewAppt.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new ReceptionistAppointmentsForm()); };
            if (btnQuickTodayAppts != null) btnQuickTodayAppts.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new ReceptionistAppointmentsForm()); };
            if (btnQuickPayment != null) btnQuickPayment.Click += (s, e) => { HighlightActiveButton(btnBilling); OpenChildForm(new Billing()); };

            // Wire the main KPI card for Today's Appointments
            if (cardTodayAppts != null)
            {
                cardTodayAppts.Cursor = Cursors.Hand;
                cardTodayAppts.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new ReceptionistAppointmentsForm()); };
                foreach (Control c in cardTodayAppts.Controls) { 
                    c.Cursor = Cursors.Hand; 
                    c.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new ReceptionistAppointmentsForm()); }; 
                }
            }
        }

        private void HighlightActiveButton(IconButton? btn)
        {
            if (btn == null) return;
            
            if (previousActiveButton != null)
            {
                previousActiveButton.BackColor = SidebarDefaultBackColor;
                previousActiveButton.ForeColor = Color.Gainsboro;
                previousActiveButton.IconColor = Color.Gainsboro;
            }
            
            btn.BackColor = SidebarActiveBackColor;
            btn.ForeColor = Color.White;
            btn.IconColor = Color.White;
            previousActiveButton = btn;
        }
private void RebuildDashboardContent()
{
    try
    {
        if (panelMiddleContent == null) return;

        panelMiddleContent.SuspendLayout();
        panelMiddleContent.Controls.Clear();

        // Re-create columns
        panelLeftColumn = CreateContentCard("📋 Patient Queue - Live", 380);
        panelCenterColumn = CreateContentCard("⚡ Quick Actions Center", 520);
        panelRightColumn = CreateContentCard("👨‍⚕️ Doctors On-Duty", 360);

        panelLeftColumn.AutoScroll = true;
        panelCenterColumn.AutoScroll = false;
        panelRightColumn.AutoScroll = true;

        // Re-populate content (initially empty, then filled by LoadDashboardStats)
        PopulatePatientQueue(panelLeftColumn, null);
        PopulateQuickActions(panelCenterColumn);
        PopulateDoctorReadyAlerts(panelRightColumn, null);


        panelMiddleContent.Controls.Add(panelRightColumn);
        panelMiddleContent.Controls.Add(panelCenterColumn);
        panelMiddleContent.Controls.Add(panelLeftColumn);

        ApplyFixedDesignLayout();

        panelMiddleContent.ResumeLayout();
    }
    catch { }
}

       private void ShowDashboard()
{
    try
    {
        // Close any active child form
        if (activeForm != null)
        {
            activeForm.Close();
            activeForm = null;
        }

        // Remove embedded child forms from panelMain
        if (panelMain != null)
        {
            foreach (Control ctrl in panelMain.Controls.OfType<Form>().ToList())
            {
                panelMain.Controls.Remove(ctrl);
            }
        }

        // Show dashboard layout
        SetDashboardVisibility(true);

        if (pnlAppointmentsView != null)
            pnlAppointmentsView.Visible = false;

        if (btnNewAppt != null)
            btnNewAppt.Visible = false;

        ApplyFixedDesignLayout();
        RebuildDashboardContent();
        LoadDashboardStats();
        // if (lblPageTitle != null) lblPageTitle.Text = "Dashboard Overview";
    }
    catch { }
}

        private void SetDashboardVisibility(bool visible)
        {
            try
            {
                if (panelTopToolbar != null)
                    panelTopToolbar.Visible = visible; // Toggle with dashboard

                if (panelWelcome != null)
                    panelWelcome.Visible = visible;

                if (panelCardsRow != null)
                    panelCardsRow.Visible = visible;

                if (panelMiddleContent != null)
                    panelMiddleContent.Visible = visible;

                if (panelMain != null)
                {
                    // Restore dashboard padding if visible, else remove it
                    panelMain.Padding = visible ? new Padding(30, 0, 30, 20) : new Padding(0);
                }

                if (pnlAppointmentsView != null)
                    pnlAppointmentsView.Visible = !visible;

                if (visible)
                {
                    panelTopToolbar?.BringToFront();
                    panelWelcome?.BringToFront();
                    panelCardsRow?.BringToFront();
                    panelMiddleContent?.BringToFront();
                }
            }
            catch { }
        }

private void ShowCheckInQueue()
{
    ShowDashboard();

    MessageBox.Show(
        "Check-In / Queue management.\nScroll to the Patient Queue section.",
        "Queue Management",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
    );
}

        private void OpenChildForm(Form childForm)
        {
            if (activeForm != null)
                activeForm.Close();

            SetDashboardVisibility(false);
            // if (lblPageTitle != null) lblPageTitle.Text = childForm.Text;
            if (pnlAppointmentsView != null) pnlAppointmentsView.Visible = false;
            if (btnNewAppt != null) btnNewAppt.Visible = false;

            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            if (panelMain != null)
            {
                panelMain.Padding = new Padding(0); // Remove padding for child forms
                panelMain.Controls.Add(childForm);
                panelMain.Tag = childForm;
            }

            childForm.BringToFront();
            childForm.Show();
        }

        private void HandleLogout()
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to logout?", 
                "Confirm Logout", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // Close this form and show login
                this.Hide();
                Login loginForm = new Login();
                loginForm.ShowDialog();
                this.Close();
            }
        }

        private void OpenProfileForm()
        {
            using (ProfileForm profileForm = new ProfileForm(loggedInUsername, "Receptionist", UpdateHeaderProfile))
            {
                profileForm.ShowDialog(this);
            }
        }
        // Re-routing old resize handler if called by designer logic or events
        private void ReceptionistDashboard_Resize(object sender, EventArgs e)
        {
            ApplyFixedDesignLayout();
        }

        // ================= ENHANCED DRAWING METHODS =================
        // These methods are used for custom painting of cards and charts
        
        // Draw mini line chart for stat cards
        public void DrawMiniLineChart(Graphics g, Rectangle rect, Color color)
        {
            PointF[] points = new PointF[]
            {
                new PointF(rect.X, rect.Bottom - 10),
                new PointF(rect.X + 20, rect.Bottom - 20),
                new PointF(rect.X + 40, rect.Bottom - 15),
                new PointF(rect.X + 60, rect.Bottom - 25),
                new PointF(rect.X + 80, rect.Bottom - 18),
                new PointF(rect.Right, rect.Bottom - 22)
            };
            
            // Draw gradient fill under the line
            PointF[] fillPoints = new PointF[points.Length + 2];
            fillPoints[0] = new PointF(rect.X, rect.Bottom);
            Array.Copy(points, 0, fillPoints, 1, points.Length);
            fillPoints[fillPoints.Length - 1] = new PointF(rect.Right, rect.Bottom);
            
            using (GraphicsPath fillPath = new GraphicsPath())
            {
                fillPath.AddLines(fillPoints);
                fillPath.CloseAllFigures();
                
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(40, color.R, color.G, color.B),
                    Color.FromArgb(5, color.R, color.G, color.B),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(fillBrush, fillPath);
                }
            }
            
            // Draw the line
            using (Pen pen = new Pen(color, 2.5f))
            {
                pen.LineJoin = LineJoin.Round;
                pen.EndCap = LineCap.Round;
                pen.StartCap = LineCap.Round;
                g.DrawLines(pen, points);
            }
            
            // Draw data points
            foreach (PointF point in points)
            {
                using (SolidBrush pointBrush = new SolidBrush(color))
                {
                    g.FillEllipse(pointBrush, point.X - 2, point.Y - 2, 4, 4);
                }
            }
        }

        // Draw mini bar chart
        public void DrawMiniBarChart(Graphics g, Rectangle rect, Color color)
        {
            int barWidth = 12;
            int[] heights = { 25, 20, 30, 18, 24, 19 };
            int spacing = 3;
            int x = rect.X;
            
            for (int i = 0; i < heights.Length; i++)
            {
                Rectangle barRect = new Rectangle(x, rect.Bottom - heights[i], barWidth, heights[i]);
                
                // Draw rounded top corners
                GraphicsPath barPath = new GraphicsPath();
                int cornerRadius = 3;
                barPath.AddArc(barRect.X, barRect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
                barPath.AddLine(barRect.X + cornerRadius, barRect.Y, barRect.Right - cornerRadius, barRect.Y);
                barPath.AddArc(barRect.Right - cornerRadius * 2, barRect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
                barPath.AddLine(barRect.Right, barRect.Y + cornerRadius, barRect.Right, barRect.Bottom);
                barPath.AddLine(barRect.Right, barRect.Bottom, barRect.X, barRect.Bottom);
                barPath.AddLine(barRect.X, barRect.Bottom, barRect.X, barRect.Y + cornerRadius);
                barPath.CloseAllFigures();
                
                // Draw gradient fill
                using (LinearGradientBrush barBrush = new LinearGradientBrush(
                    barRect,
                    Color.FromArgb(220, color.R, color.G, color.B),
                    Color.FromArgb(160, color.R, color.G, color.B),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(barBrush, barPath);
                }
                
                x += barWidth + spacing;
            }
        }

        // Draw mini donut chart
        public void DrawMiniDonutChart(Graphics g, Rectangle rect, Color color, int percentage)
        {
            Rectangle outerRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            Rectangle innerRect = new Rectangle(rect.X + 15, rect.Y + 15, rect.Width - 30, rect.Height - 30);
            
            // Draw background circle
            using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                outerRect,
                Color.FromArgb(245, 245, 245),
                Color.FromArgb(235, 235, 235),
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(bgBrush, outerRect);
            }
            
            // Draw percentage segment
            float sweepAngle = (percentage / 100f) * 360f;
            using (GraphicsPath piePath = new GraphicsPath())
            {
                piePath.AddPie(outerRect.X, outerRect.Y, outerRect.Width, outerRect.Height, -90, sweepAngle);
                
                using (LinearGradientBrush segmentBrush = new LinearGradientBrush(
                    outerRect,
                    color,
                    Color.FromArgb(Math.Min(255, color.R + 30), Math.Min(255, color.G + 30), Math.Min(255, color.B + 30)),
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillPath(segmentBrush, piePath);
                }
            }
            
            // Draw inner circle (donut hole)
            using (SolidBrush whiteBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(whiteBrush, innerRect);
            }
            
            // Draw percentage text
            Font font = new Font("Segoe UI", 8, FontStyle.Bold);
            string text = $"{percentage}%";
            SizeF textSize = g.MeasureString(text, font);
            PointF textPos = new PointF(
                rect.X + (rect.Width - textSize.Width) / 2,
                rect.Y + (rect.Height - textSize.Height) / 2);
            
            g.DrawString(text, font, new SolidBrush(color), textPos);
        }

        // ================= DOCTOR READY ALERTS FEATURE =================

        // Initialize real-time doctor monitoring
        private void InitializeDoctorMonitoring()
        {
            doctorStatusTimer = new System.Windows.Forms.Timer();
            doctorStatusTimer.Interval = 5000; // Check every 5 seconds
            doctorStatusTimer.Tick += (s, e) => RefreshDoctorReadyAlerts();
            doctorStatusTimer.Start();
        }

        // Refresh Doctor Ready Alerts panel
        private void RefreshDoctorReadyAlerts()
        {
            try
            {
                // Find the alert list panel
                Panel? alertList = panelRightColumn?.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Name == "pnlAlertList");
                
                if (alertList == null) return;

                // This would normally query the database for ready doctors
                //  For now, keeping the sample data from Designer.cs
                // In production, call GetReadyDoctors() and update UI if changed
            }
            catch (Exception ex)
            {
                // Log error silently - don't interrupt workflow
                System.Diagnostics.Debug.WriteLine($"Error refreshing doctor alerts: {ex.Message}");
            }
        }

        // Event handler for Send Next Patient button
        private void BtnSendPatient_Click(object? sender, EventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            string? doctorId = btn.Tag?.ToString();
            if (string.IsNullOrEmpty(doctorId)) return;

            try
            {
                // Get next waiting patient
                var patient = GetNextWaitingPatient(doctorId);

                if (patient == null)
                {
                    // DIAGNOSTIC DUMP: Show what IS in the database to confirm mismatch
                    string debugInfo = "Diagnosis: No strict match found.\n\nCurrent Queue Data (Today):\n";
                    try {
                        using (var con = new SqlConnection(connectionString)) {
                            con.Open();
                            string dumpQ = "SELECT V.VisitID, P.PatientName, V.DoctorId, V.DoctorName, V.Status FROM Visits V LEFT JOIN Patients P ON V.PatientId = P.PatientID WHERE V.Status='WAITING' AND CAST(V.VisitDate AS DATE)=CAST(GETDATE() AS DATE)";
                            using (var cmd = new SqlCommand(dumpQ, con)) 
                            using (var r = cmd.ExecuteReader()) {
                                while(r.Read()) {
                                    string dId = r["DoctorId"] != DBNull.Value ? r["DoctorId"].ToString() : "NULL";
                                    string dName = r["DoctorName"] != DBNull.Value ? r["DoctorName"].ToString() : "NULL";
                                    string pName = r["PatientName"] != DBNull.Value ? r["PatientName"].ToString() : "Unknown";
                                    debugInfo += $"- Pat: {pName}, DocID: {dId}, DocName: {dName}\n";
                                }
                            }
                        }
                    } catch (Exception ex) { debugInfo += "Error reading DB: " + ex.Message; }

                    MessageBox.Show(
                        $"Could not find next patient for Doctor ID: {doctorId}.\n\n{debugInfo}",
                        "Queue Mismatch Diagnosis",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Confirm assignment
                DialogResult result = MessageBox.Show(
                    $"Send patient '{patient.PatientName}' to the doctor?",
                    "Confirm Assignment",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;

                // Assign patient to doctor (using VisitID for precision)
                bool success = AssignPatientToDoctor(patient.VisitID, doctorId, patient.PatientName);

                if (success)
                {
                    MessageBox.Show(
                        $"Patient {patient.PatientName} has been sent to the doctor.",
                        "Patient Assigned",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Refresh UI
                    RefreshDoctorReadyAlerts();
                    // Note: You would also refresh patient queue here
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error assigning patient: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private PatientInfo? GetNextWaitingPatient(string doctorId)
        {
            try
            {
                // Updated query to strict prioritize WAITING patients for THIS DOCTOR
                string query = @"
                    SELECT TOP 1 V.VisitID, V.AppointmentIntId, P.PatientName 
                    FROM Visits V
                    INNER JOIN Patients P ON V.PatientId = P.PatientID
                    WHERE V.Status = 'WAITING' 
                    AND V.DoctorId = @docId
                    AND CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER BY V.VisitID ASC";

                int parsedDocId;
                if (!int.TryParse(doctorId, out parsedDocId)) return null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // 1. Try Strict Match
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@docId", SqlDbType.Int).Value = parsedDocId;
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new PatientInfo
                                {
                                    VisitID = (int)reader["VisitID"],
                                    AppointmentIntId = reader["AppointmentIntId"] != DBNull.Value ? (int)reader["AppointmentIntId"] : 0,
                                    PatientName = reader["PatientName"] != DBNull.Value ? reader["PatientName"].ToString() : "Unknown"
                                };
                            }
                        }
                    }

                    // 2. Fallback: Self-Healing for Legacy/Corrupt Data
                    // If we are here, no patient was found by ID. Let's check by Name.
                    string nameQuery = "SELECT DoctorName FROM Doctors WHERE DoctorID = @did";
                    string docName = "";
                    using(var nameCmd = new SqlCommand(nameQuery, con))
                    {
                        nameCmd.Parameters.AddWithValue("@did", parsedDocId);
                        object result = nameCmd.ExecuteScalar();
                        if (result != null) docName = result.ToString();
                    }

                    if (!string.IsNullOrEmpty(docName))
                    {
                        // Find patient with matching Doctor Name but Invalid ID
                        string fallbackQ = @"SELECT TOP 1 V.VisitID, V.AppointmentIntId, P.PatientName 
                                           FROM Visits V
                                           INNER JOIN Patients P ON V.PatientId = P.PatientID
                                           WHERE V.Status = 'WAITING' 
                                           AND (V.DoctorId IS NULL OR V.DoctorId = 0)
                                           AND V.DoctorName = @dname
                                           AND CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)";
                        
                        int foundVisitId = 0;
                        PatientInfo? recoveredPatient = null;

                        using(var fbCmd = new SqlCommand(fallbackQ, con))
                        {
                            fbCmd.Parameters.AddWithValue("@dname", docName);
                            using(var rdr = fbCmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    foundVisitId = (int)rdr["VisitID"];
                                    recoveredPatient = new PatientInfo {
                                        VisitID = foundVisitId,
                                        AppointmentIntId = rdr["AppointmentIntId"] != DBNull.Value ? (int)rdr["AppointmentIntId"] : 0,
                                        PatientName = rdr["PatientName"]?.ToString() ?? "Unknown"
                                    };
                                }
                            }
                        }

                        // Auto-Fix the record if found
                        if (foundVisitId > 0 && recoveredPatient != null)
                        {
                            string fixQ = "UPDATE Visits SET DoctorId = @did WHERE VisitID = @vid";
                            using(var fixCmd = new SqlCommand(fixQ, con))
                            {
                                fixCmd.Parameters.AddWithValue("@did", parsedDocId);
                                fixCmd.Parameters.AddWithValue("@vid", foundVisitId);
                                fixCmd.ExecuteNonQuery();
                            }
                            return recoveredPatient;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting next patient: {ex.Message}");
            }
            return null;
        }

        // Assign patient to doctor
        private bool AssignPatientToDoctor(int visitId, string doctorId, string patientName)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. Update Visit Status to 'WITH_DOCTOR'
                            string updateVisit = @"
                                UPDATE Visits 
                                SET Status = 'WITH_DOCTOR', DoctorId = @docId 
                                WHERE VisitID = @vid AND Status = 'WAITING'"; 

                            using (var cmdVisit = new SqlCommand(updateVisit, con, transaction)) {
                                cmdVisit.Parameters.AddWithValue("@docId", doctorId);
                                cmdVisit.Parameters.AddWithValue("@vid", visitId);
                                int rows = cmdVisit.ExecuteNonQuery();

                                if (rows == 0) throw new Exception("Could not find a 'WAITING' visit for this patient. Status may have changed.");
                            }

                            // 2. Update Appointment Status to 'WITH_DOCTOR'
                            string updateAppt = @"
                                UPDATE Appointments 
                                SET Status = 'WITH_DOCTOR' 
                                WHERE AppointmentIntId = (SELECT AppointmentIntId FROM Visits WHERE VisitID = @vid)";
                            using (var cmdAppt = new SqlCommand(updateAppt, con, transaction)) {
                                cmdAppt.Parameters.AddWithValue("@vid", visitId);
                                cmdAppt.ExecuteNonQuery();
                            }

                            // 3. Update Doctor Status to 'Busy'
                            string updateDoc = "UPDATE Doctors SET Status = 'Busy' WHERE DoctorID = @docId";
                            using (var cmdDoc = new SqlCommand(updateDoc, con, transaction)) {
                                cmdDoc.Parameters.AddWithValue("@docId", doctorId);
                                cmdDoc.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Assignment failed: " + ex.Message);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Assignment failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Helper classes for Doctor Ready Alerts
        private class PatientInfo
        {
            public int VisitID { get; set; }
            public int AppointmentIntId { get; set; }
            public string PatientName { get; set; }
        }

        private class DoctorAlert
        {
            public string? DoctorID { get; set; }
            public string? DoctorName { get; set; }
            public string? Department { get; set; }
            public string? RoomNo { get; set; }
        }


        // Enhanced card paint with shadows
        public void PanelCard_Paint(object? sender, PaintEventArgs e)
        {
            try {
                Panel? panel = sender as Panel;
                if (panel == null) return;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                int radius = 12;
                
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, rect.Width, rect.Height, radius))
                {
                    // Draw multi-layer shadow for depth
                    for (int i = 3; i >= 1; i--)
                    {
                        Rectangle shadowRect = new Rectangle(i, i * 2, panel.Width - 1, panel.Height - 1);
                        using (GraphicsPath shadowPath = CreateRoundedRectPath(shadowRect.X, shadowRect.Y, shadowRect.Width, shadowRect.Height, radius))
                        {
                            int alpha = 5 * i;
                            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                            {
                                g.FillPath(shadowBrush, shadowPath);
                            }
                        }
                    }

                    // Draw white background with subtle gradient
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        rect,
                        Color.White,
                        Color.FromArgb(252, 252, 252),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bgBrush, path);
                    }
                    
                    // Draw subtle border
                    using (Pen borderPen = new Pen(Color.FromArgb(245, 245, 245), 1))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }
            } catch { /* Silent guard for GDI+ errors */ }
        }

        // Enhanced button paint with rounded corners
        public void BtnRounded_Paint(object? sender, PaintEventArgs e)
        {
            try 
            {
                Button? btn = sender as Button;
                if (btn == null) return;
                
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                int radius = 8;
                
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseAllFigures();
                
                // Draw button background with gradient
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect,
                    btn.BackColor,
                    Color.FromArgb(Math.Max(0, btn.BackColor.R - 10), 
                                  Math.Max(0, btn.BackColor.G - 10), 
                                  Math.Max(0, btn.BackColor.B - 10)),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
                
                // Draw text
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                
                using (SolidBrush textBrush = new SolidBrush(btn.ForeColor))
                {
                    g.DrawString(btn.Text, btn.Font, textBrush, rect, sf);
                }
            } catch { }
        }

        // Setup button hover effects
        public void SetupButtonHover(Button btn, Color hoverColor)
        {
            Color originalColor = btn.BackColor;
            
            btn.MouseEnter += (s, e) => {
                btn.BackColor = hoverColor;
                btn.Cursor = Cursors.Hand;
            };
            btn.MouseLeave += (s, e) => {
                btn.BackColor = originalColor;
                btn.Cursor = Cursors.Default;
            };
            btn.MouseDown += (s, e) => {
                btn.BackColor = Color.FromArgb(
                    Math.Max(0, hoverColor.R - 20),
                    Math.Max(0, hoverColor.G - 20),
                    Math.Max(0, hoverColor.B - 20));
            };
            btn.MouseUp += (s, e) => {
                btn.BackColor = hoverColor;
            };
        }

        // ================= HELPERS & LOGIC MOVED FROM DESIGNER =================

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private void PopulatePatientQueue(Panel p, DataTable? data = null)
        {
            Panel list = p.Controls.OfType<Panel>().FirstOrDefault(x => x.Name == "pnlPatientQueueList");

            if (list == null)
            {
                list = new Panel
                {
                    Name = "pnlPatientQueueList",
                    Location = new Point(15, CONTENT_TOP_OFFSET),
                    Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20),
                    AutoScroll = true,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0, 0, 8, 0)
                };
                p.Controls.Add(list);
                p.Resize += (s, e) => {
                    list.Width = p.Width - 30;
                    list.Height = p.Height - CONTENT_TOP_OFFSET - 20;
                };
            }

            list.Controls.Clear();

            if (data == null || data.Rows.Count == 0)
            {
                // Skeleton loading state
                for (int i = 0; i < 3; i++)
                {
                    Panel skeleton = CreateQueueItem("----", "Loading...", "Wait", "--:--", "...", "");
                    skeleton.Location = new Point(0, i * 140);
                    skeleton.Width = list.Width - 25;
                    skeleton.Enabled = false; 
                    list.Controls.Add(skeleton);
                }
                return;
            }

            int y = 0;
            foreach (DataRow row in data.Rows)
            {
                // Safely get Doctor ID for debug display
                string docId = "N/A";
                if (data.Columns.Contains("DoctorId") && row["DoctorId"] != DBNull.Value)
                    docId = row["DoctorId"].ToString();

                Panel item = CreateQueueItem(
                    row["TokenNumber"].ToString() ?? "0",
                    row["PatientName"].ToString() ?? "Unknown",
                    row["DoctorName"].ToString() ?? "Not Assigned",
                    row["AppointmentTime"] != DBNull.Value ? Convert.ToDateTime(row["AppointmentTime"]).ToString("hh:mm tt") : "--:--",
                    row["Status"].ToString() ?? "Waiting",
                    docId 
                );

                item.Location = new Point(0, y);
                item.Width = list.Width - 25;
                list.Controls.Add(item);
                y += 140; 
            }
        }

        private Panel CreateQueueItem(string token, string patientName, string doctor, string time, string status, string docId)
        {
            Panel item = new Panel { Size = new Size(330, 120), BackColor = Color.FromArgb(252, 253, 254), Cursor = Cursors.Hand, Padding = new Padding(5) };
            item.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, item.Width - 1, item.Height - 1, 10)) {
                    item.Region = new Region(path);
                    using (Pen p = new Pen(Color.FromArgb(240, 243, 246))) e.Graphics.DrawPath(p, path);
                }
            };

            Label lblToken = new Label { Text = token, Font = new Font("Segoe UI Bold", 14), ForeColor = primaryBlue, Location = new Point(20, 15), AutoSize = true };
            Label lblName = new Label { Text = patientName, Font = new Font("Segoe UI Bold", 10), ForeColor = textPrimary, Location = new Point(20, 45), AutoSize = true };
            // Added ID to display
            Label lblDoctor = new Label { Text = $"👨‍⚕️ {doctor} (ID:{docId})", Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(20, 68), AutoSize = true };
            Label lblTime = new Label { Text = $"🕐 {time}", Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(20, 88), AutoSize = true };

            // Format status text
            string displayStatus = status.Replace("_", " ");
            displayStatus = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(displayStatus.ToLower());

            Label statusBadge = new Label { Text = displayStatus, Font = new Font("Segoe UI Semibold", 7.5F), TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
            
            Color statusColor = Color.Gray;
            if (status.Equals("Waiting", StringComparison.OrdinalIgnoreCase)) statusColor = accentOrange;
            else if (status.Equals("Checked-In", StringComparison.OrdinalIgnoreCase)) statusColor = primaryBlue;
            else if (status.Equals("WITH_DOCTOR", StringComparison.OrdinalIgnoreCase) || status.Equals("With Doctor", StringComparison.OrdinalIgnoreCase)) statusColor = accentGreen;
            
            statusBadge.BackColor = Color.FromArgb(30, statusColor);
            statusBadge.ForeColor = statusColor;
            statusBadge.Location = new Point(item.Width - statusBadge.PreferredWidth - 20, 15);

            item.Controls.AddRange(new Control[] { lblToken, lblName, lblDoctor, lblTime, statusBadge });
            return item;
        }

        private void PopulateQuickActions(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Location = new Point(20, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width - 40, 300),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Name = "pnlQuickActionsFlow"
            };
            
            // Check if already added
            if (p.Controls.ContainsKey("pnlQuickActionsFlow")) return;

            p.Controls.Add(flow);
            p.Resize += (s, e) => { flow.Width = p.Width - 40; };

            btnQuickNewPatient = CreateQuickActionButton("➕ New Patient Registration", primaryBlue);
            btnQuickNewAppt = CreateQuickActionButton("📅 New Appointment", accentPurple);
            btnQuickTodayAppts = CreateQuickActionButton("📋 Today's Appointments", Color.FromArgb(59, 130, 246));
            btnQuickPayment = CreateQuickActionButton("💳 Collect Payment", accentGreen);

            // Re-wire events because we created new buttons
            WireQuickActionEvents();

            flow.Controls.AddRange(new Control[] { btnQuickNewPatient, btnQuickNewAppt, btnQuickTodayAppts, btnQuickPayment });

            // Recent Activity removed from here and moved to dedicated Right Column

        }

        private Button CreateQuickActionButton(string text, Color bgColor)
        {
             Button btn = new Button { 
                Text = text, 
                Size = new Size(475, 60),
                BackColor = bgColor, 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI Semibold", 11), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 25, 0),
                Margin = new Padding(0, 0, 0, 18)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(Math.Max(0, bgColor.R - 20), Math.Max(0, bgColor.G - 20), Math.Max(0, bgColor.B - 20)); 
            btn.MouseLeave += (s, e) => btn.BackColor = bgColor;
            btn.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, btn.Width - 1, btn.Height - 1, 10)) { 
                    btn.Region = new Region(path); 
                } 
            };
            return btn;
        }

        private void PopulateDoctorReadyAlerts(Panel p, DataTable? data = null)
        {
             // Check if title exists
             if (!p.Controls.OfType<Label>().Any(l => l.Text.Contains("Doctor Ready Alerts"))) {
                Label lblTitle = new Label { 
                    Text = "🟢 Doctor Ready Alerts", 
                    Font = new Font("Segoe UI Semibold", 12), 
                    ForeColor = textPrimary, 
                    Location = new Point(20, 20), 
                    AutoSize = true 
                };
                p.Controls.Add(lblTitle);
             }

            Panel? alertList = p.Controls.OfType<Panel>().FirstOrDefault(x => x.Name == "pnlAlertList");
            if (alertList == null)
            {
                alertList = new Panel
                {
                    Name = "pnlAlertList",
                    Location = new Point(20, CONTENT_TOP_OFFSET),
                    Size = new Size(p.Width - 40, p.Height - CONTENT_TOP_OFFSET - 20),
                    AutoScroll = true,
                    BackColor = Color.FromArgb(249, 250, 251)
                };
                p.Controls.Add(alertList);
                p.Resize += (s, e) => {
                    alertList.Width = p.Width - 40;
                    alertList.Height = p.Height - CONTENT_TOP_OFFSET - 20;
                };
            }

            alertList.Controls.Clear();

            if (data == null || data.Rows.Count == 0) {
                Label emptyMsg = new Label {
                    Text = "No doctors ready at the moment.\nAlerts will appear here automatically.",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = textSecondary,
                    Location = new Point(0, 100),
                    Size = new Size(alertList.Width, 60),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                alertList.Controls.Add(emptyMsg);
                return;
            }

            int y = 0;
            foreach (DataRow row in data.Rows)
            {
                Panel card = CreateDoctorReadyCard(
                    row["DoctorID"].ToString() ?? "0", 
                    row["DoctorName"].ToString() ?? "Unknown",
                    row["Specialization"].ToString() ?? "General",
                    row["RoomNo"].ToString() ?? "N/A",
                    row["CurrentStatus"]?.ToString() ?? "Ready"
                );

                card.Location = new Point(10, y);
                card.Width = alertList.Width - 25;
                
                alertList.Controls.Add(card);
                y += 180;
            }
        }

        private Panel CreateDoctorReadyCard(string doctorId, string doctorName, string department, string room, string currentStatus = "Ready")
        {
            bool isBusy = currentStatus.Equals("Busy", StringComparison.OrdinalIgnoreCase);

            Panel card = new Panel { 
                Size = new Size(330, 170), 
                BackColor = Color.White, 
                Padding = new Padding(15),
                Tag = doctorId 
            };
            
            card.Paint += (s, e) => {
                try {
                    if (e.Graphics == null || card.Width <= 0 || card.Height <= 0) return;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12)) {
                        e.Graphics.FillPath(Brushes.White, path);
                        
                        Color statusIndicatorColor = isBusy ? accentRose : accentGreen;

                        using (SolidBrush statusBrush = new SolidBrush(statusIndicatorColor)) {
                            e.Graphics.FillRectangle(statusBrush, 0, 15, 4, card.Height - 30);
                        }
                        
                        using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220))) {
                            e.Graphics.DrawPath(borderPen, path);
                        }
                    }
                } catch { }
            };

            Label lblName = new Label { Text = doctorName, Font = new Font("Segoe UI Bold", 11), ForeColor = textPrimary, Location = new Point(20, 15), AutoSize = true };
            Label lblDept = new Label { Text = $"{department} • Room {room}", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(20, 40), AutoSize = true };

            string statusText = isBusy ? "🔴 Doctor Busy" : "✅ Doctor Ready";
            Color badgeForeColor = isBusy ? Color.FromArgb(153, 27, 27) : Color.FromArgb(21, 128, 61);
            Color badgeBackColor = isBusy ? Color.FromArgb(254, 226, 226) : Color.FromArgb(220, 252, 231);

            Label lblStatus = new Label { 
                Text = statusText, 
                Font = new Font("Segoe UI Semibold", 9), 
                ForeColor = badgeForeColor, 
                BackColor = badgeBackColor, 
                Location = new Point(20, 65), 
                AutoSize = true, 
                Padding = new Padding(5, 2, 5, 2) 
            };

            Button btnSend = new Button { 
                Text = isBusy ? "⏳ In Session" : "➡️ Send Patient", 
                Size = new Size(130, 35), 
                Location = new Point(20, 110), 
                BackColor = isBusy ? Color.FromArgb(209, 213, 219) : accentGreen, 
                ForeColor = isBusy ? Color.FromArgb(107, 114, 128) : Color.White, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9),
                Cursor = isBusy ? Cursors.No : Cursors.Hand,
                Enabled = !isBusy,
                Tag = doctorId
            };
            btnSend.FlatAppearance.BorderSize = 0;
            if (!isBusy) SetupButtonHover(btnSend, accentGreen);
            btnSend.Click += BtnSendPatient_Click;

            Button btnReady = new Button { 
                Text = "🔔 Alert Ready", 
                Size = new Size(130, 35), 
                Location = new Point(160, 110), 
                BackColor = Color.FromArgb(243, 244, 246), 
                ForeColor = textPrimary, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9),
                Cursor = Cursors.Hand,
                Tag = doctorId
            };
            btnReady.FlatAppearance.BorderSize = 0;
            SetupButtonHover(btnReady, Color.FromArgb(229, 231, 235));
            btnReady.Click += (s, e) => {
                string? dId = btnReady.Tag?.ToString();
                if (!string.IsNullOrEmpty(dId)) {
                    SetDoctorReady(dId);
                }
            };

            card.Controls.AddRange(new Control[] { lblName, lblDept, lblStatus, btnSend, btnReady });
            return card;
        }

        private void SetDoctorReady(string doctorId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "UPDATE Doctors SET Status = 'Active' WHERE DoctorID = @docId";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@docId", doctorId);
                        cmd.ExecuteNonQuery();
                    }
                }
                
                // Refresh the dashboard to show changes
                LoadDashboardStats();
                
                MessageBox.Show("Doctor status has been updated to Ready.", "System Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating doctor status: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
