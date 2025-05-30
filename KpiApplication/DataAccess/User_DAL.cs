using System;
using System.Configuration;
using System.Configuration.Provider;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace KpiApplication.DataAccess
{
    public class User_DAL
    {
        private string connectionString;

        public User_DAL()
        {
            //connectionString = ConfigurationManager.ConnectionStrings["strCon"]?.ConnectionString;
            connectionString = "Data Source=10.30.0.116;Initial Catalog=KPI-DATA; User Id=sa; Password=12345;";
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Chuỗi kết nối 'strCon' không được tìm thấy trong file cấu hình.");
            }
        }
        public bool UpdatePassword(string username, string newPassword)
        {
            string hashedPassword = ComputeHash(newPassword); 

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("UPDATE Users SET Password = @newPassword WHERE Username = @username", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@newPassword", hashedPassword);
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
        public string GetDepartmentByUsername(string username)
        {
            string department = string.Empty;
            using (SqlConnection dbConnection = new SqlConnection(connectionString))
            {
                string query = "SELECT [Department] FROM [Users] WHERE [username] = @username";

                using (SqlCommand command = new SqlCommand(query, dbConnection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    dbConnection.Open();

                    object result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        department = result.ToString();
                    }
                }
            }
            return department;
        }
    
        public bool UserExists(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM dbo.Users WHERE Username=@Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count == 1;
                }
            }
        }
        public bool ValidateUser(string username, string password)
        {
            string hashedPassword = ComputeHash(password);
            return CheckLogin(username, hashedPassword);
        }
        public string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public bool CheckLogin(string username, string hashedPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT IsActive FROM dbo.Users WHERE Username=@Username AND Password=@Password", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool isActive = reader.GetBoolean(0);
                            return isActive;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        public bool IsActive(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT IsActive FROM dbo.Users WHERE Username=@Username", connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool isActive = reader.GetBoolean(0);
                            return isActive;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
