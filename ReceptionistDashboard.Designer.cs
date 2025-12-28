#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class ReceptionistDashboard : Form
    {
        private System.ComponentModel.IContainer components = null;

        // Modern Color Palette (Identical to Admin Dashboard)
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
            accentPurple = Color.FromArgb(139, 92, 246);
            accentRose = Color.FromArgb(244, 63, 94);
            
            SidebarDefaultBackColor = Color.FromArgb(31, 41, 55);
            SidebarHoverBackColor = Color.FromArgb(55, 65, 81);
            SidebarActiveBackColor = Color.FromArgb(59, 130, 246); // Unified Modern Blue
        }

        // Sidebar Components
        private Panel panelSidebar, panelLogo;
        private IconButton btnDashboard, btnPatients, btnAppointments, btnCheckIn, btnBilling, btnPrescriptions, btnLogout;

        // Main Layout Panels
        private Panel panelMain, panelTopToolbar, pnlProfileComp;
        private Panel panelWelcome, panelCardsRow, panelMiddleContent;
        private Panel panelLeftColumn, panelCenterColumn, panelRightColumn;

        private TextBox txtPatientSearch;
        private Label lblWelcome, lblSubWelcome, lblDateShift, lblPageTitle;
        private Label lblHeaderName, lblHeaderRole;
        private PictureBox pbHeaderProfile;

        // Stat Cards
        private Panel cardWaiting, cardTodayAppts, cardCheckedIn, cardPendingPayments;

        // Content Panels
        private Panel pnlAppointmentsView, pnlApptsHeader, pnlApptsList;
        private Label lblApptsTitle;
        private IconButton btnNewAppt;
        private Button btnQuickNewPatient, btnQuickNewAppt, btnQuickTodayAppts, btnQuickPayment;

        private IconButton previousActiveButton = null;

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
            this.Name = "ReceptionistDashboard";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Receptionist Dashboard";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.DoubleBuffered = true;

            // ================ MAIN PANEL =================
            // IMPORTANT: NO AutoScroll on main panel - only content panels scroll
            panelMain = new Panel() { Dock = DockStyle.Fill, BackColor = surfaceColor, Padding = new Padding(30, 0, 30, 20), AutoScroll = false };

            // ================ TOP TOOLBAR (Search & Profile) =================
            // Improved height and padding for better spacing
            panelTopToolbar = new Panel() { Dock = DockStyle.Top, Height = 70, BackColor = Color.White, Padding = new Padding(25, 12, 25, 12) };
            
            pnlProfileComp = CreateProfileComp();
            Panel pnlSearchComp = CreatePatientSearch();
            Panel pnlDateShiftComp = CreateDateShiftIndicator();
            
            lblPageTitle = new Label { 
                Text = "Dashboard", 
                Font = new Font("Segoe UI Semibold", 16), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(25, 20),
                Visible = false
            };

            panelTopToolbar.Controls.AddRange(new Control[] { pnlProfileComp, pnlDateShiftComp, pnlSearchComp });

            // WELCOME SECTION: Reverted to 110 height
            panelWelcome = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 130, 
                BackColor = Color.Transparent, 
                Padding = new Padding(30, 15, 30, 0) 
            };
            
            lblWelcome = new Label { 
                Text = "Welcome, Receptionist!", 
                Font = new Font("Segoe UI", 22, FontStyle.Bold), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(30, 15) 
            };
            
            lblSubWelcome = new Label { 
                Text = "Quick access to patient queue, appointments, and billing operations.", 
                Font = new Font("Segoe UI", 11), 
                ForeColor = textSecondary, 
                AutoSize = true, 
                Location = new Point(32, 70) 
            };
            
            panelWelcome.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome });


            // STAT CARDS ROW: Restored height
            panelCardsRow = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 240,                     // ⬆️ thori zyada height
                BackColor = Color.Transparent,
                Padding = new Padding(30, 30, 30, 10), // thora aur neeche
