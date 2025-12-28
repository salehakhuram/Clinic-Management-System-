using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class PatientConsultationForm
    {
        private System.ComponentModel.IContainer components = null;

        // Layout
        private Panel pnlHeader;
        private Panel pnlFooter;
        private Panel pnlContent;
        private TableLayoutPanel tlpMain;

        // Header Controls
        private Label lblPatientName;
        private Label lblPatientDetails;
        private Label lblAppointmentInfo;
        private Label lblStatus;

        // Content Sections (Cards)
        private Panel cardPatientDetails;
        private Panel cardClinical;
        private Panel cardPrescription;

        // Clinical Details
        private Label lblSymptoms;
        private TextBox txtSymptoms;
        private Label lblDiagnosis;
        private TextBox txtDiagnosis;
        private Label lblNotes;
        private TextBox txtNotes;

        // Prescription Builder
        private ComboBox cmbMedicines;
        private TextBox txtDosage;
        private TextBox txtDuration;
        private TextBox txtQty;
        private IconButton btnAddPrescription;
        private DataGridView dgvPrescriptions;

        // Actions
        private IconButton btnSaveProgress;
        private IconButton btnCompleteAppointment;
        private IconButton btnPrint;
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
            this.ClientSize = new System.Drawing.Size(1250, 850);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(249, 250, 251);

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            // 1. HEADER
            pnlHeader = new Panel {
                Dock = DockStyle.Top,
                Height = 125,
                BackColor = Color.White,
                Padding = new Padding(35, 15, 35, 15)
            };
            pnlHeader.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblPatientName = new Label {
                Text = "Patient Name",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(35, 20)
            };

            lblPatientDetails = new Label {
                Text = "Gender, Age | Patient ID",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(107, 114, 128),
                AutoSize = true,
                Location = new Point(37, 65)
            };

            lblAppointmentInfo = new Label {
                Text = "Appt ID: #000 | Date: --/--/----",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                TextAlign = ContentAlignment.TopRight,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 5, 0, 0)
            };

            lblStatus = new Label {
                Text = "",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(35, 95)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblPatientName, lblPatientDetails, lblAppointmentInfo, lblStatus });

            // 2. FOOTER
            pnlFooter = new Panel {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(35, 15, 35, 15)
            };
            pnlFooter.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(229, 231, 235)))
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            btnSaveProgress = CreateFooterButton("Save Progress", IconChar.Save, Color.FromArgb(243, 244, 246), Color.FromArgb(55, 65, 81));
            btnSaveProgress.Dock = DockStyle.Left;

            btnCompleteAppointment = CreateFooterButton("Complete Appointment", IconChar.CheckDouble, Color.FromArgb(59, 130, 246), Color.White);
            btnCompleteAppointment.Dock = DockStyle.Right;
            btnCompleteAppointment.Width = 240;

            btnPrint = CreateFooterButton("Print RX", IconChar.Print, Color.FromArgb(243, 244, 246), Color.FromArgb(55, 65, 81));
            btnPrint.Dock = DockStyle.Right;
            btnPrint.Width = 140;

            btnClose = CreateFooterButton("Close", IconChar.Times, Color.Transparent, Color.FromArgb(107, 114, 128));
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 120;

            pnlFooter.Controls.AddRange(new Control[] { btnSaveProgress, btnCompleteAppointment, btnPrint, btnClose });

            // 3. CONTENT
            pnlContent = new Panel {
                Dock = DockStyle.Fill,
                Padding = new Padding(35),
                AutoScroll = true
            };

            tlpMain = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

            // Clinical Details Card
            cardClinical = CreateCard("Clinical Examination");
            lblSymptoms = CreateLabel("Chief Symptoms", 25, 60);
            txtSymptoms = CreateTextBox(25, 85, true, 80);
            lblDiagnosis = CreateLabel("Diagnosis / Clinical Impression", 25, 185);
            txtDiagnosis = CreateTextBox(25, 210, true, 80);
            lblNotes = CreateLabel("Doctor Notes & Advice", 25, 310);
            txtNotes = CreateTextBox(25, 335, true, 120);

            cardClinical.Controls.AddRange(new Control[] { lblSymptoms, txtSymptoms, lblDiagnosis, txtDiagnosis, lblNotes, txtNotes });
            cardClinical.Height = 490;

            // Prescription Card
            cardPrescription = CreateCard("Prescription Management");
            
            TableLayoutPanel tlpPrescriptionInput = new TableLayoutPanel {
                Location = new Point(15, 60),
                Size = new Size(cardPrescription.Width - 30, 80),
                ColumnCount = 5,
                RowCount = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            tlpPrescriptionInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPrescriptionInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tlpPrescriptionInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            tlpPrescriptionInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            tlpPrescriptionInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));

            Label lblMed = CreateLabel("Select Medicine", 0, 0); lblMed.Dock = DockStyle.Bottom;
            Label lblDosage = CreateLabel("Dosage", 0, 0); lblDosage.Dock = DockStyle.Bottom;
            Label lblDuration = CreateLabel("Duration", 0, 0); lblDuration.Dock = DockStyle.Bottom;

            cmbMedicines = new ComboBox {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10),
                Height = 35,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            
            txtDosage = new TextBox {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "e.g. 1-0-1",
                BorderStyle = BorderStyle.FixedSingle
            };

            txtDuration = new TextBox {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "5 Days",
                BorderStyle = BorderStyle.FixedSingle
            };

            txtQty = new TextBox {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 10),
                ReadOnly = true,
                Text = "0",
                BackColor = Color.FromArgb(243, 244, 246),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };

            btnAddPrescription = new IconButton {
                IconChar = IconChar.Plus,
                IconSize = 20,
                IconColor = Color.White,
                BackColor = Color.FromArgb(59, 130, 246),
                Dock = DockStyle.Top,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            btnAddPrescription.FlatAppearance.BorderSize = 0;

            tlpPrescriptionInput.Controls.Add(lblMed, 0, 0);
            tlpPrescriptionInput.Controls.Add(lblDosage, 1, 0);
            tlpPrescriptionInput.Controls.Add(lblDuration, 2, 0);
            tlpPrescriptionInput.Controls.Add(CreateLabel("Qty", 0, 0), 3, 0);
            tlpPrescriptionInput.Controls.Add(cmbMedicines, 0, 1);
            tlpPrescriptionInput.Controls.Add(txtDosage, 1, 1);
            tlpPrescriptionInput.Controls.Add(txtDuration, 2, 1);
            tlpPrescriptionInput.Controls.Add(txtQty, 3, 1);
            tlpPrescriptionInput.Controls.Add(btnAddPrescription, 4, 1);

            dgvPrescriptions = new DataGridView {
                Location = new Point(25, 145),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Size = new Size(cardPrescription.Width - 50, 480),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 45,
                RowTemplate = { Height = 40 },
                AllowUserToAddRows = false,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(243, 244, 246),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvPrescriptions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            dgvPrescriptions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
            dgvPrescriptions.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(75, 85, 99);
            dgvPrescriptions.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvPrescriptions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgvPrescriptions.DefaultCellStyle.SelectionForeColor = Color.FromArgb(37, 99, 235);

            cardPrescription.Controls.AddRange(new Control[] { tlpPrescriptionInput, dgvPrescriptions });
            cardClinical.Dock = DockStyle.Fill;
            cardPrescription.Dock = DockStyle.Fill;
            
            tlpMain.Controls.Add(cardClinical, 0, 0);
            tlpMain.Controls.Add(cardPrescription, 1, 0);

            pnlContent.Controls.Add(tlpMain);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private Panel CreateCard(string title)
        {
            Panel card = new Panel {
                BackColor = Color.White,
                Margin = new Padding(0, 0, 15, 15),
                Padding = new Padding(25, 60, 25, 25)
            };
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 12))
                using (Pen pen = new Pen(Color.FromArgb(229, 231, 235))) {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            Label lbl = new Label {
                Text = title,
                Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                Location = new Point(25, 20),
                AutoSize = true
            };
            card.Controls.Add(lbl);
            return card;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.FromArgb(107, 114, 128),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private TextBox CreateTextBox(int x, int y, bool multiline, int height)
        {
            return new TextBox {
                Location = new Point(x, y),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Width = 480, // Reduced width to prevent stretching
                Height = height,
                Multiline = multiline,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
                BackColor = Color.FromArgb(250, 251, 252)
            };
        }

        private IconButton CreateFooterButton(string text, IconChar icon, Color backColor, Color foreColor)
        {
            IconButton btn = new IconButton {
                Text = "  " + text,
                IconChar = icon,
                IconSize = 18,
                IconColor = foreColor,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 50),
                Font = new Font("Segoe UI Semibold", 10),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Margin = new Padding(10, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            if (backColor == Color.Transparent) btn.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            return btn;
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
