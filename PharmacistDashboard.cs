using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.IO;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PharmacistDashboard : Form
    {
        private string loggedInUsername;
        private IconButton previousActiveButton;
        private Form activeForm;

        private System.Windows.Forms.Timer refreshTimer;
        
        public PharmacistDashboard(string username)
        {
            this.loggedInUsername = username;
            InitializeComponent();
            WireSidebarEvents();
            UpdateWelcomeMessage();
            WireAvatarClick();
            WireStatusToggle();
            SetupGlobalSearch();
            InitializeRefreshTimer();
            ShowDashboard();
            Task.Run(() => LoadAllData());
        }

        private void InitializeRefreshTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 15000; // 15 seconds
            refreshTimer.Tick += (s, e) => Task.Run(() => LoadAllData());
            refreshTimer.Start();
        }

        private async Task LoadAllData()
        {
            await LoadDashboardStats();
            await LoadPrescriptionQueue();
            await LoadInventoryOverview();
            await LoadRecentActivity();
        }

        private void WireSidebarEvents()
        {
   btnDashboard.Click += (s, e) =>
{
    HighlightActiveButton(btnDashboard);
    SetToolbarVisibility(true);          // ✅ Dashboard = toolbar ON
    ShowDashboard();
};

btnPrescriptions.Click += (s, e) =>
{
    HighlightActiveButton(btnPrescriptions);
    SetToolbarVisibility(false);         // ❌ hide toolbar
    ShowFormInMainContent(new PharmacistPrescriptionsForm(loggedInUsername));
};

btnInventory.Click += (s, e) =>
{
    HighlightActiveButton(btnInventory);
    SetToolbarVisibility(false);         // ❌ hide toolbar
    ShowFormInMainContent(new PharmacistInventoryForm());
};

btnMedicines.Click += (s, e) =>
{
    HighlightActiveButton(btnMedicines);
    SetToolbarVisibility(false);         // ❌ hide toolbar
    ShowFormInMainContent(new MedicinesForm());
};

btnHistory.Click += (s, e) =>
{
    HighlightActiveButton(btnHistory);
    SetToolbarVisibility(false);         // ❌ hide toolbar
    ShowFormInMainContent(new PharmacistSalesHistoryForm());
};


btnLogout.Click += (s, e) => HandleLogout();


            // Hover effects
            foreach (Control ctrl in panelSidebar.Controls)
            {
                if (ctrl is IconButton btn && btn != btnLogout)
                {
                    btn.MouseEnter += (s, e) => { if (btn != previousActiveButton) btn.BackColor = SidebarHoverBackColor; };
                    btn.MouseLeave += (s, e) => { if (btn != previousActiveButton) btn.BackColor = SidebarDefaultBackColor; };
                }
            }
        }

        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";
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
                        string patientInfo = result.DisplayText + "\n\nPrescription history available in Prescriptions module.";
                        MessageBox.Show(patientInfo, "Patient Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnPrescriptions);
                        ShowFormInMainContent(new PharmacistPrescriptionsForm());
                        break;

                    case SearchResultType.Appointment:
                        MessageBox.Show(result.DisplayText, "Appointment Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case SearchResultType.QueueToken:
                        MessageBox.Show(result.DisplayText + "\n\nCheck Prescriptions module for pending orders.", "Queue Token Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        HighlightActiveButton(btnPrescriptions);
                        ShowFormInMainContent(new PharmacistPrescriptionsForm());
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
        }

        private void UpdateHeaderProfile()
        {
            try
            {
                if (this.lblHeaderName != null) this.lblHeaderName.Text = loggedInUsername;
                if (this.lblHeaderRole != null) this.lblHeaderRole.Text = "Pharmacist";

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

        private void HighlightActiveButton(IconButton btn)
        {
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
            if (activeForm != null) activeForm.Close();
            panelMiddleContent.Visible = true;
            panelCardsRow.Visible = true;
            panelWelcome.Visible = true;
            SetToolbarVisibility(true);          // ✅ Dashboard = toolbar ON
            
            // Show Dashboard-only components
            var pnlStatus = panelTopToolbar.Controls.Find("pnlStatusShift", true).FirstOrDefault();
            if (pnlStatus != null) pnlStatus.Visible = true;
            
            var pnlSearch = panelTopToolbar.Controls.Find("pnlSearch", true).FirstOrDefault();
            if (pnlSearch != null) pnlSearch.Visible = true;

            // if (lblPageTitle != null) lblPageTitle.Text = "Dashboard Overview";
            HighlightActiveButton(btnDashboard);
        }

        private void ShowFormInMainContent(Form form)
        {
            if (activeForm != null) activeForm.Close();
            
            activeForm = form;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            
            panelWelcome.Visible = false;
            panelCardsRow.Visible = false;
            panelMiddleContent.Visible = false;
            SetToolbarVisibility(false);         // ❌ hide toolbar

            // if (lblPageTitle != null) lblPageTitle.Text = form.Text;

            panelMain.Controls.Add(form);
            panelMain.Tag = form;
            form.BringToFront();
            form.Show();
        }

        private void HandleLogout()
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                new Login().Show();
            }
        }

        private void WireAvatarClick()
        {
            var pnl = pnlProfileComp;
            if (pnl != null)
            {
                var pic = pnl.Controls.OfType<PictureBox>().FirstOrDefault();
                if (pic != null)
                {
                    pic.Click += (s, e) => ShowProfileDropdown(pic);
                }
            }
        }

        private void ShowProfileDropdown(PictureBox pic)
        {
            Panel dropdown = new Panel
            {
                Size = new Size(180, 100),
                BackColor = Color.White,
                Location = new Point(panelMain.Width - panelMain.Padding.Right - 200, 85)
            };
            
            dropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, dropdown.Width - 1, dropdown.Height - 1, 8))
                {
                    using (Pen pen = new Pen(borderGray, 2)) e.Graphics.DrawPath(pen, path);
                }
            };

            Button btnMyProfile = new Button
            {
                Text = "👤 My Profile",
                Size = new Size(160, 40),
                Location = new Point(10, 10),
                BackColor = Color.White,
                ForeColor = textPrimary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnMyProfile.FlatAppearance.BorderSize = 0;
            btnMyProfile.MouseEnter += (s, e) => btnMyProfile.BackColor = surfaceColor;
            btnMyProfile.MouseLeave += (s, e) => btnMyProfile.BackColor = Color.White;
            btnMyProfile.Click += (s, e) =>
            {
                panelMain.Controls.Remove(dropdown);
                dropdown.Dispose();
                OpenProfileForm();
            };

            Button btnLogoutDropdown = new Button
            {
                Text = "🚪 Logout",
                Size = new Size(160, 40),
                Location = new Point(10, 50),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnLogoutDropdown.FlatAppearance.BorderSize = 0;
            btnLogoutDropdown.MouseEnter += (s, e) => btnLogoutDropdown.BackColor = Color.FromArgb(254, 226, 226);
            btnLogoutDropdown.MouseLeave += (s, e) => btnLogoutDropdown.BackColor = Color.White;
            btnLogoutDropdown.Click += (s, e) =>
            {
                panelMain.Controls.Remove(dropdown);
                dropdown.Dispose();
                HandleLogout();
            };

            dropdown.Controls.AddRange(new Control[] { btnMyProfile, btnLogoutDropdown });
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
                    if (panelMain.Controls.Contains(dropdown))
                    {
                        panelMain.Controls.Remove(dropdown);
                        dropdown.Dispose();
                    }
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void OpenProfileForm()
        {
            ShowFormInMainContent(new ProfileForm(loggedInUsername, "Pharmacist", UpdateHeaderProfile));
        }

        private async Task LoadDashboardStats()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // 1. Pending Orders (Filtered by Appointment Status = Completed)
                    string pendingQuery = @"SELECT COUNT(DISTINCT p.PrescriptionId) 
                                           FROM Prescriptions p 
                                           JOIN PrescriptionDetails pd ON p.PrescriptionId = pd.PrescriptionId 
                                           JOIN Appointments a ON p.AppointmentIntId = a.AppointmentIntId 
                                           WHERE pd.Status = 'Pending' AND a.Status = 'Completed'";
                    SqlCommand cmdPending = new SqlCommand(pendingQuery, con);
                    int pendingCount = (int)await cmdPending.ExecuteScalarAsync();

                    // 2. Low Stock Items (Quantity <= 10)
                    string stockQuery = "SELECT COUNT(*) FROM Medicines WHERE Quantity <= 10";
                    SqlCommand cmdStock = new SqlCommand(stockQuery, con);
                    int stockCount = (int)await cmdStock.ExecuteScalarAsync();

                    // 3. Dispensed Today
                    string dispensedQuery = @"SELECT COUNT(DISTINCT pd.PrescriptionId) 
                                             FROM PrescriptionDetails pd 
                                             JOIN Prescriptions p ON pd.PrescriptionId = p.PrescriptionID 
                                             WHERE pd.Status = 'Dispensed' AND CAST(p.DateCreated AS DATE) = CAST(GETDATE() AS DATE)";
                    SqlCommand cmdDispensed = new SqlCommand(dispensedQuery, con);
                    int dispensedCount = (int)await cmdDispensed.ExecuteScalarAsync();

                    this.SafeInvoke(() => {
                        UpdateCardValue(cardPending, pendingCount.ToString());
                        UpdateCardValue(cardStockAlert, stockCount.ToString());
                        UpdateCardValue(cardDispensed, dispensedCount.ToString());
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine("Stats error: " + ex.Message); }
        }

        private void UpdateCardValue(Panel card, string value)
        {
            if (card == null) return;
            var lbl = card.Controls.Find("lblVal", true).FirstOrDefault() as Label;
            if (lbl != null) lbl.Text = value;
        }

        private async Task LoadPrescriptionQueue()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    string query = @"SELECT DISTINCT p.PrescriptionCode, pt.PatientName, p.Diagnosis, p.DateCreated 
                                    FROM Prescriptions p
                                    JOIN PrescriptionDetails pd ON p.PrescriptionId = pd.PrescriptionId
                                    JOIN Appointments a ON p.AppointmentIntId = a.AppointmentIntId
                                    JOIN Patients pt ON a.PatientID = pt.PatientID
                                    WHERE pd.Status = 'Pending' AND a.Status = 'Completed'
                                    ORDER BY p.DateCreated DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    this.SafeInvoke(() => {
                        var flow = panelLeftColumn.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                        if (flow == null) return;
                        flow.Controls.Clear();

                        if (dt.Rows.Count == 0)
                        {
                            flow.Controls.Add(new Label { Text = "No pending orders.", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Padding = new Padding(10) });
                            return;
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            flow.Controls.Add(CreateQueueItem(
                                row["PrescriptionCode"].ToString(), 
                                row["PatientName"].ToString(), 
                                row["Diagnosis"].ToString(), 
                                Convert.ToDateTime(row["DateCreated"]).ToString("HH:mm")));
                        }
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine("Queue error: " + ex.Message); }
        }

        private Control CreateQueueItem(string code, string patient, string task, string time)
        {
            Panel p = new Panel { Size = new Size(340, 70), BackColor = Color.White, Margin = new Padding(0, 0, 0, 10) };
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 10))
                {
                    using (Pen pen = new Pen(borderGray, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            Label lblCode = new Label { Text = code, Font = new Font("Segoe UI Bold", 9), ForeColor = primaryBlue, Location = new Point(12, 12), AutoSize = true };
            Label lblPatient = new Label { Text = patient, Font = new Font("Segoe UI Semibold", 9), ForeColor = textPrimary, Location = new Point(12, 32), AutoSize = true };
            Label lblTask = new Label { Text = task, Font = new Font("Segoe UI", 8), ForeColor = textSecondary, Location = new Point(12, 50), AutoSize = true };
            Label lblTime = new Label { Text = time, Font = new Font("Segoe UI", 8), ForeColor = textSecondary, Location = new Point(280, 12), AutoSize = true };

            p.Controls.AddRange(new Control[] { lblCode, lblPatient, lblTask, lblTime });
            return p;
        }

        private async Task LoadInventoryOverview()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    // Select ALL stock, ordered by low stock first
                    string query = "SELECT TradeName, Quantity FROM Medicines ORDER BY Quantity ASC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    this.SafeInvoke(() => {
                        var flow = panelCenterColumn.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                        if (flow == null) return;
                        flow.Controls.Clear();

                        if (dt.Rows.Count == 0)
                        {
                            flow.Controls.Add(new Label { Text = "No inventory records found.", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Padding = new Padding(10) });
                            return;
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            flow.Controls.Add(CreateInventoryItem(
                                row["TradeName"].ToString(), 
                                Convert.ToInt32(row["Quantity"])));
                        }
                    });
                }
            }
            catch { }
        }

        private Control CreateInventoryItem(string name, int qty)
        {
            Panel p = new Panel { Size = new Size(580, 50), BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
            Label lblName = new Label { Text = name, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = textPrimary, Location = new Point(15, 15), AutoSize = true };
            Label lblQty = new Label { Text = "Stock: " + qty, Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(250, 15), AutoSize = true };
            
            string statusText;
            Color statusColor;
            Color statusBg;

            if (qty == 0)
            {
                statusText = "OUT OF STOCK";
                statusColor = accentRose; // Red
                statusBg = Color.FromArgb(254, 242, 242);
            }
            else if (qty < 10)
            {
                statusText = "LOW STOCK"; // User asked for red if < 10
                statusColor = accentRose; 
                statusBg = Color.FromArgb(254, 242, 242);
            }
            else
            {
                statusText = "IN STOCK";
                statusColor = accentGreen;
                statusBg = Color.FromArgb(236, 253, 245);
            }

            Label lblStatus = new Label { 
                Text = statusText, 
                Font = new Font("Segoe UI Bold", 8), 
                ForeColor = statusColor, 
                BackColor = statusBg,
                Padding = new Padding(8, 3, 8, 3),
                Location = new Point(480, 12),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            p.Controls.AddRange(new Control[] { lblName, lblQty, lblStatus });
            return p;
        }

        private async Task LoadRecentActivity()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    string query = @"SELECT TOP 8 p.PrescriptionCode, pt.PatientName, p.DateCreated 
                                    FROM Prescriptions p
                                    JOIN PrescriptionDetails pd ON p.PrescriptionId = pd.PrescriptionId
                                    JOIN Patients pt ON (SELECT PatientID FROM Appointments WHERE AppointmentIntId = p.AppointmentIntId) = pt.PatientID
                                    WHERE pd.Status = 'Dispensed' AND CAST(p.DateCreated AS DATE) = CAST(GETDATE() AS DATE)
                                    ORDER BY p.DateCreated DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    this.SafeInvoke(() => {
                        var flow = panelRightColumn.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                        if (flow == null) return;
                        flow.Controls.Clear();

                        if (dt.Rows.Count == 0)
                        {
                            flow.Controls.Add(new Label { Text = "No dispensing activity today yet.", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Padding = new Padding(10) });
                            return;
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            flow.Controls.Add(CreateActivityItem(
                                $"Dispensed {row["PrescriptionCode"]}", 
                                $"To {row["PatientName"]}", 
                                Convert.ToDateTime(row["DateCreated"]).ToString("HH:mm")));
                        }
                    });
                }
            }
            catch { }
        }

        private Control CreateActivityItem(string title, string subtitle, string time)
        {
            Panel p = new Panel { Size = new Size(300, 60), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 12) };
            Label lblTitle = new Label { Text = "🔵 " + title, Font = new Font("Segoe UI Semibold", 9), ForeColor = textPrimary, Location = new Point(0, 5), AutoSize = true };
            Label lblSub = new Label { Text = subtitle, Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(22, 25), AutoSize = true };
            Label lblTime = new Label { Text = time, Font = new Font("Segoe UI", 8), ForeColor = textSecondary, Location = new Point(22, 42), AutoSize = true };
            
            p.Controls.AddRange(new Control[] { lblTitle, lblSub, lblTime });
            return p;
        }

        private void WireStatusToggle()
        {
            if (btnOnDuty != null)
            {
                btnOnDuty.Click += (s, e) => {
                    UpdateDutyStatus(true);
                };
            }
            if (btnOnBreak != null)
            {
                btnOnBreak.Click += (s, e) => {
                    UpdateDutyStatus(false);
                };
            }
        }

        private void UpdateDutyStatus(bool onDuty)
        {
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
                
                UpdateCardValue(cardStatus, "ON DUTY");
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

                UpdateCardValue(cardStatus, "ON BREAK");
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (this.InvokeRequired)
            {
                try { this.Invoke(action); } catch { }
            }
            else
            {
                action();
            }
        }
    }
}