// ⬅️ TOP padding ↑
                Name = "pnlKpiCards" 
            };
 
            
            cardWaiting = CreateOperationalCard("Waiting Patients", "12", "In Queue", accentOrange);
            cardTodayAppts = CreateOperationalCard("Today's Appointments", "48", "Scheduled Today", primaryBlue);
            cardCheckedIn = CreateOperationalCard("Checked-In Patients", "32", "Ready for Doctor", accentGreen);
            cardPendingPayments = CreateOperationalCard("Pending Payments", "7", "Payment Due", accentRose);

            panelCardsRow.Controls.AddRange(new Control[] { cardWaiting, cardTodayAppts, cardCheckedIn, cardPendingPayments });

            // Reduced top gap to pull content up
            panelMiddleContent = new Panel() { Dock = DockStyle.Fill, BackColor = Color.Transparent,Padding = new Padding(30, 0, 30, 20) };
            
            panelLeftColumn = CreateContentCard("📋 Patient Queue - Live", 380);
            panelCenterColumn = CreateContentCard("⚡ Quick Actions Center", 520);
            panelRightColumn = CreateContentCard("👨‍⚕️ Doctors On-Duty", 380);

            // Enable AutoScroll on the container cards themselves
            panelLeftColumn.AutoScroll = true;
            panelCenterColumn.AutoScroll = false; // Layout handled by FlowPanel inside
            panelRightColumn.AutoScroll = true;

            // Populate columns with content
            PopulatePatientQueue(panelLeftColumn);
            PopulateQuickActions(panelCenterColumn);
            PopulateDoctorReadyAlerts(panelRightColumn);

            panelMiddleContent.Controls.AddRange(new Control[] { panelRightColumn, panelCenterColumn, panelLeftColumn });

            // ================ DASHBOARD OVERVIEW CONTAINER =================

            // ================ ADD TO FORM =================
            // 1. Sidebar
            InitializeSidebar();
            this.Controls.Add(panelSidebar);
            
            // 2. Main content area
            this.Controls.Add(panelMain);
            this.Controls.SetChildIndex(panelMain, 0);

            // ================ ADD COMPONENTS TO MAIN (FLAT HIERARCHY - DOCKING ORDER) =================
            // Rule: Add Top panels first (pushed to back), then Fill panel last (stays at front/index 0)
            // Rule: Add Top panels first (pushed to back), then Fill panel last (stays at front/index 0).
            // Docking processes from Back (Index N) to Front (Index 0).
            // So: Toolbar(Back) docked first -> Welcome -> Cards -> Content(Front) docked last.
            
            // CORRECT DOCKING ORDER: Add Fill panel first (Bottom), then Top panels last (Top)
            // This ensures Toolbar is docked at the very top, followed by Welcome, then Cards.
            panelMain.Controls.Add(panelMiddleContent);
            panelMain.Controls.Add(panelCardsRow);
            panelMain.Controls.Add(panelWelcome);
            panelMain.Controls.Add(panelTopToolbar);

            // Note: Since we added Content last, it is at Index 0 (Front). 
            // This ensures it fills ONLY the remaining space after Toolbar/Welcome/Cards take their Top slices.

            this.Load += (s, e) => {
                try {
                    ApplyFixedDesignLayout();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Layout Error (Load): {ex.Message}");
                }
            };
            
            this.Resize += (s, e) => {
                try {
                    ApplyFixedDesignLayout();
                } catch { }
            };

            this.ResumeLayout(false);
        }

     private const int CONTENT_TOP_OFFSET = 55;
     private const int CARD_TOP_OFFSET = 20;
