using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public partial class Billing : Form
    {
        private bool isPharmacistMode = false;

        public Billing(bool isPharmacist = false)
        {
            InitializeComponent();
            this.isPharmacistMode = isPharmacist;
            SetupGrid();
            LoadPatients();
            LoadDoctors();
            LoadAppointments();
            WireEvents();
            
            if (isPharmacistMode) 
            {
               // For Pharmacist, maybe hide the Appointment ComboBox or Consultation logic?
               // User said "cannot deal with billing of appointments"
               // The LoadAppointments still loads them so they can pick a patient's prescription?
               // Or should we hide it?
               // For now, we will use the flag to prevent adding Consultation Fees.
            }
        }

        private int? currentAppointmentIntId = null;
        private int? currentDoctorId = null;
        private decimal currentConsultationFee = 0;

        private readonly string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;Encrypt=False;";

        private void SetupGrid()
        {
            dgvBillItems.AutoGenerateColumns = false;
            dgvBillItems.Columns.Clear();

            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "Description", Width = 250, ReadOnly = true });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category (Consultation / Medicine)", Width = 150, ReadOnly = true });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Unit Price", Width = 100, ReadOnly = true });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Quantity", Width = 80, ReadOnly = false });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", Width = 120, ReadOnly = true });
            dgvBillItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemRefId", HeaderText = "RefId", Visible = false });
            
            dgvBillItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void WireEvents()
        {
            btnSaveBill.Click += BtnSaveBill_Click;
            btnClear.Click += (s, e) => ClearAll();
            btnRefresh.Click += (s, e) => { if (cmbPatientID.SelectedItem != null) LoadPatientVisit(((dynamic)cmbPatientID.SelectedItem).Value.ToString()); };
            btnAddMedicine.Click += (s, e) => {
                using (var dlg = new SelectMedicineDialog())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        AddItem(dlg.SelectedMedicineName, dlg.SelectedCategory, (double)dlg.SelectedPrice, dlg.SelectedQuantity, dlg.SelectedId);
                    }
                }
            };
            btnPrint.Click += (s, e) => {
                if (dgvBillItems.Rows.Count == 0) { MessageBox.Show("No items to print."); return; }
                printPreviewDialog1.Document = printDocument1;
                printPreviewDialog1.ShowDialog();
            };
            btnViewHistory.Click += (s, e) => {
                var reportsForm = new Reports();
                reportsForm.Show();
            };
            printDocument1.PrintPage += PrintDocument1_PrintPage;
            
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    SearchPatient(txtSearch.Text);
                    e.SuppressKeyPress = true;
                }
            };

            dgvBillItems.CellValueChanged += (s, e) => {
                if (e.RowIndex >= 0 && (e.ColumnIndex == dgvBillItems.Columns["Qty"].Index || e.ColumnIndex == dgvBillItems.Columns["Price"].Index)) {
                    UpdateRowSubtotal(e.RowIndex);
                    UpdateTotals();
                }
            };

            txtDiscount.Inner.TextChanged += (s, e) => UpdateTotals();

            cmbDoctorName.SelectedIndexChanged += (s, e) => {
                if (cmbDoctorName.SelectedItem != null) {
                    dynamic selected = cmbDoctorName.SelectedItem;
                    currentDoctorId = selected.Value;
                    string name = selected.Text;
                    decimal fee = FetchDoctorFee((int)currentDoctorId);
                    AddConsultationFee(name, fee);
                }
            };

            cmbPatientID.SelectedIndexChanged += (s, e) => {
                if (cmbPatientID.SelectedItem != null) {
                    dynamic selected = cmbPatientID.SelectedItem;
                    LoadPatientVisit(selected.Value.ToString());
                }
            };
        }

        private decimal FetchDoctorFee(int doctorId)
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = "SELECT ConsultationFee FROM Doctors WHERE DoctorId = @id";
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@id", doctorId);
                    object res = cmd.ExecuteScalar();
                    return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 1500;
                }
            } catch { return 1500; }
        }

        private void AddConsultationFee(string doctorName, decimal fee)
        {
            // Remove existing consultation if any
            for (int i = dgvBillItems.Rows.Count - 1; i >= 0; i--) {
                var cat = dgvBillItems.Rows[i].Cells["Category"].Value?.ToString();
                if (cat == "Consultation") {
                    dgvBillItems.Rows.RemoveAt(i);
                }
            }

            // Insert at the top
            dgvBillItems.Rows.Insert(0, $"Consultation - {doctorName}", "Consultation", fee.ToString("F2"), 1, fee.ToString("F2"), currentDoctorId);
            dgvBillItems.Rows[0].ReadOnly = true;
            dgvBillItems.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(245, 250, 255);
            UpdateTotals();
        }

        private void UpdateRowSubtotal(int rowIndex)
        {
            try {
                var row = dgvBillItems.Rows[rowIndex];
                double price = Convert.ToDouble(row.Cells["Price"].Value ?? 0);
                int qty = Convert.ToInt32(row.Cells["Qty"].Value ?? 0);
                row.Cells["Subtotal"].Value = (price * qty).ToString("F2");
            } catch { }
        }

        private void LoadPatients()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientId, PatientCode, PatientName FROM Patients ORDER BY PatientName";
                    var cmd = new SqlCommand(query, con);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            cmbPatientID.Items.Add(new { Text = dr["PatientCode"].ToString(), Value = dr["PatientId"] });
                            cmbPatientName.Items.Add(new { Text = dr["PatientName"].ToString(), Value = dr["PatientId"] });
                        }
                    }
                }
                cmbPatientID.DisplayMember = "Text";
                cmbPatientID.ValueMember = "Value";
                cmbPatientName.DisplayMember = "Text";
                cmbPatientName.ValueMember = "Value";
                
                // Wire selection events
                cmbPatientID.SelectedIndexChanged += (s, e) => SyncPatientSelection(cmbPatientID, cmbPatientName);
                cmbPatientName.SelectedIndexChanged += (s, e) => SyncPatientSelection(cmbPatientName, cmbPatientID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading patients: " + ex.Message);
            }
        }

        private void SyncPatientSelection(ComboBox source, ComboBox target)
        {
            if (source.SelectedItem != null)
            {
                dynamic selected = source.SelectedItem;
                int patientId = selected.Value;
                
                for (int i = 0; i < target.Items.Count; i++)
                {
                    dynamic item = target.Items[i];
                    if (item.Value == patientId)
                    {
                        target.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadDoctors()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // REMOVED FILTER: Show ALL doctors to prevent empty list issues
                    string query = "SELECT DoctorId, DoctorName FROM Doctors ORDER BY DoctorName";
                    var cmd = new SqlCommand(query, con);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            cmbDoctorName.Items.Add(new { Text = dr["DoctorName"].ToString(), Value = dr["DoctorId"] });
                        }
                    }
                }
                cmbDoctorName.DisplayMember = "Text";
                cmbDoctorName.ValueMember = "Value";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading doctors: " + ex.Message);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Show appointments from Last 7 Days so user can see data
                    string query = @"SELECT A.AppointmentIntId, A.AppointmentCode, A.PatientName, D.DoctorName, A.PatientId, D.DoctorId, 
                                           ISNULL(B.PaymentStatus, 'Unpaid') as PaymentStatus, A.AppointmentDate
                                    FROM Appointments A
                                    LEFT JOIN Doctors D ON A.DoctorID = D.DoctorID
                                    LEFT JOIN Bills B ON A.AppointmentIntId = B.AppointmentIntId
                                    WHERE CAST(A.AppointmentDate AS DATE) >= CAST(DATEADD(day, -7, GETDATE()) AS DATE)
                                    AND ISNULL(B.PaymentStatus, 'Unpaid') != 'Paid'
                                    AND (ISNULL(A.PaymentStatus, 'Unpaid') != 'Paid' AND A.PaymentStatus != 'paid')
                                    AND A.Status != 'Paid'
                                    ORDER BY A.AppointmentDate DESC, A.AppointmentTime";
                    var cmd = new SqlCommand(query, con);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string code = dr["AppointmentCode"]?.ToString() ?? "A" + dr["AppointmentIntId"];
                            string status = dr["PaymentStatus"]?.ToString() ?? "Unpaid";
                            string statusIcon = status == "Paid" ? "✅" : "⚠";

                            cmbAppointmentCode.Items.Add(new { 
                                Text = $"{statusIcon} {code} - {dr["PatientName"]} ({status})", 
                                Value = dr["AppointmentIntId"],
                                PatientId = dr["PatientId"],
                                DoctorId = dr["DoctorId"]
                            });
                        }
                    }
                }
                cmbAppointmentCode.DisplayMember = "Text";
                cmbAppointmentCode.ValueMember = "Value";

                // Wire appointment selection event
                cmbAppointmentCode.SelectedIndexChanged += (s, e) =>
                {
                    if (cmbAppointmentCode.SelectedItem != null)
                    {
                        dynamic selected = cmbAppointmentCode.SelectedItem;
                        currentAppointmentIntId = selected.Value;
                        
                        // Auto-select patient and doctor
                        for (int i = 0; i < cmbPatientID.Items.Count; i++)
                        {
                            dynamic item = cmbPatientID.Items[i];
                            if (item.Value == selected.PatientId)
                            {
                                cmbPatientID.SelectedIndex = i;
                                break;
                            }
                        }
                        
                        for (int i = 0; i < cmbDoctorName.Items.Count; i++)
                        {
                            dynamic item = cmbDoctorName.Items[i];
                            if (item.Value == selected.DoctorId)
                            {
                                cmbDoctorName.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                };
            }
            catch (SqlException ex) when (ex.Number == 207) // Invalid Column Name
            {
                MessageBox.Show("Database schema update required!\n\nPlease run the 'Migration.sql' script to add missing columns:\n- AppointmentIntId\n- PaymentStatus\n\nError: " + ex.Message, 
                    "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments: " + ex.Message);
            }
        }

        private void SearchPatient(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return;

            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    // Search for latest active visit for patient name or ID (not just completed)
                    string q = @"SELECT TOP 1 V.VisitID, P.PatientID, P.PatientName, V.DoctorName, V.AppointmentIntId, D.ConsultationFee
                               FROM Visits V 
                               JOIN Patients P ON V.PatientID = P.PatientID 
                               LEFT JOIN Doctors D ON V.DoctorId = D.DoctorId
                               WHERE (P.PatientName LIKE @search OR P.Phone LIKE @search OR P.PatientCode LIKE @search OR CAST(P.PatientID AS NVARCHAR) = @search)
                               AND V.Status != 'CANCELLED'
                               ORDER BY V.VisitDate DESC";
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        if (dr.Read()) {
                            int patientId = Convert.ToInt32(dr["PatientID"]);
                            currentAppointmentIntId = dr["AppointmentIntId"] != DBNull.Value ? (int?)dr["AppointmentIntId"] : null;
                            currentConsultationFee = dr["ConsultationFee"] != DBNull.Value ? Convert.ToDecimal(dr["ConsultationFee"]) : 0;
                            if (currentConsultationFee == 0) currentConsultationFee = 1500; // Default fallback
                           dynamic selectedPatient = cmbPatientID.SelectedItem;
                           if (selectedPatient != null)
                           {
                               // Set the selected patient based on search
                               for (int i = 0; i < cmbPatientID.Items.Count; i++)
                               {
                                   dynamic item = cmbPatientID.Items[i];
                                   if (item.Value == patientId)
                                   {
                                       cmbPatientID.SelectedIndex = i;
                                       break;
                                   }
                               }
                           }
                           if (cmbDoctorName.Items.Count > 0)
                           {
                               string doctorName = dr["DoctorName"].ToString() ?? "";
                               for (int i = 0; i < cmbDoctorName.Items.Count; i++)
                               {
                                   dynamic item = cmbDoctorName.Items[i];
                                   if (item.Text == doctorName)
                                   {
                                       cmbDoctorName.SelectedIndex = i;
                                       break;
                                   }
                               }
                           }
                            LoadPrescriptions(patientId);
                        } else {
                            // If no visit found, still check for dispensed medicines for this patient?
                            // Try to find the patient and load their dispensed meds anyway
                            int pId = TryGetPatientId(searchTerm);
                            if (pId > 0) LoadPrescriptions(pId);
                            else MessageBox.Show("No patient or visit found for this search.");
                        }
                    }
                }
            } catch (Exception ex) { MessageBox.Show("Search Error: " + ex.Message); }
        }

        private void LoadPatientVisit(string patientId)
        {
             SearchPatient(patientId);
        }

        private void LoadPrescriptions(int patientId)
        {
            dgvBillItems.Rows.Clear();
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    
                    // 1. Trigger consultation fee for the latest visit if applicable
                    if (!isPharmacistMode && cmbDoctorName.SelectedItem != null) {
                        dynamic selected = cmbDoctorName.SelectedItem;
                        AddConsultationFee(selected.Text, FetchDoctorFee(selected.Value));
                    }

                    // 2. Load ALL dispensed medicines for this patient that are NOT YET BILLED
                    // Note: Use m.UnitPrice instead of RetailPrice
                    string q = @"SELECT pd.PrescriptionDetailId, pd.DispensedQty, m.TradeName, m.UnitPrice 
                                FROM PrescriptionDetails pd
                                JOIN Medicines m ON pd.MedicineId = m.MedIntId
                                WHERE pd.PatientId = @pid 
                                AND pd.DispensedQty > 0
                                AND pd.PrescriptionDetailId NOT IN (
                                    SELECT ISNULL(CAST(ItemRefId AS INT), 0) 
                                    FROM BillItems 
                                    WHERE ItemType = 'Medicine'
                                )";
                    
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@pid", patientId);
                    using (SqlDataReader dr = cmd.ExecuteReader()) {
                        while (dr.Read()) {
                            string medName = dr["TradeName"]?.ToString() ?? "Unknown";
                            int qty = Convert.ToInt32(dr["DispensedQty"]);
                            double price = dr["UnitPrice"] != DBNull.Value ? Convert.ToDouble(dr["UnitPrice"]) : 0;
                            int detailId = Convert.ToInt32(dr["PrescriptionDetailId"]);
                            
                            AddItem(medName, "Medicine", price, qty, detailId);
                        }
                    }
                }
                UpdateTotals();
            } catch (Exception ex) { MessageBox.Show("Error loading dispensed medicines: " + ex.Message); }
        }

        private int TryGetPatientId(string term)
        {
            try {
                using (var con = new SqlConnection(connectionString)) {
                    con.Open();
                    string q = "SELECT PatientID FROM Patients WHERE PatientCode = @t OR CAST(PatientID AS NVARCHAR) = @t OR PatientName LIKE @t";
                    var cmd = new SqlCommand(q, con);
                    cmd.Parameters.AddWithValue("@t", term);
                    object res = cmd.ExecuteScalar();
                    return res != null ? Convert.ToInt32(res) : 0;
                }
            } catch { return 0; }
        }

        private void BtnSaveBill_Click(object sender, EventArgs e)
        {
            if (cmbPatientID.SelectedItem == null)
            {
                MessageBox.Show("Please select or enter a Patient ID.");
                return;
            }

            if (dgvBillItems.Rows.Count == 0)
            {
                MessageBox.Show("The bill is empty.");
                return;
            }

            // Ensure Doctor is selected
            if (!currentDoctorId.HasValue && cmbDoctorName.SelectedIndex != -1)
            {
                dynamic sel = cmbDoctorName.SelectedItem;
                currentDoctorId = sel.Value;
            }

            if (!currentDoctorId.HasValue || currentDoctorId == 0)
            {
                MessageBox.Show("Please select a Physician.");
                return;
            }

            // Save to Bills and BillItems tables
            try 
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try {
                        decimal subTotal = double.TryParse(txtSubTotal.Text.Replace("₨ ", ""), out double s) ? (decimal)s : 0;
                        decimal discountPct = decimal.TryParse(txtDiscount.Inner.Text, out decimal d) ? d : 0;
                        string totalStr = txtTotal.Text.Replace("₨ ", "").Trim();
                        decimal totalAmount = decimal.TryParse(totalStr, out decimal t) ? t : 0;
                        
                        dynamic selectedPatient = cmbPatientID.SelectedItem;
                        int patientId = selectedPatient.Value;
                        int doctorId = currentDoctorId ?? 0;
                        int createdBy = 6; // Hardcoded placeholder (saleha.khuram)

                        string insertBill = @"INSERT INTO Bills 
                            (PatientId, DoctorId, BillDate, SubTotal, DiscountPercent, TotalAmount, PaymentType, CreatedBy, AppointmentIntId, PaymentStatus) 
                            OUTPUT INSERTED.BillId
                            VALUES (@PatientId, @DoctorId, GETDATE(), @SubTotal, @DiscountPercent, @TotalAmount, @PaymentType, @CreatedBy, @AppointmentIntId, 'Paid')";
                        
                        var cmdBill = new SqlCommand(insertBill, con, trans);
                        cmdBill.Parameters.AddWithValue("@PatientId", patientId);
                        cmdBill.Parameters.AddWithValue("@DoctorId", doctorId);
                        cmdBill.Parameters.AddWithValue("@SubTotal", subTotal);
                        cmdBill.Parameters.AddWithValue("@DiscountPercent", discountPct);
                        cmdBill.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        cmdBill.Parameters.AddWithValue("@PaymentType", cmbPaymentMethod.Text);
                        cmdBill.Parameters.AddWithValue("@CreatedBy", createdBy);
                        cmdBill.Parameters.AddWithValue("@AppointmentIntId", currentAppointmentIntId.HasValue ? (object)currentAppointmentIntId.Value : DBNull.Value);

                        int billId = (int)cmdBill.ExecuteScalar();

                        string insertItem = @"INSERT INTO BillItems 
                            (BillId, ItemType, ItemRefId, Description, UnitPrice, Qty, LineTotal) 
                            VALUES (@BillId, @ItemType, @ItemRefId, @Description, @UnitPrice, @Qty, @LineTotal)";

                        foreach (DataGridViewRow row in dgvBillItems.Rows)
                        {
                            if (row.IsNewRow) continue;

                            var cmdItem = new SqlCommand(insertItem, con, trans);
                            cmdItem.Parameters.AddWithValue("@BillId", billId);
                            cmdItem.Parameters.AddWithValue("@ItemType", row.Cells["Category"].Value?.ToString() ?? "Unknown");
                            cmdItem.Parameters.AddWithValue("@ItemRefId", row.Cells["ItemRefId"].Value ?? DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@Description", row.Cells["ItemName"].Value?.ToString() ?? "");
                            cmdItem.Parameters.AddWithValue("@UnitPrice", Convert.ToDecimal(row.Cells["Price"].Value ?? 0));
                            cmdItem.Parameters.AddWithValue("@Qty", Convert.ToInt32(row.Cells["Qty"].Value ?? 1));
                            cmdItem.Parameters.AddWithValue("@LineTotal", Convert.ToDecimal(row.Cells["Subtotal"].Value ?? 0));
                            
                            cmdItem.ExecuteNonQuery();
                        }

                        // Sync back to Appointments - Update Status to 'Paid' to complete the workflow
                        if (currentAppointmentIntId.HasValue)
                        {
                            string syncAppt = "UPDATE Appointments SET Status = 'Completed', PaymentStatus = 'Paid' WHERE AppointmentIntId = @aid";
                            var cmdSync = new SqlCommand(syncAppt, con, trans);
                            cmdSync.Parameters.AddWithValue("@aid", currentAppointmentIntId.Value);
                            cmdSync.ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show($"Bill saved successfully!\nBill ID: {billId}\nTotal: {txtTotal.Text}");
                        ClearAll();
                    } catch (Exception ex) {
                        trans.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving bill: " + ex.Message);
            }
        }

        private void AddMockData()
        {
            dgvBillItems.Rows.Clear();
            AddItem("Aspirin", "Tablet", 5.00, 3);
            AddItem("Amoxicillin", "Capsule", 45.00, 1);
            AddItem("Vitamin C", "Supplement", 15.00, 2);
            UpdateTotals();
        }

        private void AddItem(string name, string cat, double price, int qty, int? refId = null)
        {
            // If it's a medicine, check for existing row with same refId
            if (cat == "Medicine" && refId.HasValue)
            {
                foreach (DataGridViewRow row in dgvBillItems.Rows)
                {
                    if (row.Cells["Category"].Value?.ToString() == "Medicine" && 
                        row.Cells["ItemRefId"].Value != null && 
                        Convert.ToInt32(row.Cells["ItemRefId"].Value) == refId.Value)
                    {
                        int currentQty = Convert.ToInt32(row.Cells["Qty"].Value ?? 0);
                        row.Cells["Qty"].Value = currentQty + qty;
                        UpdateRowSubtotal(row.Index);
                        UpdateTotals();
                        return;
                    }
                }
            }

            double sub = price * qty;
            dgvBillItems.Rows.Add(name, cat, price.ToString("F2"), qty, sub.ToString("F2"), refId);
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            double subtotal = 0;
            foreach (DataGridViewRow row in dgvBillItems.Rows)
            {
                if (row.Cells["Subtotal"].Value != null)
                    subtotal += Convert.ToDouble(row.Cells["Subtotal"].Value);
            }

            txtSubTotal.Text = "₨ " + subtotal.ToString("F2");
            double discountPct = 0;
            double.TryParse(txtDiscount.Inner.Text, out discountPct);
            
            double discountAmount = subtotal * (discountPct / 100);
            double total = subtotal - discountAmount;
            
            txtTotal.Text = "₨ " + total.ToString("F2");
        }

        private void ClearAll()
        {
            cmbPatientID.SelectedIndex = -1;
            cmbPatientName.SelectedIndex = -1;
            cmbDoctorName.SelectedIndex = -1;
            txtDiscount.Inner.Text = "0";
            dgvBillItems.Rows.Clear();
            cmbPaymentMethod.SelectedIndex = 0;
            UpdateTotals();
        }

        private void PrintDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font titleFont = new Font("Arial", 22, FontStyle.Bold);
            Font headerFont = new Font("Arial", 12, FontStyle.Bold);
            Font bodyFont = new Font("Arial", 10);
            Font medFont = new Font("Arial", 10, FontStyle.Bold);
            float yPos = 50;
            float leftMargin = 50;
            float rightMargin = e.PageBounds.Width - 50;
            float col1 = 50, col2 = 350, col3 = 450, col4 = 550, col5 = 650;

            // --- Clinic Header ---
            string headerText = "AL REHMAN CLINIC";
            g.DrawString(headerText, titleFont, Brushes.Navy, new RectangleF(0, yPos, e.PageBounds.Width, 40), new StringFormat { Alignment = StringAlignment.Center });
            yPos += 45;
            g.DrawString("Near Civil Hospital, Shujabad, Multan | Tel: +92-300-1234567", bodyFont, Brushes.DimGray, new RectangleF(0, yPos, e.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
            yPos += 40;
            g.DrawLine(new Pen(Color.Navy, 2), leftMargin, yPos, rightMargin, yPos);
            yPos += 20;

            // --- Patient & Bill Info ---
            g.DrawString($"Invoice No: INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}", bodyFont, Brushes.Black, leftMargin, yPos);
            g.DrawString($"Date: {DateTime.Now:dd MMM yyyy HH:mm}", bodyFont, Brushes.Black, rightMargin - 200, yPos);
            yPos += 25;
            
            string patientName = cmbPatientName.Text;
            string physicianName = cmbDoctorName.Text;
            g.DrawString($"Patient: {patientName}", medFont, Brushes.Black, leftMargin, yPos);
            g.DrawString($"Physician: {physicianName}", medFont, Brushes.Black, rightMargin - 200, yPos);
            yPos += 40;

            // --- Table Header ---
            g.FillRectangle(Brushes.WhiteSmoke, leftMargin, yPos, rightMargin - leftMargin, 30);
            g.DrawRectangle(Pens.Gray, leftMargin, yPos, rightMargin - leftMargin, 30);
            g.DrawString("Description", headerFont, Brushes.Black, col1 + 5, yPos + 7);
            g.DrawString("Category", headerFont, Brushes.Black, col2 + 5, yPos + 7);
            g.DrawString("Price", headerFont, Brushes.Black, col3 + 5, yPos + 7);
            g.DrawString("Qty", headerFont, Brushes.Black, col4 + 5, yPos + 7);
            g.DrawString("Subtotal", headerFont, Brushes.Black, col5 + 5, yPos + 7);
            yPos += 35;

            // --- Items ---
            foreach (DataGridViewRow row in dgvBillItems.Rows)
            {
                if (row.IsNewRow) continue;
                string desc = row.Cells["ItemName"].Value?.ToString() ?? "";
                string cat = row.Cells["Category"].Value?.ToString() ?? "";
                string price = row.Cells["Price"].Value?.ToString() ?? "0";
                string qty = row.Cells["Qty"].Value?.ToString() ?? "0";
                string sub = row.Cells["Subtotal"].Value?.ToString() ?? "0";

                g.DrawString(desc, bodyFont, Brushes.Black, col1 + 5, yPos);
                g.DrawString(cat, bodyFont, Brushes.Black, col2 + 5, yPos);
                g.DrawString(price, bodyFont, Brushes.Black, col3 + 5, yPos);
                g.DrawString(qty, bodyFont, Brushes.Black, col4 + 5, yPos);
                g.DrawString(sub, bodyFont, Brushes.Black, col5 + 5, yPos);
                
                yPos += 25;
                if (yPos > e.PageBounds.Height - 150) { // Page break check
                    e.HasMorePages = true;
                    return;
                }
            }

            yPos += 20;
            g.DrawLine(Pens.LightGray, leftMargin, yPos, rightMargin, yPos);
            yPos += 15;

            // --- Totals ---
            float totalsLeft = col4;
            g.DrawString("Sub-Total:", medFont, Brushes.Black, totalsLeft, yPos);
            g.DrawString(txtSubTotal.Text, medFont, Brushes.Black, col5 + 5, yPos);
            yPos += 25;
            
            g.DrawString($"Discount ({txtDiscount.Inner.Text}%):", medFont, Brushes.Black, totalsLeft, yPos);
            double subVal = double.TryParse(txtSubTotal.Text.Replace("₨ ", ""), out double s) ? s : 0;
            double discPct = double.TryParse(txtDiscount.Inner.Text, out double d) ? d : 0;
            double discAmt = subVal * (discPct / 100);
            g.DrawString("- ₨ " + discAmt.ToString("F2"), medFont, Brushes.Black, col5 + 5, yPos);
            yPos += 30;

            g.FillRectangle(Brushes.Navy, totalsLeft - 10, yPos, (rightMargin - totalsLeft) + 10, 35);
            g.DrawString("TOTAL DUE:", medFont, Brushes.White, totalsLeft, yPos + 10);
            g.DrawString(txtTotal.Text, medFont, Brushes.White, col5 + 5, yPos + 10);
            yPos += 60;

            // --- Footer ---
            g.DrawString("Thank you for visiting! Get well soon.", new Font("Arial", 10, FontStyle.Italic), Brushes.Gray, new RectangleF(0, yPos, e.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
            yPos += 25;
            g.DrawString("Software by Antigravity AI", new Font("Arial", 8), Brushes.Silver, new RectangleF(0, e.PageBounds.Height - 40, e.PageBounds.Width, 20), new StringFormat { Alignment = StringAlignment.Center });
        }
    }
}
