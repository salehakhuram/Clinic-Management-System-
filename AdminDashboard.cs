using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicManagement
{
    public partial class AdminDashboard : Form
    {
        private string loggedInUsername = "";
        private Form? activeForm = null;
        private string connectionString = "Server=DESKTOP-5NPQD72\\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True;";
        
        // Sidebar colors
        private Color SidebarDefaultBackColor = Color.FromArgb(31, 41, 55);
        private Color SidebarHoverBackColor = Color.FromArgb(55, 65, 81);
        private Color SidebarActiveBackColor = Color.FromArgb(59, 130, 246); // Modern Blue
        private IconButton? previousActiveButton = null;
        private System.Windows.Forms.Timer refreshTimer;
private void LoadDashboardData()
{
    LoadDashboardStats();
}

        public AdminDashboard(string username)
        {
            try
            {
                loggedInUsername = username;
                InitializeComponent();
                
                // Update welcome message with username
                if (lblWelcome != null)
                {
                    lblWelcome.Text = $"Welcome, {username}!";
                }

                // Setup sidebar hover effects
                WireSidebarEvents();

                // Wire filter events
                if (cmbTimeFilter != null) cmbTimeFilter.SelectedIndexChanged += (s, e) => LoadDashboardStats();
                if (btnExport != null) btnExport.Click += BtnExport_Dashboard_Click;

                // Setup refresh timer (15 seconds)
                refreshTimer = new System.Windows.Forms.Timer();
                refreshTimer.Interval = 15000;
                refreshTimer.Tick += (s, e) => LoadDashboardStats();
                refreshTimer.Start();

                // Setup global search
                SetupGlobalSearch();

                // Initial load: wire Load event to show dashboard
                this.Load += (s, e) => ShowDashboard();
                this.Shown += (s, e) =>   LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing dashboard: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupGlobalSearch()
        {
            if (txtTopSearch == null) return;

            txtTopSearch.KeyDown += (s, e) =>
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
            if (txtTopSearch == null) return;

            string query = txtTopSearch.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query) || query == "Search anything here...")
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
                txtTopSearch.Text = "Search anything here...";
                txtTopSearch.ForeColor = Color.Gray;

                // Navigate based on result type
                switch (result.Type)
                {
                    case SearchResultType.Patient:
                        MessageBox.Show(result.DisplayText, "Patient Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnPatients);
                        btnPatients?.PerformClick();
                        break;

                    case SearchResultType.Appointment:
                        MessageBox.Show(result.DisplayText, "Appointment Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnAppointments);
                        btnAppointments?.PerformClick();
                        break;

                    case SearchResultType.QueueToken:
                        MessageBox.Show(
                            result.DisplayText + "\n\nQueue management is handled by the receptionist.",
                            "Queue Token Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDashboardStats()
        {
            // Run database queries on a background thread to prevent UI freezing
            Task.Run(() =>
            {
                try
                {
                    using (var con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        
                        // Get Filter Days on UI Thread
                        int filterDays = 0;
                        if (this.IsHandleCreated)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                if (cmbTimeFilter != null)
                                {
                                    if (cmbTimeFilter.SelectedIndex == 1) filterDays = 7;
                                    else if (cmbTimeFilter.SelectedIndex == 2) filterDays = 30;
                                }
                            });
                        }

                        string dateFilter = filterDays == 0 
                            ? "CAST(BillDate AS DATE) = CAST(GETDATE() AS DATE)" 
                            : $"BillDate >= DATEADD(day, -{filterDays}, GETDATE())";

                        // --- 1. KPI Stats ---
                        int totalPatients = 0, totalDoctors = 0, totalStaff = 0, apptsToday = 0;
                        decimal totalRevenue = 0;

                        try { totalPatients = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Patients", con).ExecuteScalar()); } catch { }
                        try { totalDoctors = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Doctors", con).ExecuteScalar()); } catch { }
                        try { totalStaff = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Staff", con).ExecuteScalar()); } catch { }
                        try 
                        { 
                            var cmdRev = new SqlCommand($"SELECT ISNULL(SUM(TotalAmount), 0) FROM Bills WHERE {dateFilter}", con);
                            totalRevenue = Convert.ToDecimal(cmdRev.ExecuteScalar());
                        } catch { }
                        try { apptsToday = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }

                        // --- 2. Appointments Overview ---
                        DataTable dtAppts = new DataTable();
                        try 
                        {
                            string apptQuery = @"SELECT TOP 10 P.PatientName, S.StaffName as DoctorName, A.AppointmentDate as Date, A.Status 
                                                   FROM Appointments A 
                                                   LEFT JOIN Patients P ON A.PatientId = P.PatientID 
                                                   LEFT JOIN Doctors D ON A.DoctorId = D.DoctorId 
                                                   LEFT JOIN Staff S ON D.StaffId = S.StaffId
                                                   WHERE CAST(A.AppointmentDate AS DATE) = CAST(GETDATE() AS DATE) 
                                                   ORDER BY A.AppointmentTime ASC";
                            new SqlDataAdapter(apptQuery, con).Fill(dtAppts);
                        } catch { }

                        // --- 3. Doctors On-Duty ---
                        DataTable dtDocs = new DataTable();
                        try 
                        {
                            string docQuery = @"SELECT S.StaffName as DoctorName, D.Specialization, D.Status 
                                                  FROM Doctors D 
                                                  LEFT JOIN Staff S ON D.StaffId = S.StaffId 
                                                  WHERE D.Status IN ('Available', 'On Break', 'Active')";
                            new SqlDataAdapter(docQuery, con).Fill(dtDocs);
                        } catch { }

                        // --- 4. Pending Overview ---
                        DataTable dtPending = new DataTable();
                        dtPending.Columns.Add("Item");
                        dtPending.Columns.Add("Count");

                        int pendingBillsCount = 0, pendingApptsCount = 0, lowStockCount = 0;
                        
                        try { pendingApptsCount = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE Status = 'Scheduled' AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar()); } catch { }
                        try { lowStockCount = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Medicines WHERE Quantity < 10", con).ExecuteScalar()); } catch { }
                        
                        // Combined Unpaid logic: Actual Unpaid Bills + Scheduled/Checked-In/With-Doctor Appointments today (which are essentially pending payments)
                        try { 
                            int unpaidBills = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Bills WHERE PaymentStatus = 'Unpaid'", con).ExecuteScalar());
                            int apptsNotPaid = Convert.ToInt32(new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE Status IN ('Scheduled', 'Checked-In', 'With Doctor') AND CAST(AppointmentDate AS DATE) = CAST(GETDATE() AS DATE)", con).ExecuteScalar());
                            pendingBillsCount = unpaidBills + apptsNotPaid;
                        } catch { }

                        dtPending.Rows.Add("Pending Appointments", pendingApptsCount);
                        dtPending.Rows.Add("Unpaid Invoices", pendingBillsCount);
                        dtPending.Rows.Add("Low Stock Medicines", lowStockCount);

                        // Update UI on the main thread
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                UpdateDashboardUI(totalPatients, totalDoctors, totalStaff, totalRevenue, apptsToday, dtAppts, dtDocs, dtPending);

                                if (lblSubWelcome != null)
                                {
                                    lblSubWelcome.Text = $"System Overview: {pendingApptsCount} pending appointments, {pendingBillsCount} unpaid invoices, and {lowStockCount} low stock items found.";
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Silent fail or log
                }
            });
        }

        private void UpdateHeaderProfile()
        {
            try
            {
                // Immediate update with whatever we have (The login username)
                if (this.lblHeaderName != null) this.lblHeaderName.Text = loggedInUsername;
                if (this.lblHeaderRole != null) this.lblHeaderRole.Text = "Administrator";

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Fetch both FullName and Username separately to be sure
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

        private void BtnExport_Dashboard_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"Dashboard_Summary_{DateTime.Now:yyyyMMdd}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Dashboard Overview Report");
                        sb.AppendLine($"Generated on: {DateTime.Now}");
                        sb.AppendLine();
                        sb.AppendLine("Summary Metrics");
                        // We'd ideally pull current values from the labels or a cached state
                        sb.AppendLine($"Total Patients, Total Doctors, Total Staff");
                        // ... simplified for brevity ...
                        
                        File.WriteAllText(sfd.FileName, sb.ToString());
                        MessageBox.Show("Dashboard summary exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Export error: " + ex.Message); }
                }
            }
        }
        
        private void UpdateDashboardUI(int totalPatients, int totalDoctors, int totalStaff, decimal totalRevenue, int apptsToday,
                                     DataTable dtAppts, DataTable dtDocs, DataTable dtPending)
        {
            // Cards
            UpdateCardValue(cardPatients, totalPatients.ToString());
            UpdateCardValue(cardRevenue, $"₨ {totalRevenue:N0}");
            UpdateCardValue(cardApptsToday, apptsToday.ToString());
            var lblAppt = cardApptsToday.Controls.OfType<Label>().LastOrDefault();
            if (lblAppt != null) lblAppt.Text = "Total today";
            UpdateCardValue(cardStats, totalStaff.ToString());
            UpdateCardValue(cardDemographics, totalDoctors.ToString());

            // Lists
            PopulateAppointmentsList(panelLeftColumn, dtAppts);
            PopulateOnDutyDoctors(panelCenterColumn, dtDocs);
            PopulatePendingOverview(panelRightColumn, dtPending);
        }

        private void UpdateCardValue(Panel card, string value)
        {
            if (card == null) return;
            var lblVal = card.Controls.OfType<Label>().FirstOrDefault(l => l.Font.Size > 15);
            if (lblVal != null) lblVal.Text = value;
        }

        private void WireSidebarEvents()
        {
            if (panelSidebar != null)
            {
                foreach (Control ctrl in panelSidebar.Controls)
                {
                    if (ctrl is IconButton btn)
                    {
                        btn.MouseEnter += (s, e) => btn.BackColor = SidebarHoverBackColor;
                        btn.MouseLeave += (s, e) => btn.BackColor = SidebarDefaultBackColor;
                    }
                }
            }
            
            // Wire specific buttons to functional logic
            if (btnDashboard != null) btnDashboard.Click += (s, e) => { HighlightActiveButton(btnDashboard); ShowDashboard();LoadDashboardData(); };
            if (btnPatients != null) btnPatients.Click += (s, e) => { HighlightActiveButton(btnPatients); OpenChildForm(new PatientsForm()); };
            if (btnAppointments != null) btnAppointments.Click += (s, e) => { HighlightActiveButton(btnAppointments); OpenChildForm(new AppointmentsForm(loggedInUsername)); };
            if (btnDoctors != null) btnDoctors.Click += (s, e) => { HighlightActiveButton(btnDoctors); OpenChildForm(new DoctorsForm()); };
            if (btnUsers != null) btnUsers.Click += (s, e) => { HighlightActiveButton(btnUsers); OpenChildForm(new Users()); };
            if (btnMedicines != null) btnMedicines.Click += (s, e) => { HighlightActiveButton(btnMedicines); OpenChildForm(new MedicinesForm()); };

            if (btnStaff != null) btnStaff.Click += (s, e) => { HighlightActiveButton(btnStaff); OpenChildForm(new Staff()); };
            if (btnBilling != null) btnBilling.Click += (s, e) => { HighlightActiveButton(btnBilling); OpenChildForm(new Billing()); };
            if (btnReports != null) btnReports.Click += (s, e) => { HighlightActiveButton(btnReports); OpenChildForm(new Reports()); };
            if (btnPrescriptions != null) btnPrescriptions.Click += (s, e) => { HighlightActiveButton(btnPrescriptions); OpenChildForm(new PrescriptionsForm()); };
            
            if (btnLogout != null) btnLogout.Click += (s, e) => this.Close();

            // Default selection
            HighlightActiveButton(btnDashboard);
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

        private void ShowDashboard()
        {
            if (activeForm != null)
            {
                activeForm.Close();
                activeForm = null;
            }
            
            // Correctly remove all child forms and controls except essential dashboard panels
            bool itemsRemoved = true;
            while (itemsRemoved)
            {
                itemsRemoved = false;
                foreach (Control ctrl in panelMain.Controls)
                {
                    if (ctrl != panelTopToolbar && ctrl != panelDashboardOverview)
                    {
                        panelMain.Controls.Remove(ctrl);
                        ctrl.Dispose();
                        itemsRemoved = true;
                        break; 
                    }
                }
            }

            if (panelMain != null)
            {
                panelMain.Padding = new Padding(0); // Dashboard handles internal padding
            }

            panelDashboardOverview.Visible = true;
            panelTopToolbar.Visible = true;
            panelTopToolbar.Height = 85; // Restore full height for dashboard
            
            // Show Profile and Search Box in Dashboard
            if (pnlProfileComp != null)
                pnlProfileComp.Visible = true;
            
            if (txtTopSearch != null && txtTopSearch.Parent != null)
                txtTopSearch.Parent.Visible = true;

            if (lblPageTitle != null) lblPageTitle.Visible = false;
            LoadDashboardStats();
            ApplyFixedDesignLayout();
            UpdateHeaderProfile();
        }

        private void OpenChildForm(Form childForm)
        {
            if (activeForm != null)
                activeForm.Close();

            panelDashboardOverview.Visible = false;
            
            // Hide AdminDashboard toolbar for all child forms (they have their own headers)
            panelTopToolbar.Visible = false;

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

        // Re-routing old resize handler if called by designer logic or events
        private void AdminDashboard_Resize(object? sender, EventArgs e)
        {
            ApplyFixedDesignLayout();
        }

        private void ShowProfileDropdown(PictureBox pic)
        {
            Panel dropdown = new Panel
            {
                Size = new Size(180, 140), // Matched width to receptionist (180), height for 3 items
                BackColor = Color.White,
                Location = new Point(panelMain.Width - 190, 85),
                Name = "pnlProfileDropdown"
            };
            
            dropdown.Paint += (s, e) =>
            {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, dropdown.Width - 1, dropdown.Height - 1, 8))
                {
                    using (Pen pen = new Pen(borderGray, 2)) e.Graphics.DrawPath(pen, path);
                }
            };

            Action<Button, string, Color> styleBtn = (b, t, color) => {
                b.Text = t;
                b.Size = new Size(160, 40);
                b.BackColor = Color.White;
                b.ForeColor = color;
                b.FlatStyle = FlatStyle.Flat;
                b.Cursor = Cursors.Hand;
                b.Font = new Font("Segoe UI", 10);
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.FlatAppearance.BorderSize = 0;
                b.MouseEnter += (s, ev) => b.BackColor = (color == Color.IndianRed) ? Color.FromArgb(254, 226, 226) : surfaceColor;
                b.MouseLeave += (s, ev) => b.BackColor = Color.White;
            };

            Button btnProf = new Button { Location = new Point(10, 10) };
            styleBtn(btnProf, "👤 User Profile", textPrimary);
            btnProf.Click += (s, e) =>
            {
                dropdown.Dispose();
                using (ProfileForm pf = new ProfileForm(loggedInUsername, "Admin", UpdateHeaderProfile, false))
                {
                    pf.ShowDialog(this);
                }
            };

            Button btnSec = new Button { Location = new Point(10, 50) };
            styleBtn(btnSec, "🛡️ Security", textPrimary);
            btnSec.Click += (s, e) =>
            {
                dropdown.Dispose();
                using (ProfileForm pf = new ProfileForm(loggedInUsername, "Admin", UpdateHeaderProfile, true))
                {
                    pf.ShowDialog(this);
                }
            };

            Button btnLog = new Button { Location = new Point(10, 90) };
            styleBtn(btnLog, "🚪 Logout", Color.IndianRed);
            btnLog.Click += (s, e) =>
            {
                dropdown.Dispose();
                this.Close();
            };

            dropdown.Controls.AddRange(new Control[] { btnProf, btnSec, btnLog });
            panelMain.Controls.Add(dropdown);
            dropdown.BringToFront();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += (s, e) =>
            {
                if (dropdown.IsDisposed) { timer.Stop(); timer.Dispose(); return; }
                Point mousePos = Cursor.Position;
                Rectangle dropdownRect = dropdown.RectangleToScreen(dropdown.ClientRectangle);
                Rectangle picRect = pic.RectangleToScreen(pic.ClientRectangle);
                if (!dropdownRect.Contains(mousePos) && !picRect.Contains(mousePos))
                {
                    dropdown.Dispose();
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }
    }
}