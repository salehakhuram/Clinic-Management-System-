using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Data;

namespace ClinicManagement
{
    public partial class DoctorDashboard : Form
    {
        private string loggedInUsername;
        private IconButton? previousActiveButton = null;
        private Form? activeForm = null;
        private ToolTip toolTip;

        public DoctorDashboard(string loggedInUsername)
        {
            try
            {
                this.loggedInUsername = loggedInUsername;
                InitializeTheme(); // Load colors
                toolTip = new ToolTip();
                InitializeComponent(); // Build UI (Flat Hierarchy)
                
                SetupToolTips();
                SetupCustomDrawing();
                WireSidebarEvents();
                UpdateWelcomeMessage();
                SetupGlobalSearch();
                WireHeaderStatusEvents();
                SeedIfEmpty();
                LoadDoctorStats();
                
                // Safety hook for layout
                this.Resize += (s, e) => {
                    try { ApplyFixedDesignLayout(); } catch { }
                };

                // Polling Timer for Real-time updates
                System.Windows.Forms.Timer pollTimer = new System.Windows.Forms.Timer();
                pollTimer.Interval = 10000; // 10 seconds
                pollTimer.Tick += (s, e) => {
                    if (this.Visible) LoadDoctorStats();
                };
                pollTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}", "Doctor Dashboard Error");
            }
        }

        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";
        private string _doctorFullName = "";
        private int _doctorId = -1;
        private TextBox txtGlobalSearch;

