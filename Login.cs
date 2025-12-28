using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace ClinicManagement
{
    public partial class Login : Form
    {
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";

        public Login()
        {
            InitializeComponent();
          
            // Initialize placeholders and combo box
            SetPlaceholders();
            
            label2.BackColor = Color.White; // Match textbox background
            label2.Cursor = Cursors.Hand;
            label2.BringToFront();

            // Programmatically center the panel
            CenterLoginPanel();
            this.Resize += (s, e) => CenterLoginPanel();
        }

        private void CenterLoginPanel()
        {
            panel1.Location = new Point(
                (this.ClientSize.Width - panel1.Width) / 2,
                (this.ClientSize.Height - panel1.Height) / 2
            );
            panel1.BringToFront();
            pictureBox1.SendToBack();
        }

        private void SetPlaceholders()
        {
            txtUser.PlaceholderText = "Username";
            txtPass.PlaceholderText = "Password";
            txtPass.Inner.UseSystemPasswordChar = true;

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(new string[] { "Admin", "Doctor", "Receptionist", "Pharmacist" });
            comboBox1.SelectedIndex = 0;
        }

        // Removed legacy placeholder methods as RoundedTextBox handles them
        

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private string ValidateLogin(string username, string password, string role)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Select the Status to check if the account is active
                SqlCommand cmd = new SqlCommand(@"
                    SELECT Status
                    FROM Users
                    WHERE LTRIM(RTRIM(Username)) = @Username
                      AND LTRIM(RTRIM(PasswordHash)) = @Password
                      AND LOWER(Role) = LOWER(@Role)", conn);

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@Role", role);

                object statusResult = cmd.ExecuteScalar();
                
                if (statusResult == null)
                {
                    return "Invalid";
                }

                string status = statusResult.ToString();
                if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    return "Inactive";
                }

                return "Success";
            }
        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Inner.Text;   
            string role = comboBox1.SelectedItem?.ToString();

            // Username validation
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter your username.", "Login Error");
                return;
            }

            if (username.Length < 4)
            {
                MessageBox.Show("Username must be at least 4 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Username format validation (same as Users form)
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9\._\-\@\#\$]+$"))
            {
                MessageBox.Show("Username can only contain letters, numbers, dots, underscores, hyphens, and @ # $.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(password) || password == "Password")
            {
                MessageBox.Show("Please enter your password.", "Login Error");
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Please select a role.", "Login Error");
                return;
            }

            // Hashing the password for comparison
            string hashedPassword = ComputeSha256Hash(password);

            string loginResult = ValidateLogin(username, hashedPassword, role);

            if (loginResult == "Success")
            {
                // Set Session
                Session.CurrentUser = username;
                Session.Role = role;

                MessageBox.Show($"Login Successful! Welcome {role} {username}", "Success");

                this.Hide();

                if (role == "Admin")
                    new AdminDashboard(username).Show();
                else if (role == "Receptionist")
                    new ReceptionistDashboard(username).Show();
                else if (role == "Doctor")
                    new DoctorDashboard(username).Show();
                else if (role == "Pharmacist")
                    new PharmacistDashboard(username).Show();
                else
                {
                    MessageBox.Show("Selected role is not configured.");
                    this.Show();
                }
            }
            else if (loginResult == "Inactive")
            {
                MessageBox.Show("Your account is currently inactive. Please contact the administrator.", "Account Disabled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Invalid username, password, or role.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool passwordVisible = false;
        private void label2_Click(object sender, EventArgs e)
        {
            passwordVisible = !passwordVisible;
            txtPass.Inner.UseSystemPasswordChar = !passwordVisible;
            
            if (passwordVisible)
            {
                label2.Text = "👁️ Hide";
            }
            else
            {
                label2.Text = "👁️ Show";
            }
        }
    }
}

