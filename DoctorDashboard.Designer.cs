using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class DoctorDashboard : Form
    {
        private System.ComponentModel.IContainer components = null;

        // Modern Color Palette
        private Color primaryBlue;
        private Color surfaceColor;
        private Color cardColor;
        private Color textPrimary;
        private Color textSecondary;
        private Color borderGray;
        private Color accentGreen;
        private Color accentOrange;
        private Color accentPurple;
        private Color accentRose;

        // Sidebar Colors
        private Color SidebarDefaultBackColor;
        private Color SidebarHoverBackColor;
        private Color SidebarActiveBackColor;

        public void InitializeTheme()
        {
            primaryBlue = Color.FromArgb(59, 130, 246);
            surfaceColor = Color.FromArgb(243, 246, 249);
            cardColor = Color.White;
            textPrimary = Color.FromArgb(31, 41, 55);
            textSecondary = Color.FromArgb(107, 114, 128);
            borderGray = Color.FromArgb(229, 231, 235);
            accentGreen = Color.FromArgb(16, 185, 129);
            accentOrange = Color.FromArgb(245, 158, 11);
            accentPurple = Color.FromArgb(79, 70, 229);
            accentRose = Color.FromArgb(244, 63, 94);
            
            SidebarDefaultBackColor = Color.FromArgb(31, 41, 55);
            SidebarHoverBackColor = Color.FromArgb(55, 65, 81);
            SidebarActiveBackColor = Color.FromArgb(59, 130, 246); // Unified Modern Blue
        }

        // Sidebar Components
        private Panel panelSidebar, panelLogo;
        private IconButton btnDashboard, btnAppointments, btnQueue, btnHistory, btnConsultation, btnPrescriptions, btnLogout;

        // Main Layout Panels
        private Panel panelMain, panelTopToolbar, pnlProfileComp;
        private Panel panelWelcome, panelCardsRow, panelMiddleContent;
        private Panel panelLeftColumn, panelCenterColumn, panelRightColumn;

        private Label lblWelcome, lblSubWelcome, lblPageTitle, lblDateShift;
        private Label lblHeaderName, lblHeaderRole;
        private PictureBox pbHeaderProfile;
        
        // Header Toggles
        private Panel pnlDateShift, pnlStatusToggle;
        private IconButton btnOnDuty, btnOnBreak;
        
        // Stat Cards
        private Panel cardWaiting, cardTodaySeen, cardUpcoming, cardCurrentStatus;
        private FlowLayoutPanel flowPatientQueue;

        // Content
        private const int CONTENT_TOP_OFFSET = 80;

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
            this.Name = "DoctorDashboard";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Doctor Dashboard";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.DoubleBuffered = true;

            // ================ MAIN PANEL =================
            panelMain = new Panel() { Dock = DockStyle.Fill, BackColor = surfaceColor, Padding = new Padding(30, 0, 30, 20), AutoScroll = false };

            // ================ TOP TOOLBAR =================
            panelTopToolbar = new Panel() { Dock = DockStyle.Top, Height = 70, BackColor = Color.White, Padding = new Padding(25, 12, 25, 12) };
            
            pnlProfileComp = CreateProfileComp();
            Panel pnlSearchComp = CreateGlobalSearch();
            pnlDateShift = CreateDateShiftIndicator();
            pnlStatusToggle = CreateStatusTogglePanel();
            
            lblPageTitle = new Label { 
                Text = "Dashboard", 
                Font = new Font("Segoe UI Semibold", 16), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(25, 20),
                Visible = false
            };

            panelTopToolbar.Controls.AddRange(new Control[] { pnlProfileComp, pnlStatusToggle, pnlDateShift, pnlSearchComp });

            // WELCOME SECTION
            panelWelcome = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 130, 
                BackColor = Color.Transparent, 
                Padding = new Padding(30, 15, 30, 0) 
            };
            
            lblWelcome = new Label { 
                Text = "Welcome, Doctor!", 
                Font = new Font("Segoe UI", 22, FontStyle.Bold), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(30, 15) 
            };
            
            lblSubWelcome = new Label { 
                Text = "Here is your daily overview and patient schedule.", 
                Font = new Font("Segoe UI", 11), 
                ForeColor = textSecondary, 
                AutoSize = true, 
                Location = new Point(32, 70) 
            };
            
            panelWelcome.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome });

            // STAT CARDS ROW
            panelCardsRow = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 240, 
                BackColor = Color.Transparent, 
                Padding = new Padding(30, 30, 30, 10),
                Name = "pnlKpiCards"
            };
            
            cardWaiting = CreateOperationalCard("Patients Waiting", "5", "In Queue", accentOrange);
            cardTodaySeen = CreateOperationalCard("Patients Seen Today", "18", "Completed", accentGreen);
            cardUpcoming = CreateOperationalCard("Upcoming Appointments", "12", "Today", primaryBlue);
            cardCurrentStatus = CreateOperationalCard("Current Status", "ON DUTY", "Activity", accentPurple);

            panelCardsRow.Controls.AddRange(new Control[] { cardWaiting, cardTodaySeen, cardUpcoming, cardCurrentStatus });

            // MIDDLE CONTENT
            panelMiddleContent = new Panel() { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(30, 0, 30, 20) };
            
            panelLeftColumn = CreateContentCard("My Queue", 380);
            panelCenterColumn = CreateContentCard("Today's Appointments", 520);
            panelRightColumn = CreateContentCard("Patient Details", 340);

            panelLeftColumn.AutoScroll = true;
            panelCenterColumn.AutoScroll = false; 
            panelRightColumn.AutoScroll = true;

            PopulatePatientQueue(panelLeftColumn);
            PopulateTodaySchedule(panelCenterColumn);
            PopulatePatientHistory(panelRightColumn);

            panelMiddleContent.Controls.AddRange(new Control[] { panelRightColumn, panelCenterColumn, panelLeftColumn });

            // ================ ADD TO FORM =================
            InitializeSidebar();
            this.Controls.Add(panelSidebar);
            this.Controls.Add(panelMain);
            this.Controls.SetChildIndex(panelMain, 0);

            // ================ DOCKING ORDER (Back to Front) =================
            // CORRECT DOCKING ORDER: Add Fill panel first (Bottom), then Top panels last (Top)
            panelMain.Controls.Add(panelMiddleContent);
            panelMain.Controls.Add(panelCardsRow);
            panelMain.Controls.Add(panelWelcome);
            panelMain.Controls.Add(panelTopToolbar);

            this.Load += (s, e) => { try { ApplyFixedDesignLayout(); } catch { } };
            this.Resize += (s, e) => { try { ApplyFixedDesignLayout(); } catch { } };

            this.ResumeLayout(false);
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

            // Create menu buttons
            btnDashboard = CreateSidebarButton("Dashboard", IconChar.TachometerAlt);
            btnAppointments = CreateSidebarButton("My Appointments", IconChar.CalendarCheck);
            btnQueue = CreateSidebarButton("My Queue", IconChar.Users);
            btnHistory = CreateSidebarButton("Patient History", IconChar.History);
            btnConsultation = CreateSidebarButton("Consultation", IconChar.Stethoscope);
            btnPrescriptions = CreateSidebarButton("Prescriptions", IconChar.Prescription);

            // Logout at bottom
            btnLogout = CreateSidebarButton("Logout", IconChar.SignOutAlt, Color.IndianRed);
            btnLogout.Dock = DockStyle.Bottom;
            
            Panel pnlLogoutSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(55, 65, 81), Margin = new Padding(0, 20, 0, 0) };
            
            // Add controls in correct order: Bottom items first, then top items
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(pnlLogoutSep);
            
            // Add menu buttons

            panelSidebar.Controls.Add(btnPrescriptions);
            panelSidebar.Controls.Add(btnConsultation);
            panelSidebar.Controls.Add(btnHistory);
            panelSidebar.Controls.Add(btnQueue);
            panelSidebar.Controls.Add(btnAppointments);
            panelSidebar.Controls.Add(btnDashboard);
            
            // Add branding LAST so it appears at TOP
            panelSidebar.Controls.Add(panelLogo);

            this.Controls.Add(panelSidebar);
        }

        private IconButton CreateSidebarButton(string text, IconChar icon, Color? iconColor = null)
        {
            IconButton btn = new IconButton { 
                Text = " " + text, 
                IconChar = icon, 
                IconSize = 24, 
                IconColor = iconColor ?? Color.Gainsboro, 
                ForeColor = Color.Gainsboro, 
                TextAlign = ContentAlignment.MiddleLeft, 
                ImageAlign = ContentAlignment.MiddleLeft, 
                Dock = DockStyle.Top, 
                Height = 45, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(31, 41, 55), 
                Font = new Font("Segoe UI", 10), 
                IconFont = IconFont.Auto, 
                TextImageRelation = TextImageRelation.ImageBeforeText 
            };
            btn.FlatAppearance.BorderSize = 0; 
            return btn;
        }

        private void ApplyFixedDesignLayout()
        {
            try
            {
                if (panelMain == null || panelMiddleContent == null) return;

                int availableWidth = panelMain.Width - panelMain.Padding.Left - panelMain.Padding.Right;

                // Toolbar Items
                if (pnlProfileComp != null && panelTopToolbar != null)
                {
                    pnlProfileComp.Height = panelTopToolbar.Height;
                    pnlProfileComp.Left = availableWidth - pnlProfileComp.Width - 30;
                    pnlProfileComp.Top = 0;
                }

                Panel searchPanel = panelTopToolbar?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlSearch");
                if (searchPanel != null)
                {
                    searchPanel.Location = new Point(30, (panelTopToolbar.Height - searchPanel.Height) / 2);
                }

                if (pnlDateShift != null)
                {
                    int centerGroupWidth = pnlDateShift.Width + (pnlStatusToggle?.Width ?? 0) + 40;
                    int startX = (availableWidth - centerGroupWidth) / 2;
                    pnlDateShift.Location = new Point(startX, (panelTopToolbar.Height - pnlDateShift.Height) / 2);
                    
                    if (pnlStatusToggle != null)
                        pnlStatusToggle.Location = new Point(startX + pnlDateShift.Width + 40, (panelTopToolbar.Height - pnlStatusToggle.Height) / 2);
                }

                // KPI Cards
                int horizontalMargin = 0;
                int cardGap = 20;
                int totalCardsWidth = availableWidth - (horizontalMargin * 2);
                int cardWidth = (totalCardsWidth - (cardGap * 3)) / 4;
                int cardHeight = 165;
                int cardTop = 20;

                cardWaiting?.SetBounds(0, cardTop, cardWidth, cardHeight);
                cardTodaySeen?.SetBounds(cardWidth + cardGap, cardTop, cardWidth, cardHeight);
                cardUpcoming?.SetBounds((cardWidth + cardGap) * 2, cardTop, cardWidth, cardHeight);
                cardCurrentStatus?.SetBounds((cardWidth + cardGap) * 3, cardTop, cardWidth, cardHeight);

                // Middle Content Columns
                int columnGap = 25;
                int contentWidth = availableWidth - (horizontalMargin * 2);
                int leftWidth = (int)(contentWidth * 0.28);
                int centerWidth = (int)(contentWidth * 0.44);
                int rightWidth = contentWidth - leftWidth - centerWidth - (columnGap * 2);
                int columnHeight = panelMiddleContent.Height;

                panelLeftColumn?.SetBounds(horizontalMargin, 0, leftWidth, columnHeight);
                panelCenterColumn?.SetBounds(horizontalMargin + leftWidth + columnGap, 0, centerWidth, columnHeight);
                panelRightColumn?.SetBounds(horizontalMargin + leftWidth + centerWidth + (columnGap * 2), 0, rightWidth, columnHeight);
            }
            catch {}
        }

        // ================= HELPERS =================
        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddLine(x + radius, y, x + width - radius, y);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddLine(x + width, y + radius, x + width, y + height - radius);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddLine(x + width - radius, y + height, x + radius, y + height);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            return path;
        }



        private Panel CreateDateShiftIndicator()
        {
            Panel p = new Panel { Size = new Size(320, 36), BackColor = Color.Transparent, Name = "pnlDateShift" };
            string shift = DateTime.Now.Hour >= 16 ? "Evening Shift" : "Morning Shift";
            lblDateShift = new Label { 
                Text = $"📅 {DateTime.Now:dddd, MMM dd} | {shift}", 
                Font = new Font("Segoe UI Semibold", 9.5F), 
                ForeColor = textPrimary, 
                AutoSize = true 
            };
            p.Controls.Add(lblDateShift);
            p.Resize += (s, e) => lblDateShift.Location = new Point(0, (p.Height - lblDateShift.PreferredHeight) / 2);
            return p;
        }

        private Panel CreateStatusTogglePanel()
        {
            Panel p = new Panel { Name = "pnlStatusShift", Size = new Size(220, 36), BackColor = Color.Transparent };
            btnOnDuty = new IconButton { 
                Text = " On Duty", 
                IconChar = IconChar.Circle, 
                IconSize = 8, 
                IconColor = Color.White, 
                BackColor = accentGreen, 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat, 
                Size = new Size(100, 32), 
                Location = new Point(0, 2), 
                Font = new Font("Segoe UI Bold", 8F), 
                Cursor = Cursors.Hand, 
                TextImageRelation = TextImageRelation.ImageBeforeText, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            btnOnDuty.FlatAppearance.BorderSize = 0;

            btnOnBreak = new IconButton { 
                Text = " On Break", 
                IconChar = IconChar.Circle, 
                IconSize = 8, 
                IconColor = accentOrange, 
                BackColor = Color.Transparent, 
                ForeColor = accentOrange, 
                FlatStyle = FlatStyle.Flat, 
                Size = new Size(100, 32), 
                Location = new Point(110, 2), 
                Font = new Font("Segoe UI Bold", 8F), 
                Cursor = Cursors.Hand, 
                TextImageRelation = TextImageRelation.ImageBeforeText, 
                TextAlign = ContentAlignment.MiddleCenter 
            };
            btnOnBreak.FlatAppearance.BorderColor = accentOrange;
            btnOnBreak.FlatAppearance.BorderSize = 1;

            p.Controls.AddRange(new Control[] { btnOnDuty, btnOnBreak });
            return p;
        }

        private Panel CreateGlobalSearch()
        {
            Panel p = new Panel { Name = "pnlSearch", Size = new Size(300, 36), BackColor = Color.FromArgb(249, 250, 251) };
            p.Paint += (s, e) => {
                try {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 8)) {
                        using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                    }
                } catch {}
            };
            
            Label lbl = new Label { Text = "🔍   Search records...", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(15, 8) };
            p.Controls.Add(lbl);
            return p;
        }

        private Panel CreateProfileComp()
        {
            Panel p = new Panel
            {
                Size = new Size(300, 44),
                BackColor = Color.Transparent
            };

            // Avatar - moved to far right
            pbHeaderProfile = new PictureBox
            {
                Size = new Size(36, 36),
                Location = new Point(255, 8),
                BackColor = primaryBlue,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Cursor = Cursors.Hand
            };

            pbHeaderProfile.Paint += (s, e) =>
            {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, pbHeaderProfile.Width - 1, pbHeaderProfile.Height - 1);
                    // Keeping region for circular image clipping
                    pbHeaderProfile.Region = new Region(path);
                }
            };

            pbHeaderProfile.Click += (s, e) => ShowProfileDropdown(pbHeaderProfile);

            // Name
            lblHeaderName = new Label
            {
                Text = "Doctor",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = textPrimary,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Role
            lblHeaderRole = new Label
            {
                Text = "Medical Specialist",
                Font = new Font("Segoe UI", 9),
                ForeColor = textSecondary,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Position text relative to avatar
            int textRight = pbHeaderProfile.Left - 10;

            lblHeaderName.Location = new Point(
                textRight - lblHeaderName.PreferredWidth,
                10
            );

            lblHeaderRole.Location = new Point(
                textRight - lblHeaderRole.PreferredWidth,
                30
            );

            p.Controls.AddRange(new Control[] { pbHeaderProfile, lblHeaderName, lblHeaderRole });
            return p;
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
                if (e.Graphics == null) return;
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

        private Panel CreateOperationalCard(string title, string value, string subtitle, Color themeColor)
        {
            Panel card = new Panel
            {
                Size = new Size(330, 150),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 14))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(borderGray, 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };

            // ICON
            Label iconLabel = new Label
            {
                Name = "iconLabel",
                Text = GetIconForCard(title),
                Font = new Font("Segoe UI Emoji", 18),
                ForeColor = themeColor,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // TEXT
            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = textSecondary,
                Location = new Point(22, 22),
                AutoSize = true
            };

            Label lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = textPrimary,
                Location = new Point(20, 50),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10F),
                ForeColor = textSecondary,
                Location = new Point(22, 112),
                AutoSize = true
            };

            card.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblVal,
                lblSubtitle,
                iconLabel
            });

            if (title == "Current Status")
            {
                // Status Actions removed for cleaner UI - toggles available in top toolbar
            }

            // Precise vertical balance and icon Top-Right
            card.Resize += (s, e) =>
            {
                try {
                    iconLabel.Location = new Point(card.Width - iconLabel.Width - 15, 15);
                    lblTitle.Location = new Point(22, 22);
                    lblVal.Location = new Point(20, 55);
                    lblSubtitle.Location = new Point(22, card.Height - lblSubtitle.Height - 18);
                } catch { }
            };

            return card;
        }

        private string GetIconForCard(string title)
        {
            if (title.Contains("Waiting")) return "⏱️";
            if (title.Contains("Seen") || title.Contains("Completed")) return "✅";
            if (title.Contains("Upcoming") || title.Contains("Appointments")) return "📅";
            if (title.Contains("Status")) return "📊";
            return "📊";
        }

        private Panel CreateContentCard(string title, int defaultWidth)
        {
            Panel card = new Panel { BackColor = Color.Transparent, Width = defaultWidth };
            card.Paint += (s, e) => { 
                try {
                    if (e.Graphics == null) return;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) { 
                        e.Graphics.FillPath(Brushes.White, path);
                        using (Pen p = new Pen(borderGray)) e.Graphics.DrawPath(p, path); 
                    } 
                } catch { }
            };

            Label lbl = new Label { Text = title, Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 18), AutoSize = true };
            card.Controls.Add(lbl);
            return card;
        }

        private void PopulatePatientQueue(Panel p)
        {
            flowPatientQueue = new FlowLayoutPanel
            {
                Name = "flowPatientQueue",
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20),
                AutoScroll = true,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            p.Resize += (s, e) => flowPatientQueue.Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20);
            p.Controls.Add(flowPatientQueue);
        }

        private Panel CreatePatientQueueCard(string token, string name, string reason, string waitTime, string status, string payStatus)
        {
            Panel card = new Panel { Height = 135, BackColor = Color.FromArgb(252, 252, 253), Margin = new Padding(0, 0, 0, 20) };
            card.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 10)) {
                    using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                }
                Color stripColor = status == "Waiting" ? accentOrange : (status == "With Doctor" ? accentPurple : accentGreen);
                using (SolidBrush b = new SolidBrush(stripColor)) e.Graphics.FillRectangle(b, 0, 15, 4, card.Height - 30);
            };

            Label lblToken = new Label { Text = "#" + token, Font = new Font("Segoe UI Black", 14F), ForeColor = primaryBlue, Location = new Point(20, 10), AutoSize = true };
            Label lblName = new Label { Text = name, Font = new Font("Segoe UI Bold", 10.5F), ForeColor = textPrimary, Location = new Point(20, 48), AutoSize = true };
            Label lblInfo = new Label { Text = reason + " • " + waitTime, Font = new Font("Segoe UI", 9F), ForeColor = textSecondary, Location = new Point(20, 74), AutoSize = true };
            
            Label lblBadge = new Label { 
                Text = status.ToUpper(), 
                Font = new Font("Segoe UI Bold", 7F), 
                ForeColor = status == "Waiting" ? accentOrange : (status == "With Doctor" ? accentPurple : accentGreen),
                BackColor = Color.FromArgb(20, status == "Waiting" ? accentOrange : (status == "With Doctor" ? accentPurple : accentGreen)),
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(20, 98),
                AutoSize = true
            };

            // Payment Badge
            bool isPaid = payStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
            Color payColor = isPaid ? Color.FromArgb(22, 163, 74) : Color.FromArgb(225, 29, 72);
            Label lblPay = new Label {
                Text = payStatus.ToUpper(),
                Font = new Font("Segoe UI Bold", 7F),
                ForeColor = payColor,
                BackColor = Color.FromArgb(20, payColor),
                Padding = new Padding(6, 2, 6, 2),
                AutoSize = true
            };
            
            // Actions - horizontal align bottom right
            IconButton btnStart = new IconButton { IconChar = IconChar.Play, IconColor = accentPurple, IconSize = 16, Width = 26, Height = 26, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnStart.FlatAppearance.BorderSize = 0;
            
            IconButton btnDone = new IconButton { IconChar = IconChar.Check, IconColor = accentGreen, IconSize = 16, Width = 26, Height = 26, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDone.FlatAppearance.BorderSize = 0;
            
            IconButton btnCancel = new IconButton { IconChar = IconChar.Times, IconColor = accentRose, IconSize = 16, Width = 26, Height = 26, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;

            card.Controls.AddRange(new Control[] { lblToken, lblName, lblInfo, lblBadge, lblPay, btnStart, btnDone, btnCancel });
            
            card.Resize += (s, e) => {
                lblPay.Location = new Point(card.Width - lblPay.Width - 20, 15);
                btnCancel.Location = new Point(card.Width - 32, card.Height - 35);
                btnDone.Location = new Point(btnCancel.Left - 30, card.Height - 35);
                btnStart.Location = new Point(btnDone.Left - 30, card.Height - 35);
            };

            return card;
        }

        private void PopulateTodaySchedule(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel { 
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width-30, p.Height-CONTENT_TOP_OFFSET-20),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            p.Resize += (s,e) => flow.Size = new Size(p.Width-30, p.Height-CONTENT_TOP_OFFSET-20);
            p.Controls.Add(flow);
            
            string[,] appts = {
                {"09:00 AM", "John Doe", "Completed"},
                {"10:30 AM", "Jane Smith", "With Doctor"},
                {"11:45 AM", "Robert Wilson", "Checked-In"},
                {"01:00 PM", "Mary Johnson", "Scheduled"},
                {"02:30 PM", "David Miller", "Scheduled"}
            };

            for (int i = 0; i < appts.GetLength(0); i++)
            {
                Panel card = new Panel { Height = 72, BackColor = Color.Transparent, Margin = new Padding(0) };
                
                Label time = new Label { Text = appts[i,0], Font = new Font("Segoe UI Semibold", 9), ForeColor = primaryBlue, Location = new Point(15,22), AutoSize = true};
                Label patient = new Label { Text = appts[i,1], Font = new Font("Segoe UI Bold", 10.5F), ForeColor = textPrimary, Location = new Point(100,14), AutoSize = true};
                
                string status = appts[i,2];
                Color statusColor = status == "Completed" ? accentGreen : (status == "With Doctor" ? accentPurple : (status == "Checked-In" ? accentOrange : textSecondary));
                
                Label lblStatus = new Label { Text = status, Font = new Font("Segoe UI Semibold", 8), ForeColor = statusColor, Location = new Point(100,38), AutoSize = true};
                
                card.Controls.AddRange(new Control[]{time, patient, lblStatus});
                card.Width = flow.Width-10;
                flow.Controls.Add(card);

                if (i < appts.GetLength(0) - 1)
                {
                    Panel divider = new Panel { Height = 1, BackColor = Color.FromArgb(243, 244, 246), Width = flow.Width - 40, Margin = new Padding(20, 0, 0, 0) };
                    flow.Controls.Add(divider);
                }
            }

            if (appts.GetLength(0) == 0)
            {
                Label empty = new Label { Text = "No appointments scheduled", Font = new Font("Segoe UI", 9.5F), ForeColor = textSecondary, TextAlign = ContentAlignment.MiddleCenter, Height = 100, Width = flow.Width };
                flow.Controls.Add(empty);
            }
        }

        private void PopulatePatientHistory(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel { 
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width-30, p.Height-CONTENT_TOP_OFFSET-20),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            p.Resize += (s,e) => flow.Size = new Size(p.Width-30, p.Height-CONTENT_TOP_OFFSET-20);
            p.Controls.Add(flow);

            // Doctor Info Card at Top of Right Column
            Panel docCard = new Panel { Size = new Size(flow.Width - 10, 80), BackColor = Color.FromArgb(249, 250, 251), Margin = new Padding(0, 0, 0, 20) };
            docCard.Paint += (s, e) => {
                using(GraphicsPath path = CreateRoundedRectPath(0,0,docCard.Width-1,docCard.Height-1,8))
                    using(Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
            };
            
            PictureBox docPic = new PictureBox { Size = new Size(40, 40), Location = new Point(15, 20), BackColor = primaryBlue, BorderStyle = BorderStyle.FixedSingle };
            Label docName = new Label { Text = "Dr. Saleh Akhuram", Font = new Font("Segoe UI Bold", 10.5F), ForeColor = textPrimary, Location = new Point(65, 18), AutoSize = true };
            Label docSpec = new Label { Text = "Senior Cardiologist", Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(65, 40), AutoSize = true };
            docCard.Controls.AddRange(new Control[] { docPic, docName, docSpec });
            flow.Controls.Add(docCard);

            Label lblInfo = new Label { 
                Text = "Previous Visits", 
                Font = new Font("Segoe UI Bold", 9.5F), 
                ForeColor = textPrimary, 
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            flow.Controls.Add(lblInfo);

            string[,] history = {
                {"Checkup", "Oct 12, 2023", "Routine blood pressure normal. Recommended diet plan."},
                {"Viral Fever", "Aug 05, 2023", "Prescribed Paracetamol. Advised full bed rest."}
            };

            for (int i = 0; i < history.GetLength(0); i++)
            {
                Panel card = new Panel { Size = new Size(flow.Width - 10, 100), BackColor = Color.White, Margin = new Padding(0, 0, 0, 12) };
                card.Paint += (s, e) => {
                    using(GraphicsPath path = CreateRoundedRectPath(0,0,card.Width-1,card.Height-1,8))
                        using(Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                };
                
                Label lblType = new Label { Text = history[i, 0], Font = new Font("Segoe UI Bold", 9.5F), ForeColor = textPrimary, Location = new Point(15, 12), AutoSize = true };
                Label lblDate = new Label { Text = history[i, 1], Font = new Font("Segoe UI Semibold", 8), ForeColor = primaryBlue, Location = new Point(15, 34), AutoSize = true };
                Label lblNotes = new Label { Text = history[i, 2], Font = new Font("Segoe UI", 8.5F), ForeColor = textSecondary, Location = new Point(15, 55), Width = card.Width - 30, Height = 40, AutoSize = false };
                
                card.Controls.AddRange(new Control[] { lblType, lblDate, lblNotes });
                flow.Controls.Add(card);
            }
        }
    }
}
