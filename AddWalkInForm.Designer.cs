using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace ClinicManagement
{
    partial class AddWalkInForm
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtSearch;
        private ListBox lstResults;
        private ComboBox cmbDoctor;
        private Label lblTokenValue;
        private IconButton btnSearch;
        private Button btnAdd;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Add Walk-In Patient";
            this.Size = new Size(500, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int y = 20;

            Label lblTitle = new Label { Text = "Add Existing Patient to Queue", Font = new Font("Segoe UI Bold", 14), Location = new Point(25, y), Size = new Size(450, 35), ForeColor = Color.FromArgb(17, 24, 39) };
            y += 50;

            Label lblSearch = new Label { Text = "Step 1: Search Patient (Name/Phone/ID)", Font = new Font("Segoe UI Semibold", 10), Location = new Point(25, y), AutoSize = true, ForeColor = Color.FromArgb(75, 85, 99) };
            y += 25;

            txtSearch = new TextBox { Location = new Point(25, y), Size = new Size(380, 25), Font = new Font("Segoe UI", 11) };
            btnSearch = new IconButton { IconChar = IconChar.Search, IconSize = 20, IconColor = Color.White, BackColor = Color.FromArgb(59, 130, 246), Width = 45, Height = 29, Location = new Point(410, y-1), FlatStyle = FlatStyle.Flat };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;
            y += 40;

            lstResults = new ListBox { Location = new Point(25, y), Size = new Size(430, 120), Font = new Font("Segoe UI", 10) };
            y += 140;

            Label lblDoctor = new Label { Text = "Step 2: Select Doctor", Font = new Font("Segoe UI Semibold", 10), Location = new Point(25, y), AutoSize = true, ForeColor = Color.FromArgb(75, 85, 99) };
            y += 25;

            cmbDoctor = new ComboBox { Location = new Point(25, y), Size = new Size(430, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            y += 50;

            Label lblToken = new Label { Text = "Step 3: Assign Token", Font = new Font("Segoe UI Semibold", 10), Location = new Point(25, y), AutoSize = true, ForeColor = Color.FromArgb(75, 85, 99) };
            y += 25;

            lblTokenValue = new Label { Text = "#000", Font = new Font("Segoe UI Bold", 24), Location = new Point(25, y), Size = new Size(430, 50), ForeColor = Color.FromArgb(37, 99, 235), TextAlign = ContentAlignment.MiddleCenter };
            y += 70;

            btnAdd = new Button { 
                Text = "Add to Queue", 
                Location = new Point(265, y), 
                Size = new Size(190, 45), 
                BackColor = Color.FromArgb(22, 163, 74), 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Bold", 11),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnCancel = new Button { 
                Text = "Cancel", 
                Location = new Point(25, y), 
                Size = new Size(220, 45), 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { lblTitle, lblSearch, txtSearch, btnSearch, lstResults, lblDoctor, cmbDoctor, lblToken, lblTokenValue, btnAdd, btnCancel });
        }
    }
}
