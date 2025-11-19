using KpiApplication.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace KpiApplication.DataAccess
{
    internal class ProductionSchedules_DAL
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["strCon"].ConnectionString;

        public BindingList<ProductionSchedules_Model> GetAllProductionSchedules()
        {
            BindingList<ProductionSchedules_Model> list = new BindingList<ProductionSchedules_Model>();

            string query = @"
SET NOCOUNT ON;

SELECT 
    -- Bảng tổng quan (Overview)
    o.SOID,
    o.Sales_Order,
    o.PO,
    o.Art,
    o.Model,
    o.Mold,
    o.QTY              AS Overview_QTY,
    o.Version          AS Overview_Version,
    o.Created_At       AS Overview_Created_At,

    -- Bảng lịch sản xuất (ListSchedule)
    l.ID               AS ListSchedule_ID,
    l.Version          AS ListSchedule_Version,
    l.Production_Week,
    l.Production_Date,
    l.Process_Type,
    l.Line_Number,
    l.Planning_QTY,
    l.Color_Name,
    l.Finish_Date,
    l.Finish_Week,
    l.Uppers_Source,
    l.Stockfitting,
    l.Sample_Shoes,
    l.Created_At       AS ListSchedule_Created_At,
    l.Updated_At       AS ListSchedule_Updated_At,

    -- Bảng chi tiết (ScheduleDetails)
    d.ID               AS Detail_ID,
    d.PRODUCTION_ORDER,
    d.MAIN_PRODUCTION_ORDER,
    d.MATERIAL_NO,
    d.ORDER_TYPE,
    d.SIZE_NO,
    d.QTY              AS Detail_QTY,
    d.ORG,
    d.ART              AS Detail_ART,
    d.UPDATED_AT       AS Detail_Updated_At

FROM dbo.ProductionScheduleOverview AS o
OUTER APPLY (
    SELECT TOP 1 *
    FROM dbo.ListSchedule AS l
    WHERE l.SOID = o.SOID
    ORDER BY l.Updated_At DESC
) AS l
LEFT JOIN dbo.ScheduleDetails AS d
    ON d.SOID = o.SOID

ORDER BY 
    o.SOID,
    d.PRODUCTION_ORDER,
    TRY_CONVERT(INT, d.SIZE_NO);

            ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ProductionSchedules_Model model = new ProductionSchedules_Model
                        {
                            SOID = reader.GetInt32(reader.GetOrdinal("SOID")),
                            Sales_Order = reader["Sales_Order"] as string,
                            PO = reader["PO"] as string,
                            Art = reader["Art"] as string,
                            Model = reader["Model"] as string,
                            Mold = reader["Mold"] as string,
                            Overview_QTY = reader["Overview_QTY"] as int?,
                            Overview_Version = reader["Overview_Version"] as int?,
                            Overview_Created_At = reader["Overview_Created_At"] as DateTime?,

                            Production_Week = reader["Production_Week"] as int?,
                            Production_Date = reader["Production_Date"] as string,
                            Process_Type = reader["Process_Type"] as string,
                            Line_Number = reader["Line_Number"] as string,
                            Planning_QTY = reader["Planning_QTY"] as int?,
                            Color_Name = reader["Color_Name"] as string,
                            Finish_Date = reader["Finish_Date"] as string,
                            Finish_Week = reader["Finish_Week"] as int?,
                            Uppers_Source = reader["Uppers_Source"] as string,
                            Stockfitting = reader["Stockfitting"] as string,
                            Sample_Shoes = reader["Sample_Shoes"] as string,
                            ListSchedule_Created_At = reader["ListSchedule_Created_At"] as DateTime?,
                            ListSchedule_Updated_At = reader["ListSchedule_Updated_At"] as DateTime?,

                            PRODUCTION_ORDER = reader["PRODUCTION_ORDER"] as string,
                            MAIN_PRODUCTION_ORDER = reader["MAIN_PRODUCTION_ORDER"] as string,
                            MATERIAL_NO = reader["MATERIAL_NO"] as string,
                            ORDER_TYPE = reader["ORDER_TYPE"] as string,
                            SIZE_NO = reader["SIZE_NO"] as string,
                            Detail_Size_QTY = reader["Detail_QTY"] as int?,
                            ORG = reader["ORG"] as string,
                            Detail_ART = reader["Detail_ART"] as string,
                            Detail_Updated_At = reader["Detail_Updated_At"] as DateTime?
                        };

                        list.Add(model);
                    }
                }
            }

            return list;
        }
    }
}
