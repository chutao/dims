using System;
using System.Collections.Generic;
using System.Linq;

using MySql.Data;
using DIMS.Models;
using Splat;
using MySql.Data.MySqlClient;
using NLog.LayoutRenderers;
using System.Xml.Linq;
using MySqlX.XDevAPI.Common;
using System.Reflection;

namespace DIMS.Helpers
{
    internal class MysqlDbHelper : IEnableLogger
    {
        private MysqlDbHelper()
        {

        }

        private static MysqlDbHelper instance = new MysqlDbHelper();
        public static MysqlDbHelper Default => instance;

        private string? _ConnectionString;
        public string? ConnectionString
        {
            get
            {
                if (_ConnectionString == null)
                {
                    var settings = Settings.Instance;
                    _ConnectionString = settings.Application.MysqlConnectionString;
                }

                return _ConnectionString;
            }
        }

        private static string? FormatDateTime(DateTime? dt)
        {
            return dt?.ToString("yyyy-dd-MM HH:mm:ss");
        }

        private static DateTime ToDateTime(string dt)
        {
            return Convert.ToDateTime(dt);
        }

        private bool ExecuteNonQuery(Func<MySqlConnection, bool> action)
        {
            bool result = false;
            MySqlConnection? conn = null;

            try
            {
                conn = new MySqlConnection(ConnectionString);
                conn.Open();

                result = action.Invoke(conn);
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex);
            }
            finally
            {
                conn?.Close();
            }

            return result;
        }

        private IEnumerable<T>? ExecuteReader<T>(Func<MySqlConnection, IEnumerable<T>?> action) where T : new()
        {
            IEnumerable<T>? result = null;
            MySqlConnection? conn = null;

            try
            {
                conn = new MySqlConnection(ConnectionString);
                conn.Open();

                result = action.Invoke(conn);
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex);
            }
            finally
            {
                conn?.Close();
            }

