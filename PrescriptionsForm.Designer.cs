using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class PrescriptionsForm : Form
    {
        private readonly Color primaryColor = Color.FromArgb(59, 130, 246); // Blue Theme
        private readonly Color secondaryColor = Color.FromArgb(37, 99, 235);
        private readonly Color accentColor = Color.FromArgb(14, 165, 233);
        private readonly Color surfaceColor = Color.FromArgb(243, 246, 249);
        private readonly Color cardColor = Color.White;
        private readonly Color textPrimary = Color.FromArgb(31, 41, 55);
        private readonly Color textSecondary = Color.FromArgb(107, 114, 128);
        private readonly Color borderGray = Color.FromArgb(229, 231, 235);
        private readonly Color successColor = Color.FromArgb(16, 185, 129);
        private readonly Color dangerColor = Color.FromArgb(239, 68, 68);

        private const int CONTENT_PADDING = 20;
        private const int CARD_PADDING = 20;
        private const int FIELD_SPACING = 55;

        // Panels
        private Panel pnlHeader, pnlContent, pnlLeft, pnlRight, pnlBottomGrid;
        private Panel cardPrescriptionInfo, cardClinicalDetails, cardActions, cardGrid;

        // Controls
        private RoundedTextBox txtPrescriptionID, txtDiagnosis, txtSymptoms, txtNotes, txtQty, txtDosage, txtDuration;
        private ComboBox cmbMedicines, cmbAppointmentIntId, cmbItemStatus, cmbDoctor;
        private DateTimePicker dtpCreatedDate;
        private DataGridView dgvItems, dgvMaster;
        private TextBox txtSearch; // Added Search
        private Button btnSave, btnNew, btnDelete, btnPrint, btnPrintAudit, btnRefresh, btnAddItem; // Added btnPrintAudit

        private void InitializeComponent()
        {
            this.Size = new Size(1500, 900);
            this.Text = "Prescription Management";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.DoubleBuffered = true;

            // ================ HEADER =================
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 20, 5)
            };
            pnlHeader.Paint += (s, e) => {
                using (Pen p = new Pen(borderGray)) e.Graphics.DrawLine(p, 0, 99, pnlHeader.Width, 99);
            };

            Label lblTitle = new Label {
                Text = "Prescription Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                Location = new Point(20, 10),
                AutoSize = true
            };
            Label lblSub = new Label {
                Text = "Create, search, and manage patient medication records",
                Font = new Font("Segoe UI", 10),
                ForeColor = textSecondary,
                Location = new Point(22, 55),
                AutoSize = true
            };
            // Search Box in Header
            txtSearch = new TextBox { PlaceholderText = "Search Prescription / Patient...", Location = new Point(pnlHeader.Width - 350, 25), Size = new Size(300, 30), Font = new Font("Segoe UI", 11) };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, txtSearch });
            pnlHeader.Resize += (s, e) => txtSearch.Left = pnlHeader.Width - 350;

            // ================ CONTENT =================
            pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(35, 15, 35, 15) };
            
            pnlLeft = new Panel { Dock = DockStyle.Left, Width = 460, AutoScroll = true }; // Enabled AutoScroll
            pnlBottomGrid = CreateModernCard("📋 Prescription History");
            pnlBottomGrid.Dock = DockStyle.Bottom;
            pnlBottomGrid.Height = 180; // Reduced height
            pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 0, 0) }; // Closer to left

            // Left Cards
            cardPrescriptionInfo = CreateModernCard("📝 Prescription ID & Link");
            int y = 55;
            txtPrescriptionID = AddField(cardPrescriptionInfo, "Prescription ID", "e.g PR-001", ref y);
            txtPrescriptionID.ReadOnly = true;
            cmbAppointmentIntId = AddComboBox(cardPrescriptionInfo, "Appointment Code", ref y);
            cmbDoctor = AddComboBox(cardPrescriptionInfo, "Prescribing Doctor", ref y);
            dtpCreatedDate = AddDatePicker(cardPrescriptionInfo, "Date Created", ref y);
            cardPrescriptionInfo.Height = y + 20;

            cardClinicalDetails = CreateModernCard("🏥 Clinical Information");
            y = 55;
            txtDiagnosis = AddField(cardClinicalDetails, "Diagnosis", "Enter diagnosis details...", ref y, true);
            txtSymptoms = AddField(cardClinicalDetails, "Symptoms", "Enter patient symptoms...", ref y, true);
            txtNotes = AddField(cardClinicalDetails, "Pharmacist Notes", "Optional notes...", ref y, true);
            cardClinicalDetails.Top = cardPrescriptionInfo.Bottom + 10; // Reduced gap
            cardClinicalDetails.Height = y + 15;

            pnlLeft.Controls.AddRange(new Control[] { cardClinicalDetails, cardPrescriptionInfo });

            // Right Cards
            cardGrid = CreateModernCard("💊 Medication Items");
            
            // Add Item Row
            cmbMedicines = new ComboBox { Location = new Point(20, 60), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            txtDosage = new RoundedTextBox { Location = new Point(180, 60), Width = 100, PlaceholderText = "Dosage (1-0-1)", PlaceholderColor = Color.Gray, BorderRadius = 5 };
            txtDuration = new RoundedTextBox { Location = new Point(290, 60), Width = 100, PlaceholderText = "Duration (Days)", PlaceholderColor = Color.Gray, BorderRadius = 5 };
            txtQty = new RoundedTextBox { Location = new Point(400, 60), Width = 60, PlaceholderText = "Qty", PlaceholderColor = Color.Gray, BorderRadius = 5 };
            cmbItemStatus = new ComboBox { Location = new Point(470, 60), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            btnAddItem = CreateBtn("➕ Add", accentColor);
            btnAddItem.SetBounds(570, 60, 80, 38);
            btnAddItem.BackColor = accentColor;

            cardGrid.Controls.AddRange(new Control[] { cmbMedicines, txtDosage, txtDuration, txtQty, cmbItemStatus, btnAddItem });

            dgvItems = CreateDataGridView();
            dgvItems.Location = new Point(20, 110);
            cardGrid.Controls.Add(dgvItems);
            cardGrid.Dock = DockStyle.Fill;
            
            dgvMaster = CreateDataGridView();
            pnlBottomGrid.Controls.Add(dgvMaster);

            cardActions = CreateModernCard("⚡ Actions");
            cardActions.Dock = DockStyle.Bottom;
            cardActions.Height = 110;
            LayoutActions();

            pnlRight.Controls.AddRange(new Control[] { cardGrid, cardActions });

            pnlContent.Controls.AddRange(new Control[] { pnlRight, pnlLeft });
            pnlContent.Controls.Add(pnlBottomGrid); // Bottom grid inside content
            
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader); // Header outside content

            this.Load += (s, e) => { 
                dgvItems.SetBounds(20, 110, cardGrid.Width - 40, cardGrid.Height - 130);
                dgvMaster.SetBounds(20, 60, pnlBottomGrid.Width - 40, pnlBottomGrid.Height - 80);
            };
            cardGrid.Resize += (s, e) => {
                dgvItems.SetBounds(20, 110, cardGrid.Width - 40, cardGrid.Height - 130);
                btnAddItem.Left = cardGrid.Width - btnAddItem.Width - 20;
                cmbItemStatus.Left = btnAddItem.Left - cmbItemStatus.Width - 10;
                txtDosage.Width = cmbItemStatus.Left - txtDosage.Left - 10;
            };
            pnlBottomGrid.Resize += (s, e) => {
                dgvMaster.SetBounds(20, 60, pnlBottomGrid.Width - 40, pnlBottomGrid.Height - 80);
            };
        }

        private void LayoutActions()
        {
            btnSave = CreateBtn("💾 Save", successColor);
            btnNew = CreateBtn("✨ New", Color.FromArgb(59, 130, 246));
            btnDelete = CreateBtn("🗑️ Delete", dangerColor);
            btnPrint = CreateBtn("🖨️ Print", textSecondary);
            btnPrintAudit = CreateBtn("📑 Audit", Color.DarkSlateGray); // Added Audit Button
            btnRefresh = CreateBtn("🔄 Refresh", accentColor);

            int bW = 120, bH = 45, gap = 10;
            btnSave.SetBounds(20, 45, bW, bH);
            btnNew.SetBounds(btnSave.Right + gap, 45, bW, bH);
            btnDelete.SetBounds(btnNew.Right + gap, 45, bW, bH);
            btnPrint.SetBounds(btnDelete.Right + gap, 45, bW, bH);
            btnPrintAudit.SetBounds(btnPrint.Right + gap, 45, bW, bH);
            btnRefresh.SetBounds(cardActions.Width - bW - 20, 45, bW, bH);
            
            cardActions.Controls.AddRange(new Control[] { btnSave, btnNew, btnDelete, btnPrint, btnPrintAudit, btnRefresh });
            cardActions.Resize += (s, e) => btnRefresh.Left = cardActions.Width - btnRefresh.Width - 20;
        }

        private Panel CreateModernCard(string title)
        {
            Panel p = new Panel { BackColor = cardColor, Width = 450 };
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, p.Width - 1, p.Height - 1, 12))
                {
                    p.Region = new Region(path);
                    using (Pen pen = new Pen(borderGray, 1)) e.Graphics.DrawPath(pen, path);
                }
            };
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI Semibold", 12), ForeColor = textPrimary, AutoSize = true, Location = new Point(20, 15) };
            p.Controls.Add(lbl);
            Panel sep = new Panel { BackColor = borderGray, Height = 1, Location = new Point(20, 45), Width = p.Width - 40 };
            p.Controls.Add(sep);
            return p;
        }

        private RoundedTextBox AddField(Panel p, string label, string hint, ref int y, bool multi = false)
        {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9), ForeColor = textSecondary, AutoSize = true, Location = new Point(20, y) };
            RoundedTextBox txt = new RoundedTextBox { PlaceholderText = hint, PlaceholderColor = Color.Gray, Multiline = multi, Height = multi ? 80 : 38, Location = new Point(20, y + 22), Width = p.Width - 40, BorderRadius = 8, BorderColor = borderGray };
            p.Controls.AddRange(new Control[] { lbl, txt });
            y += txt.Height + (multi ? 30 : 35);
            return txt;
        }

        private DateTimePicker AddDatePicker(Panel p, string label, ref int y)
        {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9), ForeColor = textSecondary, AutoSize = true, Location = new Point(20, y) };
            DateTimePicker dtp = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(20, y + 22), Width = p.Width - 40, Height = 38 };
            p.Controls.AddRange(new Control[] { lbl, dtp });
            y += 75;
            return dtp;
        }

        private ComboBox AddComboBox(Panel p, string label, ref int y)
        {
            Label lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 9), ForeColor = textSecondary, AutoSize = true, Location = new Point(20, y) };
            ComboBox cmb = new ComboBox { Location = new Point(20, y + 22), Width = p.Width - 40, Height = 38, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            p.Controls.AddRange(new Control[] { lbl, cmb });
            y += 75;
            return cmb;
        }

        private Button CreateBtn(string txt, Color bg)
        {
            Button b = new Button { Text = txt, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private DataGridView CreateDataGridView()
        {
            DataGridView dgv = new DataGridView { 
                BackgroundColor = Color.White, 
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 40 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = primaryColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
            dgv.ColumnHeadersHeight = 45;
            return dgv;
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
