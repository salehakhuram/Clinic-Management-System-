using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class PatientHistoryViewForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout
        private Panel pnlHeader;
        private Panel pnlFooter;
        private Panel pnlContent;
        private FlowLayoutPanel flowTimeline;

        // Header Controls
        private Label lblPatientName;
        private Label lblPatientDetails;
        private Label lblPatientID;
        private Label lblContactInfo;

        // Actions
        private IconButton btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 850);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(249, 250, 251);

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // 1. HEADER (Patient Info)
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.White,
                Padding = new Padding(40, 25, 40, 20)
            };
            pnlHeader.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblPatientID = new Label {
                Text = "PATIENT ID: P-1002",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(40, 25)
            };

            lblPatientName = new Label {
                Text = "Patient Full Name",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(38, 45)
            };

            lblPatientDetails = new Label {
                Text = "Male, 34 yrs",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true,
                Location = new Point(41, 90)
            };

            lblContactInfo = new Label {
                Text = "📞 0300-1234567 | ✉ contact@email.com",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                TextAlign = ContentAlignment.TopRight,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 5, 0, 0)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblPatientID, lblPatientName, lblPatientDetails, lblContactInfo });

            // 2. FOOTER
            pnlFooter = new Panel {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 15)
            };
            pnlFooter.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            btnClose = new IconButton {
                Text = "  Close History View",
                IconChar = IconChar.ArrowLeft,
                IconSize = 20,
                IconColor = Color.FromArgb(107, 114, 128),
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Color.FromArgb(55, 65, 81),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(220, 46),
                Font = new Font("Segoe UI Semibold", 10),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Dock = DockStyle.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;

            pnlFooter.Controls.Add(btnClose);

            // 3. CONTENT (Timeline)
            pnlContent = new Panel {
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 10, 40, 10),
                AutoScroll = true
            };

            flowTimeline = new FlowLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 20, 0, 40),
                BackColor = Color.Transparent
            };

            pnlContent.Controls.Add(flowTimeline);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
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