private void ApplyFixedDesignLayout()
{
    try
    {
        if (panelMain == null || panelMiddleContent == null) return;

        int availableWidth =
            panelMain.Width - panelMain.Padding.Left - panelMain.Padding.Right;

        // ================= TOP TOOLBAR =================
        if (pnlProfileComp != null && panelTopToolbar != null)
        {
            pnlProfileComp.Height = panelTopToolbar.Height;
            pnlProfileComp.Left = availableWidth - pnlProfileComp.Width - 30;
            pnlProfileComp.Top = 0;
        }

        Panel searchPanel =
            panelTopToolbar.Controls.OfType<Panel>()
            .FirstOrDefault(p => p.Name == "pnlSearch");

        if (searchPanel != null)
        {
            searchPanel.Location = new Point(
                30,
                (panelTopToolbar.Height - searchPanel.Height) / 2
            );
        }

        Panel dateShiftPanel =
            panelTopToolbar.Controls.OfType<Panel>()
            .FirstOrDefault(p => p.Name == "pnlDateShift");

        if (dateShiftPanel != null)
        {
            dateShiftPanel.Location = new Point(
                (availableWidth - dateShiftPanel.Width) / 2,
                (panelTopToolbar.Height - dateShiftPanel.Height) / 2
            );
        }

        // ================= KPI CARDS =================
        int horizontalMargin = 0;
        int cardGap = 20;

        int totalCardsWidth = availableWidth - (horizontalMargin * 2);
        int cardWidth = (totalCardsWidth - (cardGap * 3)) / 4;
        int cardHeight = 165;
 // jitna neeche chahiye

   int cardTop = CARD_TOP_OFFSET;

cardWaiting?.SetBounds(0, cardTop, cardWidth, cardHeight);
cardTodayAppts?.SetBounds(cardWidth + cardGap, cardTop, cardWidth, cardHeight);
cardCheckedIn?.SetBounds((cardWidth + cardGap) * 2, cardTop, cardWidth, cardHeight);
cardPendingPayments?.SetBounds((cardWidth + cardGap) * 3, cardTop, cardWidth, cardHeight);


        // ================= MIDDLE CONTENT =================
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
    catch
    {
        // silent fail (layout safety)
    }
}

       private Panel CreatePatientSearch()
{
    Panel p = new Panel
    {
        Name = "pnlSearch",
        Size = new Size(440, 44),
        Location = new Point(25, 12),
        BackColor = Color.FromArgb(249, 250, 251)
    };

    // Rounded border
    p.Paint += (s, e) =>
    {
        try {
            if (e.Graphics == null || p.Width <= 0 || p.Height <= 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 10))
            {
                using (Pen pen = new Pen(borderGray))
                    e.Graphics.DrawPath(pen, path);
            }
        } catch { }
    };

    // 🔍 Icon
    Label lblIcon = new Label
    {
        Text = "🔍",
        Size = new Size(20, 20),
        Font = new Font("Segoe UI Emoji", 10),
        ForeColor = Color.Gray
    };

    // ✅ STEP 1: Explicit instantiation
    txtPatientSearch = new TextBox();
    
    // ✅ STEP 2: Safe property assignments
    txtPatientSearch.Name = "txtPatientSearch";
    txtPatientSearch.Text = "Search patient (Name / Phone / ID)...";
    txtPatientSearch.BorderStyle = BorderStyle.None;
    txtPatientSearch.BackColor = p.BackColor;
    txtPatientSearch.ForeColor = Color.Gray;
    txtPatientSearch.Font = new Font("Segoe UI", 10);
    txtPatientSearch.Width = Math.Max(50, p.Width - 90);


    txtPatientSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;

    // ✅ STEP 3: Safe positioning
    lblIcon.Location = new Point(14, (p.Height - lblIcon.Height) / 2);
    txtPatientSearch.Location = new Point(48, (p.Height - txtPatientSearch.Height) / 2);

    txtPatientSearch.GotFocus += (s, e) =>
    {
        if (txtPatientSearch != null && txtPatientSearch.Text == "Search patient (Name / Phone / ID)...")
        {
            txtPatientSearch.Text = "";
            txtPatientSearch.ForeColor = textPrimary;
        }
    };

    txtPatientSearch.LostFocus += (s, e) =>
    {
        if (txtPatientSearch != null && string.IsNullOrWhiteSpace(txtPatientSearch.Text))
        {
            txtPatientSearch.Text = "Search patient (Name / Phone / ID)...";
            txtPatientSearch.ForeColor = Color.Gray;
        }
    };

    p.Controls.Add(lblIcon);
    p.Controls.Add(txtPatientSearch);

    // Resize safety
    p.Resize += (s, e) =>
    {
        if (txtPatientSearch != null) {
            txtPatientSearch.Width = Math.Max(50, p.Width - 60);
            txtPatientSearch.Location = new Point(48, (p.Height - txtPatientSearch.Height) / 2);
        }
        if (lblIcon != null) {
            lblIcon.Location = new Point(16, (p.Height - lblIcon.Height) / 2);
        }
    };

    return p;
}


       private Panel CreateDateShiftIndicator()
{
    Panel p = new Panel
    {
        Size = new Size(440, 44),
        BackColor = Color.Transparent,
        Name = "pnlDateShift"
    };

    // Determine shift based on current time
    string shift = DateTime.Now.Hour >= 16 ? "Evening Shift" : "Morning Shift";
    
    lblDateShift = new Label
    {
        Text = $"📅 {DateTime.Now:dddd, MMMM dd, yyyy} | {shift}",
        Font = new Font("Segoe UI Semibold", 10),
        ForeColor = textPrimary,
        AutoSize = true
    };

    // Vertical center dynamically
    lblDateShift.Location = new Point(
        0,
        (p.Height - lblDateShift.PreferredHeight) / 2
    );

    // Re-center on resize (VERY important)
    p.Resize += (s, e) =>
    {
        lblDateShift.Location = new Point(
            0,
            (p.Height - lblDateShift.PreferredHeight) / 2
        );
    };

    p.Controls.Add(lblDateShift);
    return p;
}


       private Panel CreateProfileComp()
{
    Panel p = new Panel
    {
        Size = new Size(300, 44),
        BackColor = Color.Transparent
    };

    // Avatar
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
            // Clipping for circular image
            pbHeaderProfile.Region = new Region(path);
        }
    };

    pbHeaderProfile.Click += (s, e) => ShowProfileDropdown(pbHeaderProfile);

    // Name
    lblHeaderName = new Label
    {
        Text = "Receptionist",
        Font = new Font("Segoe UI Semibold", 10),
        ForeColor = textPrimary,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleRight
    };

    // Role
    lblHeaderRole = new Label
    {
        Text = "Front Desk",
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
            // Create dropdown panel - position it below the avatar
            // Adding to panelMain instead of topToolbar to avoid clipping
            Panel dropdown = new Panel
            {
                Size = new Size(180, 100),
                BackColor = Color.White,
                // Position relative to panelMain
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

            // My Profile button
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

            // Logout button
            Button btnLogout = new Button
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
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(254, 226, 226);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.White;
            btnLogout.Click += (s, e) =>
            {
                panelMain.Controls.Remove(dropdown);
                dropdown.Dispose();
                HandleLogout();
            };

            dropdown.Controls.AddRange(new Control[] { btnMyProfile, btnLogout });
            
            // Add to panelMain and bring to front so it's over the header and content
            panelMain.Controls.Add(dropdown);
            dropdown.BringToFront();

            // Auto-hide when clicking elsewhere
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

    // Hover effect
    card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(249, 250, 251);
    card.MouseLeave += (s, e) => card.BackColor = Color.White;

    return card;
}


        private string GetIconForCard(string title)
        {
            if (title.Contains("Waiting")) return "⏱️";
            if (title.Contains("Appointments")) return "📅";
            if (title.Contains("Checked-In")) return "✅";
            if (title.Contains("Payments")) return "💰";
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

            // Create menu buttons (will appear below branding)
            btnDashboard = CreateSidebarButton("Dashboard", IconChar.TachometerAlt);
            btnPatients = CreateSidebarButton("Patients", IconChar.UserInjured);
            btnAppointments = CreateSidebarButton("Appointments", IconChar.CalendarCheck);
            btnCheckIn = CreateSidebarButton("Check-In / Queue", IconChar.ClipboardCheck);
            btnBilling = CreateSidebarButton("Billing / Payments", IconChar.FileInvoiceDollar);
            btnPrescriptions = CreateSidebarButton("Prescriptions", IconChar.FilePrescription);
            
            // Logout at bottom
            btnLogout = CreateSidebarButton("Logout", IconChar.SignOutAlt, Color.IndianRed);
            btnLogout.Dock = DockStyle.Bottom;
            
            Panel pnlLogoutSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(55, 65, 81), Margin = new Padding(0, 20, 0, 0) };
            
            // Add controls in correct order: Bottom items first, then top items
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(pnlLogoutSep);
            
            // Add menu buttons (they will stack below the branding)

            panelSidebar.Controls.Add(btnBilling);
            panelSidebar.Controls.Add(btnCheckIn);
            panelSidebar.Controls.Add(btnAppointments);
            panelSidebar.Controls.Add(btnPatients);
            panelSidebar.Controls.Add(btnDashboard);
            
            // Add branding LAST so it appears at TOP (DockStyle.Top works from last to first when adding)
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
    }
}
