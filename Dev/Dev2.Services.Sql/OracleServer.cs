﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dev2.Common;
using Dev2.Common.Interfaces.Services.Sql;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Dev2.Services.Sql
{
    public sealed class OracleServer : IDbServer
    {
        private readonly IDbFactory _factory;
        private IDbCommand _command;
        private OracleConnection _connection;
        private IDbTransaction _transaction;
        private string _owner;

        private readonly bool _isTesting;

        public OracleServer(IDbFactory factory, IDbCommand command, IDbTransaction transaction)
        {
            _factory = factory;
            _command = command;

            var con = new OracleConnection()
            {
                ConnectionString = "",
            };

            _isTesting = true;
            _connection = con;
            _transaction = transaction;
        }

        public bool IsConnected
        {
            get
            {
                if (_isTesting)
                {
                    return true;
                }

                return _connection != null && _connection.State == ConnectionState.Open;
            }
        }
        public string ConnectionString
        {
            get { return _connection == null ? null : _connection.ConnectionString; }
        }

        public void FetchStoredProcedures(Func<IDbCommand, List<IDbDataParameter>, List<IDbDataParameter>, string, string, bool> procedureProcessor, Func<IDbCommand, List<IDbDataParameter>, List<IDbDataParameter>, string, string, bool> functionProcessor, bool continueOnProcessorException = false, string dbName = "")
        {
            VerifyArgument.IsNotNull("procedureProcessor", procedureProcessor);
            VerifyArgument.IsNotNull("functionProcessor", functionProcessor);
            VerifyConnection();
            _owner = dbName;
            DataTable proceduresDataTable = GetSchema(_connection);

            // ROUTINE_CATALOG - ROUTINE_SCHEMA ,SPECIFIC_SCHEMA

            foreach (DataRow row in proceduresDataTable.Rows)
            {
                string fullProcedureName = row["NAME"].ToString();

                if (row["DB"].ToString().Equals(dbName, StringComparison.OrdinalIgnoreCase))
                {
                    using (IDbCommand command = _factory.CreateCommand(_connection, CommandType.StoredProcedure,
                            _owner + "." + fullProcedureName))
                    {
                        try
                        {
                            List<IDbDataParameter> outParameters;

                            List<IDbDataParameter> parameters = GetProcedureParameters(command, dbName, fullProcedureName, out outParameters);

                            string helpText = FetchHelpTextContinueOnException(fullProcedureName, _connection);

                            procedureProcessor(command, parameters, outParameters, helpText, fullProcedureName);
                        }
                        catch (Exception)
                        {
                            if (!continueOnProcessorException)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public IDbCommand CreateCommand()
        {
            VerifyConnection();
            IDbCommand command = _connection.CreateCommand();
            if (!_isTesting)
            {
                command.Transaction = _transaction;
            }
            return command;
        }

        public void BeginTransaction()
        {
            if (IsConnected)
            {
                _transaction = _connection.BeginTransaction();
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        #region FetchDatabases

        public List<string> FetchDatabases()
        {
            VerifyConnection();
            OracleDataReader reader = null;
            List<string> result = new List<string>();
            OracleCommand cmd = new OracleCommand("SELECT DISTINCT(OWNER) AS DATABASE_NAME FROM DBA_SEGMENTS WHERE OWNER IN (SELECT USERNAME FROM DBA_USERS WHERE DEFAULT_TABLESPACE NOT IN ('SYSTEM','SYSAUX'))", _connection);
            try
            {
                if (!_isTesting)
                {
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }

            finally
            {
                if (reader != null) reader.Close();
            }

            return result;
        }

        #endregion

        #region FetchDataTable

        public DataTable FetchDataTable(IDbCommand command)
        {
            VerifyArgument.IsNotNull("command", command);

            return ExecuteReader(command, CommandBehavior.SchemaOnly & CommandBehavior.KeyInfo,
                reader => _factory.CreateTable(reader, LoadOption.OverwriteChanges));
        }

        public DataTable FetchDataTable(IDbDataParameter[] parameters, IEnumerable<IDbDataParameter> outparameters)
        {
            VerifyConnection();
            if (!_isTesting)
            {
                AddParameters(_command, parameters);

                foreach (var par in outparameters)
                {
                    _command.Parameters.Add(par);
                }
            }
            return FetchDataTable(_command);
        }

        public DataTable FetchDataTable(params IDbDataParameter[] parameters)
        {
            VerifyConnection();
            AddParameters(_command, parameters);
            return FetchDataTable(_command);
        }


        #endregion

        #region FetchDataSet

        public DataSet FetchDataSet(params OracleParameter[] parameters)
        {
            VerifyConnection();
            return FetchDataSet(_command, parameters);
        }

        public DataSet FetchDataSet(IDbCommand command, params OracleParameter[] parameters)
        {
            VerifyArgument.IsNotNull("command", command);
            AddParameters(command, parameters);
            return _factory.FetchDataSet(command);
        }

        #endregion

        #region FetchStoredProcedures

        public void FetchStoredProcedures(
            Func<IDbCommand, List<IDbDataParameter>, string, string, bool> procedureProcessor,
            Func<IDbCommand, List<IDbDataParameter>, string, string, bool> functionProcessor,
            bool continueOnProcessorException = false, string dbName = "")
        {
            VerifyArgument.IsNotNull("procedureProcessor", procedureProcessor);
            VerifyArgument.IsNotNull("functionProcessor", functionProcessor);
            VerifyConnection();
            _owner = dbName;
            DataTable proceduresDataTable = GetSchema(_connection);




            // ROUTINE_CATALOG - ROUTINE_SCHEMA ,SPECIFIC_SCHEMA

            foreach (DataRow row in proceduresDataTable.Rows)//Procedure 2
            {
                string fullProcedureName = row["NAME"].ToString();

                if (row["DB"].ToString().Equals(dbName, StringComparison.OrdinalIgnoreCase))
                {
                    using (
                        IDbCommand command = _factory.CreateCommand(_connection, CommandType.StoredProcedure,
                           _owner + "." + fullProcedureName))
                    {
                        try
                        {
                            List<IDbDataParameter> isOut;
                            string helpText;
                            var parameters = DbDataParameters(dbName, command, fullProcedureName, out isOut, out helpText);

                            procedureProcessor(command, parameters, helpText, fullProcedureName);


                        }
                        catch (Exception)
                        {
                            if (!continueOnProcessorException)
                            {
                                throw;
                            }
                        }
                    }
                }
            }


        }

        public List<IDbDataParameter> DbDataParameters(string dbName, IDbCommand command, string fullProcedureName, out List<IDbDataParameter> isOut,
            out string helpText)
        {
            List<IDbDataParameter> parameters = GetProcedureParameters(command, dbName, fullProcedureName, out isOut);
            helpText = FetchHelpTextContinueOnException(fullProcedureName, _connection);
            return parameters;
        }

        // ReSharper disable InconsistentNaming

        public string FetchHelpTextContinueOnException(string fullProcedureName, IDbConnection con)
        {
            string helpText = "";

            try
            {
                if (!_isTesting)
                {
                    helpText = GetHelpText(con, fullProcedureName);
                }

            }
            catch (Exception e)
            {
                helpText = "Could not fetch because of : " + e.Message;
            }

            return helpText;
        }

        #endregion

        #region VerifyConnection

        public void VerifyConnection()
        {
            if (!IsConnected)
            {
                throw new Exception("Please connect first.");
            }
        }

        #endregion

        #region Connect

        public bool Connect(string connectionString)
        {
            if (connectionString.Contains("Database"))
            {
                connectionString = connectionString.Replace(connectionString.Substring(connectionString.IndexOf("Database", StringComparison.Ordinal)), "");
            }
            _connection = (OracleConnection)_factory.CreateConnection(connectionString);
            if (!_isTesting)
            {
                _connection.Open();
            }

            return true;
        }

        public bool Connect(string connectionString, CommandType commandType, string commandText)
        {
            if (connectionString != null)
            {
                if (connectionString.Contains("Database"))
                {
                    connectionString = connectionString.Replace(connectionString.Substring(connectionString.IndexOf("Database", StringComparison.Ordinal)), "");
                }
            }

            CreateConnect(connectionString, commandType, commandText);

            return true;
        }

        private void CreateConnect(string connectionString, CommandType commandType, string commandText)
        {
            if (!_isTesting)
            {
                _connection = (OracleConnection)_factory.CreateConnection(connectionString);

                VerifyArgument.IsNotNull("commandText", commandText);
                if (commandText.ToLower().StartsWith("select "))
                {
                    commandType = CommandType.Text;
                }

                _command = _factory.CreateCommand(_connection, commandType, commandText);

                _connection.Open();
            }
        }

        #endregion

        private static T ExecuteReader<T>(IDbCommand command, CommandBehavior commandBehavior, Func<IDataReader, T> handler)
        {
            try
            {
                if (command.CommandType == CommandType.StoredProcedure)
                {
                    OracleParameter obj = new OracleParameter();

                    for (int i = 0; i < command.Parameters.Count; i++)
                    {
                        OracleParameter temp = (OracleParameter)command.Parameters[i];

                        if (temp.OracleDbType == OracleDbType.RefCursor)
                        {
                            obj = (OracleParameter)command.Parameters[i];
                        }
                    }
                    if (obj.ParameterName.Length > 0)
                    {
                        try
                        {
                            command.ExecuteNonQuery();
                            using (OracleDataReader reader = ((OracleRefCursor)obj.Value).GetDataReader())
                            {
                                return handler(reader);
                            }
                        }
                        catch (Exception e)
                        {
                            using (IDataReader reader = command.ExecuteReader(commandBehavior))
                            {
                                Console.WriteLine(e);
                                return handler(reader);
                            }
                        }
                    }
                    using (IDataReader reader = command.ExecuteReader(commandBehavior))
                    {
                        return handler(reader);
                    }
                }
                using (IDataReader reader = command.ExecuteReader(commandBehavior))
                {
                    return handler(reader);
                }
            }
            catch (DbException e)
            {
                if (e.Message.Contains("There is no text for object "))
                {
                    var exceptionDataTable = new DataTable("Error");
                    exceptionDataTable.Columns.Add("ErrorText");
                    exceptionDataTable.LoadDataRow(new object[] { e.Message }, true);
                    return handler(new DataTableReader(exceptionDataTable));
                }
                throw;
            }
        }

        public static void AddParameters(IDbCommand command, ICollection<IDbDataParameter> parameters)
        {
            command.Parameters.Clear();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (IDbDataParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
        }

        public DataTable GetSchema(IDbConnection connection)
        {
            const string CommandText = GlobalConstants.SchemaQueryOracle;
            if (_isTesting)
            {
                DataTable proceduresDataTable = new DataTable();
                proceduresDataTable.Columns.Add("NAME");
                proceduresDataTable.Columns.Add("DB");
                proceduresDataTable.Rows.Add("Test", "Test");
                return proceduresDataTable;
            }
            using (IDbCommand command = _factory.CreateCommand(connection, CommandType.Text, string.Format(CommandText, _owner)))
            {
                return FetchDataTable(command);
            }
        }

        public string GetHelpText(IDbConnection connection, string objectName)
        {
            using (IDbCommand command = _factory.CreateCommand(connection, CommandType.Text,
                    string.Format("SELECT text FROM all_source WHERE name='{0}' ORDER BY line", objectName)))
            {
                return ExecuteReader(command, CommandBehavior.SchemaOnly & CommandBehavior.KeyInfo,
                    delegate(IDataReader reader)
                    {
                        return GetStringBuilder(reader);
                    });
            }
        }

        public string GetStringBuilder(IDataReader reader)
        {
            var sb = new StringBuilder();
            while (reader.Read())
            {
                object value = reader.GetValue(0);
                if (value != null)
                {
                    sb.Append(value);
                }
            }
            return sb.ToString();
        }

        public List<OracleParameter> GetProcedureOutParams(string fullProcedureName, string dbName)
        {
            using (IDbCommand command = _factory.CreateCommand(_connection, CommandType.StoredProcedure, fullProcedureName))
            {
                if (!_isTesting)
                {
                    List<IDbDataParameter> isOut;
                    GetProcedureParameters(command, dbName, fullProcedureName, out isOut);
                    return isOut.Select(a => a as OracleParameter).ToList();
                }
            }

            return new List<OracleParameter>();
        }

        public List<IDbDataParameter> GetProcedureParameters(IDbCommand command, string dbName, string procedureName, out List<IDbDataParameter> outParams)
        {
            outParams = new List<IDbDataParameter>();
            //Please do not use SqlCommandBuilder.DeriveParameters(command); as it does not handle CLR procedures correctly.
            string originalCommandText = command.CommandText;
            var parameters = new List<IDbDataParameter>();
            command.CommandType = CommandType.Text;
            command.CommandText = string.Format("SELECT * from all_arguments where owner = '{0}' and object_name = '{1}'",
                    dbName, procedureName.Substring(procedureName.IndexOf(".", StringComparison.Ordinal) + 1));

            DataTable dataTable = FetchDataTable(command);
            if (!_isTesting)
            {
                GetParamValues(command, outParams, dataTable, parameters);
            }
            command.CommandText = originalCommandText;
            return parameters;
        }

        public void GetParamValues(IDbCommand command, List<IDbDataParameter> outParams, DataTable dataTable, List<IDbDataParameter> parameters)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                var parameterName = row["ARGUMENT_NAME"] as string;
                var InOut = row["IN_OUT"] as string;

                bool isout = GetIsout(InOut);
                const ParameterDirection direction = ParameterDirection.Output;

                if (!String.IsNullOrEmpty(parameterName))
                {
                    OracleDbType OracleType;

                    Enum.TryParse(((string)row["DATA_TYPE"]).Replace(" ", ""), true, out OracleType);
                    OracleParameter OracleParameter = GetOracleParameter(OracleType, row, parameterName, direction);

                    GetOutParamProperties(command, outParams, parameters, isout, OracleParameter);
                }
            }
        }

        public OracleParameter GetOracleParameter(OracleDbType OracleType, DataRow row, string parameterName, ParameterDirection direction)
        {
            OracleParameter OracleParameter;
            if (OracleType == 0)
            {
                string dataType = row["DATA_TYPE"].ToString();
                OracleType = GetOracleDbType(dataType);

                OracleParameter = new OracleParameter(parameterName, OracleType) { Direction = direction };
            }
            else
            {
                OracleParameter = new OracleParameter(parameterName, OracleType) { Direction = direction };
            }
            return OracleParameter;
        }

        public void GetOutParamProperties(IDbCommand command, List<IDbDataParameter> outParams, List<IDbDataParameter> parameters, bool isout, OracleParameter OracleParameter)
        {
            if (!isout)
            {
                OracleParameter.Direction = ParameterDirection.Input;
                if (!_isTesting)
                {
                    command.Parameters.Add(OracleParameter);
                }
                parameters.Add(OracleParameter);
            }
            else
            {
                outParams.Add(OracleParameter);
            }
        }

        public OracleDbType GetOracleDbType(string dataType)
        {
            OracleDbType OracleType;
            switch (dataType.ToUpper())
            {
                case "NUMBER":
                    {
                        OracleType = OracleDbType.Decimal;
                        break;
                    }
                case "FLOAT":
                    {
                        OracleType = OracleDbType.Double;
                        break;
                    }
                default:
                    {
                        OracleType = OracleDbType.Varchar2;
                        break;
                    }
            }
            return OracleType;
        }

        public bool GetIsout(string InOut)
        {
            bool isout = !(InOut != null && InOut.Contains("IN/OUT"));
            if (InOut == "IN ")
            {
                return false;
            }
            return isout;
        }


        public static bool IsStoredProcedure(DataRow row, DataColumn procedureTypeColumn)
        {
            if (row == null || procedureTypeColumn == null)
            {
                return false;
            }
            return row[procedureTypeColumn].ToString().Equals("SQL_STORED_PROCEDURE") ||
                   row[procedureTypeColumn].ToString().Equals("CLR_STORED_PROCEDURE");
        }

        public static bool IsFunction(DataRow row, DataColumn procedureTypeColumn)
        {
            if (row == null || procedureTypeColumn == null)
            {
                return false;
            }

            return row[procedureTypeColumn].ToString().Equals("SQL_SCALAR_FUNCTION");
        }

        public static bool IsTableValueFunction(DataRow row, DataColumn procedureTypeColumn)
        {
            if (row == null || procedureTypeColumn == null)
            {
                return false;
            }

            return row[procedureTypeColumn].ToString().Equals("SQL_TABLE_VALUED_FUNCTION");
        }

        #region IDisposable

        private bool _disposed;

        public OracleServer()
        {
            _factory = new OracleSqlFactory();
        }

        public OracleServer(IDbFactory dbFactory)
        {
            _factory = dbFactory;
        }

        // Implement IDisposable. 
        // Do not make this method virtual. 
        // A derived class should not be able to override this method. 
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        ~OracleServer()
        {
            // Do not re-create Dispose clean-up code here. 
            // Calling Dispose(false) is optimal in terms of 
            // readability and maintainability.
            Dispose(false);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. 
        // If disposing equals true, the method has been called directly 
        // or indirectly by a user's code. Managed and unmanaged resources 
        // can be disposed. 
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed. 
        void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                    }

                    if (_command != null)
                    {
                        _command.Dispose();
                    }

                    if (_connection != null)
                    {
                        if (_connection.State != ConnectionState.Closed)
                        {
                            _connection.Close();
                        }
                        _connection.Dispose();
                    }
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here. 
                // If disposing is false, 
                // only the following code is executed.

                // Note disposing has been done.
                _disposed = true;
            }
        }

        #endregion
    }
}
