using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class DoctorsQueueForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout Containers
        private Panel pnlSidebar;
        private Panel pnlMainContent;
        private Panel pnlHeader;
        private Panel pnlSidebarControls;

        // Sidebar Controls
        private Label lblSidebarTitle;
        private Label lblSidebarSubtitle;
        private Label lblFilterStatus;
        private ComboBox cmbFilterStatus;
        private Label lblSort;
        private ComboBox cmbSort;
        private IconButton btnResetFilters;

        // Main Content Controls
        private Label lblPageTitle;
        private Label lblPageSubtitle;
        private FlowLayoutPanel flowQueue;

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
            this.BackColor = Color.FromArgb(249, 250, 251);

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // 1. SIDEBAR PANEL
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 350;
            pnlSidebar.BackColor = Color.White;
            pnlSidebar.Padding = new Padding(25);
            pnlSidebar.Paint += (s, e) => {
                if (e.Graphics == null) return;
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };

            lblSidebarTitle = new Label {
                Text = "Queue Filters",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(40, 30),
                AutoSize = true
            };

            lblSidebarSubtitle = new Label {
                Text = "Manage active patient flow",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(40, 55),
                AutoSize = true
            };

            // Status Filter
            lblFilterStatus = new Label {
                Text = "Filter Status",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(40, 110),
                AutoSize = true
            };
            cmbFilterStatus = new ComboBox {
                Width = 270,
                Location = new Point(40, 135),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };
            cmbFilterStatus.Items.AddRange(new object[] { "All Statuses", "Waiting", "With Doctor", "Completed" });
            cmbFilterStatus.SelectedIndex = 0;

            // Sort
            lblSort = new Label {
                Text = "Sort By",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(40, 190),
                AutoSize = true
            };
            cmbSort = new ComboBox {
                Width = 270,
                Location = new Point(40, 215),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };
            cmbSort.Items.AddRange(new object[] { "Token Number", "Time (Oldest First)", "Time (Newest First)" });
            cmbSort.SelectedIndex = 0;

            btnResetFilters = new IconButton {
                Text = "Reset Filters",
                IconChar = IconChar.Undo,
                IconSize = 16,
                IconColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.FromArgb(107, 114, 128),
                Width = 270,
                Height = 35,
                Location = new Point(40, 280),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            btnResetFilters.FlatAppearance.BorderSize = 1;
            btnResetFilters.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);

            pnlSidebar.Controls.AddRange(new Control[] {
                lblSidebarTitle, lblSidebarSubtitle, 
                lblFilterStatus, cmbFilterStatus, 
                lblSort, cmbSort, btnResetFilters
            });

            // 2. MAIN CONTENT
            pnlMainContent = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 250, 251),
                Padding = new Padding(20)
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
                Text = "My Patient Queue",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(30, 10),
                AutoSize = true
            };
            lblPageSubtitle = new Label {
                Text = "Real-time list of patients waiting for you",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(32, 55),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblPageSubtitle);

            flowQueue = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(40),
                BackColor = Color.FromArgb(249, 250, 251),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlMainContent.Controls.Add(flowQueue);
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
