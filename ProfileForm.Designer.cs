using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClinicManagement
{
    partial class ProfileForm
    {
        private System.ComponentModel.IContainer components = null;
        
        // Profile Tab Controls
        private PictureBox picProfile;
        private Button btnUploadPicture;
        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtUsername;
        private Label lblRole;
        private Button btnSaveProfile;
        
        // Security Tab Controls
        private TextBox txtCurrentPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Label lblPasswordStrength;
        private CheckBox chkShowPassword;
        private Button btnChangePassword;
        
        // Tab Buttons
        private Panel panelTabButtons;
        private Button btnProfileTab;
        private Button btnSecurityTab;
        private Button btnClose;
        
        // Content Panels
        private Panel panelProfileContent;
        private Panel panelSecurityContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Color primaryBlue = Color.FromArgb(59, 130, 246);
            Color accentGreen = Color.FromArgb(16, 185, 129);
            Color textPrimary = Color.FromArgb(31, 41, 55);
            Color textSecondary = Color.FromArgb(107, 114, 128);
            Color surfaceColor = Color.FromArgb(243, 246, 249);
            Color borderGray = Color.FromArgb(229, 231, 235);
            
            this.SuspendLayout();

            // ============ FORM SETUP ============
            this.Text = "My Profile";
            this.Size = new Size(650, 700); 
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None; // Back to borderless
            this.BackColor = surfaceColor;
            this.Font = new Font("Segoe UI", 9.5F);

            // ============ TAB BUTTONS ============
            panelTabButtons = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70, // Standard modern header height
                BackColor = surfaceColor,
                Padding = new Padding(25, 5, 25, 0)
            };

            btnProfileTab = new Button
            {
                Text = "👤 Profile",
                Size = new Size(150, 40),
                Location = new Point(25, 20), // Higher up in header
                BackColor = Color.White,
                ForeColor = primaryBlue,
                Font = new Font("Segoe UI Semibold", 11),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnProfileTab.FlatAppearance.BorderColor = primaryBlue;
            btnProfileTab.FlatAppearance.BorderSize = 2;
            btnProfileTab.Click += (s, e) => ShowProfileTab();

            btnSecurityTab = new Button
            {
                Text = "🔑 Security",
                Size = new Size(150, 40),
                Location = new Point(185, 20), // Higher up in header
                BackColor = surfaceColor,
                ForeColor = textSecondary,
                Font = new Font("Segoe UI Semibold", 11),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSecurityTab.FlatAppearance.BorderSize = 0;
            btnSecurityTab.Click += (s, e) => ShowSecurityTab();


            // Close Button
            btnClose = new Button
            {
                Text = "✕",
                Size = new Size(40, 40),
                BackColor = Color.White,
                ForeColor = textSecondary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(595, 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68); // Red-500
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = textSecondary; };
            btnClose.Click += (s, e) => this.Close();

            panelTabButtons.Controls.AddRange(new Control[] { btnProfileTab, btnSecurityTab, btnClose });
            this.Controls.Add(panelTabButtons);

            // ============ PROFILE TAB CONTENT ============
        panelProfileContent = new Panel
{
    Dock = DockStyle.Fill,
    BackColor = Color.White,
    Padding = new Padding(40, 20, 40, 40),
    AutoScroll = true,
    Visible = true
};


            // Profile Picture
            picProfile = new PictureBox
            {
                Size = new Size(120, 120),
                Top = 80, // Massive shift down from top of content
                BackColor = surfaceColor,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

panelProfileContent.Resize += (s, e) =>
{
    picProfile.Left = (panelProfileContent.Width - picProfile.Width) / 2;
    btnUploadPicture.Left = (panelProfileContent.Width - btnUploadPicture.Width) / 2;
};

            picProfile.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, 119, 119);
                    picProfile.Region = new Region(path);
                }
            };

            btnUploadPicture = new Button
            {
                Text = "📷 Change Photo",
                Size = new Size(140, 35),
                Location = new Point(270, 210), // Lowered
                BackColor = surfaceColor,
                ForeColor = primaryBlue,
                Font = new Font("Segoe UI Semibold", 9),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUploadPicture.FlatAppearance.BorderColor = borderGray;
            btnUploadPicture.Click += btnUploadPicture_Click;

            // Full Name
            Label lblFullName = new Label { Text = "Full Name", Location = new Point(50, 280), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtFullName = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 305),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Username
            Label lblUsername = new Label { Text = "Username", Location = new Point(50, 360), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtUsername = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 385),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Email (editable)
            Label lblEmail = new Label { Text = "Email Address", Location = new Point(50, 440), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtEmail = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 465),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = false,
                BackColor = Color.White
            };

            // Role (readonly)
            Label lblRoleLabel = new Label { Text = "User Role", Location = new Point(50, 520), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            lblRole = new Label
            {
                Text = userRole,
                Size = new Size(580, 32),
                Location = new Point(50, 545),
                Font = new Font("Segoe UI", 11),
                ForeColor = textSecondary
            };

            // Save Button
        btnSaveProfile = new Button
{
    Text = "💾 Save Changes",
    Size = new Size(200, 45),
    BackColor = accentGreen,
    ForeColor = Color.White,
    Font = new Font("Segoe UI Semibold", 11),
    FlatStyle = FlatStyle.Flat,
    Cursor = Cursors.Hand,
    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
};

panelProfileContent.Resize += (s, e) =>
{
    btnSaveProfile.Location = new Point(
        panelProfileContent.Width - btnSaveProfile.Width - 40,
        panelProfileContent.Height - btnSaveProfile.Height - 20
    );
};

            btnSaveProfile.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, btnSaveProfile.Width - 1, btnSaveProfile.Height - 1, 8))
                {
                    btnSaveProfile.Region = new Region(path);
                }
            };

            panelProfileContent.Controls.AddRange(new Control[] {
                picProfile, btnUploadPicture, lblFullName, txtFullName,
                lblUsername, txtUsername, lblEmail, txtEmail, lblRoleLabel, lblRole, btnSaveProfile
            });

            // ============ SECURITY TAB CONTENT ============
            panelSecurityContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(40, 30, 40, 30),
                AutoScroll = true, // Added scrolling support
                Visible = false
            };

            Label lblSecurityTitle = new Label
            {
                Text = "🔐 Update Security Credentials",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = textPrimary,
                Location = new Point(50, 100), // Shifted down
                AutoSize = true
            };

            // Current Password
            Label lblCurrentPwd = new Label { Text = "Current Password", Location = new Point(50, 170), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtCurrentPassword = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 200),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };

            // New Password
            Label lblNewPwd = new Label { Text = "New Password", Location = new Point(50, 260), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtNewPassword = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 290),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };
            txtNewPassword.TextChanged += txtNewPassword_TextChanged;

            // Password Strength
            lblPasswordStrength = new Label
            {
                Text = "",
                Location = new Point(50, 330),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9)
            };

            // Confirm Password
            Label lblConfirmPwd = new Label { Text = "Confirm New Password", Location = new Point(50, 370), AutoSize = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = textPrimary };
            txtConfirmPassword = new TextBox
            {
                Size = new Size(580, 35),
                Location = new Point(50, 400),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };

            // Show Password
            chkShowPassword = new CheckBox
            {
                Text = "Show Password",
                Location = new Point(50, 440),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = textSecondary,
                Cursor = Cursors.Hand
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                char charToUse = chkShowPassword.Checked ? '\0' : '●';
                txtCurrentPassword.PasswordChar = charToUse;
                txtNewPassword.PasswordChar = charToUse;
                txtConfirmPassword.PasswordChar = charToUse;
            };

            // Change Password Button
            btnChangePassword = new Button
            {
                Text = "🛡️ Update Password",
                Size = new Size(220, 45),
                Location = new Point(410, 470),
                BackColor = primaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChangePassword.FlatAppearance.BorderSize = 0;
            btnChangePassword.Click += btnChangePassword_Click;
            btnChangePassword.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, btnChangePassword.Width - 1, btnChangePassword.Height - 1, 8))
                {
                    btnChangePassword.Region = new Region(path);
                }
            };

            panelSecurityContent.Controls.AddRange(new Control[] {
                lblSecurityTitle, lblCurrentPwd, txtCurrentPassword,
                lblNewPwd, txtNewPassword, lblPasswordStrength,
                lblConfirmPwd, txtConfirmPassword, chkShowPassword, btnChangePassword
            });

            this.Controls.Add(panelTabButtons);
            this.Controls.Add(panelProfileContent);
            this.Controls.Add(panelSecurityContent);

            panelTabButtons.BringToFront();
            panelProfileContent.SendToBack();
            panelSecurityContent.SendToBack();

            // Add form border
            this.Paint += (s, e) =>
            {
                using (Pen p = new Pen(borderGray, 2))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            this.ResumeLayout(false);
        }

        private void ShowProfileTab()
        {
            panelProfileContent.Visible = true;
            panelSecurityContent.Visible = false;
            
            btnProfileTab.BackColor = Color.White;
            btnProfileTab.ForeColor = Color.FromArgb(59, 130, 246);
            btnProfileTab.FlatAppearance.BorderSize = 2;
            
            btnSecurityTab.BackColor = Color.FromArgb(243, 246, 249);
            btnSecurityTab.ForeColor = Color.FromArgb(107, 114, 128);
            btnSecurityTab.FlatAppearance.BorderSize = 0;
        }

        private void ShowSecurityTab()
        {
            panelProfileContent.Visible = false;
            panelSecurityContent.Visible = true;
            
            btnSecurityTab.BackColor = Color.White;
            btnSecurityTab.ForeColor = Color.FromArgb(59, 130, 246);
            btnSecurityTab.FlatAppearance.BorderSize = 2;
            
            btnProfileTab.BackColor = Color.FromArgb(243, 246, 249);
            btnProfileTab.ForeColor = Color.FromArgb(107, 114, 128);
            btnProfileTab.FlatAppearance.BorderSize = 0;
        }
    }
}
