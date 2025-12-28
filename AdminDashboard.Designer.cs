using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class AdminDashboard : Form
    {
        private System.ComponentModel.IContainer components = null;

        // Modern Color Palette (Identical to reference)
        private readonly Color primaryBlue = Color.FromArgb(59, 130, 246);
        private readonly Color surfaceColor = Color.FromArgb(250, 252, 255); // Brighter background
        private readonly Color cardColor = Color.White;
        private readonly Color textPrimary = Color.FromArgb(31, 41, 55);
        private readonly Color textSecondary = Color.FromArgb(107, 114, 128);
        private readonly Color borderGray = Color.FromArgb(229, 231, 235);
        private readonly Color accentGreen = Color.FromArgb(16, 185, 129);
        private readonly Color accentOrange = Color.FromArgb(245, 158, 11);
        private readonly Color accentPurple = Color.FromArgb(139, 92, 246);

        // Sidebar (Preserved but integrated into the look)
        private Panel panelSidebar, panelLogo;
        private IconButton btnDashboard, btnUsers, btnPatients, btnDoctors, btnAppointments;
        private IconButton btnMedicines, btnStaff, btnBilling, btnReports, btnPrescriptions, btnLogout;

        // Main Layout Panels
        private Panel panelMain, panelTopToolbar, panelDashboardOverview, pnlProfileComp;
        private Panel panelWelcome, panelCardsRow, panelMiddleContent;
        private Panel panelLeftColumn, panelCenterColumn, panelRightColumn;

        // Header Controls
        private TextBox txtTopSearch;
        private Label lblWelcome, lblSubWelcome, lblPageTitle;
        private ComboBox cmbTimeFilter;
        private Button btnExport;
        private PictureBox pbHeaderProfile;
        private Label lblHeaderName, lblHeaderRole;

        // Stat Cards
        private Panel cardPatients, cardRevenue, cardStats, cardDemographics, cardApptsToday;
        
        // List Controls
        private Panel pnlAppointmentsList, pnlReportsList;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ================ FORM SETUP =================
            this.ClientSize = new Size(1600, 950);
            this.MinimumSize = new Size(1300, 800);
            this.Name = "AdminDashboard";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Admin Dashboard";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.DoubleBuffered = true;

            // ================ MAIN PANEL =================
            panelMain = new Panel() { 
                Dock = DockStyle.Fill, 
                BackColor = surfaceColor, 
                Padding = new Padding(0), // Padding will be handled by sub-panels or layout logic
                AutoScroll = true 
            };

            // ================ TOP TOOLBAR (Search & Profile) =================
            panelTopToolbar = new Panel() { Dock = DockStyle.Top, Height = 85, BackColor = Color.White, Padding = new Padding(20, 15, 20, 15) };
            
            pnlProfileComp = CreateProfileComp();
            Panel pnlSearchComp = CreateTopSearch();
            
            lblPageTitle = new Label { 
                Text = "Dashboard", 
                Font = new Font("Segoe UI Semibold", 16), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(20, 25) 
            };

            panelTopToolbar.Controls.AddRange(new Control[] { pnlProfileComp, pnlSearchComp, lblPageTitle });

            // ================ WELCOME SECTION =================
            panelWelcome = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 110, // Standard height
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 20, 0, 0) 
            };
            lblWelcome = new Label { 
                Text = "Welcome, Admin!", 
                Font = new Font("Segoe UI", 20, FontStyle.Bold), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(0, 20) 
            };
            lblSubWelcome = new Label { 
                Text = "Dashboard overview: 12 pending lab reports and 4 critical stock alerts found.", 
                Font = new Font("Segoe UI", 10), 
                ForeColor = textSecondary, 
                AutoSize = true, 
                Location = new Point(2, 75) 
            };

            cmbTimeFilter = new ComboBox { 
                Size = new Size(120, 35), 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Font = new Font("Segoe UI", 10), 
                BackColor = Color.White, 
                FlatStyle = FlatStyle.Flat 
            };
            cmbTimeFilter.Items.AddRange(new string[] { "Today", "Weekly", "Monthly" }); 
            cmbTimeFilter.SelectedIndex = 0;
            
            btnExport = CreateFlatButton("Export   📤", cardColor, 120, 40);
            btnExport.ForeColor = textPrimary;
            btnExport.Font = new Font("Segoe UI Semibold", 9.5F);
            
            // Hide filter and export as per user request
            cmbTimeFilter.Visible = false;
            btnExport.Visible = false;
            
            panelWelcome.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome, cmbTimeFilter, btnExport });

            // ================ STAT CARDS ROW =================
            panelCardsRow = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 220, 
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 10, 0, 10), // Padding between Welcome and Middle
                Name = "pnlKpiCards" 
            };
            
            cardPatients = CreateStatCard("Total Patients", "0", "Total registered", accentGreen, true);
            cardRevenue = CreateStatCard("Today's Revenue", "₨ 0", "Today earnings", primaryBlue, false);
            cardApptsToday = CreateStatCard("Today's Appointments", "0", "Scheduled today", accentOrange, true);
            cardStats = CreateStatCard("Total Staff", "0", "Active staff", accentOrange, false);
            cardDemographics = CreateStatCard("Total Doctors", "0", "Registered specialists", accentPurple, true);

            panelCardsRow.Controls.AddRange(new Control[] { cardPatients, cardRevenue, cardApptsToday, cardStats, cardDemographics });

            // ================ MIDDLE CONTENT (3 COLUMNS) =================
            panelMiddleContent = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 500, // Fixed height to enable scrollbar in parent
                BackColor = Color.Transparent, 
                Padding = new Padding(0, 10, 0, 20) 
            };
            
            panelLeftColumn = CreateContentCard("📅 Appointments Overview", 400);
            panelCenterColumn = CreateContentCard("👨‍⚕️ Doctors On-Duty", 340);
            panelRightColumn = CreateContentCard("📊 Pending Overview", 460);

            PopulateAppointmentsList(panelLeftColumn);
            PopulateOnDutyDoctors(panelCenterColumn);
            PopulatePendingOverview(panelRightColumn);

            panelMiddleContent.Controls.AddRange(new Control[] { panelRightColumn, panelCenterColumn, panelLeftColumn });

            // ================ DASHBOARD OVERVIEW CONTAINER =================
            panelDashboardOverview = new Panel() { 
                Dock = DockStyle.Fill, 
                BackColor = Color.Transparent, 
                AutoScroll = true, 
                Padding = new Padding(30, 0, 30, 20) 
            };
            
            // Add in reverse order for Dock.Top (Last Added = Topmost)
            panelDashboardOverview.Controls.Add(panelMiddleContent); // Bottom
            panelDashboardOverview.Controls.Add(panelCardsRow);      // Middle
            panelDashboardOverview.Controls.Add(panelWelcome);       // Top
            
            // ================ MAIN PANEL =================
            panelMain.Controls.Clear();
            panelMain.Controls.Add(panelDashboardOverview); // Docks Fill last
            panelMain.Controls.Add(panelTopToolbar);        // Docks Top first
            
            // ================ SIDEBAR INITIALIZE =================
            InitializeSidebar(); // This initializes and adds panelSidebar to this.Controls

            // ================ FINAL FORM ASSEMBLE =================
            if (!this.Controls.Contains(panelMain)) this.Controls.Add(panelMain);
            
            // In WinForms, the first control added is OUTERMOST.
            // But we can also use BringToFront/SendToBack to force order.
            panelSidebar.SendToBack(); // Outer context
            panelMain.BringToFront();  // Center context

            this.Load += (s, e) => {
                ApplyFixedDesignLayout();
            };
            this.Resize += (s, e) => ApplyFixedDesignLayout();

            this.ResumeLayout(false);
        }

        private void ApplyFixedDesignLayout()
        {
            if (panelMain == null || panelDashboardOverview == null) return;
            int availableWidth = panelMain.Width - panelMain.Padding.Left - panelMain.Padding.Right;
            
            panelTopToolbar.Width = availableWidth;
            panelDashboardOverview.Width = availableWidth;
            
            // Stat Cards Proportions - 5 Cards Layout
            int cardGap = 15; // Reduced from 20 for better fit
            int contentWidth = availableWidth - 60; // Accounting for 30px padding on each side
            int cardWidth = (contentWidth - cardGap * 4) / 5;
            
            // Re-align Export button and filter
            cmbTimeFilter.Location = new Point(contentWidth + 30 - 260, 32);
            btnExport.Location = new Point(contentWidth + 30 - 135, 29); // Moved slightly left
            if (pnlProfileComp != null) pnlProfileComp.Left = availableWidth - pnlProfileComp.Width - 20;

            cardPatients.SetBounds(0, 10, cardWidth, 180);
            cardRevenue.SetBounds(cardWidth + cardGap, 10, cardWidth, 180);
            cardApptsToday.SetBounds((cardWidth + cardGap) * 2, 10, cardWidth, 180);
            cardStats.SetBounds((cardWidth + cardGap) * 3, 10, cardWidth, 180);
            cardDemographics.SetBounds((cardWidth + cardGap) * 4, 10, cardWidth, 180);

            // Middle Content Layout - Rebalanced for Admin Monitoring
            int apptWidth = (int)(contentWidth * 0.40); // 40% for appointments
            int docWidth = (int)(contentWidth * 0.25);  // 25% for narrow doctors list
            int pendingWidth = contentWidth - apptWidth - docWidth - cardGap * 2; // ~35%

            panelLeftColumn.SetBounds(0, 0, apptWidth, panelMiddleContent.Height - 10);
            panelCenterColumn.SetBounds(apptWidth + cardGap, 0, docWidth, panelMiddleContent.Height - 10);
            panelRightColumn.SetBounds(contentWidth - pendingWidth, 0, pendingWidth, panelMiddleContent.Height - 10);

            panelMain.Invalidate(true);
        }

        private Panel CreateTopSearch()
        {
            Panel p = new Panel { Size = new Size(400, 42), Location = new Point(20, 14), BackColor = Color.FromArgb(249, 250, 251) };
            p.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 10)) { 
                    p.Region = new Region(path); 
                    using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path); 
                } 
            };
            
            Label lblIcon = new Label { Text = "🔍", Location = new Point(12, 12), AutoSize = true, Font = new Font("Segoe UI Emoji", 10), ForeColor = Color.Gray };
            txtTopSearch = new TextBox { Text = "Search anything here...", Location = new Point(52, 11), Size = new Size(330, 24), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(249, 250, 251), ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            txtTopSearch.GotFocus += (s, e) => { if (txtTopSearch.Text == "Search anything here...") txtTopSearch.Text = ""; txtTopSearch.ForeColor = textPrimary; };
            txtTopSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtTopSearch.Text)) { txtTopSearch.Text = "Search anything here..."; txtTopSearch.ForeColor = Color.Gray; } };
            
            p.Controls.AddRange(new Control[] { lblIcon, txtTopSearch });
            return p;
        }

        private Panel CreateProfileComp()
        {
            Panel p = new Panel { Size = new Size(350, 65), BackColor = Color.Transparent };
            
            pbHeaderProfile = new PictureBox { Size = new Size(46, 46), Location = new Point(290, 10), BackColor = primaryBlue, SizeMode = PictureBoxSizeMode.StretchImage };
            pbHeaderProfile.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = new GraphicsPath()) { 
                    path.AddEllipse(0, 0, 45, 45); 
                    pbHeaderProfile.Region = new Region(path); 
                } 
            };
            
            lblHeaderName = new Label { Text = "Admin User", Font = new Font("Segoe UI Semibold", 10.5F), Location = new Point(10, 12), Size = new Size(270, 22), TextAlign = ContentAlignment.MiddleRight, ForeColor = textPrimary };
            lblHeaderRole = new Label { Text = "Super Administrator", Font = new Font("Segoe UI", 9), Location = new Point(10, 34), Size = new Size(270, 20), TextAlign = ContentAlignment.MiddleRight, ForeColor = textSecondary };
            
            p.Controls.AddRange(new Control[] { pbHeaderProfile, lblHeaderName, lblHeaderRole });
            pbHeaderProfile.Cursor = Cursors.Hand;
            pbHeaderProfile.Click += (s, e) => ShowProfileDropdown(pbHeaderProfile);
            return p;
        }

        private Panel CreateStatCard(string title, string value, string trend, Color themeColor, bool isArea)
        {
            Panel card = new Panel { BackColor = Color.Transparent, Padding = new Padding(20) };
            card.Paint += (s, e) => { 
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) 
                { 
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(borderGray, 1)) e.Graphics.DrawPath(pen, path); 
                } 
            };

            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = textSecondary, Location = new Point(20, 20), AutoSize = true, BackColor = Color.Transparent };
            Label lblVal = new Label { Text = value, Font = new Font("Segoe UI Bold", 18), ForeColor = textPrimary, Location = new Point(20, 50), AutoSize = true, BackColor = Color.Transparent };
            Label lblTrend = new Label { Text = trend, Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = (trend.Contains("↑") ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68)), Location = new Point(20, 95), AutoSize = true, BackColor = Color.Transparent };

            // Chart section (Right) - Reduced width to prevent overlapping text
            Panel chart = new Panel { Size = new Size(85, 60), BackColor = Color.Transparent };
            chart.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int[] data = { 30, 60, 45, 80, 55, 90, 75 };
                if (isArea) {
                    PointF[] pts = new PointF[data.Length + 2];
                    for (int i = 0; i < data.Length; i++) pts[i] = new PointF(i * (chart.Width / 6.0f), chart.Height - (data[i] * chart.Height / 100.0f));
                    pts[data.Length] = new PointF(chart.Width, chart.Height); pts[data.Length + 1] = new PointF(0, chart.Height);
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(40, themeColor))) e.Graphics.FillPolygon(sb, pts);
                    using (Pen p = new Pen(themeColor, 2)) e.Graphics.DrawLines(p, pts.Take(data.Length).ToArray());
                } else {
                    for (int i = 0; i < data.Length; i++) {
                        int h = data[i] * (chart.Height - 10) / 100;
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(160, themeColor)))
                            e.Graphics.FillRectangle(sb, i * 16, chart.Height - h, 10, h);
                    }
                }
            };
            
            card.Controls.AddRange(new Control[] { chart, lblTitle, lblVal, lblTrend }); // Reordered for Z-order just in case
            card.Resize += (s, e) => chart.Location = new Point(card.Width - chart.Width - 15, (card.Height - chart.Height) / 2 + 10);
            return card;
        }

        private Panel CreateReceptionStatsCard()
        {
            Panel card = new Panel { BackColor = Color.White };
            card.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                e.Graphics.Clear(Color.White);
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) { 
                    card.Region = new Region(path); 
                    using (Pen p = new Pen(borderGray)) e.Graphics.DrawPath(p, path); 
                } 
            };
            
            Label lbl = new Label { Text = "Patient Queue", Font = new Font("Segoe UI Semibold", 12), ForeColor = textPrimary, Location = new Point(20, 22), AutoSize = true, BackColor = Color.Transparent };
            
            // Queue Status Overview
            Label lWaiting = new Label { Text = "• Waiting:   0", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(245, 158, 11), Location = new Point(20, 62), AutoSize = true, BackColor = Color.Transparent };
            Label lCheckedIn = new Label { Text = "• Checked-In: 0", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(59, 130, 246), Location = new Point(20, 92), AutoSize = true, BackColor = Color.Transparent };
            Label lWithDoctor = new Label { Text = "• With Doctor: 0", Font = new Font("Segoe UI Bold", 9.5F), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(20, 122), AutoSize = true, BackColor = Color.Transparent };

            // Visual Queue Indicator (Right)
            Panel donut = new Panel { Name = "donutMain", Size = new Size(95, 95), BackColor = Color.Transparent };
            donut.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p1 = new Pen(Color.FromArgb(243, 244, 246), 10)) e.Graphics.DrawArc(p1, 5, 5, 84, 84, 0, 360);
                using (Pen p2 = new Pen(accentGreen, 10)) e.Graphics.DrawArc(p2, 5, 5, 84, 84, -90, 180);
                string pct = "0"; string sub = "Queue";
                using (Font f1 = new Font("Segoe UI Bold", 12)) 
                using (Font f2 = new Font("Segoe UI", 8)) 
                using (SolidBrush b1 = new SolidBrush(textPrimary))
                using (SolidBrush b2 = new SolidBrush(textSecondary)) {
                    SizeF s1 = e.Graphics.MeasureString(pct, f1); SizeF s2 = e.Graphics.MeasureString(sub, f2);
                    e.Graphics.DrawString(pct, f1, b1, (donut.Width - s1.Width) / 2, (donut.Height - s1.Height) / 2 - 8);
                    e.Graphics.DrawString(sub, f2, b2, (donut.Width - s2.Width) / 2, (donut.Height - s2.Height) / 2 + 12);
                }
            };

            card.Controls.AddRange(new Control[] { lbl, lWaiting, lCheckedIn, lWithDoctor, donut });
            card.Resize += (s, e) => donut.Location = new Point(card.Width - 115, (card.Height - donut.Height) / 2);
            return card;
        }

        private Panel CreateDemographicsCard()
        {
            Panel card = new Panel { BackColor = Color.White };
            card.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) { 
                    card.Region = new Region(path); 
                    using (Pen p = new Pen(borderGray)) e.Graphics.DrawPath(p, path); 
                } 
            };
            
            Label lbl = new Label { Text = "Total Doctors", Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = textPrimary, Location = new Point(20, 22), AutoSize = true, BackColor = Color.Transparent };
            
            // Donut Chart for Demographics
            Panel donut = new Panel { Size = new Size(100, 100), BackColor = Color.Transparent };
            donut.Paint += (s, ev) => {
                if (ev.Graphics == null) return;
                ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float[] angles = { 48 * 3.6f, 42 * 3.6f, 10 * 3.6f };
                Color[] colors = { Color.FromArgb(59, 130, 246), Color.FromArgb(236, 72, 153), Color.Gainsboro };
                float start = -90;
                for (int i = 0; i < angles.Length; i++) {
                    using (SolidBrush sb = new SolidBrush(colors[i])) ev.Graphics.FillPie(sb, 5, 5, 90, 90, start, angles[i]);
                    start += angles[i];
                }
                using (SolidBrush sb = new SolidBrush(Color.White)) ev.Graphics.FillEllipse(sb, 28, 28, 44, 44);
            };
            
            Label lMale = new Label { Text = "■ Male: 48%", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(59, 130, 246), Location = new Point(20, 140), AutoSize = true, BackColor = Color.Transparent };
            Label lFem = new Label { Text = "■ Female: 42%", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(236, 72, 153), Location = new Point(120, 140), AutoSize = true, BackColor = Color.Transparent };
            
            card.Controls.AddRange(new Control[] { lbl, donut, lMale, lFem });
            card.Resize += (s, e) => { 
                donut.Location = new Point((card.Width - donut.Width) / 2, 42);
                lMale.Left = 25; lFem.Left = card.Width - lFem.Width - 25;
            };
            return card;
        }

        private Panel CreateContentCard(string title, int defaultWidth)
        {
            Panel card = new Panel { BackColor = Color.Transparent, Width = defaultWidth };
            card.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) { 
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen p = new Pen(borderGray, 1)) e.Graphics.DrawPath(p, path); 
                } 
            };

            Label lbl = new Label { Text = title, Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 20), AutoSize = true };
            card.Controls.Add(lbl);
            return card;
        }

        public void PopulateAppointmentsList(Panel p, DataTable data = null)
        {
            p.Controls.Clear();
            p.BackColor = Color.Transparent; // Ensure parent is transparent for AA
            Label lbl = new Label { Text = "📅 Appointments Overview", Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 20), AutoSize = true };
            p.Controls.Add(lbl);

            Panel list = new Panel { Location = new Point(15, 65), Size = new Size(p.Width - 30, p.Height - 85), AutoScroll = true };
            p.Controls.Add(list); p.Resize += (s, e) => list.Width = p.Width - 30;

            if (data == null || data.Rows.Count == 0) {
                Label empty = new Label { Text = "No appointments today", Font = new Font("Segoe UI", 10), ForeColor = textSecondary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                list.Controls.Add(empty);
                return;
            }

            int y = 0;
            foreach (DataRow row in data.Rows) {
                Panel item = new Panel { Size = new Size(list.Width - 25, 75), Location = new Point(0, y), BackColor = Color.FromArgb(252, 253, 254) };
                item.Paint += (s, e) => { 
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, item.Width - 1, item.Height - 1, 10)) {
                        using (Pen pen = new Pen(Color.FromArgb(240, 243, 246))) e.Graphics.DrawPath(pen, path);
                    }
                };

                Label time = new Label { Text = Convert.ToDateTime(row["Date"]).ToString("HH:mm"), Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = textSecondary, Location = new Point(15, 28), AutoSize = true };
                
                string statsText = row["Status"].ToString();
                Label status = new Label { 
                    Text = statsText, 
                    Font = new Font("Segoe UI Semibold", 8F), 
                    TextAlign = ContentAlignment.MiddleCenter, 
                    AutoSize = true, 
                    Padding = new Padding(8, 4, 8, 4)
                };
                
                if (statsText == "Completed") { status.BackColor = Color.FromArgb(220, 252, 231); status.ForeColor = Color.FromArgb(21, 128, 61); }
                else if (statsText == "Pending") { status.BackColor = Color.FromArgb(254, 243, 199); status.ForeColor = Color.FromArgb(180, 83, 9); }
                else { status.BackColor = Color.FromArgb(219, 234, 254); status.ForeColor = Color.FromArgb(29, 78, 216); }
                
                status.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, status.Width - 1, status.Height - 1, 6)) status.Region = new Region(path); };

                Label name = new Label { Text = row["PatientName"].ToString(), Font = new Font("Segoe UI Bold", 10), ForeColor = textPrimary, Location = new Point(75, 15), AutoSize = true };
                Label desc = new Label { Text = row["DoctorName"].ToString() + " (" + row["Status"].ToString() + ")", Font = new Font("Segoe UI", 8.8F), ForeColor = textSecondary, Location = new Point(75, 40), AutoSize = true };
                
                item.Controls.AddRange(new Control[] { time, name, desc, status });
                status.Location = new Point(item.Width - status.PreferredWidth - 15, (item.Height - status.PreferredHeight) / 2);
                item.Resize += (s, e) => status.Location = new Point(item.Width - status.PreferredWidth - 15, (item.Height - status.PreferredHeight) / 2);

                list.Controls.Add(item); y += 85; 
            }
        }

        public void PopulateOnDutyDoctors(Panel p, DataTable data = null)
        {
            p.Controls.Clear();
            Label lbl = new Label { Text = "👨‍⚕️ Doctors On-Duty", Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 20), AutoSize = true };
            p.Controls.Add(lbl);

            Panel list = new Panel { Location = new Point(15, 65), Size = new Size(p.Width - 30, p.Height - 85), AutoScroll = true };
            p.Controls.Add(list); p.Resize += (s, e) => list.Bounds = new Rectangle(15, 65, p.Width - 30, p.Height - 85);

            if (data == null || data.Rows.Count == 0) {
                Label empty = new Label { Text = "No doctors on duty", Font = new Font("Segoe UI", 10), ForeColor = textSecondary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                list.Controls.Add(empty);
                return;
            }

            int y = 0;
            foreach (DataRow row in data.Rows) {
                Panel dChip = new Panel { Size = new Size(list.Width - 25, 60), Location = new Point(0, y), BackColor = Color.FromArgb(252, 253, 254) };
                dChip.Paint += (s, e) => { 
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, dChip.Width - 1, dChip.Height - 1, 10)) {
                        using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                    }
                };
                
                Label name = new Label { Text = row["DoctorName"].ToString(), Font = new Font("Segoe UI Bold", 10), ForeColor = textPrimary, Location = new Point(15, 10), AutoSize = true };
                Label spec = new Label { Text = row["Specialization"].ToString(), Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(15, 32), AutoSize = true };
                
                string stats = row["Status"].ToString();
                Label status = new Label { 
                    Text = stats, 
                    Font = new Font("Segoe UI Bold", 8), 
                    ForeColor = stats == "Active" ? accentGreen : accentOrange,
                    BackColor = Color.FromArgb(30, stats == "Active" ? accentGreen : accentOrange),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(8, 4, 8, 4),
                    AutoSize = true
                };
                status.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, status.Width - 1, status.Height - 1, 4)) status.Region = new Region(path); };

                status.Location = new Point(dChip.Width - status.PreferredWidth - 15, (dChip.Height - status.PreferredHeight) / 2);
                dChip.Resize += (s, e) => status.Left = dChip.Width - status.PreferredWidth - 15;

                dChip.Controls.AddRange(new Control[] { name, spec, status });
                list.Controls.Add(dChip); y += 70;
            }
        }

        public void PopulatePendingOverview(Panel p, DataTable data = null)
        {
            p.Controls.Clear();
            Label lbl = new Label { Text = "📊 Pending Overview", Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 20), AutoSize = true };
            p.Controls.Add(lbl);

            Panel list = new Panel { Location = new Point(15, 65), Size = new Size(p.Width - 30, p.Height - 85), AutoScroll = true };
            p.Controls.Add(list); p.Resize += (s, e) => list.Bounds = new Rectangle(15, 65, p.Width - 30, p.Height - 85);

            if (data == null || data.Rows.Count == 0) {
                Label empty = new Label { Text = "All operations up to date", Font = new Font("Segoe UI", 10), ForeColor = textSecondary, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
                list.Controls.Add(empty);
                return;
            }

            int y = 0;
            foreach (DataRow row in data.Rows) {
                Panel aCard = new Panel { Size = new Size(list.Width - 25, 65), Location = new Point(0, y), BackColor = Color.White };
                Color priorityColor = accentOrange;
                
                aCard.Paint += (s, e) => { 
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, aCard.Width - 1, aCard.Height - 1, 10)) {
                        using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                    }
                    using (SolidBrush sb = new SolidBrush(priorityColor))
                        e.Graphics.FillRectangle(sb, 0, 15, 5, aCard.Height - 30);
                };

                Label msg = new Label { Text = row["Count"].ToString() + " " + row["Item"].ToString(), Font = new Font("Segoe UI Bold", 11), ForeColor = textPrimary, Location = new Point(20, 12), AutoSize = true };
                Label time = new Label { Text = "Awaiting Action", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(20, 36), AutoSize = true };
                
                aCard.Controls.AddRange(new Control[] { msg, time });
                list.Controls.Add(aCard); y += 75;
            }
        }


        private Button CreateFlatButton(string text, Color bg, int w, int h)
        {
            Button b = new Button { Text = text, Size = new Size(w, h), BackColor = bg, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
            b.FlatAppearance.BorderSize = 0;
            b.Paint += (s, e) => { 
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, b.Width - 1, b.Height - 1, 10)) b.Region = new Region(path); 
            };
            return b;
        }

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        private void InitializeSidebar()
        {
            panelSidebar = new Panel { Name = "panelSidebar", Dock = DockStyle.Left, Width = 230, BackColor = Color.FromArgb(31, 41, 55), AutoScroll = false };
            
            // Branding panel at the TOP of sidebar
            panelLogo = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.FromArgb(17, 24, 39) };
            Label lblLogo = new Label { 
                Text = "🩺 AL REHMAN CLINIC", 
                Font = new Font("Segoe UI", 13, FontStyle.Bold), 
                ForeColor = Color.White, 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 10, 0, 0) // Shift slightly down
            };
            panelLogo.Controls.Add(lblLogo);

            btnDashboard = CreateSidebarButton("Dashboard", IconChar.TachometerAlt);
            btnUsers = CreateSidebarButton("Users", IconChar.Users);
            btnPatients = CreateSidebarButton("Patients", IconChar.UserInjured);
            btnDoctors = CreateSidebarButton("Doctors", IconChar.UserDoctor);
            btnAppointments = CreateSidebarButton("Appointments", IconChar.CalendarCheck);
            btnMedicines = CreateSidebarButton("Medicines", IconChar.Pills);

            btnStaff = CreateSidebarButton("Staff", IconChar.UserTie);
            btnBilling = CreateSidebarButton("Billing", IconChar.FileInvoiceDollar);
            btnReports = CreateSidebarButton("Reports", IconChar.FileAlt);

            // Visual separator for Logout
            Panel pnlLogoutSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(55, 65, 81), Margin = new Padding(0, 20, 0, 0) };
            btnLogout = CreateSidebarButton("Logout", IconChar.SignOutAlt, Color.IndianRed);
            btnLogout.Dock = DockStyle.Bottom;
            
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(pnlLogoutSep);

            // Wiring
         btnDashboard.Click += (s, e) => ShowDashboard();

            btnLogout.Click += (s, e) => this.Close();

            // Add controls in correct order: Bottom items first, then top items
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(pnlLogoutSep);
            
            btnPrescriptions = CreateSidebarButton("Prescriptions", IconChar.FilePrescription);

            panelSidebar.Controls.AddRange(new Control[] { 
                btnReports, 
                btnBilling,
                btnPrescriptions,
                btnStaff, 
                btnMedicines, 
                btnAppointments, 
                btnDoctors, 
                btnPatients, 
                btnUsers, 
                btnDashboard 
            });
            
            // Add branding LAST so it appears at TOP
            panelSidebar.Controls.Add(panelLogo);
            
            this.Controls.Add(panelSidebar);
        }

        private IconButton CreateSubmenuButton(string text, IconChar icon)
        {
            IconButton btn = new IconButton { 
                Text = "   " + text, 
                IconChar = icon, 
                IconSize = 18, 
                IconColor = Color.Gainsboro, 
                ForeColor = Color.Gainsboro, 
                TextAlign = ContentAlignment.MiddleLeft, 
                ImageAlign = ContentAlignment.MiddleLeft, 
                Dock = DockStyle.Top, 
                Height = 40, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(43, 53, 67), 
                Font = new Font("Segoe UI", 9), 
                IconFont = IconFont.Auto, 
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(35, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private IconButton CreateSidebarButton(string text, IconChar icon, Color? iconColor = null)
        {
            IconButton btn = new IconButton { Text = " " + text, IconChar = icon, IconSize = 24, IconColor = iconColor ?? Color.Gainsboro, ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleLeft, ImageAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Top, Height = 45, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 41, 55), Font = new Font("Segoe UI", 10), IconFont = IconFont.Auto, TextImageRelation = TextImageRelation.ImageBeforeText };
            btn.FlatAppearance.BorderSize = 0; return btn;
        }
    }
}
