using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PharmacistSalesHistoryForm : Form
    {
        // ================== UI FIELDS ==================
        private Panel pnlHeader;
        private Panel pnlContent;
        private Panel cardGrid;

        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblSectionTitle;

        private DataGridView dgvSales;

        private IconButton btnPrint;
        private IconButton btnExport;
        private IconButton btnRefresh;

        // ================== COLORS ==================
        private Color surfaceColor = Color.FromArgb(243, 246, 249);
        private Color textPrimary = Color.FromArgb(31, 41, 55);
        private Color textSecondary = Color.FromArgb(107, 114, 128);
        private Color borderGray = Color.FromArgb(229, 231, 235);

        private void InitializeComponent()
        {
            this.ClientSize = new Size(1300, 800);
            this.BackColor = surfaceColor;
            this.FormBorderStyle = FormBorderStyle.None;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // ================== HEADER ==================
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 10)
            };

            lblTitle = new Label
            {
                Text = "Sales History",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(30, 10)
            };

            lblSubtitle = new Label
            {
                Text = "View and export prescription sales records",
                Font = new Font("Segoe UI", 11),
                ForeColor = textSecondary,
                AutoSize = true,
                 Location = new Point(30, 55)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // ================== CONTENT ==================
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 10, 30, 30) // Adjusted padding
            };

            // Section title (Dock Top)
            lblSectionTitle = new Label
            {
                Text = "Recent Sales Record",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = false,
                Height = 35,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ================== CARD ==================
            cardGrid = CreateCard();
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(20, 55, 20, 20);

            // ================== BUTTONS ==================
            btnPrint = CreateButton("Print", IconChar.Print, Color.FromArgb(75, 85, 99));
            btnExport = CreateButton("Export", IconChar.FileExport, Color.FromArgb(99, 102, 241));
            btnRefresh = CreateButton("Refresh", IconChar.Sync, Color.FromArgb(16, 185, 129));

            cardGrid.Controls.Add(btnPrint);
            cardGrid.Controls.Add(btnExport);
            cardGrid.Controls.Add(btnRefresh);

            // ================== GRID ==================
            dgvSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 40 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                EnableHeadersVisualStyles = false
            };

            dgvSales.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            dgvSales.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);

            cardGrid.Controls.Add(dgvSales);

            // ================== LAYOUT COMPOSITION ==================
            // Add cardGrid FIRST so it fills the remaining space
            // Add lblSectionTitle SECOND so it docks to TOP above the Fill content
            // (Note: In standard WinForms Controls.Add, the LAST added control is at index 0 (Top of Z-order). 
            // Docking priority is confusing, so BringToFront/SendToBack helps.)
            
            pnlContent.Controls.Add(cardGrid);
            pnlContent.Controls.Add(lblSectionTitle);

            // Establish correct docking priority
            lblSectionTitle.SendToBack(); // Send to back of Z-order -> First in Docking priority? 
            // Actually: Control.Dock works such that the LAST control added (or BringToFron'd?) takes priority?
            // "Controls docked to the top or bottom are processed in reverse z-order priority."
            // So if I want Label at Top, it should be at the BOTTOM of Z-order (SendToBack).
            
            this.Controls.Add(pnlContent); // Content fills rest of form
            pnlHeader.SendToBack(); // Header is Top docked, needs to be top priority (bottom Z)? 
            // Actually, we added pnlHeader to 'this.Controls' earlier. 
            // pnlContent is Fill.
            // Let's just ensure pnlHeader is visually at top. 
            // 'this.Controls.Add(pnlHeader)' then 'this.Controls.Add(pnlContent)'. 
            // pnlContent will be index 0. pnlHeader index 1.
            // Dock=Top (pnlHeader) works. Dock=Fill (pnlContent) fills rest.
            
            // ================== BUTTON POSITIONS ==================
            cardGrid.Resize += (s, e) =>
            {
                int right = cardGrid.Width - 20;
                btnRefresh.Location = new Point(right - btnRefresh.Width, 10);
                btnExport.Location  = new Point(right - btnRefresh.Width - btnExport.Width - 10, 10);
                btnPrint.Location   = new Point(right - btnRefresh.Width - btnExport.Width - btnPrint.Width - 20, 10);
            };
        }

        // ================== CARD DESIGN ==================
        private Panel CreateCard()
        {
            Panel card = new Panel
            {
                BackColor = Color.White
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12);
                using Pen pen = new Pen(borderGray);
                e.Graphics.FillPath(Brushes.White, path);
                e.Graphics.DrawPath(pen, path);
            };

            return card;
        }

        // ================== BUTTON FACTORY ==================
        private IconButton CreateButton(string text, IconChar icon, Color bg)
        {
            IconButton btn = new IconButton
            {
                Text = text,
                IconChar = icon,
                IconSize = 18,
                IconColor = Color.White,
                BackColor = bg,
                ForeColor = Color.White,
                Size = new Size(110, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ================== ROUNDED RECT ==================
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
