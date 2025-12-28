using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    public partial class PharmacistPrescriptionsForm : Form
    {
        private System.ComponentModel.IContainer components = null;

        // UI Components
        private Panel pnlContent, pnlHeader;
        private Label lblTitle, lblSubtitle;
        private Panel cardSearch, cardGrid;
        private TextBox txtSearch;
        private CheckBox chkShowHistory;
        private IconButton btnSearch, btnRefresh, btnDispense, btnExport, btnPrint;
        private NumericUpDown numQty;
        private Label lblQtyDispense;
        private DataGridView dgvPrescriptions;

        private Color primaryColor = Color.FromArgb(16, 185, 129); // Keep for legacy refs
        private Color primaryBlue = Color.FromArgb(59, 130, 246); // Unified Blue
        private Color accentGreen = Color.FromArgb(16, 185, 129);
        private Color secondaryColor = Color.FromArgb(31, 41, 55); // Slate
        private Color surfaceColor = Color.FromArgb(243, 246, 249);
        private Color textPrimary = Color.FromArgb(31, 41, 55);
        private Color textSecondary = Color.FromArgb(107, 114, 128);
        private Color borderGray = Color.FromArgb(229, 231, 235);

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 800);
            this.BackColor = surfaceColor;
            this.Text = "Prescription Dispensing";

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // --- HEADER ---
              pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            lblTitle = new Label
            {
                Text = "Prescription Dispensing",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            lblSubtitle = new Label
            {
                Text = "Process and dispense pending prescriptions",
                Font = new Font("Segoe UI", 11),
                ForeColor = textSecondary,
                AutoSize = true,
                Location = new Point(22, 55)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);
            
            // --- CONTENT ---
            pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30, 40, 30, 30) };

            // SEARCH CARD (Top)
            cardSearch = CreateCard("Search Prescription");
            cardSearch.Height = 100; // Reduced height
            cardSearch.Dock = DockStyle.Top;
            
            txtSearch = new TextBox { Width = 400, Location = new Point(30, 50), Font = new Font("Segoe UI", 12), PlaceholderText = "Patient Name, ID or Token..." };
            btnSearch = new IconButton { Text = "Search", IconChar = IconChar.Search, IconSize = 20, IconColor = Color.White, BackColor = primaryBlue, ForeColor = Color.White, Size = new Size(120, 36), Location = new Point(450, 48), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnSearch.FlatAppearance.BorderSize = 0;

            chkShowHistory = new CheckBox
            {
                Text = "Show History (Dispensed)",
                Location = new Point(590, 54),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = textPrimary,
                Cursor = Cursors.Hand
            };

            cardSearch.Controls.AddRange(new Control[] { txtSearch, btnSearch, chkShowHistory });

            // SECTION TITLE (Middle)
            Label lblSectionTitle = new Label
            {
                Text = "Pending Prescriptions",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = false,
                Height = 45, // Slightly taller for spacing
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.BottomLeft // Bottom aligned to sit just above grid
            };

            // GRID CARD (Fill)
            cardGrid = CreateCard(""); // No title inside card
            cardGrid.Dock = DockStyle.Fill;
            cardGrid.Padding = new Padding(20, 55, 20, 80); // Increased bottom padding for Dispense button

            // Buttons inside Grid Card
            btnRefresh = new IconButton { Text = "Refresh", IconChar = IconChar.Sync, IconSize = 18, IconColor = Color.White, BackColor = accentGreen, ForeColor = Color.White, Size = new Size(110, 36), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnRefresh.FlatAppearance.BorderSize = 0;

            btnExport = new IconButton { Text = "Export", IconChar = IconChar.FileExport, IconSize = 18, IconColor = Color.White, BackColor = Color.FromArgb(99, 102, 241), ForeColor = Color.White, Size = new Size(110, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnExport.FlatAppearance.BorderSize = 0;
            
            btnExport.FlatAppearance.BorderSize = 0;
            
            btnPrint = new IconButton { Text = "Print", IconChar = IconChar.Print, IconSize = 18, IconColor = Color.White, BackColor = Color.FromArgb(75, 85, 99), ForeColor = Color.White, Size = new Size(110, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText };
            btnPrint.FlatAppearance.BorderSize = 0;

            cardGrid.Controls.Add(btnRefresh);
            cardGrid.Controls.Add(btnExport);
            cardGrid.Controls.Add(btnPrint);

            dgvPrescriptions = new DataGridView {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 40 },
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvPrescriptions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            dgvPrescriptions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
            
            lblQtyDispense = new Label {
                Text = "Qty to Dispense:",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = textPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            numQty = new NumericUpDown {
                Font = new Font("Segoe UI", 12),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Minimum = 1,
                Maximum = 999
            };
            
            btnDispense = new IconButton { 
                Text = "Dispense Selected", 
                IconChar = IconChar.PrescriptionBottle, 
                IconSize = 22, 
                IconColor = Color.White, 
                BackColor = primaryBlue, 
                ForeColor = Color.White, 
                Size = new Size(200, 45), 
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, 
                Cursor = Cursors.Hand, 
                TextImageRelation = TextImageRelation.ImageBeforeText 
            };
            btnDispense.FlatAppearance.BorderSize = 0;
            
            cardGrid.Controls.Add(dgvPrescriptions);
            cardGrid.Controls.Add(lblQtyDispense);
            cardGrid.Controls.Add(numQty);
            cardGrid.Controls.Add(btnDispense);
            btnDispense.BringToFront();

            // COMPOSITION
            pnlContent.Controls.Add(cardGrid);        // 1. Fill (Grid)
            pnlContent.Controls.Add(lblSectionTitle); // 2. Top (Title above Grid)
            pnlContent.Controls.Add(cardSearch);      // 3. Top (Search above Title)
            
            // DOCKING PRIORITY (Send Top items to Back)
            lblSectionTitle.SendToBack();  // 1st (will be pushed down by subsequent SendToBack)
            cardSearch.SendToBack();       // 2nd (Becomes the 'most back' -> Topmost visual)
            
            this.Controls.Add(pnlContent);
            // Header added via Controls.Add(pnlHeader) earlier, need to ensure order
            pnlHeader.BringToFront(); // Ensure Header is above content if they overlap (they shouldn't with Dock)

            // Dynamic Positioning
            cardGrid.Resize += (s, e) => {
                if (btnRefresh == null || btnExport == null || btnPrint == null) return;
                int right = cardGrid.Width - 20;
                btnRefresh.Location = new Point(right - btnRefresh.Width, 10);
                btnExport.Location  = new Point(right - btnRefresh.Width - btnExport.Width - 10, 10);
                btnPrint.Location   = new Point(right - btnRefresh.Width - btnExport.Width - btnPrint.Width - 20, 10);
                
                btnDispense.Location = new Point(right - btnDispense.Width, cardGrid.Height - 65);
                numQty.Location = new Point(btnDispense.Left - numQty.Width - 10, btnDispense.Top + 3);
                lblQtyDispense.Location = new Point(numQty.Left - lblQtyDispense.Width - 5, numQty.Top + 6);
            };
            
            // Initial positioning
            if (btnRefresh != null && btnExport != null && btnPrint != null) {
                int r = cardGrid.Width - 20;
                btnRefresh.Location = new Point(r - btnRefresh.Width, 10);
                btnExport.Location  = new Point(r - btnRefresh.Width - btnExport.Width - 10, 10);
                btnPrint.Location   = new Point(r - btnRefresh.Width - btnExport.Width - btnPrint.Width - 20, 10);
                
                btnDispense.Location = new Point(r - btnDispense.Width, cardGrid.Height - 65);
                numQty.Location = new Point(btnDispense.Left - numQty.Width - 10, btnDispense.Top + 3);
                lblQtyDispense.Location = new Point(numQty.Left - lblQtyDispense.Width - 5, numQty.Top + 6);
            }
        }

        private Panel CreateCard(string title)
        {
            Panel card = new Panel { BackColor = Color.White };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                using (Pen pen = new Pen(borderGray)) {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            
            Label lblCardTitle = new Label { Text = title, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = textPrimary, Location = new Point(25, 15), AutoSize = true };
            card.Controls.Add(lblCardTitle);
            
            return card;
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
