using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ClinicManagement
{
    public partial class Users : Form
    {
        private string connectionString =
            @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        public Users()
        {
            InitializeComponent();
            SetupGrid();
            LoadStaffData();
            WireEvents();
            LoadUsersGrid();
        }

        // ================= EVENTS =================

        private void WireEvents()
        {
            btnSave.Click += BtnSave_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnNew.Click += BtnNew_Click;
            btnRefreshGrid.Click += (s, e) => LoadUsersGrid();
            btnUploadProfile.Click += BtnUploadProfile_Click;
            dgvUsers.CellDoubleClick += DgvUsers_CellDoubleClick;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            cmbStaffId.SelectedIndexChanged += (s, e) => {
                if (cmbStaffId.SelectedItem is DataRowView row) {
                    cmbStaffName.SelectedValue = row["StaffId"];
                }
            };

            cmbStaffName.SelectedIndexChanged += (s, e) => {
                if (cmbStaffName.SelectedItem is DataRowView row) {
                    cmbStaffId.SelectedValue = row["StaffId"];
                    
                    // Autofill Full Name if empty
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                        txtName.Text = row["StaffName"].ToString();
                }
            };

            lblTogglePassword.Click += (s, e) =>
            {
                if (txtPassword.Inner.PasswordChar == '•')
                {
                    txtPassword.Inner.PasswordChar = '\0'; // Show password
                    lblTogglePassword.Text = "👁️ Hide";
                }
                else
                {
                    txtPassword.Inner.PasswordChar = '•'; // Mask password
                    lblTogglePassword.Text = "👁️ Show";
                }
            };
        }

        private void BtnUploadProfile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Select Profile Picture";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Resize image to max 200x200 to save space
                        using (Image img = Image.FromFile(ofd.FileName))
                        {
                            pbProfile.Image = new Bitmap(img, new Size(200, 200));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading image: " + ex.Message);
                    }
                }
            }
        }


        // ================= VALIDATION =================

        private bool ValidateInputs()
        {
            // Full Name validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Full Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (txtName.Text.Trim().Length < 3)
            {
                MessageBox.Show("Full Name must be at least 3 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            // Father Name validation
            if (string.IsNullOrWhiteSpace(txtFatherName.Text))
            {
                MessageBox.Show("Father Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFatherName.Focus();
                return false;
            }

            if (txtFatherName.Text.Trim().Length < 3)
            {
                MessageBox.Show("Father Name must be at least 3 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFatherName.Focus();
                return false;
            }

            // Username validation
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            if (txtUsername.Text.Trim().Length < 4)
            {
                MessageBox.Show("Username must be at least 4 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            // Username format validation (allows letters, numbers, dots, underscores, and common symbols)
            if (!System.Text.RegularExpressions.Regex.IsMatch(txtUsername.Text.Trim(), @"^[a-zA-Z0-9\._\-\@\#\$]+$"))
            {
                MessageBox.Show("Username can only contain letters, numbers, dots, underscores, hyphens, and @ # $.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return false;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Password is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Text.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            // Email validation
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(txtEmail.Text.Trim(), emailPattern))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }

            // Role validation
            if (cmbRole.SelectedIndex < 0 || string.IsNullOrWhiteSpace(cmbRole.Text))
            {
                MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRole.Focus();
                return false;
            }

            // Status validation
            if (cmbStatus.SelectedIndex < 0 || string.IsNullOrWhiteSpace(cmbStatus.Text))
            {
                MessageBox.Show("Please select a status.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbStatus.Focus();
                return false;
            }

            return true;
        }

        // ================= SAVE =================

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            bool isEdit = txtUserID.Tag != null;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    // 1. Duplicate checks and Command Setup
                    if (!isEdit)
                    {
                        // Duplicate username check
                        cmd.Parameters.Clear();
                        cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username=@Username";
                        cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());

                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show("Username already exists.");
                            return;
                        }

                        cmd.Parameters.Clear();
                        cmd.CommandText = @"
                            INSERT INTO Users
                            (UserCode, FullName, FatherName, Username, PasswordHash, PlainPassword, Email, Role, Status, CreatedAt, ProfilePic, StaffId, StaffName)
                            VALUES
                            (@UserCode, @FullName,@FatherName,@Username,@Password,@PlainPassword,@Email,@Role,@Status,GETDATE(), @ProfilePic, @StaffId, @StaffName)";
                    }
                    else
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = @"
                            UPDATE Users SET
                                FullName=@FullName,
                                FatherName=@FatherName,
                                Username=@Username,
                                PasswordHash=@Password,
                                PlainPassword=@PlainPassword,
                                Email=@Email,
                                Role=@Role,
                                Status=@Status,
                                ProfilePic=@ProfilePic,
                                StaffId=@StaffId,
                                StaffName=@StaffName
                            WHERE UserID=@UserID";
                            
                        cmd.Parameters.AddWithValue("@UserID", txtUserID.Tag);
                    }

                    // 2. Set Parameters and Execute
                    cmd.Parameters.AddWithValue("@UserCode", txtUserID.Text.Trim());
                    cmd.Parameters.AddWithValue("@FullName", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@FatherName", txtFatherName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                    
                    // Image to Byte Array
                    if (pbProfile.Image != null)
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            pbProfile.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            cmd.Parameters.Add("@ProfilePic", SqlDbType.VarBinary).Value = ms.ToArray();
                        }
                    }
                    else
                    {
                        cmd.Parameters.Add("@ProfilePic", SqlDbType.VarBinary).Value = DBNull.Value;
                    }
                    
                    string plainPassword = txtPassword.Text.Trim();
                    string hashedPassword = ComputeSha256Hash(plainPassword);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@PlainPassword", plainPassword);
                    cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@Role", cmbRole.Text);
                    cmd.Parameters.AddWithValue("@Status", cmbStatus.Text);
                    
                    var pStaffId = new SqlParameter("@StaffId", SqlDbType.Int);
                    pStaffId.Value = cmbStaffId.SelectedValue ?? DBNull.Value;
                    cmd.Parameters.Add(pStaffId);
                    
                    cmd.Parameters.AddWithValue("@StaffName", cmbStaffName.Text);

                    cmd.ExecuteNonQuery();
                }

                LoadUsersGrid();
                ClearForm();
                MessageBox.Show("User saved successfully.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error saving user: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================= EDIT =================

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (txtUserID.Tag != null)
            {
                // If a user is already selected (edit mode), clicking "Update Info" will trigger the save logic
                BtnSave_Click(sender, e);
            }
            else
            {
                // If no user is selected, try to load the selected user from the grid
                if (dgvUsers.CurrentRow == null)
                {
                    MessageBox.Show("Please select a user from the grid first to edit.", "Selection Required");
                    return;
                }

                DgvUsers_CellDoubleClick(
                    dgvUsers,
                    new DataGridViewCellEventArgs(0, dgvUsers.CurrentRow.Index));
            }
        }

        // ================= DELETE =================

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (txtUserID.Tag == null)
            {
                MessageBox.Show("Select a user to delete.");
                return;
            }

            if (MessageBox.Show("Are you sure?", "Confirm",
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                        "DELETE FROM Users WHERE UserID=@id", con);
                    cmd.Parameters.AddWithValue("@id", txtUserID.Tag);
                    cmd.ExecuteNonQuery();
                }

                LoadUsersGrid();
                ClearForm();
                MessageBox.Show("User deleted.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting user: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================= NEW =================

        private void BtnNew_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        // ================= GRID DOUBLE CLICK =================

        private void DgvUsers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvUsers.Rows[e.RowIndex];

            txtUserID.Tag = row.Cells["UserID"].Value; // Use Tag to store primary key
            txtUserID.Text = row.Cells["UserCode"].Value.ToString();
            txtName.Text = row.Cells["FullName"].Value.ToString();
            txtFatherName.Text = row.Cells["FatherName"].Value.ToString();
            txtUsername.Text = row.Cells["Username"].Value.ToString();
            txtEmail.Text = row.Cells["Email"].Value.ToString();
            cmbRole.Text = row.Cells["Role"].Value.ToString();
            cmbStatus.Text = row.Cells["Status"].Value.ToString();
            
            if (row.Cells["StaffId"].Value != DBNull.Value)
            {
                cmbStaffId.SelectedValue = row.Cells["StaffId"].Value;
                cmbStaffName.SelectedValue = row.Cells["StaffId"].Value;
            }
            else
            {
                cmbStaffId.SelectedIndex = -1;
                cmbStaffName.SelectedIndex = -1;
            }

            // Load password from PlainPassword column
            // Load password and Profile Pic
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT PlainPassword, ProfilePic FROM Users WHERE UserID=@id", con);
                cmd.Parameters.AddWithValue("@id", txtUserID.Tag);
                
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtPassword.Text = reader["PlainPassword"]?.ToString();
                        
                        if (reader["ProfilePic"] != DBNull.Value)
                        {
                            byte[] imgData = (byte[])reader["ProfilePic"];
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(imgData))
                            {
                                pbProfile.Image = Image.FromStream(ms);
                            }
                        }
                        else
                        {
                            pbProfile.Image = null;
                        }
                    }
                }
            }
        }

        // ================= LOAD GRID =================

        private void SetupGrid()
        {
            if (dgvUsers == null) return;

            dgvUsers.AutoGenerateColumns = true;
            dgvUsers.Columns.Clear();
        }

        private void LoadUsersGrid()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT UserID, UserCode, FullName, FatherName, Username, PlainPassword, Email, Role, Status, StaffId, StaffName, CreatedAt FROM Users ORDER BY CreatedAt ASC", con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvUsers.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading users: " + ex.Message);
                }
            }
        }

        // ================= SEARCH =================

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // If the search text is the placeholder, treat it as empty
            string searchText = txtSearch.Text.Trim();
            if (searchText == "Search users...") searchText = "";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT UserID, UserCode, FullName, FatherName, Username, PlainPassword, Email, Role, Status, StaffId, StaffName, CreatedAt
                    FROM Users
                    WHERE (FullName LIKE @s
                       OR FatherName LIKE @s
                       OR Username LIKE @s
                       OR UserCode LIKE @s
                       OR Email LIKE @s
                       OR Role LIKE @s
                       OR Status LIKE @s)
                    ORDER BY CreatedAt ASC";
                
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@s", "%" + searchText + "%");
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvUsers.DataSource = dt;
            }
        }

        // ================= CLEAR =================

        private void ClearForm()
        {
            txtUserID.Tag = null; // Clear primary key
            txtUserID.Text = FetchNextUserCode();
            txtName.Clear();
            txtFatherName.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            txtEmail.Clear();
            cmbRole.SelectedIndex = 0;
            cmbStatus.SelectedIndex = -1;
            cmbStaffId.SelectedIndex = -1;
            cmbStaffName.SelectedIndex = -1;
            pbProfile.Image = null;
        }

        private string FetchNextUserCode()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ISNULL(IDENT_CURRENT('Users'), 0) + ISNULL(IDENT_INCR('Users'), 1)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        long nextId = Convert.ToInt64(result);
                        return "U" + nextId.ToString("D3");
                    }
                }
            }
            catch { }
            return "U---";
        }

        // ================= PASSWORD HASHING =================

        private void LoadStaffData()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT StaffId, StaffCode, StaffName FROM Staff ORDER BY StaffName ASC", con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Setup ID ComboBox
                    cmbStaffId.DisplayMember = "StaffCode";
                    cmbStaffId.ValueMember = "StaffId";
                    cmbStaffId.DataSource = dt.Copy();
                    cmbStaffId.SelectedIndex = -1;

                    // Setup Name ComboBox
                    cmbStaffName.DisplayMember = "StaffName";
                    cmbStaffName.ValueMember = "StaffId";
                    cmbStaffName.DataSource = dt.Copy();
                    cmbStaffName.SelectedIndex = -1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading staff data: " + ex.Message);
                }
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
