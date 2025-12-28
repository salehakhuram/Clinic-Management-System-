using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class ReceptionistAppointmentsForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout Containers
        private Panel pnlSidebar;
        private Panel pnlMainContent;
        private Panel pnlHeader;
        private Panel pnlCalendarContainer; // Styled Card for Calendar
        private Panel pnlSidebarBottom; // For New Appt Button

        // Sidebar Controls
        private Label lblSidebarTitle;
        private Label lblSidebarSubtitle;
        private MonthCalendar monthCalendar;
        private Label lblFilterDoctor;
        private ComboBox cmbFilterDoctor;
        private Label lblFilterStatus;
        private ComboBox cmbFilterStatus;
        private IconButton btnResetFilters;
        private IconButton btnNewAppointment; // Retaining this feature
        private TextBox txtSearch; // Added Search


        // Main Content Controls
        private Label lblPageTitle;
        private Label lblPageSubtitle;
        private FlowLayoutPanel flowAppts;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = Color.FromArgb(249, 250, 251); // Light Gray Background

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // 1. SIDEBAR PANEL
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 320;
            pnlSidebar.BackColor = Color.White;
            pnlSidebar.Padding = new Padding(20);
            pnlSidebar.Paint += (s, e) => {
                if (e.Graphics == null) return;
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };

            // Sidebar Title
            lblSidebarTitle = new Label {
                Text = "Browse Schedule",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(20, 20),
                AutoSize = true
            };

            lblSidebarSubtitle = new Label {
                Text = "Select a date to view appointments",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(107, 114, 128), // Text-gray-500
                Location = new Point(20, 45),
                AutoSize = true
            };

            // Calendar Container (Styled Card)
            pnlCalendarContainer = new Panel {
                Size = new Size(270, 200), // Height adjusts to calendar
                Location = new Point(20, 80),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            pnlCalendarContainer.Paint += (s, e) => {
                 if (e.Graphics == null) return;
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 Rectangle rect = new Rectangle(0, 0, pnlCalendarContainer.Width - 1, pnlCalendarContainer.Height - 1);
                 using (GraphicsPath path = RoundedRect(rect, 12))
                 using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                     e.Graphics.DrawPath(pen, path);
                 }
            };

            // Native MonthCalendar
            monthCalendar = new MonthCalendar {
                MaxSelectionCount = 1,
                ShowTodayCircle = true,
                ShowToday = true,
                Margin = new Padding(0)
            };
            pnlCalendarContainer.Controls.Add(monthCalendar); // Add to styled container

            // Filters
            lblFilterDoctor = new Label {
                Text = "Filter by Doctor",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(20, 360),
                AutoSize = true
            };
            cmbFilterDoctor = new ComboBox {
                Width = 270,
                Location = new Point(20, 385),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };

            lblFilterStatus = new Label {
                Text = "Filter by Status",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(20, 430),
                AutoSize = true
            };
            cmbFilterStatus = new ComboBox {
                Width = 270,
                Location = new Point(20, 455),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };
            cmbFilterStatus.Items.AddRange(new object[] { "All Statuses", "Scheduled", "Checked-In", "With Doctor", "Completed", "Cancelled" });

            btnResetFilters = new IconButton {
                Text = "Reset Filters",
                IconChar = IconChar.Undo,
                IconSize = 16,
                IconColor = Color.Gray,
                ForeColor = Color.Gray,
                Width = 270,
                Height = 35,
                Location = new Point(20, 510),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            btnResetFilters.FlatAppearance.BorderSize = 1;
            btnResetFilters.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);

            // Bottom Panel for New Appointment
            pnlSidebarBottom = new Panel {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(20, 10, 20, 10),
                BackColor = Color.White
            };
            
            btnNewAppointment = new IconButton {
                Text = "New Appointment",
                IconChar = IconChar.Plus,
                IconSize = 16,
                IconColor = Color.White,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 99, 235), // Blue
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            btnNewAppointment.FlatAppearance.BorderSize = 0;
            pnlSidebarBottom.Controls.Add(btnNewAppointment);

            pnlSidebar.Controls.Add(lblSidebarTitle);
            pnlSidebar.Controls.Add(lblSidebarSubtitle);
            pnlSidebar.Controls.Add(pnlCalendarContainer);
            pnlSidebar.Controls.Add(lblFilterDoctor);
            pnlSidebar.Controls.Add(cmbFilterDoctor);
            pnlSidebar.Controls.Add(lblFilterStatus);
            pnlSidebar.Controls.Add(cmbFilterStatus);
            pnlSidebar.Controls.Add(btnResetFilters);
            pnlSidebar.Controls.Add(pnlSidebarBottom); // Docked Bottom

            // 2. MAIN CONTENT
            pnlMainContent = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 250, 251)
            };

            // Header
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 5)
            };
            pnlHeader.Paint += (s, e) => {
                 if (e.Graphics == null) return;
                 using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                     e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblPageTitle = new Label {
                Text = "Today's Appointments",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(20, 10),
                AutoSize = true
            };
            lblPageSubtitle = new Label {
                Text = "Showing appointments for Today",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(22, 55),
                AutoSize = true
            };
            
            // Search Box
            txtSearch = new TextBox { 
                PlaceholderText = "Search by Patient, Doctor or Code...", 
                Location = new Point(pnlHeader.Width - 350, 30), 
                Size = new Size(300, 30), 
                Font = new Font("Segoe UI", 11) 
            };

            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblPageSubtitle);
            pnlHeader.Controls.Add(txtSearch);
            pnlHeader.Resize += (s, e) => txtSearch.Left = pnlHeader.Width - 350;

            // Flow Panel
            flowAppts = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(40),
                BackColor = Color.FromArgb(249, 250, 251)
            };

            pnlMainContent.Controls.Add(flowAppts);
            pnlMainContent.Controls.Add(pnlHeader);

            // Add Panels to Form
            this.Controls.Add(pnlMainContent);
            this.Controls.Add(pnlSidebar);
        }
    }
}
