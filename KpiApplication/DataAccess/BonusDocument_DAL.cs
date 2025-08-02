using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace KpiApplication.DataAccess
{
    public class BonusDocument_DAL
    {
        private static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public void Insert(BonusDocument_Model doc)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
                INSERT INTO BonusDocuments (ModelName, FileName, DocumentType, PdfData, CreatedBy)
                VALUES (@ModelName, @FileName, @DocumentType, @PdfData, @CreatedBy)", conn))
            {
                cmd.Parameters.AddWithValue("@ModelName", doc.ModelName);
                cmd.Parameters.AddWithValue("@FileName", (object)doc.FileName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DocumentType", (object)doc.DocumentType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PdfData", doc.PdfData ?? new byte[0]);
                cmd.Parameters.AddWithValue("@CreatedBy", doc.CreatedBy ?? (object)DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(BonusDocument_Model doc)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
        UPDATE BonusDocuments
        SET ModelName = @ModelName,
            FileName = @FileName,
            DocumentType = @DocumentType,
            PdfData = @PdfData,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
        WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@ModelName", doc.ModelName);
                cmd.Parameters.AddWithValue("@FileName", (object)doc.FileName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DocumentType", (object)doc.DocumentType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PdfData", doc.PdfData ?? new byte[0]);
                cmd.Parameters.AddWithValue("@UpdatedAt", (object)doc.UpdatedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedBy", (object)doc.UpdatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", doc.Id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void RenameFileNameById(int id, string newFileName, DateTime updatedAt, int updatedBy)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
                UPDATE BonusDocuments
                SET FileName = @FileName,
                    UpdatedAt = @UpdatedAt,
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@FileName", newFileName);
                cmd.Parameters.AddWithValue("@UpdatedAt", updatedAt);
                cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int id)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("DELETE FROM BonusDocuments WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public bool Exists(string modelName, string fileName)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM BonusDocuments
                WHERE ModelName = @ModelName AND FileName = @FileName", conn))
            {
                cmd.Parameters.AddWithValue("@ModelName", modelName);
                cmd.Parameters.AddWithValue("@FileName", fileName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public List<BonusDocument_Model> GetMetadataByModelName(string modelName, string documentType = null)
        {
            var list = new List<BonusDocument_Model>();
            if (string.IsNullOrWhiteSpace(modelName)) return list;

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
        SELECT d.Id, d.ModelName, d.FileName, d.DocumentType, d.CreatedAt, d.CreatedBy, d.UpdatedAt, d.UpdatedBy,
               u1.EmployeeName AS CreatedByName,
               u2.EmployeeName AS UpdatedByName
        FROM BonusDocuments d
        LEFT JOIN Users u1 ON d.CreatedBy = u1.UserID
        LEFT JOIN Users u2 ON d.UpdatedBy = u2.UserID
        WHERE d.ModelName = @ModelName
        " + (string.IsNullOrWhiteSpace(documentType) ? "" : "AND d.DocumentType = @DocumentType") + @"
        ORDER BY d.Id DESC", conn))
            {
                cmd.Parameters.AddWithValue("@ModelName", modelName);
                if (!string.IsNullOrWhiteSpace(documentType))
                    cmd.Parameters.AddWithValue("@DocumentType", documentType);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new BonusDocument_Model
                        {
                            Id = (int)reader["Id"],
                            ModelName = reader["ModelName"]?.ToString(),
                            FileName = reader["FileName"]?.ToString(),
                            DocumentType = reader["DocumentType"]?.ToString(),
                            CreatedAt = reader["CreatedAt"] as DateTime?,
                            CreatedBy = reader["CreatedBy"] as int?,
                            UpdatedAt = reader["UpdatedAt"] as DateTime?,
                            UpdatedBy = reader["UpdatedBy"] as int?,
                            CreatedByName = reader["CreatedByName"]?.ToString(),
                            UpdatedByName = reader["UpdatedByName"]?.ToString()
                        });
                    }
                }
            }

            return list;
        }

        public BonusDocument_Model GetById(int id)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(@"
        SELECT d.Id, d.ModelName, d.FileName, d.DocumentType, d.PdfData, d.CreatedAt, d.CreatedBy, d.UpdatedAt, d.UpdatedBy,
               u1.EmployeeName AS CreatedByName,
               u2.EmployeeName AS UpdatedByName
        FROM BonusDocuments d
        LEFT JOIN Users u1 ON d.CreatedBy = u1.UserID
        LEFT JOIN Users u2 ON d.UpdatedBy = u2.UserID
        WHERE d.Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new BonusDocument_Model
                        {
                            Id = (int)reader["Id"],
                            ModelName = reader["ModelName"]?.ToString(),
                            FileName = reader["FileName"]?.ToString(),
                            DocumentType = reader["DocumentType"]?.ToString(), // ✅ thêm dòng này
                            PdfData = reader["PdfData"] as byte[],
                            CreatedAt = reader["CreatedAt"] as DateTime?,
                            CreatedBy = reader["CreatedBy"] as int?,
                            UpdatedAt = reader["UpdatedAt"] as DateTime?,
                            UpdatedBy = reader["UpdatedBy"] as int?,
                            CreatedByName = reader["CreatedByName"]?.ToString(),
                            UpdatedByName = reader["UpdatedByName"]?.ToString()
                        };
                    }
                }
            }

            return null;
        }
    }
}
