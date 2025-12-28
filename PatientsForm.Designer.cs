using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class PatientsForm : Form
    {
        // Modern Color Palette - Medical/Healthcare Theme
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

        // Layout Constants
        private const int LABEL_WIDTH = 160; 
        private const int INPUT_LEFT = 175;  
        private const int INPUT_WIDTH = 300;
        private const int FIELD_HEIGHT = 40;
        private const int FIELD_SPACING = 52;
        private const int CARD_PADDING = 20;
        private const int HEADER_HEIGHT = 80;  // Reduced height as requested
        private const int CONTENT_PADDING = 20;

        // Panels
        Panel pnlContent;
        Panel pnlBasicInfo, pnlMedicalInfo, pnlActions, pnlGrid;

        // Controls
        Label lblTitle, lblSubtitle;
        TextBox txtSearch, txtDoctorID;
        RoundedTextBox txtPatientID, txtPatientName, txtFatherName, txtAge, txtPhone, txtEmail, txtAddress, txtDisease;
        ComboBox cmbGender, cmbBloodGroup, cmbStatus, cmbDoctor, cmbDoctorID;
        DateTimePicker dtpDOB, dtpRegistrationDate;
        Button btnSave, btnNew, btnEdit, btnDelete, btnView,btnRefresh;
        DataGridView dgvPatients;

        private void InitializeComponent()
        {
            // ================ FORM SETUP =================
            this.Text = "Patients Management";
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 10F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            this.MinimumSize = new Size(1200, 800);

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
                Text = "Patients Management",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textPrimary,
                AutoSize = true,
                Location = new Point(20, 10)
            };

            Label lblHeaderSubtitle = new Label()
            {
                Text = "Manage patient records, medical history, and clinical data",
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

            // ================ BASIC INFO CARD =================
            pnlBasicInfo = CreateModernCard("👤  Basic Information");

            int yOffset = 58;

            txtPatientID = AddModernTextBox(pnlBasicInfo, "Patient ID", yOffset, "e.g P001");
            txtPatientID.ReadOnly = true;
            yOffset += FIELD_SPACING;

            txtPatientName = AddModernTextBox(pnlBasicInfo, "Patient Name", yOffset, "e.g Saleha Khurram");
            yOffset += FIELD_SPACING;

            txtFatherName = AddModernTextBox(pnlBasicInfo, "Father's Name", yOffset, "e.g Khurram Shoukat");
            yOffset += FIELD_SPACING;

            cmbGender = AddModernComboBox(pnlBasicInfo, "Gender", yOffset, new string[] { "Male", "Female", "Other" });
            yOffset += FIELD_SPACING;

            dtpDOB = AddModernDatePicker(pnlBasicInfo, "Date of Birth", yOffset);
            yOffset += FIELD_SPACING;

            txtAge = AddModernTextBox(pnlBasicInfo, "Age", yOffset, "Years");
            txtAge.ReadOnly = true;
            yOffset += FIELD_SPACING;

            txtPhone = AddModernTextBox(pnlBasicInfo, "Phone Number", yOffset, "+92 3XX XXXXXXX");
            yOffset += FIELD_SPACING;

            txtEmail = AddModernTextBox(pnlBasicInfo, "Email Address", yOffset, "e.g patient@example.com");
            yOffset += FIELD_SPACING; 

            // Phone number ke baad address add karein
            txtAddress = AddModernTextBox(pnlBasicInfo, "Address", yOffset, "Enter complete address...", true);
            yOffset += FIELD_SPACING + 40; 
            // ================ MEDICAL INFO CARD =================
            pnlMedicalInfo = CreateModernCard("🏥  Medical Details");

            yOffset = 58;

            txtDisease = AddModernTextBox(pnlMedicalInfo, "Diagnosis", yOffset, "Enter diagnosis");
            yOffset += FIELD_SPACING;

            cmbBloodGroup = AddModernComboBox(pnlMedicalInfo, "Blood Group", yOffset, new string[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" });
            yOffset += FIELD_SPACING;

            dtpRegistrationDate = AddModernDatePicker(pnlMedicalInfo, "Registration", yOffset);
            yOffset += FIELD_SPACING;

            cmbStatus = AddModernComboBox(pnlMedicalInfo, "Status", yOffset, new string[] { "Active", "Discharged", "Under Treatment", "Critical" });
            yOffset += FIELD_SPACING;

            // Doctor ID ComboBox - NEW
            cmbDoctorID = AddModernComboBox(pnlMedicalInfo, "Doctor ID", yOffset, new string[] {
                "D001", "D002", "D003", "D004", "D005"
            });
            yOffset += FIELD_SPACING;

            // Assigned Doctor Name ComboBox
            cmbDoctor = AddModernComboBox(pnlMedicalInfo, "Doctor Name", yOffset, new string[] {
                "Dr. Ahmed (Cardiology)",
                "Dr. Sara (Gynecology)",
                "Dr. Ali (Orthopedics)",
                "Dr. Khan (General)"
            });
            yOffset += FIELD_SPACING;

           

            // Hidden TextBox for backward compatibility
            txtDoctorID = new TextBox() { Visible = false };
            pnlMedicalInfo.Controls.Add(txtDoctorID);

            // ================ QUICK ACTIONS CARD =================
            pnlActions = CreateModernCard("⚡  Quick Actions");

            // Tips Panel - will resize with card
            Panel pnlTips = CreateTipsPanel();
            pnlTips.Location = new Point(CARD_PADDING, 58);
            pnlActions.Controls.Add(pnlTips);

            // Action Buttons
            int btnHeight = 48;
            int btnStartY = 280;
            int btnGapY = 56;

            btnSave = CreateModernButton("💾  Save Record", successColor, 200, btnHeight);
            btnNew = CreateModernButton("✨  New Patient", infoColor, 200, btnHeight);
            btnEdit = CreateModernButton("✏️  Update Record", warningColor, 200, btnHeight);
            btnDelete = CreateModernButton("🗑️  Delete Record", dangerColor, 200, btnHeight);
            btnView = CreateModernButton("👁️  View Details", secondaryColor, 200, btnHeight);
            btnRefresh = CreateModernButton("🔄  Refresh List", accentColor, 200, btnHeight);

            // Buttons will be positioned in LayoutContent
            pnlActions.Controls.AddRange(new Control[] { btnSave, btnNew, btnEdit, btnDelete, btnView, btnRefresh });

            // ================ DATA GRID CARD =================
            pnlGrid = CreateModernCard("📋  Patient Records");
            dgvPatients = CreateModernDataGridView();
            dgvPatients.Location = new Point(CARD_PADDING, 55);
            pnlGrid.Controls.Add(dgvPatients);
            dgvPatients.AutoGenerateColumns = true;
            pnlContent.Controls.AddRange(new Control[] { pnlBasicInfo, pnlMedicalInfo, pnlActions, pnlGrid });
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);

            // Initial layout
            this.Load += (s, e) =>
            {
                LayoutContent();
            };
        }

        // ================= LAYOUT METHODS =================

        private void LayoutContent()
        {
            if (pnlContent.Width < 100) return;

            int availableWidth = pnlContent.ClientSize.Width - (CONTENT_PADDING * 2);
            int availableHeight = pnlContent.ClientSize.Height - (CONTENT_PADDING * 2);

            int cardGap = 16;
            int topRowHeight = 580; // Increased to accommodate Email field
            // Increased height for Basic Info panel

            // Calculate card widths - Quick Actions gets more space
            int basicMedicalWidth = (int)((availableWidth - cardGap * 2) * 0.30);  // 30% each
            int actionsWidth = (int)((availableWidth - cardGap * 2) * 0.40);        // 40% for Quick Actions

            // Minimum widths
            basicMedicalWidth = Math.Max(basicMedicalWidth, 460);
            actionsWidth = Math.Max(actionsWidth, 500);

            // Position cards - Top Row
            pnlBasicInfo.Location = new Point(CONTENT_PADDING, CONTENT_PADDING);
            pnlBasicInfo.Size = new Size(basicMedicalWidth, topRowHeight);

            pnlMedicalInfo.Location = new Point(CONTENT_PADDING + basicMedicalWidth + cardGap, CONTENT_PADDING);
            pnlMedicalInfo.Size = new Size(basicMedicalWidth, topRowHeight);

            pnlActions.Location = new Point(CONTENT_PADDING + (basicMedicalWidth + cardGap) * 2, CONTENT_PADDING);
            pnlActions.Size = new Size(actionsWidth, topRowHeight);

            // Layout Quick Actions buttons based on panel width
            LayoutActionButtons();

            // Data Grid - Bottom Row (fill remaining height)
            int gridTop = CONTENT_PADDING + topRowHeight + cardGap;
            // Ensure grid takes all remaining space properly
            int gridHeight = availableHeight - topRowHeight - cardGap; 
            if (gridHeight < 200) gridHeight = 200; // Minimum safety height

            pnlGrid.Location = new Point(CONTENT_PADDING, gridTop);
            pnlGrid.Size = new Size(availableWidth, gridHeight);

            // Update DataGridView size - Reduced horizontal padding to increase width
            dgvPatients.Size = new Size(pnlGrid.Width - 20, pnlGrid.Height - 70); 
            dgvPatients.Location = new Point(10, 55);

            dgvPatients.Location = new Point(10, 55);

            // Update Tips panel width
            foreach (Control c in pnlActions.Controls)
            {
                if (c is Panel p && p.BackColor == Color.FromArgb(235, 245, 255))
                {
                    // Increase height to 120 or more to fit text
                    p.Size = new Size(pnlActions.Width - (CARD_PADDING * 2), 125); 
                }
            }

            // Update separators
            UpdateCardSeparator(pnlBasicInfo);
            UpdateCardSeparator(pnlMedicalInfo);
            UpdateCardSeparator(pnlActions);
            UpdateCardSeparator(pnlGrid);
        }

        private void LayoutActionButtons()
        {
            int panelWidth = pnlActions.Width;
            int btnWidth = (panelWidth - CARD_PADDING * 2 - 12) / 2;  // Two buttons per row with gap
            int btnHeight = 48;
            int btnStartY = 220; // Increased to 220 for better clearance
            int btnGapY = 56;
            int btnGapX = 12;

            btnSave.Size = new Size(btnWidth, btnHeight);
            btnSave.Location = new Point(CARD_PADDING, btnStartY);

            btnNew.Size = new Size(btnWidth, btnHeight);
            btnNew.Location = new Point(CARD_PADDING + btnWidth + btnGapX, btnStartY);

            btnEdit.Size = new Size(btnWidth, btnHeight);
            btnEdit.Location = new Point(CARD_PADDING, btnStartY + btnGapY);

            btnDelete.Size = new Size(btnWidth, btnHeight);
            btnDelete.Location = new Point(CARD_PADDING + btnWidth + btnGapX, btnStartY + btnGapY);

            btnView.Size = new Size(btnWidth * 2 + btnGapX, btnHeight);
            btnView.Location = new Point(CARD_PADDING, btnStartY + btnGapY * 2);

            // Find refresh button
            foreach (Control c in pnlActions.Controls)
            {
                if (c is Button btn && btn.Text.Contains("Refresh"))
                {
                    btn.Size = new Size(btnWidth * 2 + btnGapX, btnHeight);
                    btn.Location = new Point(CARD_PADDING, btnStartY + btnGapY * 3);
                }
            }
        }

        private void UpdateCardSeparator(Panel card)
        {
            foreach (Control c in card.Controls)
            {
                if (c is Panel p && p.Height == 1 && p.BackColor == Color.FromArgb(228, 233, 240))
                {
                    p.Width = card.Width - (CARD_PADDING * 2);
                }
            }
        }

        // ================= MODERN UI HELPER METHODS =================

        private Panel CreateModernCard(string title)
        {
            Panel card = new Panel()
            {
                BackColor = cardColor
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, card.Width - 1, card.Height - 1, 14))
                {
                    card.Region = new Region(path);
                    using (Pen pen = new Pen(Color.FromArgb(220, 228, 235), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Card Title
            Label lblCardTitle = new Label()
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold),
                ForeColor = textPrimary,
                Location = new Point(CARD_PADDING, 16),
                AutoSize = true
            };
            card.Controls.Add(lblCardTitle);

            // Separator Line
            Panel separator = new Panel()
            {
                Location = new Point(CARD_PADDING, 46),
                Height = 1,
                BackColor = Color.FromArgb(228, 233, 240)
            };
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
            txtSearch = new TextBox { Text = "Search patients...", Location = new Point(52, 11), Size = new Size(330, 24), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(249, 250, 251), ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search patients...") { txtSearch.Text = ""; txtSearch.ForeColor = textPrimary; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search patients..."; txtSearch.ForeColor = Color.Gray; } };
            searchPanel.Controls.AddRange(new Control[] { searchIcon, txtSearch });
            return searchPanel;
        }

        private RoundedTextBox AddModernTextBox(Panel parent, string labelText, int yPos, string placeholder, bool isMultiline = false)
        {
            // 1. Label Setup
            Label lbl = new Label()
            {
                Text = labelText,
                Location = new Point(10, yPos + 8),
                AutoSize = false,
                Width = 145, // Match LABEL_WIDTH
                Font = new Font("Segoe UI Semibold", 9.5F), // Semibold
                ForeColor = Color.FromArgb(80, 95, 110),
                TextAlign = ContentAlignment.MiddleRight
            };

            // 2. Custom RoundedTextBox
            RoundedTextBox txt = new RoundedTextBox()
            {
                Location = new Point(160, yPos), // Match INPUT_LEFT
                Size = new Size(parent.Width - 180, isMultiline ? 85 : FIELD_HEIGHT), // Adjusted size
                PlaceholderText = placeholder,
                PlaceholderColor = Color.Gray,
                Multiline = isMultiline,
                BorderColor = Color.FromArgb(210, 220, 230),
                BorderFocusColor = accentColor, // Uses the class-level accentColor
                BorderRadius = 8, 
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Responsive
            };

            parent.Controls.Add(lbl);
            parent.Controls.Add(txt);

            return txt;
        }
    

        private ComboBox AddModernComboBox(Panel parent, string label, int top, string[] items)
        {
            // Label - Right aligned
            Label lbl = new Label()
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 9.5F), // Updated to match TextBox
                ForeColor = Color.FromArgb(80, 95, 110), // Updated color
                Location = new Point(10, top + 8), // Updated alignment
                Size = new Size(145, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            ComboBox cmb = new ComboBox()
            {
                Location = new Point(160, top),
                Size = new Size(parent.Width - 180, FIELD_HEIGHT),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = surfaceColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cmb.Items.AddRange(items);

            parent.Controls.AddRange(new Control[] { lbl, cmb });
            return cmb;
        }

        private DateTimePicker AddModernDatePicker(Panel parent, string label, int top)
        {
            // Label - Right aligned
            Label lbl = new Label()
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 9.5F), // Updated to match TextBox
                ForeColor = Color.FromArgb(80, 95, 110), // Updated color
                Location = new Point(10, top + 8), // Updated alignment
                Size = new Size(145, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            DateTimePicker dtp = new DateTimePicker()
            {
                Location = new Point(160, top),
                Size = new Size(parent.Width - 180, FIELD_HEIGHT),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10),
                CalendarForeColor = textPrimary,
                CalendarMonthBackground = cardColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            parent.Controls.AddRange(new Control[] { lbl, dtp });
            return dtp;
        }

        private Button CreateModernButton(string text, Color bgColor, int width, int height)
        {
            Button btn = new Button()
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = bgColor,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;

            Color originalColor = bgColor;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(originalColor, 0.12f);
            btn.MouseLeave += (s, e) => btn.BackColor = originalColor;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, btn.Width - 1, btn.Height - 1, 10))
                {
                    btn.Region = new Region(path);
                }
            };

            return btn;
        }

        private void LayoutGridSearch()
        {
            if (txtSearch != null)
            {
                txtSearch.Location = new Point(pnlGrid.Width - txtSearch.Width - 30, 18);
            }
        }

        private Panel CreateTipsPanel()
        {
            Panel tipsPanel = new Panel()
            {
                Size = new Size(460, 125), // Increased height default
                BackColor = Color.FromArgb(235, 245, 255)
            };
            tipsPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, tipsPanel.Width - 1, tipsPanel.Height - 1, 12))
                {
                    tipsPanel.Region = new Region(path);
                }
            };

            Label lblTipsTitle = new Label()
            {
                Text = "💡 Quick Tips",
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
                ForeColor = secondaryColor,
                Location = new Point(16, 14),
                AutoSize = true
            };

            Label lblTipsText = new Label()
            {
                Text = "• Select a patient from the table to edit details\n• Click Save to add new or update existing\n• All required fields must be filled",
                Font = new Font("Segoe UI", 9),
                ForeColor = textSecondary,
                Location = new Point(16, 40),
                AutoSize = true
            };

            tipsPanel.Controls.AddRange(new Control[] { lblTipsTitle, lblTipsText });
            return tipsPanel;
        }

        private DataGridView CreateModernDataGridView() {
            DataGridView dgv = new DataGridView { BackgroundColor = cardColor, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Both, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(235, 240, 245), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = primaryColor; dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold); dgv.ColumnHeadersHeight = 46; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.DefaultCellStyle.BackColor = Color.White; dgv.DefaultCellStyle.ForeColor = textPrimary; dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 240, 255); dgv.DefaultCellStyle.SelectionForeColor = textPrimary; dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10F); dgv.DefaultCellStyle.Padding = new Padding(12, 5, 12, 5); dgv.RowTemplate.Height = 44; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            return dgv;
        }

        private GraphicsPath CreateRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
