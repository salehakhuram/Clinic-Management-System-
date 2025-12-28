using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PharmacistDashboard : Form
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
            accentPurple = Color.FromArgb(139, 92, 246);
            accentRose = Color.FromArgb(244, 63, 94);
            
            SidebarDefaultBackColor = Color.FromArgb(31, 41, 55);
            SidebarHoverBackColor = Color.FromArgb(55, 65, 81);
            SidebarActiveBackColor = Color.FromArgb(59, 130, 246); // Unified Modern Blue
        }

        // Sidebar Components
        private Panel panelSidebar, panelLogo;
        private IconButton btnDashboard, btnPrescriptions, btnInventory, btnHistory, btnMedicines, btnLogout;

        // Main Layout Panels
        private Panel panelMain, panelTopToolbar, pnlProfileComp;
        private Panel panelWelcome, panelCardsRow, panelMiddleContent;
        private Panel panelLeftColumn, panelCenterColumn, panelRightColumn;

        private Label lblWelcome, lblSubWelcome, lblPageTitle, lblDateShift;
        private Label lblHeaderName, lblHeaderRole;
        private PictureBox pbHeaderProfile;
        
        // Header Toggles
        private Panel pnlDateShift;
        
        // Stat Cards
        private Panel cardPending, cardStockAlert, cardDispensed, cardStatus;
        private IconButton btnOnDuty, btnOnBreak;

        private const int CONTENT_TOP_OFFSET = 80;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            InitializeTheme();

            // ================ FORM SETUP =================
            this.ClientSize = new Size(1600, 950);
            this.Name = "PharmacistDashboard";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Pharmacist Dashboard";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.DoubleBuffered = true;

            // ================ MAIN PANEL =================
            panelMain = new Panel() { Dock = DockStyle.Fill, BackColor = surfaceColor, Padding = new Padding(30, 0, 30, 20), AutoScroll = false };

            // ================ TOP TOOLBAR =================
            panelTopToolbar = new Panel() { Dock = DockStyle.Top, Height = 85, BackColor = Color.White, Padding = new Padding(20, 15, 20, 15) };
            
            pnlProfileComp = CreateProfileComp();
            Panel pnlSearchComp = CreateGlobalSearch();
            Panel pnlStatusToggleComp = CreateStatusTogglePanel();
            pnlDateShift = CreateDateShiftIndicator();
            
            lblPageTitle = new Label { 
                Text = "Dashboard", 
                Font = new Font("Segoe UI Semibold", 16), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(20, 25),
                Visible = false
            };

            panelTopToolbar.Controls.AddRange(new Control[] { pnlProfileComp, pnlDateShift, pnlStatusToggleComp, pnlSearchComp });

            // WELCOME SECTION
            panelWelcome = new Panel() { 
                Dock = DockStyle.Top, 
                Height = 130, 
                BackColor = Color.Transparent, 
                Padding = new Padding(30, 15, 30, 0) 
            };
            
            lblWelcome = new Label { 
                Text = "Welcome, Pharmacist!", 
                Font = new Font("Segoe UI", 22, FontStyle.Bold), 
                ForeColor = textPrimary, 
                AutoSize = true, 
                Location = new Point(30, 15) 
            };
            
            lblSubWelcome = new Label { 
                Text = "Manage prescriptions, medicine inventory, and dispensing history.", 
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
            
            cardPending = CreateOperationalCard("Pending Orders", "8", "Need Action", accentOrange);
            cardStockAlert = CreateOperationalCard("Low Stock Items", "12", "Alerts", accentRose);
            cardDispensed = CreateOperationalCard("Dispensed Today", "45", "Invoices", accentGreen);
            cardStatus = CreateOperationalCard("Current Status", "ON DUTY", "Activity", accentPurple);

            panelCardsRow.Controls.AddRange(new Control[] { cardPending, cardStockAlert, cardDispensed, cardStatus });

            // MIDDLE CONTENT
            panelMiddleContent = new Panel() { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(30, 0, 30, 20) };
            
            panelLeftColumn = CreateContentCard("Prescription Queue", 380);
            panelCenterColumn = CreateContentCard("Inventory Overview", 520);
            panelRightColumn = CreateContentCard("Recent Activity", 340);

            panelLeftColumn.AutoScroll = true;
            panelCenterColumn.AutoScroll = true; 
            panelRightColumn.AutoScroll = true;

            PopulatePrescriptionQueue(panelLeftColumn);
            PopulateInventoryOverview(panelCenterColumn);
            PopulateRecentActivity(panelRightColumn);

            panelMiddleContent.Controls.AddRange(new Control[] { panelRightColumn, panelCenterColumn, panelLeftColumn });

            // ================ ADD TO FORM =================
            InitializeSidebar();
            this.Controls.Add(panelSidebar);
            this.Controls.Add(panelMain);
            this.Controls.SetChildIndex(panelMain, 0);

            // ================ DOCKING ORDER =================
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
            
            panelLogo = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.FromArgb(17, 24, 39) };
            Label lblLogo = new Label { 
                Text = "💊 AL REHMAN PHARMA", 
                Font = new Font("Segoe UI", 12, FontStyle.Bold), 
                ForeColor = Color.White, 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 10, 0, 0)
            };
            panelLogo.Controls.Add(lblLogo);

            btnDashboard = CreateSidebarButton("Dashboard", IconChar.TachometerAlt);
            btnPrescriptions = CreateSidebarButton("Prescriptions", IconChar.FileInvoice);
            btnInventory = CreateSidebarButton("Inventory / Stock", IconChar.Boxes);
            btnMedicines = CreateSidebarButton("Medicines Management", IconChar.Pills);
            btnHistory = CreateSidebarButton("Sales History", IconChar.History);
            
            btnLogout = CreateSidebarButton("Logout", IconChar.SignOutAlt, Color.IndianRed);
            btnLogout.Dock = DockStyle.Bottom;
            
            Panel pnlLogoutSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(55, 65, 81), Margin = new Padding(0, 20, 0, 0) };
            
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(pnlLogoutSep);
            
            panelSidebar.Controls.Add(btnHistory);
            panelSidebar.Controls.Add(btnMedicines);
            panelSidebar.Controls.Add(btnInventory);
            panelSidebar.Controls.Add(btnPrescriptions);
            panelSidebar.Controls.Add(btnDashboard);
            panelSidebar.Controls.Add(panelLogo);
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

                if (pnlProfileComp != null && panelTopToolbar != null)
                {
                    pnlProfileComp.Height = panelTopToolbar.Height;
                    pnlProfileComp.Left = availableWidth - pnlProfileComp.Width - 30;
                    pnlProfileComp.Top = 0;
                }

                Panel searchPanel = panelTopToolbar?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlSearch");
                Panel statusPanel = panelTopToolbar?.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "pnlStatusShift");
                
                if (searchPanel != null)
                {
                    searchPanel.Location = new Point(30, (panelTopToolbar.Height - searchPanel.Height) / 2);
                }

                if (pnlDateShift != null)
                {
                    int centerGroupWidth = pnlDateShift.Width + (statusPanel?.Width ?? 0) + 40;
                    int startX = (availableWidth - centerGroupWidth) / 2;
                    pnlDateShift.Location = new Point(startX, (panelTopToolbar.Height - pnlDateShift.Height) / 2);
                    
                    if (statusPanel != null)
                        statusPanel.Location = new Point(startX + pnlDateShift.Width + 40, (panelTopToolbar.Height - statusPanel.Height) / 2);
                }

                int cardGap = 20;
                int totalCardsWidth = availableWidth;
                int cardWidth = (totalCardsWidth - (cardGap * 3)) / 4;
                int cardHeight = 165;
                int cardTop = 20;

                cardPending?.SetBounds(0, cardTop, cardWidth, cardHeight);
                cardStockAlert?.SetBounds(cardWidth + cardGap, cardTop, cardWidth, cardHeight);
                cardDispensed?.SetBounds((cardWidth + cardGap) * 2, cardTop, cardWidth, cardHeight);
                cardStatus?.SetBounds((cardWidth + cardGap) * 3, cardTop, cardWidth, cardHeight);

                int columnGap = 25;
                int leftWidth = (int)(availableWidth * 0.28);
                int centerWidth = (int)(availableWidth * 0.44);
                int rightWidth = availableWidth - leftWidth - centerWidth - (columnGap * 2);
                int columnHeight = panelMiddleContent.Height;

                panelLeftColumn?.SetBounds(0, 0, leftWidth, columnHeight);
                panelCenterColumn?.SetBounds(leftWidth + columnGap, 0, centerWidth, columnHeight);
                panelRightColumn?.SetBounds(leftWidth + centerWidth + (columnGap * 2), 0, rightWidth, columnHeight);
            }
            catch {}
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

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int r = radius * 2;
            path.AddArc(x, y, r, r, 180, 90);
            path.AddArc(x + width - r, y, r, r, 270, 90);
            path.AddArc(x + width - r, y + height - r, r, r, 0, 90);
            path.AddArc(x, y + height - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Panel CreateStatusTogglePanel()
        {
            Panel p = new Panel { Name = "pnlStatusShift", Size = new Size(230, 36), BackColor = Color.Transparent };
            btnOnDuty = new IconButton { Text = " On Duty", IconChar = IconChar.Circle, IconSize = 8, IconColor = Color.White, BackColor = accentGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(105, 34), Location = new Point(0, 1), Font = new Font("Segoe UI Bold", 8.25F), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleCenter };
            btnOnDuty.FlatAppearance.BorderSize = 0;
            btnOnBreak = new IconButton { Text = " On Break", IconChar = IconChar.Circle, IconSize = 8, IconColor = accentOrange, BackColor = Color.Transparent, ForeColor = accentOrange, FlatStyle = FlatStyle.Flat, Size = new Size(105, 34), Location = new Point(115, 1), Font = new Font("Segoe UI Bold", 8.25F), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleCenter };
            btnOnBreak.FlatAppearance.BorderColor = accentOrange;
            btnOnBreak.FlatAppearance.BorderSize = 1;
            p.Controls.AddRange(new Control[] { btnOnDuty, btnOnBreak });
            return p;
        }

        private Panel CreateGlobalSearch()
        {
            Panel p = new Panel { Name = "pnlSearch", Size = new Size(300, 36), BackColor = Color.FromArgb(249, 250, 251) };
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 8)) {
                    using (Pen pen = new Pen(borderGray)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label lbl = new Label { Text = "🔍   Search inventory...", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true, Location = new Point(15, 8) };
            p.Controls.Add(lbl);
            return p;
        }

        private Panel CreateProfileComp()
        {
            Panel p = new Panel { Size = new Size(240, 44), BackColor = Color.Transparent };
            pbHeaderProfile = new PictureBox { Size = new Size(36, 36), Location = new Point(190, 8), BackColor = primaryBlue, SizeMode = PictureBoxSizeMode.StretchImage, Cursor = Cursors.Hand };
            pbHeaderProfile.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = new GraphicsPath()) { path.AddEllipse(0, 0, pbHeaderProfile.Width - 1, pbHeaderProfile.Height - 1); /* Keeping region only for circular image clipping */ pbHeaderProfile.Region = new Region(path); } };
            pbHeaderProfile.Click += (s, e) => ShowProfileDropdown(pbHeaderProfile);

            lblHeaderName = new Label { Text = "Pharmacist", Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary, AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            lblHeaderRole = new Label { Text = "Lead Pharmacist", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, AutoSize = true, TextAlign = ContentAlignment.MiddleRight };
            p.Controls.AddRange(new Control[] { pbHeaderProfile, lblHeaderName, lblHeaderRole });
            p.Resize += (s, e) => {
                pbHeaderProfile.Location = new Point(p.Width - 40, 4);
                lblHeaderName.Location = new Point(pbHeaderProfile.Left - lblHeaderName.PreferredWidth - 10, 4);
                lblHeaderRole.Location = new Point(pbHeaderProfile.Left - lblHeaderRole.PreferredWidth - 10, 22);
            };
            return p;
        }

        private Panel CreateOperationalCard(string title, string value, string subtitle, Color themeColor)
        {
            Panel card = new Panel { Size = new Size(330, 150), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 14)) {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(borderGray, 1)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label iconLabel = new Label { Name = "lblIcon", Text = GetIconForCard(title), Font = new Font("Segoe UI Emoji", 18), ForeColor = themeColor, Size = new Size(44, 44), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White };
            Label lblTitle = new Label { Name = "lblTitle", Text = title, Font = new Font("Segoe UI Semibold", 10F), ForeColor = textSecondary, AutoSize = true, BackColor = Color.White };
            Label lblVal = new Label { Name = "lblVal", Text = value, Font = new Font("Segoe UI", 26, FontStyle.Bold), ForeColor = textPrimary, AutoSize = true, BackColor = Color.White };
            Label lblSubtitle = new Label { Name = "lblSubtitle", Text = subtitle, Font = new Font("Segoe UI", 10F), ForeColor = textSecondary, AutoSize = true, BackColor = Color.White };
            card.Controls.AddRange(new Control[] { lblTitle, lblVal, lblSubtitle, iconLabel });
            card.Resize += (s, e) => {
                iconLabel.Location = new Point(card.Width - 60, 15);
                lblTitle.Location = new Point(22, 22);
                lblVal.Location = new Point(20, 55);
                lblSubtitle.Location = new Point(22, card.Height - 40);
            };
            return card;
        }

        private string GetIconForCard(string title)
        {
            if (title.Contains("Pending")) return "⌛";
            if (title.Contains("Stock")) return "⚠️";
            if (title.Contains("Dispensed")) return "🧾";
            return "📦";
        }

        private Panel CreateContentCard(string title, int defaultWidth)
        {
            Panel card = new Panel { BackColor = Color.Transparent, Width = defaultWidth };
            card.Paint += (s, e) => { 
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; 
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 16)) { 
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen p = new Pen(borderGray)) e.Graphics.DrawPath(p, path); 
                } 
            };
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI Semibold", 13), ForeColor = textPrimary, Location = new Point(20, 18), AutoSize = true, BackColor = Color.White };
            card.Controls.Add(lbl);
            return card;
        }

        private void PopulatePrescriptionQueue(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            p.Resize += (s, e) => flow.Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20);
            p.Controls.Add(flow);

            Label placeholder = new Label { Text = "Loading pending prescriptions...", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, AutoSize = true, Location = new Point(10, 10) };
            flow.Controls.Add(placeholder);
        }

        private void PopulateInventoryOverview(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            p.Resize += (s, e) => flow.Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20);
            p.Controls.Add(flow);
        }

        private void PopulateRecentActivity(Panel p)
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Location = new Point(15, CONTENT_TOP_OFFSET),
                Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            p.Resize += (s, e) => flow.Size = new Size(p.Width - 30, p.Height - CONTENT_TOP_OFFSET - 20);
            p.Controls.Add(flow);
        }
        public void SetToolbarVisibility(bool visible)
{
    if (panelTopToolbar != null)
        panelTopToolbar.Visible = visible;
}

    }
}