            return result;
        }

        private bool CheckIfTableExist(string dbName, string tableName)
        {
            bool result = false;
            MySqlConnection? conn = null;

            try
            {
                conn = new MySqlConnection(ConnectionString);
                conn.Open();

                string sql = $"select TABLE_NAME from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA='{dbName}' and TABLE_NAME='{tableName}'";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read() && reader.HasRows)
                    result = true;

                reader.Close();
            }
            catch (Exception ex)
            {
                this.Log().Debug(ex);
            }
            finally
            {
                conn?.Close();
            }

            return result;
        }

        #region Device Table
        public bool DeviceCreateTable()
        {
            if (CheckIfTableExist("panasonic", "devices"))
                return true;

            return ExecuteNonQuery((conn) =>
            {
                string sql = $"create table devices(id INT NOT NULL, name VARCHAR(100) NOT NULL, description VARCHAR(255), PRIMARY KEY(id))";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public IEnumerable<DeviceDataModel>? DeviceQuery(string? name, string? id)
        {
            Func<MySqlConnection, IEnumerable<DeviceDataModel>?> func = new Func<MySqlConnection, IEnumerable<DeviceDataModel>?>((conn) =>
            {
                List<DeviceDataModel>? list = null;
                string sql = "select * from devices";

                bool nameCondition = false;
                bool idCondition = false;
                if (!string.IsNullOrEmpty(name))
                {
                    nameCondition = true;
                }
                if (!string.IsNullOrEmpty(id))
                {
                    idCondition = true;
                }

                if (nameCondition && !idCondition)
                {
                    sql += $" where name='{name}'";
                }
                else if (!nameCondition && idCondition)
                {
                    sql += $" where id={id}";
                }
                else if (nameCondition && idCondition)
                {
                    sql += $" where name='{name}' and id={id}";
                }

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (list == null)
                        list = new List<DeviceDataModel>();

                    list.Add(new DeviceDataModel()
                    {
                        Id = reader.GetInt32("id"),
                        DeviceName = reader["name"].ToString(),
                        Description = reader["description"].ToString()
                    });
                }

                reader.Close();

                return list;
            });

            return ExecuteReader(func);
        }

        public bool DeviceInsert(DeviceDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = $"insert into device(id, name, description) values(@id, @name, @description)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.Add("@id", MySqlDbType.Int32);
                cmd.Parameters.Add("@name", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@description", MySqlDbType.VarString, 255);

                cmd.Parameters["@id"].Value = model.Id;
                cmd.Parameters["@name"].Value = model.DeviceName != null ? model.DeviceName : DBNull.Value;
                cmd.Parameters["@description"].Value = model.Description != null ? model.Description : DBNull.Value;

                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool DeviceDelete(int id)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = $"delete from devices where id={id}";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool DeviceUpdate(DeviceDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = $"update devices set name=@name, description=@description where id=@id";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.Add("@id", MySqlDbType.Int32);
                cmd.Parameters.Add("@name", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@description", MySqlDbType.VarString, 255);
                cmd.Parameters["@id"].Value = model.Id;
                cmd.Parameters["@name"].Value = model.DeviceName != null ? model.DeviceName : DBNull.Value;
                cmd.Parameters["@description"].Value = model.Description != null ? model.Description : DBNull.Value;

                return cmd.ExecuteNonQuery() > 0;
            });
        }
        #endregion

        #region Product Table
        public bool ProductCreateTable()
        {
            if (CheckIfTableExist("panasonic", "products"))
                return true;

            return ExecuteNonQuery((conn) =>
            {
                string sql = $"create table products(pcode VARCHAR(50) NOT NULL, pname VARCHAR(50), mcode VARCHAR(50) NOT NULL, mname VARCHAR(50), category INT NOT NULL, codelen INT NOT NULL, PRIMARY KEY(pcode))";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public IEnumerable<ProductDataModel>? ProductQuery(string? pcode, string? mcode)
        {
            return ExecuteReader((conn) =>
            {
                List<ProductDataModel>? result = null;
                string sql = "select * from products";

                bool pcodeCondition = false;
                bool mcodeCondition = false;
                if (!string.IsNullOrEmpty(pcode))
                {
                    pcodeCondition = true;
                }
                if (!string.IsNullOrEmpty(mcode))
                {
                    mcodeCondition = true;
                }

                if (pcodeCondition || mcodeCondition)
                    sql += " where";

                bool needAndFlag = false;

                if (pcodeCondition)
                {
                    if (needAndFlag)
                    {
                        sql += $" and pcode='{pcode}'";
                    }
                    else
                    {
                        sql += $" pcode='{pcode}'";
                        needAndFlag = true;
                    }
                }

                if (mcodeCondition)
                {
                    if (needAndFlag)
                    {
                        sql += $" and mcode='{mcode}'";
                    }
                    else
                    {
                        sql += $" mcode='{mcode}'";
                        needAndFlag = true;
                    }
                }

                MySqlCommand cmd = new MySqlCommand(sql, conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (result == null)
                        result = new List<ProductDataModel>();

                    result.Add(new ProductDataModel()
                    {
                        Model = reader["mcode"].ToString(),
                        POSCode = reader["pcode"].ToString(),
                        ModelName = reader["mname"].ToString(),
                        ProductName = reader["pname"].ToString(),
                        Category = reader.GetInt32("category"),
                        CodeLength = reader.GetInt32("codelen"),
                    });
                }

                reader.Close();

                return result;
            });
        }

        public bool ProductInsert(ProductDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = "insert into products(pname, pcode, mname, mcode, category, codelen) values(@pname, @pcode, @mname, @mcode, @category, @codelen)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.Add("@pcode", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@pname", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@mcode", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@mname", MySqlDbType.VarString, 50);
                cmd.Parameters.Add("@category", MySqlDbType.Int32);
                cmd.Parameters.Add("@codelen", MySqlDbType.Int32);

                cmd.Parameters["@pcode"].Value = model.POSCode != null ? model.POSCode : DBNull.Value;
                cmd.Parameters["@pname"].Value = model.ProductName != null ? model.ProductName : DBNull.Value;
                cmd.Parameters["@mcode"].Value = model.Model != null ? model.Model : DBNull.Value;
                cmd.Parameters["@mname"].Value = model.ModelName != null ? model.ModelName : DBNull.Value;
                cmd.Parameters["@category"].Value = model.Category;
                cmd.Parameters["@codelen"].Value = model.CodeLength;

                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool ProductDelete(string pcode)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = $"delete from products where pcode='{pcode}'";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool ProductUpdate(ProductDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = "update products set pname=@pname, mcode=@mcode, mname=@mname, category=@category, codelen=@codelen where pcode=@pcode";
                MySqlCommand cmd = new MySqlCommand( sql, conn);
                cmd.Parameters.Add("@category", MySqlDbType.Int32);
                cmd.Parameters.Add("@codelen", MySqlDbType.Int32);
                cmd.Parameters.Add("@pcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@pname", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@mcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@mname", MySqlDbType.VarChar, 50);

                cmd.Parameters["@category"].Value = model.Category;
                cmd.Parameters["@codelen"].Value = model.CodeLength;
                cmd.Parameters["@pcode"].Value = model.POSCode != null ? model.POSCode : DBNull.Value;
                cmd.Parameters["@pname"].Value = model.ProductName != null ? model.ProductName : DBNull.Value;
                cmd.Parameters["@mcode"].Value = model.Model != null ? model.Model : DBNull.Value;
                cmd.Parameters["@mname"].Value = model.ModelName != null ? model.ModelName : DBNull.Value;

                return cmd.ExecuteNonQuery() > 0;
            });
        }
        #endregion

        #region History Table
        public bool CreateHistoryTable()
        {
            if (CheckIfTableExist("panasonic", "history"))
                return true;

            return ExecuteNonQuery((conn) =>
            {
                string sql = $"create table history(id INT NOT NULL AUTO_INCREMENT, mcode VARCHAR(50) NOT NULL, tcode VARCHAR(50) NOT NULL, timetamp DATETIME NOT NULL, state BIGINT NOT NULL, PRIMARY KEY(id))";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public IEnumerable<ProductionDataModel>? HistoryQuery(DateTime? begin, DateTime? end, string? traycode, string? productcode)
        {
            return ExecuteReader((conn) =>
            {
                List<ProductionDataModel>? result = null;

                bool productCondition = false;
                bool traycodeCondition = false;
                bool datetimeCondition = false;
                bool needAndFlag = false;

                productCondition = !string.IsNullOrEmpty(productcode);
                traycodeCondition = !string.IsNullOrEmpty(traycode);
                datetimeCondition = begin.HasValue && end.HasValue;

                string sql = "select * from history";
                if (productCondition || traycodeCondition || datetimeCondition)
                    sql += " where";

                if (productCondition)
                {
                    if (needAndFlag)
                    {
                        sql += $" and mcode='{productcode}'";
                    }
                    else
                    {
                        sql += $" mcode='{productcode}'";
                        needAndFlag = true;
                    }
                }

                if (traycodeCondition)
                {
                    if (needAndFlag)
                    {
                        sql += $" and tcode='{traycode}'";
                    }
                    else
                    {
                        sql += $" tcode='{traycode}'";
                        needAndFlag = true;
                    }
                }

                if (datetimeCondition)
                {
                    if (needAndFlag)
                    {
                        sql += $" and timestamp>={FormatDateTime(begin)} and timestamp<={FormatDateTime(end)}";
                    }
                    else
                    { 
                        sql += $" timestamp>={FormatDateTime(begin)} and timestamp<={FormatDateTime(end)}";
                        needAndFlag = true;
                    }
                }

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (result == null)
                        result = new List<ProductionDataModel>();

                    result.Add(new ProductionDataModel()
                    {
                        TrayCode = reader["tcode"].ToString(),
                        ProductCode = reader["mcode"].ToString(),
                        State = reader.GetUInt64("state"),
                        Timestamp = reader.GetDateTime("timestamp"),
                        Id = reader.GetUInt64("id")
                    });
                }

                reader.Close();
                return result;
            });
        }

        public bool HistoryInsert(ProductionDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = "insert into history(id, mcode, tcode, timestamp, state) values(@mcode, @tcode, @timestamp, @state)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.Add("@mcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@tcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@timestamp", MySqlDbType.DateTime);
                cmd.Parameters.Add("@state", MySqlDbType.UInt64);

                cmd.Parameters["@mcode"].Value = model.ProductCode != null ? model.ProductCode : DBNull.Value;
                cmd.Parameters["@tcode"].Value = model.TrayCode != null ? model.TrayCode : DBNull.Value;
                cmd.Parameters["@timestamp"].Value = model.Timestamp != null ? FormatDateTime(model.Timestamp) : DBNull.Value;
                cmd.Parameters["@state"].Value = model.State;

                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool HistoryDelete(string? productcode, string? traycode) {
            return ExecuteNonQuery((conn) =>
            {
                if (string.IsNullOrEmpty(productcode) && string.IsNullOrEmpty(traycode))
                    return false;

                string sql = "delete from history where";
                if (!string.IsNullOrEmpty(productcode) && string.IsNullOrEmpty(traycode))
                    sql = $" mcode='{productcode}'";
                else if (string.IsNullOrEmpty(productcode) && !string.IsNullOrEmpty(traycode))
                    sql = $" tcode='{traycode}'";
                else
                    sql = $" mcode='{productcode}' and tcode='{traycode}'";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool HistoryDelete(int id)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = $"delete from history where id={id}";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public bool HistoryUpdate(ProductionDataModel model)
        {
            return ExecuteNonQuery((conn) =>
            {
                string sql = "update history set mcode=@mcode, tcode=@tcode, state=@state, timestamp=@timestamp where id=@id";
                MySqlCommand cmd = new MySqlCommand( sql, conn);
                cmd.Parameters.Add("@id", MySqlDbType.Int64);
                cmd.Parameters.Add("@mcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@tcode", MySqlDbType.VarChar, 50);
                cmd.Parameters.Add("@state", MySqlDbType.UInt64);
                cmd.Parameters.Add("@timestamp", MySqlDbType.DateTime);

                cmd.Parameters["@id"].Value = model.Id;
                cmd.Parameters["@state"].Value = model.State;
                cmd.Parameters["@mcode"].Value = model.ProductCode != null ? model.ProductCode : DBNull.Value;
                cmd.Parameters["@tcode"].Value = model.TrayCode != null ? model.TrayCode : DBNull.Value;
                cmd.Parameters["@timestamp"].Value = model.Timestamp != null ? FormatDateTime(model.Timestamp) : DBNull.Value;

                return cmd.ExecuteNonQuery() > 0;
            });
        }
        #endregion
    }
}
