using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class AppointmentsForm : Form
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
        Panel pnlBasicInfo, pnlMedicalInfo, pnlActions, pnlGrid;

        // Controls
        Label lblTitle, lblSubtitle;
        TextBox txtSearch;
        RoundedTextBox txtAppointmentIntId, txtReason, txtCreatedBy, txtUpdatedBy;
        ComboBox cmbStatus, cmbPayment, cmbFollowUp;
        ComboBox cmbPatientID, cmbPatientName, cmbDoctorID, cmbDoctorName;
        DateTimePicker dtpDate, dtpTime, dtpCreatedAt, dtpUpdatedAt;
        Button btnSave, btnNew, btnEdit, btnDelete, btnView, btnRefresh;
        DataGridView dgvAppointments;

        private void InitializeComponent()
        {
            // ================ FORM SETUP =================
            this.Text = "Appointments Management";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.DoubleBuffered = true;
            this.Size = new Size(1600, 1050);
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
                Text = "Appointments Management",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            Label lblHeaderSubtitle = new Label()
            {
                Text = "Schedule patient visits, manage doctor availability, and follow-ups",
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

            // ================ APPOINTMENT INFO CARD (Matches Basic Info) =================
            pnlBasicInfo = CreateModernCard("📅  Appointment Details");
            int yOffset = 58;

            txtAppointmentIntId = AddModernTextBox(pnlBasicInfo, "Appointment Int ID", yOffset, "e.g A001");
            txtAppointmentIntId.ReadOnly = true;
            yOffset += FIELD_SPACING;

            cmbPatientID = AddModernComboBox(pnlBasicInfo, "Patient ID", yOffset, new string[] { });
            yOffset += FIELD_SPACING;

            cmbPatientName = AddModernComboBox(pnlBasicInfo, "Patient Name", yOffset, new string[] { });
            yOffset += FIELD_SPACING;

            cmbDoctorID = AddModernComboBox(pnlBasicInfo, "Doctor ID", yOffset, new string[] { });
            yOffset += FIELD_SPACING;

            cmbDoctorName = AddModernComboBox(pnlBasicInfo, "Doctor Name", yOffset, new string[] { });
            yOffset += FIELD_SPACING;

            dtpDate = AddModernDatePicker(pnlBasicInfo, "Appointment Date", yOffset);
            yOffset += FIELD_SPACING;

            dtpTime = AddModernDatePicker(pnlBasicInfo, "Appointment Time", yOffset);
            dtpTime.Format = DateTimePickerFormat.Time;
            dtpTime.ShowUpDown = true;
            yOffset += FIELD_SPACING;

            txtReason = AddModernTextBox(pnlBasicInfo, "Reason", yOffset, "Enter reason for visit...", true);
            yOffset += FIELD_SPACING + 40;

            // ================ STATUS & LOGS CARD (Matches Medical Info) =================
            pnlMedicalInfo = CreateModernCard("⚙️  Execution & Status");
            yOffset = 58;

            cmbStatus = AddModernComboBox(pnlMedicalInfo, "Status", yOffset, new string[] { "Scheduled", "Completed", "Cancelled" });
            yOffset += FIELD_SPACING;

            cmbPayment = AddModernComboBox(pnlMedicalInfo, "Payment Status", yOffset, new string[] { "Unpaid", "Paid", "Partial" });
            yOffset += FIELD_SPACING;

            cmbFollowUp = AddModernComboBox(pnlMedicalInfo, "Follow-Up", yOffset, new string[] { "No", "Yes" });
            yOffset += FIELD_SPACING;

            txtCreatedBy = AddModernTextBox(pnlMedicalInfo, "Created By", yOffset, "System User");
            txtCreatedBy.ReadOnly = true;
            yOffset += FIELD_SPACING;

            dtpCreatedAt = AddModernDatePicker(pnlMedicalInfo, "Created At", yOffset);
            dtpCreatedAt.Enabled = false;
            yOffset += FIELD_SPACING;

            txtUpdatedBy = AddModernTextBox(pnlMedicalInfo, "Updated By", yOffset, "System User");
            txtUpdatedBy.ReadOnly = true;
            yOffset += FIELD_SPACING;

            dtpUpdatedAt = AddModernDatePicker(pnlMedicalInfo, "Updated At", yOffset);
            dtpUpdatedAt.Enabled = false;

            // ================ QUICK ACTIONS CARD =================
            pnlActions = CreateModernCard("⚡  Quick Actions");
            Panel pnlTips = CreateTipsPanel();
            pnlTips.Location = new Point(CARD_PADDING, 58);
            pnlActions.Controls.Add(pnlTips);

            int btnHeight = 48;
            btnSave = CreateModernButton("💾  Save Appointment", successColor, 200, btnHeight);
            btnNew = CreateModernButton("✨  New Booking", infoColor, 200, btnHeight);
            btnEdit = CreateModernButton("✏️  Update Schedule", warningColor, 200, btnHeight);
            btnDelete = CreateModernButton("🗑️  Cancel Appointment", dangerColor, 200, btnHeight);
            btnView = CreateModernButton("👁️  View Calendar", secondaryColor, 200, btnHeight);
            btnRefresh = CreateModernButton("🔄  Refresh List", accentColor, 200, btnHeight);

            pnlActions.Controls.AddRange(new Control[] { btnSave, btnNew, btnEdit, btnDelete, btnView, btnRefresh });

            // ================ DATA GRID CARD =================
            pnlGrid = CreateModernCard("📋  Appointment Records");
            dgvAppointments = CreateModernDataGridView();
            dgvAppointments.Location = new Point(CARD_PADDING, 55);
            pnlGrid.Controls.Add(dgvAppointments);

            // ================ ADD ALL CONTROLS =================
            // ================ DATA GRID CARD =================
            pnlGrid = CreateModernCard("📋  Appointment Records");
            dgvAppointments = CreateModernDataGridView();
            dgvAppointments.Location = new Point(CARD_PADDING, 55);
            pnlGrid.Controls.Add(dgvAppointments);
            dgvAppointments.AutoGenerateColumns = true;

            // ================ ADD ALL CONTROLS =================
            pnlContent.Controls.AddRange(new Control[] { pnlBasicInfo, pnlMedicalInfo, pnlActions, pnlGrid });
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
            int topRowHeight = 580; // Increased to ensure 'Reason' is fully visible

            int basicMedicalWidth = (int)((availableWidth - cardGap * 2) * 0.30);
            int actionsWidth = (int)((availableWidth - cardGap * 2) * 0.40);
            basicMedicalWidth = Math.Max(basicMedicalWidth, 460);
            actionsWidth = Math.Max(actionsWidth, 500);

            pnlBasicInfo.SetBounds(CONTENT_PADDING, CONTENT_PADDING, basicMedicalWidth, topRowHeight);
            pnlMedicalInfo.SetBounds(CONTENT_PADDING + basicMedicalWidth + cardGap, CONTENT_PADDING, basicMedicalWidth, topRowHeight);
            pnlActions.SetBounds(CONTENT_PADDING + (basicMedicalWidth + cardGap) * 2, CONTENT_PADDING, actionsWidth, topRowHeight);

            LayoutActionButtons();

            int gridTop = CONTENT_PADDING + topRowHeight + cardGap;
            int gridHeight = availableHeight - topRowHeight - cardGap; 
            if (gridHeight < 200) gridHeight = 200;

            pnlGrid.SetBounds(CONTENT_PADDING, gridTop, availableWidth, gridHeight);
            dgvAppointments.SetBounds(10, 55, pnlGrid.Width - 20, pnlGrid.Height - 70);

            foreach (Control c in pnlActions.Controls) if (c is Panel p && p.BackColor == Color.FromArgb(235, 245, 255)) p.Size = new Size(pnlActions.Width - (CARD_PADDING * 2), 125);
            UpdateCardSeparator(pnlBasicInfo); UpdateCardSeparator(pnlMedicalInfo); UpdateCardSeparator(pnlActions); UpdateCardSeparator(pnlGrid);
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
            Panel searchPanel = new Panel { Size = new Size(300, 40), BackColor = surfaceColor };
            searchPanel.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, searchPanel.Width - 1, searchPanel.Height - 1, 20)) {
                    searchPanel.Region = new Region(path);
                    using (Pen pen = new Pen(Color.FromArgb(200, 210, 220), 1)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label searchIcon = new Label { Text = "🔍", Font = new Font("Segoe UI Emoji", 9), Size = new Size(24, 40), Location = new Point(8, 0), TextAlign = ContentAlignment.MiddleCenter };
            txtSearch = new TextBox { Location = new Point(38, 10), Size = new Size(250, 24), BorderStyle = BorderStyle.None, BackColor = surfaceColor, Font = new Font("Segoe UI", 10), ForeColor = placeholderColor, Text = "Search appointments..." };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search appointments...") { txtSearch.Text = ""; txtSearch.ForeColor = textPrimary; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search appointments..."; txtSearch.ForeColor = placeholderColor; } };
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
            ComboBox cmb = new ComboBox { Location = new Point(160, top), Size = new Size(parent.Width - 180, FIELD_HEIGHT), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10), FlatStyle = FlatStyle.Flat, BackColor = surfaceColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            cmb.Items.AddRange(items); parent.Controls.AddRange(new Control[] { lbl, cmb }); return cmb;
        }
 
        private DateTimePicker AddModernDatePicker(Panel parent, string label, int top) {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = Color.FromArgb(80, 95, 110), Location = new Point(10, top + 8), Size = new Size(145, 20), TextAlign = ContentAlignment.MiddleRight };
            DateTimePicker dtp = new DateTimePicker { Location = new Point(160, top), Size = new Size(parent.Width - 180, FIELD_HEIGHT), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10), CalendarForeColor = textPrimary, CalendarMonthBackground = cardColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
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
            Label l2 = new Label { Text = "• Select an appointment to see full booking details\n• Ensure doctor availability before rescheduling\n• Follow-up status helps track patient journey", Font = new Font("Segoe UI", 9), ForeColor = textSecondary, Location = new Point(16, 40), AutoSize = true };
            p.Controls.AddRange(new Control[] { l1, l2 }); return p;
        }

        private DataGridView CreateModernDataGridView() {
            DataGridView dgv = new DataGridView { BackgroundColor = cardColor, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Both, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(235, 240, 245), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
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
        }
    }
}
