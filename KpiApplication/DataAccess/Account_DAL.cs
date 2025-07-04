using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace KpiApplication.DataAccess
{ 
    public class Account_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public bool ValidateUser(string username, string password)
        {
            string hashedPassword = ComputeHash(password);
            return CheckLogin(username, hashedPassword);
        }

        public bool CheckLogin(string username, string hashedPassword)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT IsActive FROM Users WHERE Username=@Username AND Password=@Password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read() && reader.GetBoolean(0);
                    }
                }
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
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UserExists(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM Users WHERE Username=@Username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }
        }

        public bool IsActive(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT IsActive FROM Users WHERE Username=@Username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read() && reader.GetBoolean(0);
                    }
                }
            }
        }

        public EmployeeInfo_Model GetEmployeeInfo(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT UserID, Username, EmployeeName, EnglishName, Department, EmployeeID FROM Users WHERE Username = @username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeInfo_Model
                            {
                                UserID = reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : 0,
                                Username = reader["Username"]?.ToString(),
                                EmployeeName = reader["EmployeeName"]?.ToString(),
                                EnglishName = reader["EnglishName"]?.ToString(),
                                Department = reader["Department"]?.ToString(),
                                EmployeeID = reader["EmployeeID"]?.ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        private string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }

        // ===================== ACCOUNT CRUD =====================
        public List<Account_Model> GetAllAccounts()
        {
            List<Account_Model> list = new List<Account_Model>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Users";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Account_Model
                        {
                            UserID = reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : 0,
                            Username = reader["Username"]?.ToString(),
                            Password = reader["Password"]?.ToString(),
                            EmployeeName = reader["EmployeeName"]?.ToString(),
                            Department = reader["Department"]?.ToString(),
                            EmployeeID = reader["EmployeeID"]?.ToString(),
                            EnglishName = reader["EnglishName"]?.ToString(),
                            CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : (DateTime?)null,
                            UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : (DateTime?)null,
                            IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : true
                        });
                    }
                }
            }

            return list;
        }

        public void InsertOrUpdateUser(Account_Model user)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Kiểm tra tồn tại
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Username", user.Username);
                    int count = (int)checkCmd.ExecuteScalar();

                    string query;

                    if (count > 0)
                    {
                        // Cập nhật
                        query = string.IsNullOrWhiteSpace(user.Password)
                            ? @"
                                UPDATE Users
                                SET EmployeeName = @EmployeeName,
                                    Department = @Department,
                                    EmployeeID = @EmployeeID,
                                    UpdatedAt = GETDATE(),
                                    IsActive = @IsActive
                                WHERE Username = @Username"
                            : @"
                                UPDATE Users
                                SET Password = @Password,
                                    EmployeeName = @EmployeeName,
                                    Department = @Department,
                                    EmployeeID = @EmployeeID,
                                    UpdatedAt = GETDATE(),
                                    IsActive = @IsActive
                                WHERE Username = @Username";
                    }
                    else
                    {
                        // Thêm mới
                        query = @"
                            INSERT INTO Users 
                            (Username, Password, EmployeeName, EnglishName, Department, EmployeeID, CreatedAt, IsActive)
                            VALUES 
                            (@Username, @Password, @EmployeeName, @EnglishName, @Department, @EmployeeID, GETDATE(), @IsActive)";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", user.Username);
                        cmd.Parameters.AddWithValue("@EmployeeName", user.EmployeeName ?? "");
                        cmd.Parameters.AddWithValue("@EnglishName", user.EnglishName ?? "");
                        cmd.Parameters.AddWithValue("@Department", (object)user.Department ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmployeeID", (object)user.EmployeeID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);

                        if (!string.IsNullOrWhiteSpace(user.Password) || count == 0)
                            cmd.Parameters.AddWithValue("@Password", user.Password ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<string> GetDistinctPlants()
        {
            List<string> plants = new List<string>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT DISTINCT Plant FROM Department WHERE Plant IS NOT NULL AND Plant <> ''";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        plants.Add(reader.GetString(0));
                    }
                }
            }

            return plants;
        }
    }
}
