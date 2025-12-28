using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class DoctorsAppointmentsForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout Containers
        private Panel pnlSidebar;
        private Panel pnlMainContent;
        private Panel pnlCalendarContainer;
        private Panel pnlSidebarBottom;

        // Sidebar Controls
        private Label lblSidebarTitle;
        private Label lblSidebarSubtitle;
        private MonthCalendar monthCalendar;
        private Label lblFilterStatus;
        private ComboBox cmbFilterStatus;
        private IconButton btnResetFilters;

        // Main Content Controls
        private Panel pnlHeader;
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
            this.Text = "My Appointments";
            this.BackColor = Color.FromArgb(249, 250, 251);

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
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(20, 45),
                AutoSize = true
            };

            // Calendar Container (Styled Card)
            pnlCalendarContainer = new Panel {
                Size = new Size(270, 200),
                Location = new Point(20, 80),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            pnlCalendarContainer.Paint += (s, e) => {
                 if (e.Graphics == null) return;
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 Rectangle rect = new Rectangle(0, 0, pnlCalendarContainer.Width - 1, pnlCalendarContainer.Height - 1);
                 using (GraphicsPath path = CreateRoundedRectPath(0, 0, pnlCalendarContainer.Width - 1, pnlCalendarContainer.Height - 1, 12))
                 using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                     e.Graphics.DrawPath(pen, path);
                 }
            };

            monthCalendar = new MonthCalendar {
                MaxSelectionCount = 1,
                ShowTodayCircle = true,
                ShowToday = true,
                Margin = new Padding(0)
            };
            pnlCalendarContainer.Controls.Add(monthCalendar);

            // Filters
            lblFilterStatus = new Label {
                Text = "Filter Status",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(20, 360),
                AutoSize = true
            };
            cmbFilterStatus = new ComboBox {
                Width = 270,
                Location = new Point(20, 385),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };
            cmbFilterStatus.Items.AddRange(new object[] { "All Statuses", "Scheduled", "Checked-In", "With Doctor", "Completed", "Cancelled" });
            cmbFilterStatus.SelectedIndex = 0;

            btnResetFilters = new IconButton {
                Text = "Reset Filters",
                IconChar = IconChar.Undo,
                IconSize = 16,
                IconColor = Color.Gray,
                ForeColor = Color.Gray,
                Width = 270,
                Height = 35,
                Location = new Point(20, 440),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            btnResetFilters.FlatAppearance.BorderSize = 1;
            btnResetFilters.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);

            pnlSidebar.Controls.AddRange(new Control[] {
                lblSidebarTitle, lblSidebarSubtitle, pnlCalendarContainer, 
                lblFilterStatus, cmbFilterStatus, btnResetFilters
            });

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
                Text = "My Schedule & Appointments",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(30, 10),
                AutoSize = true
            };
            lblPageSubtitle = new Label {
                Text = "Browse your patients on a daily basis",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(32, 55),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblPageSubtitle);

            flowAppts = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(249, 250, 251)
            };

            pnlMainContent.Controls.Add(flowAppts);
            pnlMainContent.Controls.Add(pnlHeader);

            this.Controls.Add(pnlMainContent);
            this.Controls.Add(pnlSidebar);
        }

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath(); 
            int d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); 
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); 
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
