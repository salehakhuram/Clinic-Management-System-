using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ClinicManagement
{
    public enum SearchResultType
    {
        None,
        Patient,
        Appointment,
        QueueToken
    }

    public class SearchResult
    {
        public SearchResultType Type { get; set; }
        public int Id { get; set; }
        public string DisplayText { get; set; }
        public DataRow Data { get; set; }

        public SearchResult()
        {
            Type = SearchResultType.None;
            DisplayText = string.Empty;
        }
    }

    public static class GlobalSearchHelper
    {
        private static string connectionString = @"Server=DESKTOP-5NPQD72\SQLEXPRESS;Database=ClinicDBB;Trusted_Connection=True;TrustServerCertificate=True";

        /// <summary>
        /// Performs a global search across patients, appointments, and queue tokens
        /// </summary>
        public static SearchResult PerformGlobalSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new SearchResult();

            query = query.Trim();

            // Try patient search first (most common)
            var patientResult = SearchPatient(query);
            if (patientResult.Type != SearchResultType.None)
                return patientResult;

            // Try appointment code search
            var appointmentResult = SearchAppointment(query);
            if (appointmentResult.Type != SearchResultType.None)
                return appointmentResult;

            // Try queue token search
            var queueResult = SearchQueueToken(query);
            if (queueResult.Type != SearchResultType.None)
                return queueResult;

            return new SearchResult();
        }

        /// <summary>
        /// Search for patient by name, phone, or patient code
        /// </summary>
        public static SearchResult SearchPatient(string query)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string sql = @"SELECT TOP 1 PatientID, PatientName, Phone, PatientCode, Age, Gender 
                                  FROM Patients 
                                  WHERE PatientName LIKE @query 
                                     OR Phone LIKE @query 
                                     OR PatientCode LIKE @query
                                     OR CAST(PatientID AS NVARCHAR) = @exactQuery
                                  ORDER BY 
                                    CASE 
                                      WHEN PatientCode = @exactQuery THEN 1
                                      WHEN CAST(PatientID AS NVARCHAR) = @exactQuery THEN 2
                                      WHEN Phone = @exactQuery THEN 3
                                      WHEN PatientName LIKE @exactQuery THEN 4
                                      ELSE 5
                                    END";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@query", "%" + query + "%");
                        cmd.Parameters.AddWithValue("@exactQuery", query);

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                return new SearchResult
                                {
                                    Type = SearchResultType.Patient,
                                    Id = Convert.ToInt32(row["PatientID"]),
                                    DisplayText = $"Patient: {row["PatientName"]} (ID: {row["PatientID"]}, Phone: {row["Phone"]})",
                                    Data = row
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            return new SearchResult();
        }

        /// <summary>
        /// Search for appointment by appointment code
        /// </summary>
        public static SearchResult SearchAppointment(string query)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string sql = @"SELECT TOP 1 A.AppointmentIntId, A.AppointmentCode, A.PatientId, 
                                         P.PatientName, A.AppointmentDate, A.AppointmentTime, 
                                         A.Status, A.DoctorId, S.StaffName as DoctorName
                                  FROM Appointments A
                                  LEFT JOIN Patients P ON A.PatientId = P.PatientID
                                  LEFT JOIN Doctors D ON A.DoctorId = D.DoctorID
                                  LEFT JOIN Staff S ON D.StaffId = S.StaffId
                                  WHERE A.AppointmentCode LIKE @query
                                     OR CAST(A.AppointmentIntId AS NVARCHAR) = @exactQuery
                                  ORDER BY A.AppointmentDate DESC";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@query", "%" + query + "%");
                        cmd.Parameters.AddWithValue("@exactQuery", query);

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                DateTime apptDate = Convert.ToDateTime(row["AppointmentDate"]);
                                return new SearchResult
                                {
                                    Type = SearchResultType.Appointment,
                                    Id = Convert.ToInt32(row["AppointmentIntId"]),
                                    DisplayText = $"Appointment: {row["AppointmentCode"]} - {row["PatientName"]} on {apptDate:MMM dd, yyyy}",
                                    Data = row
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            return new SearchResult();
        }

        /// <summary>
        /// Search for queue token in today's visits
        /// </summary>
        public static SearchResult SearchQueueToken(string query)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string sql = @"SELECT TOP 1 V.VisitID, V.TokenNumber, V.PatientID, P.PatientName, 
                                         V.DoctorName, V.Status, V.VisitDate, V.AppointmentIntId
                                  FROM Visits V
                                  LEFT JOIN Patients P ON V.PatientID = P.PatientID
                                  WHERE V.TokenNumber LIKE @query
                                    AND CAST(V.VisitDate AS DATE) = CAST(GETDATE() AS DATE)
                                  ORDER BY V.VisitDate DESC";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@query", "%" + query + "%");

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                return new SearchResult
                                {
                                    Type = SearchResultType.QueueToken,
                                    Id = Convert.ToInt32(row["VisitID"]),
                                    DisplayText = $"Queue Token: {row["TokenNumber"]} - {row["PatientName"]} ({row["Status"]})",
                                    Data = row
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            return new SearchResult();
        }
    }
}
