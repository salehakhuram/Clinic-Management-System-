namespace ClinicManagement
{
    partial class Login : Form
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Modern Color Palette
        private readonly Color primaryColor = Color.FromArgb(20, 60, 90);
        private readonly Color accentColor = Color.FromArgb(0, 180, 170);
        private readonly Color textPrimary = Color.FromArgb(30, 40, 55);
        private readonly Color textSecondary = Color.FromArgb(100, 115, 130);

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            pictureBox1 = new PictureBox();
            panel1 = new Panel();
            label2 = new Label();
            comboBox1 = new ComboBox();
            txtPass = new RoundedTextBox();
            txtUser = new RoundedTextBox();
            iconLogo = new FontAwesome.Sharp.IconPictureBox();
            button1 = new Button();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)iconLogo).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1100, 700);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.White; // Fill gaps with white if image doesn't fill
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(230, 255, 255, 255); // Slightly more transparent glass
            panel1.Controls.Add(label2);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(txtPass);
            panel1.Controls.Add(txtUser);
            panel1.Controls.Add(iconLogo);
            panel1.Controls.Add(button1);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(350, 100);
            panel1.Name = "panel1";
            panel1.Size = new Size(400, 520);
            panel1.TabIndex = 1;
            panel1.Paint += panel1_Paint;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.White;
            label2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label2.ForeColor = accentColor;
            label2.Location = new Point(270, 233);
            label2.Name = "label2";
            label2.Size = new Size(65, 25);
            label2.TabIndex = 5;
            label2.Text = "👁️ Show";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            label2.Cursor = Cursors.Hand;
            label2.BackColor = Color.White;
            label2.ForeColor = accentColor;
            label2.Click += label2_Click;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.Font = new Font("Segoe UI", 11F);
            comboBox1.ForeColor = textPrimary;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(40, 295);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(320, 45);
            comboBox1.TabIndex = 4;
            // 
            // txtPass
            // 
            txtPass.BorderColor = Color.Gainsboro;
            txtPass.BorderFocusColor = accentColor;
            txtPass.BorderRadius = 12;
            txtPass.Font = new Font("Segoe UI", 11F);
            txtPass.Location = new Point(40, 225);
            txtPass.Name = "txtPass";
            txtPass.PlaceholderColor = Color.Gray;
            txtPass.PlaceholderText = "Password";
            txtPass.Size = new Size(320, 45);
            txtPass.TabIndex = 3;
            // 
            // txtUser
            // 
            txtUser.BorderColor = Color.Gainsboro;
            txtUser.BorderFocusColor = accentColor;
            txtUser.BorderRadius = 12;
            txtUser.Font = new Font("Segoe UI", 11F);
            txtUser.Location = new Point(40, 155);
            txtUser.Name = "txtUser";
            txtUser.PlaceholderColor = Color.Gray;
            txtUser.PlaceholderText = "Username";
            txtUser.Size = new Size(320, 45);
            txtUser.TabIndex = 2;
            // 
            // iconLogo
            // 
            iconLogo.BackColor = Color.Transparent;
            iconLogo.ForeColor = primaryColor;
            iconLogo.IconChar = FontAwesome.Sharp.IconChar.Stethoscope;
            iconLogo.IconColor = primaryColor;
            iconLogo.IconFont = FontAwesome.Sharp.IconFont.Auto;
            iconLogo.IconSize = 60;
            iconLogo.Location = new Point(170, 45);
            iconLogo.Name = "iconLogo";
            iconLogo.Size = new Size(60, 60);
            iconLogo.TabIndex = 6;
            iconLogo.TabStop = false;
            // 
            // button1
            // 
            button1.BackColor = primaryColor;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            button1.ForeColor = Color.White;
            button1.Location = new Point(40, 390);
            button1.Name = "button1";
            button1.Size = new Size(300, 55);
            button1.TabIndex = 1;
            button1.Text = "Login to System";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click_1;
            button1.Cursor = Cursors.Hand;
            // 
            // label1
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            label1.ForeColor = primaryColor;
            label1.Location = new Point(0, 105);
            label1.Name = "label1";
            label1.Size = new Size(400, 48);
            label1.TabIndex = 0;
            label1.Text = "AL REHMAN CLINIC";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Login
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 700);
            Controls.Add(panel1);
            Controls.Add(pictureBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Login";
            this.Text = "Clinic Management - Login";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)iconLogo).EndInit();
            ResumeLayout(false);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int radius = 20;
                int d = radius * 2;
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(panel1.Width - d, 0, d, d, 270, 90);
                path.AddArc(panel1.Width - d, panel1.Height - d, d, d, 0, 90);
                path.AddArc(0, panel1.Height - d, d, d, 90, 90);
                path.CloseFigure();
                panel1.Region = new Region(path);
                using (Pen pen = new Pen(Color.FromArgb(200, 220, 230), 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        #endregion

        private PictureBox pictureBox1;
        private Panel panel1;
        private ComboBox comboBox1;
        private RoundedTextBox txtPass;
        private RoundedTextBox txtUser;
        private FontAwesome.Sharp.IconPictureBox iconLogo;
        private Button button1;
        private Label label1;
        private Label label2;
    }
}