        private void SetupGlobalSearch()
        {
            // Find the search textbox in the pnlSearch panel
            Panel searchPanel = panelTopToolbar?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlSearch");
            if (searchPanel != null)
            {
                txtGlobalSearch = searchPanel.Controls.OfType<TextBox>().FirstOrDefault();
                if (txtGlobalSearch != null)
                {
                    txtGlobalSearch.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.SuppressKeyPress = true;
                            PerformGlobalSearch();
                        }
                    };
                }
            }
        }

        private void PerformGlobalSearch()
        {
            if (txtGlobalSearch == null) return;

            string query = txtGlobalSearch.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query) || query.Contains("Search"))
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
                txtGlobalSearch.Text = "";

                // Navigate based on result type
                switch (result.Type)
                {
                    case SearchResultType.Patient:
                        MessageBox.Show(result.DisplayText, "Patient Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnHistory);
                        OpenDoctorsHistory();
                        break;

                    case SearchResultType.Appointment:
                        MessageBox.Show(result.DisplayText, "Appointment Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnAppointments);
                        OpenDoctorsAppointments();
                        break;

                    case SearchResultType.QueueToken:
                        MessageBox.Show(result.DisplayText, "Queue Token Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnQueue);
                        OpenDoctorsQueue();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateWelcomeMessage()
        {
            if (lblWelcome != null)
            {
                lblWelcome.Text = $"Welcome, {loggedInUsername}!";
            }
            UpdateHeaderProfile();
            ResolveDoctorIdentity();
        }

        private void ResolveDoctorIdentity()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // 1. Resolve FullName from Users via Username
                    string userQuery = "SELECT FullName FROM Users WHERE Username = @User";
                    using (var cmd = new SqlCommand(userQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@User", loggedInUsername);
                        _doctorFullName = cmd.ExecuteScalar()?.ToString() ?? "";
                    }

                    if (string.IsNullOrEmpty(_doctorFullName))
                    {
                        _doctorFullName = loggedInUsername; // Fallback
                    }

                    // 2. Resolve DoctorId via FullName (Robust fuzzy match)
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
                        cmd.Parameters.AddWithValue("@User", loggedInUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _doctorId = (int)reader["DoctorId"];
                                _doctorFullName = reader["DoctorName"].ToString();
                            }
                        }
                    }

                    if (_doctorId == -1)
                    {
                        // Fuzzy Match: try without "Dr." and with "Ahmed/Ahmad" flexible
                        string fuzzyName = _doctorFullName.Replace("Dr.", "").Replace("Dr", "").Trim();
                        using (var cmd = new SqlCommand("SELECT DoctorId, DoctorName FROM Doctors WHERE DoctorName LIKE @Fuzzy", con))
                        {
                            cmd.Parameters.AddWithValue("@Fuzzy", "%" + fuzzyName + "%");
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

                    if (_doctorId == -1)
                    {
                        // Final fallback: try username as name
                        using (var cmd = new SqlCommand("SELECT DoctorId, DoctorName FROM Doctors WHERE DoctorName LIKE @User", con))
                        {
                            cmd.Parameters.AddWithValue("@User", "%" + loggedInUsername.Split('.')[0] + "%");
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

                    if (_doctorId != -1)
                    {
                        using (var cmdP = new SqlCommand("SELECT Specialization FROM Doctors WHERE DoctorId = @Id", con))
                        {
                            cmdP.Parameters.AddWithValue("@Id", _doctorId);
                            var spec = cmdP.ExecuteScalar()?.ToString();
                            if (!string.IsNullOrEmpty(spec) && lblHeaderRole != null)
                            {
                                lblHeaderRole.Text = spec;
                            }
                        }
                    }
                }
                RepositionProfileLabels();
            }
            catch { }
        }

        private void UpdateHeaderProfile()
        {
            try
            {
                if (this.lblHeaderName != null) this.lblHeaderName.Text = loggedInUsername;
                if (this.lblHeaderRole != null) this.lblHeaderRole.Text = "Doctor";

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
                                    using (var ms = new System.IO.MemoryStream(img))
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
            
            // Force layout to ensure Width/PreferredWidth is accurate for new text
            lblHeaderName.Refresh(); 
            lblHeaderRole.Refresh();

            int textRight = pbHeaderProfile.Left - 15; // 15px safe gap

            lblHeaderName.Location = new Point(
                textRight - lblHeaderName.PreferredWidth,
                10
            );

            lblHeaderRole.Location = new Point(
                textRight - lblHeaderRole.PreferredWidth,
                29
            );
        }

        private void SetupToolTips()
        {
            try
            {
                foreach (Control col in panelMiddleContent.Controls)
                {
                    if (col.Name == "panelLeftColumn")
                    {
                        foreach (Control flow in col.Controls)
                        {
                            if (flow is FlowLayoutPanel flp)
                            {
                                foreach (Control card in flp.Controls)
                                {
                                    if (card is Panel p)
                                    {
                                        foreach (Control c in p.Controls)
                                        {
                                            if (c is IconButton btn)
                                            {
                                                if (btn.IconChar == IconChar.Play) toolTip.SetToolTip(btn, "Start Consultation");
                                                else if (btn.IconChar == IconChar.Check) toolTip.SetToolTip(btn, "Complete Visit");
                                                else if (btn.IconChar == IconChar.Times) toolTip.SetToolTip(btn, "Cancel / Send Back");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void WireSidebarEvents()
        {
            if (panelSidebar != null)
            {
                foreach (Control ctrl in panelSidebar.Controls)
                {
                    if (ctrl is IconButton btn)
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


            
            if (btnDashboard != null) btnDashboard.Click += (s, e) => { HighlightActiveButton(btnDashboard); ShowDashboard(); };
            if (btnAppointments != null) btnAppointments.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenDoctorsAppointments(); };
            if (btnQueue != null) btnQueue.Click += (s, e) => { HighlightActiveButton(btnQueue); OpenDoctorsQueue(); };
            if (btnHistory != null) btnHistory.Click += (s, e) => { HighlightActiveButton(btnHistory); OpenDoctorsHistory(); };
            if (btnConsultation != null) btnConsultation.Click += (s, e) => { HighlightActiveButton(btnConsultation); OpenConsultationsView(); };
            if (btnPrescriptions != null) btnPrescriptions.Click += (s, e) => { HighlightActiveButton(btnPrescriptions); OpenPrescriptions(); };

            if (btnLogout != null) btnLogout.Click += (s, e) => HandleLogout();

            HighlightActiveButton(btnDashboard);
        }

        private void OpenDoctorsAppointments()
        {
            SetDashboardVisibility(false);
            ShowFormInMainContent(new DoctorsAppointmentsForm(loggedInUsername));
        }

        private void OpenDoctorsQueue()
        {
            SetDashboardVisibility(false);
            ShowFormInMainContent(new DoctorsQueueForm(loggedInUsername));
        }

        private void OpenDoctorsHistory()
        {
            SetDashboardVisibility(false);
            ShowFormInMainContent(new DoctorsPatientHistoryForm(loggedInUsername));
        }

        private void OpenPrescriptions()
        {
            SetDashboardVisibility(false);
            ShowFormInMainContent(new PrescriptionsForm());
        }

        private void OpenConsultationsView()
        {
            int currentVisitId = GetActiveVisitId();
            if (currentVisitId > 0)
            {
                SetDashboardVisibility(false);
                ShowFormInMainContent(new PatientConsultationForm(currentVisitId));
            }
            else
            {
                MessageBox.Show("No active consultation in progress. Please start a session from the queue.", "No Active Session", MessageBoxButtons.OK, MessageBoxIcon.Information);
                HighlightActiveButton(btnQueue);
                OpenDoctorsQueue();
            }
        }

        private int GetActiveVisitId()
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = "SELECT TOP 1 VisitID FROM Visits WHERE (DoctorId = @did OR DoctorName = @doc) AND Status = 'WITH DOCTOR' ORDER BY VisitDate DESC";
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@did", _doctorId);
                    cmd.Parameters.AddWithValue("@doc", _doctorFullName);
                    object result = cmd.ExecuteScalar();
                    return result != null ? (int)result : 0;
                }
            } catch { return 0; }
        }

        private void ShowFormInMainContent(Form form)
        {
            if (activeForm != null) activeForm.Close();
            
            SetDashboardVisibility(false);
            // if (lblPageTitle != null) lblPageTitle.Text = form.Text;
            
            activeForm = form;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            
            if (panelMain != null)
            {
                panelMain.Padding = new Padding(0); // Remove padding for child forms
                panelMain.Controls.Add(form);
                panelMain.Tag = form;
            }
            
            form.BringToFront();
            form.Show();
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

        private void SeedIfEmpty()
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    // Check if this doctor has any visits today
                    string checkQ = "SELECT COUNT(*) FROM Visits WHERE DoctorName = @doc AND CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)";
                    var checkCmd = new SqlCommand(checkQ, con);
                    checkCmd.Parameters.AddWithValue("@doc", loggedInUsername);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count == 0) {
                        // Create a patient if needed
                        string pQ = "IF NOT EXISTS (SELECT * FROM Patients WHERE PatientName = 'Test Patient (Female)') " +
                                   "INSERT INTO Patients (PatientName, Phone, Age, Gender, Status) VALUES ('Test Patient (Female)', '0300-5566778', 29, 'Female', 'Active'); " +
                                   "SELECT TOP 1 PatientID FROM Patients WHERE PatientName = 'Test Patient (Female)';";
                        var pCmd = new SqlCommand(pQ, con);
                        int pid = (int)pCmd.ExecuteScalar();

                        // Add an active visit for this doctor
                        string vQ = "INSERT INTO Visits (PatientID, DoctorName, TokenNumber, Status, ChiefComplaint, VisitDate) " +
                                   "VALUES (@pid, @doc, 'T-SEED', 'WITH_DOCTOR', 'Persistent sore throat and cough.', GETDATE());";
                        var vCmd = new SqlCommand(vQ, con);
                        vCmd.Parameters.AddWithValue("@pid", pid);
                        vCmd.Parameters.AddWithValue("@doc", loggedInUsername);
                        vCmd.ExecuteNonQuery();
                    }
                }
            } catch { }
        }

        private void ShowDashboard()
        {
            if (activeForm != null)
            {
                activeForm.Close();
            }
            SetDashboardVisibility(true);
            ApplyFixedDesignLayout();
            LoadDoctorStats();
            // if (lblPageTitle != null) lblPageTitle.Text = "Dashboard Overview";
        }

        private void LoadDoctorStats()
        {
            Task.Run(() => {
                try {
                    using (var con = new SqlConnection(connectionString)) {
                        con.Open();
                        // 1. Patients Seen Today -> COMPLETED in Visits (today)
                        int seenToday = 0;
                        string qSeen = "SELECT COUNT(*) FROM Visits WHERE (DoctorId = @docId OR DoctorName = @doc) AND Status = 'COMPLETED' AND CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)";
                        var cmdSeen = new SqlCommand(qSeen, con);
                        cmdSeen.Parameters.AddWithValue("@docId", _doctorId);
                        cmdSeen.Parameters.AddWithValue("@doc", _doctorFullName);
                        seenToday = (int)cmdSeen.ExecuteScalar();

                        // 2. Patients Waiting -> WAITING or Checked-In in Visits (today)
                        int waiting = 0;
                        string qWait = "SELECT COUNT(*) FROM Visits WHERE (DoctorId = @docId OR DoctorName = @doc) AND Status IN ('WAITING', 'Checked-In') AND CAST(VisitDate AS DATE) = CAST(GETDATE() AS DATE)"; 
                        var cmdWait = new SqlCommand(qWait, con);
                        cmdWait.Parameters.AddWithValue("@docId", _doctorId);
                        cmdWait.Parameters.AddWithValue("@doc", _doctorFullName);
                        waiting = (int)cmdWait.ExecuteScalar();
                        
                        // 3. Upcoming Appointments -> Scheduled in Appointments (today, not yet checked in)
                        int upcoming = 0;
                        string qUp = "SELECT COUNT(*) FROM Appointments WHERE (DoctorId = @docId OR DoctorName = @doc) AND Status = 'Scheduled' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)";
                        var cmdUp = new SqlCommand(qUp, con);
                        cmdUp.Parameters.AddWithValue("@docId", _doctorId);
                        cmdUp.Parameters.AddWithValue("@doc", _doctorFullName);
                        upcoming = (int)cmdUp.ExecuteScalar();

                        // 4. Current Status
                        string status = "UNKNOWN";
                        string qStatus = "SELECT Status FROM Doctors WHERE DoctorId = @docId";
                        var cmdStatus = new SqlCommand(qStatus, con);
                        cmdStatus.Parameters.AddWithValue("@docId", _doctorId);
                        status = cmdStatus.ExecuteScalar()?.ToString() ?? "ON DUTY";

                        // 5. Data for Schedule
                        DataTable dtSchedule = new DataTable();
                        string qSch = @"SELECT A.AppointmentIntId, A.PatientId, A.AppointmentTime, A.PatientName, A.Status, 
                                               ISNULL(B.PaymentStatus, 'Unpaid') as PaymentStatus 
                                        FROM Appointments A 
                                        LEFT JOIN Billing B ON A.AppointmentIntId = B.AppointmentIntId
                                        WHERE (A.DoctorId = @docId OR A.DoctorName = @doc) AND CAST(A.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE) 
                                        ORDER BY A.AppointmentTime";
                        var cmdSch = new SqlCommand(qSch, con);
                        cmdSch.Parameters.AddWithValue("@docId", _doctorId);
                        cmdSch.Parameters.AddWithValue("@doc", _doctorFullName);
                        new SqlDataAdapter(cmdSch).Fill(dtSchedule);

                        // 6. Data for Queue (Sync with My Queue Logic)
                        DataTable dtQueue = new DataTable();
                        string qQueue = @"
                            SELECT V.VisitID, P.PatientID, V.TokenNumber, V.VisitDate as AppointmentDate, 
                                   CAST(V.VisitDate AS TIME) as AppointmentTime, V.Status,
                                   P.PatientName, A.AppointmentIntId, A.AppointmentCode, 
                                   ISNULL(A.Reason, 'General Consultation') as Reason,
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
                        var cmdQueue = new SqlCommand(qQueue, con);
                        cmdQueue.Parameters.AddWithValue("@docId", _doctorId);
                        cmdQueue.Parameters.AddWithValue("@doc", _doctorFullName);
                        new SqlDataAdapter(cmdQueue).Fill(dtQueue);

                        if (this.IsHandleCreated) {
                            this.BeginInvoke((MethodInvoker)delegate {
                                UpdateDoctorDashboardUI(seenToday, waiting, upcoming, status);
                                UpdateHeaderStatusButtons(status);
                                LoadPatientQueue(dtQueue); // Left Column: Checked-In / With Doctor
                                RefreshAppointmentsSchedule(dtSchedule); // Center Column: All Today
                                
                                if (lblSubWelcome != null) {
                                    lblSubWelcome.ForeColor = Color.DimGray;
                                    lblSubWelcome.Text = $"You have {seenToday} patients seen today and {waiting} patients waiting in the queue.";
                                }
                            });
                        }
                    }
                } catch { }
            });
        }

        private void UpdateDoctorDashboardUI(int myVisits, int pendingCons, int upcoming, string status)
        {
            try {
                // Update KPI Cards using their titles or specific identifications
                if (cardTodaySeen != null) UpdateKPICard(cardTodaySeen, myVisits.ToString(), accentGreen);
                if (cardWaiting != null) UpdateKPICard(cardWaiting, pendingCons.ToString(), accentOrange);
                if (cardUpcoming != null) UpdateKPICard(cardUpcoming, upcoming.ToString(), primaryBlue);
                if (cardCurrentStatus != null) UpdateKPICard(cardCurrentStatus, status.ToUpper(), status.ToUpper() == "BREAK" ? accentOrange : (status.ToUpper() == "BUSY" ? accentPurple : accentGreen));
            } catch { }
        }

        private void RefreshAppointmentsSchedule(DataTable dt)
        {
            if (panelCenterColumn == null) return;
            FlowLayoutPanel? flow = panelCenterColumn.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (flow == null) return;

            flow.SuspendLayout();
            flow.Controls.Clear();

            foreach (DataRow row in dt.Rows) {
                int patientId = Convert.ToInt32(row["PatientId"]);
                string patientName = row["PatientName"].ToString();
                int apptId = Convert.ToInt32(row["AppointmentIntId"]);

                Panel card = new Panel { Height = 72, Width = flow.Width - 10, BackColor = Color.Transparent, Margin = new Padding(0), Cursor = Cursors.Hand };
                card.Click += (s, e) => ShowPatientDetails(patientId, patientName);
                
                DateTime time = row["AppointmentTime"] is DateTime ? (DateTime)row["AppointmentTime"] : DateTime.Now;
                Label lblTime = new Label { Text = time.ToString("hh:mm tt"), Font = new Font("Segoe UI Semibold", 9), ForeColor = primaryBlue, Location = new Point(15, 22), AutoSize = true };
                Label lblPatient = new Label { Text = patientName, Font = new Font("Segoe UI Bold", 10.5F), ForeColor = textPrimary, Location = new Point(100, 14), AutoSize = true };
                lblTime.Click += (s, e) => ShowPatientDetails(patientId, patientName);
                lblPatient.Click += (s, e) => ShowPatientDetails(patientId, patientName);
                
                string status = row["Status"].ToString();
                string payStatus = row["PaymentStatus"]?.ToString() ?? "Unpaid";
                bool isPaid = payStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
                
                Color statusColor = status == "Completed" ? accentGreen : (status == "With Doctor" ? accentPurple : (status == "Checked-In" ? accentOrange : textSecondary));
                Label lblStatus = new Label { Text = status.ToUpper(), Font = new Font("Segoe UI Bold", 7.5F), ForeColor = statusColor, Location = new Point(100, 38), AutoSize = true };
                lblStatus.Click += (s, e) => ShowPatientDetails(patientId, patientName);

                Color payColor = isPaid ? Color.FromArgb(22, 163, 74) : Color.FromArgb(225, 29, 72);
                Label lblPay = new Label { 
                    Text = payStatus.ToUpper(), 
                    Font = new Font("Segoe UI Bold", 7F), 
                    ForeColor = payColor, 
                    BackColor = Color.FromArgb(20, payColor),
                    Padding = new Padding(5, 2, 5, 2),
                    Location = new Point(lblPatient.Left + lblPatient.PreferredWidth + 10, 15), 
                    AutoSize = true 
                };
                
                card.Controls.AddRange(new Control[] { lblTime, lblPatient, lblStatus, lblPay });
                flow.Controls.Add(card);
            }

            if (dt.Rows.Count == 0) {
                flow.Controls.Add(new Label { Text = "No appointments today", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(flow.Width, 100) });
            }
            flow.ResumeLayout();
        }

        private void ShowPatientDetails(int patientId, string patientName)
        {
            if (panelRightColumn == null) return;
            FlowLayoutPanel? flow = panelRightColumn.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (flow == null) return;

            Task.Run(() => {
                try {
                    using (var con = new SqlConnection(connectionString)) {
                        con.Open();
                        // 1. Get Latest Visit Info
                        string qLatest = @"SELECT TOP 1 CAST(VisitDate AS DATE) as VisitDate, ChiefComplaint, Diagnosis, DoctorNotes 
                                          FROM Visits WHERE PatientID = @pid ORDER BY VisitDate DESC";
                        DataTable dtLatest = new DataTable();
                        new SqlDataAdapter(new SqlCommand(qLatest, con) { Parameters = { new SqlParameter("@pid", patientId) } }).Fill(dtLatest);

                        // 2. Get Full History
                        string qHistory = @"SELECT VisitDate, ChiefComplaint, Diagnosis FROM Visits WHERE PatientID = @pid ORDER BY VisitDate DESC";
                        DataTable dtHistory = new DataTable();
                        new SqlDataAdapter(new SqlCommand(qHistory, con) { Parameters = { new SqlParameter("@pid", patientId) } }).Fill(dtHistory);

                        if (this.IsHandleCreated) {
                            this.BeginInvoke((MethodInvoker)delegate {
                                flow.SuspendLayout();
                                flow.Controls.Clear();

                                // Patient Header
                                Panel pnlHeader = new Panel { Size = new Size(flow.Width - 10, 80), BackColor = Color.FromArgb(249, 250, 251), Margin = new Padding(0, 0, 0, 20) };
                                pnlHeader.Paint += (s, e) => {
                                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, pnlHeader.Width - 1, pnlHeader.Height - 1, 8))
                                        using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                                };
                                Label lblName = new Label { Text = patientName, Font = new Font("Segoe UI Bold", 12), ForeColor = textPrimary, Location = new Point(15, 15), AutoSize = true };
                                Label lblId = new Label { Text = $"PATIENT ID: {patientId}", Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(15, 42), AutoSize = true };
                                pnlHeader.Controls.AddRange(new Control[] { lblName, lblId });
                                flow.Controls.Add(pnlHeader);

                                // Latest Summary
                                flow.Controls.Add(new Label { Text = "Latest Diagnosis", Font = new Font("Segoe UI Bold", 9.5F), ForeColor = textPrimary, AutoSize = true, Margin = new Padding(0, 10, 0, 5) });
                                if (dtLatest.Rows.Count > 0) {
                                    DataRow latest = dtLatest.Rows[0];
                                    Label lblDiag = new Label { 
                                        Text = $"Date: {Convert.ToDateTime(latest["VisitDate"]):MMM dd, yyyy}\nDiagnosis: {latest["Diagnosis"]}\nSymptoms: {latest["ChiefComplaint"]}\nDoctor Notes: {latest["DoctorNotes"]}",
                                        Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Size = new Size(flow.Width - 20, 100), AutoSize = false 
                                    };
                                    flow.Controls.Add(lblDiag);
                                } else {
                                    flow.Controls.Add(new Label { Text = "No previous diagnosis found.", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true });
                                }

                                // Timeline
                                flow.Controls.Add(new Label { Text = "Medical Timeline", Font = new Font("Segoe UI Bold", 9.5F), ForeColor = textPrimary, AutoSize = true, Margin = new Padding(0, 20, 0, 10) });

                                foreach (DataRow row in dtHistory.Rows) {
                                    Panel card = new Panel { Size = new Size(flow.Width - 10, 90), BackColor = Color.White, Margin = new Padding(0, 0, 0, 12) };
                                    card.Paint += (s, e) => {
                                        using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 8))
                                            using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                                    };

                                    DateTime d = Convert.ToDateTime(row["VisitDate"]);
                                    Label lblDate = new Label { Text = d.ToString("MMM dd, yyyy"), Font = new Font("Segoe UI Bold", 9), ForeColor = primaryBlue, Location = new Point(12, 12), AutoSize = true };
                                    Label lblComp = new Label { Text = row["ChiefComplaint"].ToString(), Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(12, 34), Width = card.Width - 24, Height = 45, AutoSize = false };
                                    
                                    card.Controls.AddRange(new Control[] { lblDate, lblComp });
                                    flow.Controls.Add(card);
                                }

                                if (dtHistory.Rows.Count == 0) {
                                    flow.Controls.Add(new Label { Text = "New patient. No history found.", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true });
                                }

                                flow.ResumeLayout();
                            });
                        }
                    }
                } catch { }
            });
        }

        private void SetDashboardVisibility(bool visible)
        {
            try
            {
                if (panelTopToolbar != null)
                    panelTopToolbar.Visible = visible;

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
            }
            catch { }
        }

        private void OpenProfileForm()
        {
            using (ProfileForm profileForm = new ProfileForm(loggedInUsername, "Doctor", UpdateHeaderProfile))
            {
                profileForm.ShowDialog(this);
            }
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
                this.Hide();
                Login loginForm = new Login();
                loginForm.ShowDialog();
                this.Close();
            }
        }



        private void EnableQueueActions(bool enabled)
        {
            try
            {
                foreach (Control col in panelMiddleContent.Controls)
                {
                    if (col.Name == "panelLeftColumn")
                    {
                        foreach (Control flow in col.Controls)
                        {
                            if (flow is FlowLayoutPanel flp)
                            {
                                foreach (Control card in flp.Controls)
                                {
                                    if (card is Panel p)
                                    {
                                        foreach (Control c in p.Controls)
                                        {
                                            if (c is IconButton btn)
                                            {
                                                btn.Enabled = enabled;
                                                // Adjust visual state for disabled buttons
                                                btn.IconColor = enabled ? (btn.IconChar == IconChar.Play ? accentPurple : (btn.IconChar == IconChar.Check ? accentGreen : accentRose)) : Color.FromArgb(200, 200, 200);
                                                btn.Cursor = enabled ? Cursors.Hand : Cursors.Default;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void UpdateKPICard(Panel card, string value, Color color)
        {
            foreach (Control c in card.Controls)
            {
                if (c is Label lbl && lbl.Font.Size > 20) // Value label
                {
                    lbl.Text = value;
                    lbl.ForeColor = color;
                }
            }
        }
        private void LoadPatientQueue(DataTable dtQueue)
        {
            if (flowPatientQueue == null || dtQueue == null) return;
            
            try
            {
                flowPatientQueue.SuspendLayout();
                flowPatientQueue.Controls.Clear();
                
                bool hasData = false;
                foreach (DataRow row in dtQueue.Rows)
                {
                    int apptId = row["AppointmentIntId"] != DBNull.Value ? Convert.ToInt32(row["AppointmentIntId"]) : 0;
                    int patientId = row["PatientID"] != DBNull.Value ? Convert.ToInt32(row["PatientID"]) : 0;
                    
                    string token = row["TokenNumber"]?.ToString();
                    if (string.IsNullOrEmpty(token)) {
                        string fullCode = row["AppointmentCode"]?.ToString();
                        token = !string.IsNullOrEmpty(fullCode) && fullCode.Length >= 3 ? fullCode.Substring(fullCode.Length - 3) : "000";
                    }

                    string name = row["PatientName"]?.ToString() ?? "Unknown";
                    string reason = row.Table.Columns.Contains("Reason") ? (row["Reason"]?.ToString() ?? "General") : "General Consultation";
                    string statusValue = row["Status"]?.ToString() ?? "Checked-In";
                    string payStatus = row["PaymentStatus"]?.ToString() ?? "Unpaid";
                    
                    // Standardize display status
                    string displayStatus = statusValue;
                    if (statusValue.Equals("WAITING", StringComparison.OrdinalIgnoreCase)) displayStatus = "Waiting";
                    else if (statusValue.Equals("Checked-In", StringComparison.OrdinalIgnoreCase) || statusValue.Equals("Checked In", StringComparison.OrdinalIgnoreCase)) displayStatus = "Waiting";
                    else if (statusValue.Equals("WITH_DOCTOR", StringComparison.OrdinalIgnoreCase) || statusValue.Equals("With Doctor", StringComparison.OrdinalIgnoreCase)) displayStatus = "With Doctor";
                    else if (statusValue.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)) displayStatus = "Completed";

                    DateTime visitTime = row["AppointmentDate"] != DBNull.Value ? Convert.ToDateTime(row["AppointmentDate"]) : DateTime.Now;
                    string waitText = GetWaitTimeText(visitTime);

                    Panel card = CreatePatientQueueCard(token, name, reason, waitText, displayStatus, payStatus);
                    card.Width = flowPatientQueue.Width - 35;
                    card.Cursor = Cursors.Hand;
                    card.Click += (s, e) => ShowPatientDetails(patientId, name);
                    foreach(Control c in card.Controls) if(!(c is IconButton)) c.Click += (s,e) => ShowPatientDetails(patientId, name);
                    
                    // Mapping status for Wiring
                    WiringQueueCardActions(card, apptId, statusValue.ToUpper(), patientId, name);
                    
                    flowPatientQueue.Controls.Add(card);
                    hasData = true;
                }
                
                if (!hasData)
                {
                    Label empty = new Label { 
                        Text = "No patients in queue", 
                        Font = new Font("Segoe UI", 10), 
                        ForeColor = Color.DimGray, 
                        TextAlign = ContentAlignment.MiddleCenter, 
                        Size = new Size(flowPatientQueue.Width - 40, 100) 
                    };
                    flowPatientQueue.Controls.Add(empty);
                }
                flowPatientQueue.ResumeLayout();
            }
            catch (Exception ex) { 
                flowPatientQueue?.ResumeLayout();
            }
        }

        private void WiringQueueCardActions(Panel card, int apptId, string status, int patientId, string patientName)
        {
            foreach (Control c in card.Controls)
            {
                if (c is IconButton btn)
                {
                    if (btn.IconChar == IconChar.Play) // Start
                    {
                        btn.Click += (s, e) => {
                            if (status == "WAITING") {
                                if (IsDoctorOccupied()) {
                                    MessageBox.Show("You already have an active patient in consultation. Please complete the current session before starting a new one.", "Doctor Occupied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                                UpdateVisitStatus(apptId, "With Doctor");
                                status = "WITH_DOCTOR"; // Update local status for the OpenConsultation logic
                            }
                            OpenConsultation(apptId);
                        };
                    }
                    else if (btn.IconChar == IconChar.Check) // Complete
                    {
                        btn.Click += (s, e) => {
                            UpdateVisitStatus(apptId, "Completed");
                            LoadDoctorStats();
                        };
                    }
                    else if (btn.IconChar == IconChar.Times) // Cancel/Back
                    {
                        btn.Click += (s, e) => {
                            if (MessageBox.Show("Refer this patient back to reception?", "Refer Back", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                UpdateVisitStatus(apptId, "Checked-In");
                                LoadDoctorStats();
                            }
                        };
                    }
                }
            }
        }

        private string GetWaitTimeText(DateTime startTime)
        {
            TimeSpan diff = DateTime.Now - startTime;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} min ago";
            return $"{(int)diff.TotalHours}h {(int)diff.Minutes}m ago";
        }

        private void UpdateVisitStatus(int apptId, string status)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // 1. Update Appointment
                    string queryAppt = "UPDATE Appointments SET Status = @status WHERE AppointmentIntId = @aid";
                    var cmdAppt = new SqlCommand(queryAppt, con);
                    cmdAppt.Parameters.AddWithValue("@status", status);
                    cmdAppt.Parameters.AddWithValue("@aid", apptId);
                    cmdAppt.ExecuteNonQuery();

                    // 2. Also try to update linked Visit if exists
                    string queryVisit = "UPDATE Visits SET Status = @status WHERE AppointmentIntId = @aid";
                    var cmdVisit = new SqlCommand(queryVisit, con);
                    cmdVisit.Parameters.AddWithValue("@status", status.ToUpper()); // Most coded statuses in Visits are uppercase
                    cmdVisit.Parameters.AddWithValue("@aid", apptId);
                    cmdVisit.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error updating status: " + ex.Message); }
        }

        private void OpenConsultation(int apptId)
        {
            try
            {
                int visitId = 0;
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string q = "SELECT TOP 1 VisitID FROM Visits WHERE AppointmentIntId = @aid ORDER BY VisitDate DESC";
                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@aid", apptId);
                        var res = cmd.ExecuteScalar();
                        if (res != null) visitId = (int)res;
                    }
                }

                if (visitId > 0)
                {
                    SetDashboardVisibility(false);
                    ShowFormInMainContent(new PatientConsultationForm(visitId));
                }
                else
                {
                    MessageBox.Show("Could not find a visit record for this appointment. Please ensure the patient is Checked-In.", "Missing Visit Record");
                }
            }
            catch (Exception ex) { MessageBox.Show("Error opening consultation: " + ex.Message); }
        }

private void UpdateDoctorStatus(string newStatus)
{
    try
    {
        using (var con = new SqlConnection(connectionString))
        {
            con.Open();
            string q = "UPDATE Doctors SET Status = @status WHERE DoctorId = @did";
            using (var cmd = new SqlCommand(q, con))
            {
                cmd.Parameters.AddWithValue("@status", newStatus.ToUpper());
                cmd.Parameters.AddWithValue("@did", _doctorId);
                cmd.ExecuteNonQuery();
            }
        }
        UpdateHeaderStatusButtons(newStatus);
        LoadDoctorStats(); // Refresh UI
    }
    catch (Exception ex)
    {
        MessageBox.Show("Error updating doctor status: " + ex.Message);
    }
}

private void WireHeaderStatusEvents()
{
    if (btnOnDuty != null) btnOnDuty.Click += (s, e) => UpdateDoctorStatus("ON DUTY");
    if (btnOnBreak != null) btnOnBreak.Click += (s, e) => UpdateDoctorStatus("BREAK");
}

private void UpdateHeaderStatusButtons(string status)
{
    bool onDuty = status.ToUpper() == "ON DUTY";
    
    if (onDuty)
    {
        btnOnDuty.BackColor = accentGreen;
        btnOnDuty.ForeColor = Color.White;
        btnOnDuty.IconColor = Color.White;
        btnOnDuty.FlatAppearance.BorderSize = 0;

        btnOnBreak.BackColor = Color.Transparent;
        btnOnBreak.ForeColor = accentOrange;
        btnOnBreak.IconColor = accentOrange;
        btnOnBreak.FlatAppearance.BorderSize = 1;
    }
    else
    {
        btnOnBreak.BackColor = accentOrange;
        btnOnBreak.ForeColor = Color.White;
        btnOnBreak.IconColor = Color.White;
        btnOnBreak.FlatAppearance.BorderSize = 0;

        btnOnDuty.BackColor = Color.Transparent;
        btnOnDuty.ForeColor = accentGreen;
        btnOnDuty.IconColor = accentGreen;
        btnOnDuty.FlatAppearance.BorderSize = 1;
    }
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

        private void SetupCustomDrawing()
        {
            // Enable double buffering for smooth rendering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }
    }
}
