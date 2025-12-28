using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PharmacistInventoryForm : Form
    {
        private Panel pnlHeader, pnlContent;
        private Label lblTitle, lblSubtitle, lblSectionTitle;
        private Panel cardGrid;
        private DataGridView dgvInventory;
        private IconButton btnAddStock, btnExport, btnRefresh, btnPrint;

        private Color surfaceColor = Color.FromArgb(243, 246, 249);
        private Color textPrimary = Color.FromArgb(31, 41, 55);
        private Color textSecondary = Color.FromArgb(107, 114, 128);
        private Color borderGray = Color.FromArgb(229, 231, 235);

        private void InitializeComponent()
        {
            this.ClientSize = new Size(1300, 800);
            this.BackColor = surfaceColor;
            this.FormBorderStyle = FormBorderStyle.None;

            BuildHeader();
            BuildContent();
        }

        // ================= HEADER =================
        private void BuildHeader()
        {
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 10)
            };

            lblTitle = new Label
            {
                Text = "Inventory Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(30, 10)
            };

            lblSubtitle = new Label
            {
                Text = "Monitor and manage medicine stock levels",
                Font = new Font("Segoe UI", 11),
                ForeColor = textSecondary,
                AutoSize = true,
                 Location = new Point(30, 55)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);
        }

        // ================= CONTENT =================
        private void BuildContent()
        {
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 40, 30, 30) // Adjusted padding (increased top)
            };

            // Section title (Dock Top)
            lblSectionTitle = new Label
            {
                Text = "Current Stock",
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

            // Grid
            dgvInventory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 40 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                EnableHeadersVisualStyles = false,
                ReadOnly = true
            };

            dgvInventory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            dgvInventory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);

            cardGrid.Controls.Add(dgvInventory);

            // ================== LAYOUT COMPOSITION ==================
            // Fix docking order to match SalesHistoryForm:
            // 1. Add cardGrid (Fill)
            // 2. Add lblSectionTitle (Top)
            // 3. Send lblSectionTitle to BACK so it is processed first in Docking priority for Top.
            
            pnlContent.Controls.Add(cardGrid);
            pnlContent.Controls.Add(lblSectionTitle);
            
            lblSectionTitle.SendToBack();

            this.Controls.Add(pnlContent); // Fill content

            // Button positioning
            cardGrid.Resize += (s, e) =>
            {
                if (btnRefresh == null || btnExport == null || btnPrint == null) return;
                int right = cardGrid.Width - 20;
                btnRefresh.Location = new Point(right - btnRefresh.Width, 10);
                btnExport.Location  = new Point(right - btnRefresh.Width - btnExport.Width - 10, 10);
                btnPrint.Location   = new Point(right - btnRefresh.Width - btnExport.Width - btnPrint.Width - 20, 10);
            };
        }

        // ================= HELPERS =================
        private IconButton CreateButton(string text, IconChar icon, Color bg)
        {
            return new IconButton
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
        }

        private Panel CreateCard()
        {
            Panel card = new Panel { BackColor = Color.White };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = RoundedRect(0, 0, card.Width - 1, card.Height - 1, 12))
                using (Pen pen = new Pen(borderGray))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            return card;
        }

        private GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
        {
            GraphicsPath path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
