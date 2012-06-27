using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using RepairControlDb.Properties;

namespace RepairControlDb
{
    public class Dbc 
    {
        protected class Param
        {
            public static Param String(string name, string val)
            {
                return new Param { Name = name, Value = val };
            }

            private string Name;
            private object Value;

            public static Param Int(string name, int? val)
            {
                return new Param { Name = name, Value = (object) val ?? DBNull.Value };
            }

            public void MakeParameter(SqlCommand cmd)
            {
                cmd.Parameters.AddWithValue("@" + Name, Value);
            }

            public static Param Int(string name, bool val)
            {
                return new Param { Name = name, Value = val ? 1 : 0 };
            }

            public static Param Int(string name, Enum value)
            {
                return new Param { Name = name, Value = Convert.ToInt32(value) };
            }

            internal static Param Byte(string name, byte value)
            {
                return new Param { Name = name, Value = value };
            }
        }

        private static Dbc instance;

        private readonly string ConnectionString;

        protected Dbc()
        {
            ConnectionString = Settings.Default.ConString;
        }
        private static Dbc Instance
        {
            get { return instance ?? (instance = new Dbc()); }
        }

        private DataSet _LoadDataSet(string procedureName, IEnumerable<Param> parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = CreateCommand(procedureName, transaction))
                        {
                            BindParameters(cmd, parameters);
                            var ds = new DataSet();
                            var adapter = new SqlDataAdapter { SelectCommand = cmd };
                            adapter.Fill(ds);
                            transaction.Commit();
                            return ds;
                        }

                    }
                    catch (SqlException exc)
                    {
                        transaction.Rollback();
                        throw new BsgDbException(exc.Message);
                    }
                }
            }
            


        }

        private static SqlCommand CreateCommand(string procedureName, SqlTransaction transaction)
        {
            return new SqlCommand
            {
                Connection = transaction.Connection,
                CommandText = procedureName,
                CommandType = CommandType.StoredProcedure,
                Transaction = transaction
            };
        }

        private static void BindParameters(SqlCommand cmd, IEnumerable<Param> parameters)
        {
            foreach (var prm in parameters)
            {
                prm.MakeParameter(cmd);
            }
        }

        protected static DataSet LoadDataSet(string procedureName, params Param[] parameters)
        {
            return Instance._LoadDataSet(procedureName, parameters);
        }

        protected static DataTable LoadDataTable(string procedureName, params Param[] parameters)
        {
            var ds = Instance._LoadDataSet(procedureName, parameters);
            return ds != null && ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

        protected static void ExecuteNonQuery(string procedureName, params Param[] parameters)
        {
            Instance._ExecuteNonQuery(procedureName, parameters);
        }

        private void _ExecuteNonQuery(string procedureName, IEnumerable<Param> parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = CreateCommand(procedureName, transaction))
                        {
                            BindParameters(cmd, parameters);
                            cmd.ExecuteNonQuery();
                            transaction.Commit();
                        }

                    }
                    catch (SqlException exc)
                    {
                        transaction.Rollback();
                        throw new BsgDbException(exc.Message);
                    }
                }
            }
        }

        protected static DataRow LoadDataRow(string procedureName, params Param[] parameters)
        {
            return GetSingleRow(LoadDataTable(procedureName, parameters));
        }

        protected static DataRow GetSingleRow(DataTable dt)
        {
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        public static int? GetNullableInt(object obj)
        {
            return obj != DBNull.Value ? (int?)(int)obj : null;
        }

        private object _ExecuteScalar(string procedureName, IEnumerable<Param> parameters)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = CreateCommand(procedureName, transaction))
                        {
                            BindParameters(cmd, parameters);
                            var res = cmd.ExecuteScalar();
                            transaction.Commit();
                            return res;
                        }

                    }
                    catch (SqlException exc)
                    {
                        transaction.Rollback();
                        throw new BsgDbException(exc.Message);
                    }
                }
            }
        }

        protected static int ExecuteScalar(string procedureName, params Param[] parameters)
        {
            return Convert.ToInt32(Instance._ExecuteScalar(procedureName, parameters));
        }
    }
}
