using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class DoctorsPatientHistoryForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout Containers
        private Panel pnlSidebar;
        private Panel pnlMainContent;
        private Panel pnlHeader;

        // Sidebar Controls
        private Label lblSidebarTitle;
        private Label lblSidebarSubtitle;
        private Label lblSearch;
        private TextBox txtSearch;
        private Label lblFilterStatus;
        private ComboBox cmbFilterStatus;
        private IconButton btnResetFilters;

        // Main Content Controls
        private Label lblPageTitle;
        private Label lblPageSubtitle;
        private FlowLayoutPanel flowHistory;

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
                Text = "History Search",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(40, 30),
                AutoSize = true
            };

            lblSidebarSubtitle = new Label {
                Text = "Lookup patient records",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(40, 55),
                AutoSize = true
            };

            // Search
            lblSearch = new Label {
                Text = "Patient Name / Phone",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(40, 110),
                AutoSize = true
            };
            txtSearch = new TextBox {
                Width = 270,
                Location = new Point(40, 135),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Type to search..."
            };

            // Status Filter
            lblFilterStatus = new Label {
                Text = "Patient Status",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(40, 190),
                AutoSize = true
            };
            cmbFilterStatus = new ComboBox {
                Width = 270,
                Location = new Point(40, 215),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.System
            };
            cmbFilterStatus.Items.AddRange(new object[] { "All Records", "Active", "Discharged", "Follow-up" });
            cmbFilterStatus.SelectedIndex = 0;

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
                lblSearch, txtSearch,
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
                Height = 110,
                BackColor = Color.White,
                Padding = new Padding(50, 30, 40, 10)
            };
            pnlHeader.Paint += (s, e) => {
                 if (e.Graphics == null) return;
                 using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                     e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblPageTitle = new Label {
                Text = "Patient Clinical History",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(50, 2),
                AutoSize = true
            };
            lblPageSubtitle = new Label {
                Text = "Archives of past consultations and treatments",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(53, 68),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblPageSubtitle);

            flowHistory = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(40),
                BackColor = Color.FromArgb(249, 250, 251),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            pnlMainContent.Controls.Add(flowHistory);
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
