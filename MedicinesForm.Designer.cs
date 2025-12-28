using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class MedicinesForm : Form
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
        Panel pnlContent;
        Panel pnlBasicInfo, pnlJobInfo, pnlActions, pnlGrid;

        // Controls
        Label lblTitle, lblSubtitle;
        TextBox txtSearch;
        RoundedTextBox txtMedIntId, txtTradeName, txtGenericName, txtManufacturer, txtSource, txtUnitPrice, txtQuantity, txtSupplier, txtCreatedBy, txtUpdatedBy;
        ComboBox cmbCategory, cmbStatus;
        DateTimePicker dtpExpiry, dtpCreatedAt, dtpUpdatedAt;
        Button btnSave, btnNew, btnEdit, btnDelete, btnView, btnRefresh;
        DataGridView dgvMedicines;

        private void InitializeComponent()
        {
            // ================ FORM SETUP =================
            this.Text = "Medicines Management";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.DoubleBuffered = true;
            this.Size = new Size(1600, 950);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ================ HEADER PANEL =================
            Panel pnlHeader = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = cardColor,
                Padding = new Padding(20, 15, 20, 15)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, pnlHeader.Height - 4, pnlHeader.Width, 4), Color.FromArgb(15, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(b, 0, pnlHeader.Height - 4, pnlHeader.Width, 4);
            };

            Label lblHeaderTitle = new Label()
            {
                Text = "Medicines Management",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            Label lblHeaderSubtitle = new Label()
            {
                Text = "Manage medicine inventory, stock levels, and expiration alerts",
                Font = new Font("Segoe UI", 10),
                ForeColor = textSecondary,
                AutoSize = true,
                Location = new Point(22, 55)
            };

            Panel pnlHeaderSearch = CreateSearchBox();
            pnlHeader.Resize += (s, e) =>
            {
                pnlHeaderSearch.Location = new Point(pnlHeader.Width - pnlHeaderSearch.Width - 20, (85 - pnlHeaderSearch.Height) / 2);
            };
            pnlHeader.Controls.AddRange(new Control[] { lblHeaderTitle, lblHeaderSubtitle, pnlHeaderSearch });

            // ================ MAIN CONTENT PANEL =================
            pnlContent = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = surfaceColor,
                Padding = new Padding(20, 0, 20, 20),
                AutoScroll = true
            };
            pnlContent.Resize += (s, e) => LayoutContent();

            // ================ MEDICINE INFO CARD (Matches Basic Info) =================
            pnlBasicInfo = CreateModernCard("💊  Medicine Information");
            int yOffset = 58;

            txtMedIntId = AddModernTextBox(pnlBasicInfo, "Med Int ID", yOffset, "e.g M001");
            txtMedIntId.ReadOnly = true;
            yOffset += FIELD_SPACING;

            txtTradeName = AddModernTextBox(pnlBasicInfo, "Trade Name", yOffset, "e.g Panadol");
            yOffset += FIELD_SPACING;

            txtGenericName = AddModernTextBox(pnlBasicInfo, "Generic Name", yOffset, "e.g Paracetamol");
            yOffset += FIELD_SPACING;

            cmbCategory = AddModernComboBox(pnlBasicInfo, "Category", yOffset, new string[] { "Tablet", "Syrup", "Injection", "Capsule" });
            yOffset += FIELD_SPACING;

            dtpExpiry = AddModernDatePicker(pnlBasicInfo, "Expiry Date", yOffset);
            yOffset += FIELD_SPACING;

            txtManufacturer = AddModernTextBox(pnlBasicInfo, "Manufacturer", yOffset, "e.g GSK");
            yOffset += FIELD_SPACING;

            txtSource = AddModernTextBox(pnlBasicInfo, "Source", yOffset, "e.g Getz Pharma");
            yOffset += FIELD_SPACING;

            txtSupplier = AddModernTextBox(pnlBasicInfo, "Supplier", yOffset, "Enter supplier details...", true);
            yOffset += FIELD_SPACING + 40;

            // ================ STOCK DETAILS CARD (Matches Medical Info) =================
            pnlJobInfo = CreateModernCard("📦  Stock Details");
            yOffset = 58;

            txtUnitPrice = AddModernTextBox(pnlJobInfo, "Unit Price", yOffset, "e.g 50");
            yOffset += FIELD_SPACING;

            txtQuantity = AddModernTextBox(pnlJobInfo, "Quantity", yOffset, "e.g 100");
            yOffset += FIELD_SPACING;

            cmbStatus = AddModernComboBox(pnlJobInfo, "Status", yOffset, new string[] { "Available", "Out of Stock" });
            yOffset += FIELD_SPACING;

            txtCreatedBy = AddModernTextBox(pnlJobInfo, "Created By", yOffset, "Admin");
            txtCreatedBy.ReadOnly = true;
            yOffset += FIELD_SPACING;

            dtpCreatedAt = AddModernDatePicker(pnlJobInfo, "Created At", yOffset);
            dtpCreatedAt.Enabled = false;
            yOffset += FIELD_SPACING;

            txtUpdatedBy = AddModernTextBox(pnlJobInfo, "Updated By", yOffset, "Admin");
            txtUpdatedBy.ReadOnly = true;
            yOffset += FIELD_SPACING;

            dtpUpdatedAt = AddModernDatePicker(pnlJobInfo, "Updated At", yOffset);
            dtpUpdatedAt.Enabled = false;

            // ================ QUICK ACTIONS CARD =================
            pnlActions = CreateModernCard("⚡  Quick Actions");
            Panel pnlTips = CreateTipsPanel();
            pnlTips.Location = new Point(CARD_PADDING, 58);
            pnlActions.Controls.Add(pnlTips);

            int btnHeight = 48;
            btnSave = CreateModernButton("💾  Save Record", successColor, 200, btnHeight);
            btnNew = CreateModernButton("✨  New Record", infoColor, 200, btnHeight);
            btnEdit = CreateModernButton("✏️  Update Record", warningColor, 200, btnHeight);
            btnDelete = CreateModernButton("🗑️  Delete Record", dangerColor, 200, btnHeight);
            btnView = CreateModernButton("👁️  View Records", secondaryColor, 200, btnHeight);
            btnRefresh = CreateModernButton("🔄  Refresh Inventory", accentColor, 200, btnHeight);

            pnlActions.Controls.AddRange(new Control[] { btnSave, btnNew, btnEdit, btnDelete, btnView, btnRefresh });

            // ================ DATA GRID CARD =================
            pnlGrid = CreateModernCard("📋  Inventory Records");
            
            dgvMedicines = CreateModernDataGridView();
            dgvMedicines.Location = new Point(10, 55);
            dgvMedicines.AutoGenerateColumns = true;
            pnlGrid.Controls.Add(dgvMedicines);

            // ================ ADD ALL CONTROLS =================
            pnlContent.Controls.AddRange(new Control[] { pnlBasicInfo, pnlJobInfo, pnlActions, pnlGrid });
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
            int topRowHeight = 530; // Reduced from 600 to allow more grid space

            int basicMedicalWidth = (int)((availableWidth - cardGap * 2) * 0.30);
            int actionsWidth = (int)((availableWidth - cardGap * 2) * 0.40);
            basicMedicalWidth = Math.Max(basicMedicalWidth, 460);
            actionsWidth = Math.Max(actionsWidth, 500);

            pnlBasicInfo.SetBounds(CONTENT_PADDING, CONTENT_PADDING, basicMedicalWidth, topRowHeight);
            pnlJobInfo.SetBounds(CONTENT_PADDING + basicMedicalWidth + cardGap, CONTENT_PADDING, basicMedicalWidth, topRowHeight);
            pnlActions.SetBounds(CONTENT_PADDING + (basicMedicalWidth + cardGap) * 2, CONTENT_PADDING, actionsWidth, topRowHeight);

            LayoutActionButtons();

            int gridTop = CONTENT_PADDING + topRowHeight + cardGap;
            int gridHeight = availableHeight - topRowHeight - cardGap; 
            if (gridHeight < 200) gridHeight = 200;

            pnlGrid.SetBounds(CONTENT_PADDING, gridTop, availableWidth, gridHeight);
            dgvMedicines.SetBounds(10, 55, pnlGrid.Width - 20, pnlGrid.Height - 70);

            dgvMedicines.SetBounds(10, 55, pnlGrid.Width - 20, pnlGrid.Height - 70);

            foreach (Control c in pnlActions.Controls) if (c is Panel p && p.BackColor == Color.FromArgb(235, 245, 255)) p.Size = new Size(pnlActions.Width - (CARD_PADDING * 2), 125);
            UpdateCardSeparator(pnlBasicInfo); UpdateCardSeparator(pnlJobInfo); UpdateCardSeparator(pnlActions); UpdateCardSeparator(pnlGrid);
        }

        private void LayoutActionButtons() {
            int panelWidth = pnlActions.Width;
            int btnWidth = (panelWidth - CARD_PADDING * 2 - 12) / 2;
            int btnHeight = 48, btnStartY = 220, btnGapY = 56, btnGapX = 12;

            btnSave.SetBounds(CARD_PADDING, btnStartY, btnWidth, btnHeight);
            btnNew.SetBounds(CARD_PADDING + btnWidth + btnGapX, btnStartY, btnWidth, btnHeight);
            btnEdit.SetBounds(CARD_PADDING, btnStartY + btnGapY, btnWidth, btnHeight);
            btnDelete.SetBounds(CARD_PADDING + btnWidth + btnGapX, btnStartY + btnGapY, btnWidth, btnHeight);
            btnView.SetBounds(CARD_PADDING, btnStartY + btnGapY * 2, btnWidth * 2 + btnGapX, btnHeight);
            btnRefresh.SetBounds(CARD_PADDING, btnStartY + btnGapY * 3, btnWidth * 2 + btnGapX, btnHeight);
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
            txtSearch = new TextBox { Text = "Search medicines...", Location = new Point(52, 11), Size = new Size(330, 24), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(249, 250, 251), ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search medicines...") { txtSearch.Text = ""; txtSearch.ForeColor = textPrimary; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search medicines..."; txtSearch.ForeColor = Color.Gray; } };
            searchPanel.Controls.AddRange(new Control[] { searchIcon, txtSearch });
            return searchPanel;
        }

        private RoundedTextBox AddModernTextBox(Panel parent, string labelText, int yPos, string placeholder, bool isMultiline = false) {
            Label lbl = new Label { Text = labelText, Location = new Point(10, yPos + 8), AutoSize = false, Width = 145, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), TextAlign = ContentAlignment.MiddleRight };
            RoundedTextBox txt = new RoundedTextBox { Location = new Point(160, yPos), Size = new Size(parent.Width - 180, isMultiline ? 85 : FIELD_HEIGHT), PlaceholderText = placeholder, PlaceholderColor = Color.Gray, Multiline = isMultiline, BorderColor = Color.FromArgb(210, 220, 230), BorderFocusColor = accentColor, BorderRadius = 8, BackColor = Color.White, Font = new Font("Segoe UI", 10F), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            parent.Controls.Add(lbl); parent.Controls.Add(txt); return txt;
        }

        private ComboBox AddModernComboBox(Panel parent, string label, int top, string[] items) {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), Location = new Point(10, top + 8), Size = new Size(145, 20), TextAlign = ContentAlignment.MiddleRight };
            ComboBox cmb = new ComboBox { Location = new Point(160, top), Size = new Size(parent.Width - 180, FIELD_HEIGHT), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12), FlatStyle = FlatStyle.Flat, BackColor = surfaceColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            cmb.Items.AddRange(items); parent.Controls.AddRange(new Control[] { lbl, cmb }); return cmb;
        }

        private DateTimePicker AddModernDatePicker(Panel parent, string label, int top) {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), Location = new Point(10, top + 8), Size = new Size(145, 20), TextAlign = ContentAlignment.MiddleRight };
            DateTimePicker dtp = new DateTimePicker { Location = new Point(160, top), Size = new Size(parent.Width - 180, FIELD_HEIGHT), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 12), CalendarForeColor = textPrimary, CalendarMonthBackground = cardColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            parent.Controls.AddRange(new Control[] { lbl, dtp }); return dtp;
        }

        private Button CreateModernButton(string text, Color bgColor, int width, int height) {
            Button btn = new Button { Text = text, Size = new Size(width, height), BackColor = bgColor, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
            btn.FlatAppearance.BorderSize = 0; Color orig = bgColor;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(orig, 0.12f); btn.MouseLeave += (s, e) => btn.BackColor = orig;
            btn.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, btn.Width - 1, btn.Height - 1, 10)) btn.Region = new Region(path); };
            return btn;
        }

        private Panel CreateTipsPanel() {
            Panel p = new Panel { Size = new Size(460, 125), BackColor = Color.FromArgb(235, 245, 255) };
            p.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 12)) p.Region = new Region(path); };
            Label l1 = new Label { Text = "💡 Quick Tips", Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold), ForeColor = secondaryColor, Location = new Point(16, 14), AutoSize = true };
            Label l2 = new Label { Text = "• Select a medicine from the table to edit details\n• Monitor stock levels regularly for low items\n• All units and expiration dates must be correct", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(16, 40), AutoSize = true };
            p.Controls.AddRange(new Control[] { l1, l2 }); return p;
        }

        private DataGridView CreateModernDataGridView() {
            DataGridView dgv = new DataGridView { BackgroundColor = cardColor, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, ScrollBars = ScrollBars.Both, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(235, 240, 245), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
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
    }
}
