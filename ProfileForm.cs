using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class ProfileForm : Form
    {
        private string loggedInUsername;
        private string userRole;
        private string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";
        private int currentUserId;
        private Action onProfileUpdated;
        
        // Tab state
        private bool isProfileTabActive = true;
        
        // Colors (matching Admin Dashboard)
        private Color primaryBlue = Color.FromArgb(59, 130, 246);
        private Color accentGreen = Color.FromArgb(16, 185, 129);
        private Color textPrimary = Color.FromArgb(31, 41, 55);
        private Color textSecondary = Color.FromArgb(107, 114, 128);
        private Color surfaceColor = Color.FromArgb(243, 246, 249);
        private Color borderGray = Color.FromArgb(229, 231, 235);
        
        public ProfileForm(string username, string role, Action onUpdated = null, bool startWithSecurity = false)
        {
            this.loggedInUsername = username;
            this.userRole = role;
            this.onProfileUpdated = onUpdated;
            InitializeComponent();
            LoadUserProfile();
            
            if (startWithSecurity)
            {
                ShowSecurityTab();
            }
        }

        private void LoadUserProfile()
        {
            try
            {
                string query = @"
                    SELECT UserId, FullName, Email, Username, ProfilePic 
                    FROM Users 
                    WHERE Username = @Username";
 
                using (var con = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", loggedInUsername);
                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : 0;
                                txtFullName.Text = reader["FullName"]?.ToString() ?? "";
                                txtEmail.Text = reader["Email"]?.ToString() ?? "";
                                txtUsername.Text = reader["Username"]?.ToString() ?? "";
                                
                                // Load profile picture if exists
                                if (reader["ProfilePic"] != DBNull.Value)
                                {
                                    byte[] imageData = (byte[])reader["ProfilePic"];
                                    using (var ms = new MemoryStream(imageData))
                                    {
                                        picProfile.Image = Image.FromStream(ms);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error loading profile: " + ex.Message, false);
            }
        }

        private void btnSaveProfile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowToast("Please enter your full name", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                ShowToast("Username cannot be empty", false);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !txtEmail.Text.Contains("@"))
            {
                ShowToast("Please enter a valid email address", false);
                return;
            }

            try
            {
                string query = @"
                    UPDATE Users 
                    SET FullName = @FullName, 
                        Email = @Email,
                        Username = @NewUsername
                    WHERE UserId = @UserId";

                using (var con = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FullName", txtFullName.Text);
                        cmd.Parameters.AddWithValue("@Email", txtEmail.Text ?? "");
                        cmd.Parameters.AddWithValue("@NewUsername", txtUsername.Text);
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        
                        con.Open();
                        int result = cmd.ExecuteNonQuery();
                        
                        if (result > 0)
                        {
                            loggedInUsername = txtUsername.Text; // Important: update tracking variable
                            ShowToast("Profile updated successfully!", true);
                            onProfileUpdated?.Invoke();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error updating profile: " + ex.Message, false);
            }
        }

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtCurrentPassword.Text))
            {
                ShowToast("Please enter your current password", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewPassword.Text))
            {
                ShowToast("Please enter a new password", false);
                return;
            }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                ShowToast("New password and confirmation do not match", false);
                return;
            }

            if (txtNewPassword.Text.Length < 6)
            {
                ShowToast("Password must be at least 6 characters", false);
                return;
            }

            try
            {
                // Verify current password
                string verifyQuery = "SELECT PlainPassword FROM Users WHERE Username = @Username";
                string currentHashedPassword = "";
                
                using (var con = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand(verifyQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", loggedInUsername);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            currentHashedPassword = result.ToString();
                    }
                }

                // Simple password check (in production, use proper hashing)
                if (currentHashedPassword != txtCurrentPassword.Text)
                {
                    ShowToast("Current password is incorrect", false);
                    return;
                }

                // Compute Hash
                string passwordHash = ComputeSha256Hash(txtNewPassword.Text);

                // Update password
                    string updateQuery = @"
                    UPDATE Users 
                    SET PlainPassword = @NewPassword,
                        PasswordHash = @PasswordHash
                    WHERE UserId = @UserId";

                using (var con = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@NewPassword", txtNewPassword.Text);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@UserId", currentUserId);
                        
                        con.Open();
                        int rows = cmd.ExecuteNonQuery();
                        
                        if (rows > 0)
                        {
                            ShowToast("Password changed successfully!", true);
                            
                            // Clear password fields
                            txtCurrentPassword.Clear();
                            txtNewPassword.Clear();
                            txtConfirmPassword.Clear();
                        }
                        else
                        {
                            ShowToast("Error: Password update failed. User not found.", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error changing password: " + ex.Message, false);
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private void btnUploadPicture_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Select Profile Picture";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Image img = Image.FromFile(ofd.FileName);
                        picProfile.Image = img;
                        
                        // Save to database
                        byte[] imageData;
                        using (var ms = new MemoryStream())
                        {
                            img.Save(ms, img.RawFormat);
                            imageData = ms.ToArray();
                        }

                        string query = "UPDATE Users SET ProfilePic = @Picture WHERE Username = @Username";
                        using (var con = new SqlConnection(connectionString))
                        {
                            using (var cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@Picture", imageData);
                                cmd.Parameters.AddWithValue("@Username", loggedInUsername);
                                con.Open();
                                cmd.ExecuteNonQuery();
                            }
                        }
                        
                        ShowToast("Profile picture updated!", true);
                        onProfileUpdated?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ShowToast("Error uploading picture: " + ex.Message, false);
                    }
                }
            }
        }

        private void ShowToast(string message, bool isSuccess)
        {
            // Create toast notification panel
            Panel toast = new Panel
            {
                Size = new Size(350, 60),
                BackColor = isSuccess ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 226, 226),
                Location = new Point(this.Width - 370, 20)
            };

            Label lblMessage = new Label
            {
                Text = isSuccess ? "✓ " + message : "✗ " + message,
                ForeColor = isSuccess ? Color.FromArgb(22, 101, 52) : Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI Semibold", 10),
                AutoSize = false,
                Size = new Size(330, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            toast.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectPath(0, 0, toast.Width - 1, toast.Height - 1, 8))
                {
                    toast.Region = new Region(path);
                    using (Pen pen = new Pen(isSuccess ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 165, 165)))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            toast.Controls.Add(lblMessage);
            this.Controls.Add(toast);
            toast.BringToFront();

            // Auto-hide after 3 seconds
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 3000;
            timer.Tick += (s, e) =>
            {
                this.Controls.Remove(toast);
                toast.Dispose();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void txtNewPassword_TextChanged(object sender, EventArgs e)
        {
            UpdatePasswordStrength();
        }

        private void UpdatePasswordStrength()
        {
            string password = txtNewPassword.Text;
            int strength = 0;

            if (password.Length >= 6) strength++;
            if (password.Length >= 10) strength++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")) strength++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*]")) strength++;

            if (strength <= 1)
            {
                lblPasswordStrength.Text = "Weak";
                lblPasswordStrength.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else if (strength <= 3)
            {
                lblPasswordStrength.Text = "Medium";
                lblPasswordStrength.ForeColor = Color.FromArgb(245, 158, 11);
            }
            else
            {
                lblPasswordStrength.Text = "Strong";
                lblPasswordStrength.ForeColor = accentGreen;
            }
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
