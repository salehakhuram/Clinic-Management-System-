using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class Billing : Form
    {
        // Modern Color Palette - Medical/Healthcare Theme (Copied from PatientsForm)
        private readonly Color primaryColor = Color.FromArgb(20, 60, 90);        // Deep Navy
        private readonly Color secondaryColor = Color.FromArgb(35, 90, 130);     // Ocean Blue
        private readonly Color accentColor = Color.FromArgb(0, 180, 170);        // Teal
        private readonly Color surfaceColor = Color.FromArgb(245, 248, 250);     // Light Gray Surface
        private readonly Color cardColor = Color.White;
        private readonly Color textPrimary = Color.FromArgb(30, 40, 55);
        private readonly Color textSecondary = Color.FromArgb(100, 115, 130);
        private readonly Color placeholderColor = Color.FromArgb(140, 150, 165);
        private readonly Color successColor = Color.FromArgb(16, 185, 129);
        private readonly Color warningColor = Color.FromArgb(245, 158, 11);
        private readonly Color dangerColor = Color.FromArgb(239, 68, 68);
        private readonly Color infoColor = Color.FromArgb(59, 130, 246);

        // Layout Constants (Copied from PatientsForm)
        private const int LABEL_WIDTH = 160; 
        private const int INPUT_LEFT = 175;
        private const int INPUT_WIDTH = 300;
        private const int FIELD_HEIGHT = 40;
        private const int FIELD_SPACING = 52;
        private const int CARD_PADDING = 20;
        private const int HEADER_HEIGHT = 120;
        private const int CONTENT_PADDING = 20;

        // Panels
        Panel pnlHeader, pnlContent;
        Panel pnlPatientInfo, pnlSummary, pnlActions, pnlGrid;

        // Controls
        Label lblTitle, lblSubtitle;
        TextBox txtSearch;
        
        // Billing Details (Left & Middle Cards)
        RoundedTextBox txtSubTotal, txtDiscount, txtTax, txtTotal;
        ComboBox cmbPaymentMethod, cmbPatientID, cmbPatientName, cmbDoctorName, cmbAppointmentCode;
        // Temporary additions if needed
        RoundedTextBox txtTotalAmount, txtPatientName, txtAge, txtPhone;
        Label lblAppointment;

        // Buttons
        Button btnAddMedicine, btnSaveBill, btnPrint, btnClear, btnRefresh, btnViewHistory;
        DataGridView dgvBillItems;

        // Printing Components
        private System.Drawing.Printing.PrintDocument printDocument1;
        private PrintPreviewDialog printPreviewDialog1;

        private void InitializeComponent()
        {
            // ================ FORM SETUP =================
            this.Text = "Billing Management";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.DoubleBuffered = true;
            this.Size = new Size(1600, 950);
            this.StartPosition = FormStartPosition.CenterScreen;

            printDocument1 = new System.Drawing.Printing.PrintDocument();
            printPreviewDialog1 = new PrintPreviewDialog { Document = printDocument1 };

            // ================ HEADER PANEL =================
            pnlHeader = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = cardColor,
                Padding = new Padding(20, 15, 20, 15)
            };
            pnlHeader.Paint += PnlHeader_Paint;

            lblTitle = new Label()
            {
                Text = "Billing Management",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            lblSubtitle = new Label()
            {
                Text = "Manage invoices, payments, and financial clinic records",
                Font = new Font("Segoe UI", 10),
                ForeColor = textSecondary,
                AutoSize = true,
                Location = new Point(22, 55)
            };

            Panel pnlSearchComp = CreateSearchBox();
            pnlHeader.Resize += (s, e) =>
            {
                pnlSearchComp.Location = new Point(pnlHeader.Width - pnlSearchComp.Width - 20, (85 - pnlSearchComp.Height) / 2);
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, pnlSearchComp });

            // ================ MAIN CONTENT PANEL =================
            pnlContent = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = surfaceColor,
                Padding = new Padding(20, 0, 20, 20),
                AutoScroll = true
            };
            pnlContent.Resize += (s, e) => LayoutContent();

            // ================ PATIENT INFO CARD =================
            pnlPatientInfo = CreateModernCard("👤  Patient Information");
            int yOffset = 58;

            cmbPatientID = AddModernComboBox(pnlPatientInfo, "Patient ID", yOffset, null);
            yOffset += FIELD_SPACING;

            cmbPatientName = AddModernComboBox(pnlPatientInfo, "Patient Name", yOffset, null);
            yOffset += FIELD_SPACING;

            cmbAppointmentCode = AddModernComboBox(pnlPatientInfo, "Appointment", yOffset, null);
            yOffset += FIELD_SPACING;

            cmbDoctorName = AddModernComboBox(pnlPatientInfo, "Physician", yOffset, null);
            yOffset += FIELD_SPACING;

            cmbPaymentMethod = AddModernComboBox(pnlPatientInfo, "Payment Type", yOffset, new string[] { "Cash", "Card", "Online" });
            yOffset += FIELD_SPACING;

            // ================ BILLING SUMMARY CARD =================
            pnlSummary = CreateModernCard("💰  Billing Summary");
            yOffset = 58;

            txtSubTotal = AddModernTextBox(pnlSummary, "Sub Total", yOffset, "0.00");
            txtSubTotal.ReadOnly = true;
            yOffset += FIELD_SPACING;

            txtDiscount = AddModernTextBox(pnlSummary, "Discount (%)", yOffset, "0");
            yOffset += FIELD_SPACING;

            txtTax = AddModernTextBox(pnlSummary, "Tax (%)", yOffset, "0");
            yOffset += FIELD_SPACING;

            txtTotal = AddModernTextBox(pnlSummary, "Net Total", yOffset, "0.00");
            txtTotal.ReadOnly = true;
            yOffset += FIELD_SPACING + 20;

            // ================ QUICK ACTIONS CARD =================
            pnlActions = CreateModernCard("⚡  Quick Actions");
            Panel pnlTips = CreateTipsPanel();
            pnlTips.Location = new Point(CARD_PADDING, 58);
            pnlActions.Controls.Add(pnlTips);
            
            int btnHeight = 48;
            btnAddMedicine = CreateModernButton("💊  Add Medicine", infoColor, 200, btnHeight);
            btnSaveBill = CreateModernButton("💾  Save & Finalize", successColor, 200, btnHeight);
            btnPrint = CreateModernButton("🖨️  Print Invoice", secondaryColor, 200, btnHeight);
            btnClear = CreateModernButton("🧹  Clear All", dangerColor, 200, btnHeight);
            btnRefresh = CreateModernButton("🔄  Refresh Data", primaryColor, 200, btnHeight);
            btnViewHistory = CreateModernButton("📜  View History", accentColor, 200, btnHeight);

            pnlActions.Controls.AddRange(new Control[] { btnAddMedicine, btnSaveBill, btnPrint, btnClear, btnRefresh, btnViewHistory });

            // ================ DATA GRID CARD =================
            pnlGrid = CreateModernCard("📋  Invoiced Items");
            dgvBillItems = CreateModernDataGridView();
            dgvBillItems.Location = new Point(10, 55);
            dgvBillItems.AutoGenerateColumns = true;
            pnlGrid.Controls.Add(dgvBillItems);

            // ================ ADD ALL CONTROLS =================
            pnlContent.Controls.AddRange(new Control[] { pnlPatientInfo, pnlSummary, pnlActions, pnlGrid });
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);

            this.Load += (s, e) => { LayoutContent(); };
        }

        // ================= LAYOUT METHODS =================
        private void LayoutContent() {
            if (pnlContent.Width < 100) return;
            int availableWidth = pnlContent.ClientSize.Width - (CONTENT_PADDING * 2);
            int availableHeight = pnlContent.ClientSize.Height - (CONTENT_PADDING * 2);
            int cardGap = 16;
            int topRowHeight = 480; // Reduced to fit form without vertical scrolling

            int sideWidth = (int)((availableWidth - cardGap * 2) * 0.30);
            int actionsWidth = (int)((availableWidth - cardGap * 2) * 0.40);
            sideWidth = Math.Max(sideWidth, 460);
            actionsWidth = Math.Max(actionsWidth, 500);

            pnlPatientInfo.SetBounds(CONTENT_PADDING, CONTENT_PADDING, sideWidth, topRowHeight);
            pnlSummary.SetBounds(CONTENT_PADDING + sideWidth + cardGap, CONTENT_PADDING, sideWidth, topRowHeight);
            pnlActions.SetBounds(CONTENT_PADDING + (sideWidth + cardGap) * 2, CONTENT_PADDING, actionsWidth, topRowHeight);

            // Update Tips size
            foreach (Control c in pnlActions.Controls) if (c is Panel p && p.Size.Height == 125) p.Width = pnlActions.Width - (CARD_PADDING * 2);

            LayoutActionButtons();

            int gridTop = CONTENT_PADDING + topRowHeight + cardGap + 20; // Added extra 20px spacing
            int gridHeight = availableHeight - topRowHeight - cardGap - 20; 
            if (gridHeight < 250) gridHeight = 250;

            pnlGrid.SetBounds(CONTENT_PADDING, gridTop, availableWidth, gridHeight);
            dgvBillItems.SetBounds(10, 55, pnlGrid.Width - 20, pnlGrid.Height - 70);

            UpdateCardSeparator(pnlPatientInfo); UpdateCardSeparator(pnlSummary); UpdateCardSeparator(pnlActions); UpdateCardSeparator(pnlGrid);
        }

        private void LayoutActionButtons() {
            int panelWidth = pnlActions.Width;
            int btnWidth = (panelWidth - CARD_PADDING * 2 - 12) / 2;
            int btnHeight = 48, btnStartY = 200, btnGapY = 56, btnGapX = 12;

            btnAddMedicine.SetBounds(CARD_PADDING, btnStartY, btnWidth, btnHeight);
            btnSaveBill.SetBounds(CARD_PADDING + btnWidth + btnGapX, btnStartY, btnWidth, btnHeight);
            btnPrint.SetBounds(CARD_PADDING, btnStartY + btnGapY, btnWidth, btnHeight);
            btnClear.SetBounds(CARD_PADDING + btnWidth + btnGapX, btnStartY + btnGapY, btnWidth, btnHeight);
            btnRefresh.SetBounds(CARD_PADDING, btnStartY + btnGapY * 2, btnWidth, btnHeight);
            btnViewHistory.SetBounds(CARD_PADDING + btnWidth + btnGapX, btnStartY + btnGapY * 2, btnWidth, btnHeight);
        }

        private void UpdateCardSeparator(Panel card) {
            foreach (Control c in card.Controls) if (c is Panel p && p.Height == 1 && p.BackColor == Color.FromArgb(228, 233, 240)) p.Width = card.Width - (CARD_PADDING * 2);
        }

        private Panel CreateModernCard(string title) {
            Panel card = new Panel { BackColor = cardColor };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 14)) {
                    card.Region = new Region(path);
                    using (Pen pen = new Pen(Color.FromArgb(220, 228, 235), 1)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold), ForeColor = textPrimary, Location = new Point(CARD_PADDING, 16), AutoSize = true };
            card.Controls.Add(lbl);
            Panel separator = new Panel { Location = new Point(CARD_PADDING, 46), Height = 1, BackColor = Color.FromArgb(228, 233, 240) };
            card.Controls.Add(separator);
            return card;
        }

        private Panel CreateSearchBox() {
            Panel searchPanel = new Panel { Size = new Size(400, 42), BackColor = Color.FromArgb(249, 250, 251) };
            searchPanel.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, searchPanel.Width - 1, searchPanel.Height - 1, 10)) {
                    searchPanel.Region = new Region(path);
                    using (Pen pen = new Pen(Color.FromArgb(229, 231, 235), 1)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label searchIcon = new Label { Text = "🔍", Font = new Font("Segoe UI Emoji", 10), Location = new Point(12, 12), AutoSize = true, ForeColor = Color.Gray, BackColor = Color.Transparent };
            txtSearch = new TextBox { Text = "Search billing history...", Location = new Point(52, 11), Size = new Size(330, 24), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(249, 250, 251), ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search billing history...") { txtSearch.Text = ""; txtSearch.ForeColor = textPrimary; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search billing history..."; txtSearch.ForeColor = Color.Gray; } };
            searchPanel.Controls.AddRange(new Control[] { searchIcon, txtSearch });
            return searchPanel;
        }

        private RoundedTextBox AddModernTextBox(Panel parent, string labelText, int yPos, string placeholder, int xShift = 0) {
            Label lbl = new Label { Text = labelText, Location = new Point(10 + xShift, yPos + 8), AutoSize = false, Width = 145, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), TextAlign = ContentAlignment.MiddleRight };
            RoundedTextBox txt = new RoundedTextBox { Location = new Point(160 + xShift, yPos), Size = new Size(parent.Width - 180 - xShift, FIELD_HEIGHT), PlaceholderText = placeholder, PlaceholderColor = Color.Gray, BorderColor = Color.FromArgb(210, 220, 230), BorderFocusColor = accentColor, BorderRadius = 8, BackColor = Color.White, Font = new Font("Segoe UI", 10F), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            parent.Controls.Add(lbl); parent.Controls.Add(txt); return txt;
        }

        private ComboBox AddModernComboBox(Panel parent, string label, int top, string[] items) {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), Location = new Point(10, top + 8), Size = new Size(145, 20), TextAlign = ContentAlignment.MiddleRight };
            ComboBox cmb = new ComboBox { Location = new Point(160, top), Size = new Size(parent.Width - 180, FIELD_HEIGHT), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10), FlatStyle = FlatStyle.Flat, BackColor = surfaceColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            if (items != null) cmb.Items.AddRange(items); parent.Controls.AddRange(new Control[] { lbl, cmb }); return cmb;
            AutoSize = true;
        }

        private Button CreateModernButton(string text, Color bgColor, int width, int height) {
            Button btn = new Button { Text = text, Size = new Size(width, height), BackColor = bgColor, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
            btn.FlatAppearance.BorderSize = 0; Color orig = bgColor;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(orig, 0.12f); btn.MouseLeave += (s, e) => btn.BackColor = orig;
            btn.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, btn.Width - 1, btn.Height - 1, 10)) btn.Region = new Region(path); };
            return btn;
        }

        private Panel CreateTipsPanel() {
            Panel p = new Panel { Size = new Size(550, 125), BackColor = Color.FromArgb(235, 245, 255) };
            p.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 12)) p.Region = new Region(path); };
            Label l1 = new Label { Text = "💡 Billing Info", Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = secondaryColor, Location = new Point(16, 14), AutoSize = true };
            Label l2 = new Label { Text = "• Use 'Add Item' to select medicines for the current invoice\n• Double-check discount percentage before saving the bill\n• Printed receipts include the clinic logo and contact info", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(16, 40), AutoSize = true };
            p.Controls.AddRange(new Control[] { l1, l2 }); return p;
        }

        private DataGridView CreateModernDataGridView() {
            DataGridView dgv = new DataGridView { BackgroundColor = cardColor, BorderStyle = BorderStyle.None, ReadOnly = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.CellSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Both, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(235, 240, 245), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = primaryColor; dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold); dgv.ColumnHeadersHeight = 46; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.DefaultCellStyle.BackColor = Color.White; dgv.DefaultCellStyle.ForeColor = textPrimary; dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 240, 255); dgv.DefaultCellStyle.SelectionForeColor = textPrimary; dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10F); dgv.DefaultCellStyle.Padding = new Padding(12, 5, 12, 5); dgv.RowTemplate.Height = 44; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            return dgv;
        }

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius) {
            GraphicsPath path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        private void PnlHeader_Paint(object sender, PaintEventArgs e) {
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, pnlHeader.Height - 4, pnlHeader.Width, 4), Color.FromArgb(15, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical)) e.Graphics.FillRectangle(b, 0, pnlHeader.Height - 4, pnlHeader.Width, 4);
        }
    }
}
