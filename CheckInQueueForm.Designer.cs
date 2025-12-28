using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class CheckInQueueForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout Containers
        private Panel pnlHeader;
        private Panel pnlContent;
        private TableLayoutPanel tlpActionRow;
        private Panel pnlQueueControls;
        private FlowLayoutPanel flowQueue;

        // Search Card
        private Panel cardSearch;
        private Label lblSearchTitle;
        private Label lblSearchSub;
        private TextBox txtPatientSearch;
        private IconButton btnAddWalkIn;
        private IconButton btnRegisterNew;

        // Check-In Actions Card
        private Panel cardCheckIn;
        private Label lblCheckInTitle;
        private ComboBox cmbDoctor;
        private ComboBox cmbAppointment;
        private IconButton btnCompleteCheckIn;
        private Label lblStatusIndicator;

        // Queue Controls
        private ComboBox cmbFilterDoctor;
        private ComboBox cmbFilterStatus;
        private ComboBox cmbSort;
        private IconButton btnClearQueue;
        private IconButton btnResetFilters;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 900);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = Color.FromArgb(249, 250, 251);

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // --- 1. HEADER ---
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 85,
                Tag = "DASHBOARD_STYLE",
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 5)
            };
            pnlHeader.Paint += (s, e) => {
                if (e.Graphics == null) return;
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            Label lblTitle = new Label {
                Text = "Check-In & Queue Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(20, 10),
                AutoSize = true
            };
            Label lblSub = new Label {
                Text = "Register physical arrival and manage active patient flow",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(22, 55),
                AutoSize = true
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // --- 2. MAIN CONTENT ---
            pnlContent = new Panel {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                AutoScroll = false
            };

            // Action Row
            tlpActionRow = new TableLayoutPanel {
                Dock = DockStyle.Top,
                Height = 220, // Reduced height to prevent vertical waste
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 30)
            };
            tlpActionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpActionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // A) Search & Add Card
            cardSearch = CreateCard();
            cardSearch.Dock = DockStyle.Fill; // Fill the cell
            cardSearch.Padding = new Padding(25);
            lblSearchTitle = new Label { Text = "👤 Search & Add Patient", Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(25, 20), AutoSize = true };
            txtPatientSearch = new TextBox { Width = 300, Height = 40, Location = new Point(25, 65), Font = new Font("Segoe UI", 11), PlaceholderText = "Name / Phone / MRN" };
            IconButton btnSearch = new IconButton { IconChar = IconChar.Search, IconSize = 18, IconColor = Color.White, BackColor = Color.FromArgb(37, 99, 235), Width = 45, Height = 40, Location = new Point(330, 65), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnAddWalkIn = new IconButton { Text = "Add Walk-In", IconChar = IconChar.Walking, IconSize = 18, IconColor = Color.FromArgb(37, 99, 235), ForeColor = Color.FromArgb(37, 99, 235), Width = 150, Height = 42, Location = new Point(25, 120), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnAddWalkIn.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
            btnRegisterNew = new IconButton { Text = "New Patient", IconChar = IconChar.UserPlus, IconSize = 18, IconColor = Color.FromArgb(107, 114, 128), ForeColor = Color.FromArgb(107, 114, 128), Width = 150, Height = 42, Location = new Point(185, 120), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnRegisterNew.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            cardSearch.Controls.AddRange(new Control[] { lblSearchTitle, txtPatientSearch, btnSearch, btnAddWalkIn, btnRegisterNew });
            tlpActionRow.Controls.Add(cardSearch, 0, 0);

            // B) Check-In Actions Card
            cardCheckIn = CreateCard();
            cardCheckIn.Dock = DockStyle.Fill; // Fill the cell
            cardCheckIn.Padding = new Padding(25);
            lblCheckInTitle = new Label { Text = "✅ Check-In Actions", Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(31, 41, 55), Location = new Point(25, 20), AutoSize = true };
            Label l1 = new Label { Text = "Select Doctor", Font = new Font("Segoe UI Semibold", 9), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(25, 60), AutoSize = true };
            cmbDoctor = new ComboBox { Width = 200, Height = 40, Location = new Point(25, 82), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(243, 244, 246) };
            Label l2 = new Label { Text = "Appointment", Font = new Font("Segoe UI Semibold", 9), ForeColor = Color.FromArgb(107, 114, 128), Location = new Point(240, 60), AutoSize = true };
            cmbAppointment = new ComboBox { Width = 400, Height = 40, Location = new Point(240, 82), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(243, 244, 246) };
            btnCompleteCheckIn = new IconButton { Text = "Complete Check-In", IconChar = IconChar.CheckCircle, IconSize = 20, IconColor = Color.White, BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, Width = 300, Height = 45, Location = new Point(25, 145), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Bold", 10), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnCompleteCheckIn.FlatAppearance.BorderSize = 0;
            cardCheckIn.Controls.AddRange(new Control[] { lblCheckInTitle, l1, cmbDoctor, l2, cmbAppointment, btnCompleteCheckIn });
            tlpActionRow.Controls.Add(cardCheckIn, 1, 0);

            // --- 3. QUEUE SECTION ---
            pnlQueueControls = new Panel {
                Dock = DockStyle.Top,
                Height = 60,
                Margin = new Padding(0, 0, 0, 10)
            };

            Label l_filter = new Label { Text = "Filter Queue:", Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.FromArgb(55, 65, 81), Location = new Point(40, 15), AutoSize = true };
            
            cmbFilterDoctor = new ComboBox { Width = 180, Height = 32, Location = new Point(180, 12), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(243, 244, 246) };
            cmbFilterStatus = new ComboBox { Width = 150, Height = 32, Location = new Point(380, 12), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(243, 244, 246) };
            cmbSort = new ComboBox { Width = 150, Height = 32, Location = new Point(550, 12), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(243, 244, 246) };
            
            // Removed btnClearQueue to have only ONE clear button
            
            // Reset Filters Button (The single Clear button)
            btnResetFilters = new IconButton { Text = "Clear", IconChar = IconChar.TimesCircle, IconSize = 14, IconColor = Color.FromArgb(107, 114, 128), ForeColor = Color.FromArgb(55, 65, 81), Width = 80, Height = 32, Location = new Point(710, 12), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, BackColor = Color.White };
            btnResetFilters.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);

            pnlQueueControls.Controls.AddRange(new Control[] { l_filter, cmbFilterDoctor, cmbFilterStatus, cmbSort, btnResetFilters });

            flowQueue = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 20, 40),
                AutoScroll = true,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlContent.Controls.Add(flowQueue);
            pnlContent.Controls.Add(pnlQueueControls);
            pnlContent.Controls.Add(tlpActionRow);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);
        }

        private Panel CreateCard()
        {
            Panel p = new Panel { BackColor = Color.White, Margin = new Padding(10) };
            p.Paint += (s, e) => {
                if (e.Graphics == null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 12))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235), 1)) {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            return p;
        }

    }
}
