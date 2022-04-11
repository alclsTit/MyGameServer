using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

using ServerEngine.Common;
using ServerEngine.Log;

namespace ServerEngine.Network.SystemLib
{
    public class MssqlProcessor
    {
        public Logger logger { get; private set; }

        // https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver15
        private readonly int MAX_QUERY_STRING_LENGTH = 65536 * 4096;
        
        private static readonly Lazy<MssqlProcessor> mInstance = new Lazy<MssqlProcessor>(() => new MssqlProcessor());
        public static MssqlProcessor Instance => mInstance.Value;
        private MssqlProcessor() {}

        public void Initialize(Logger logger)
        {
            this.logger = this.logger ?? logger;
        }

        private SqlConnection Connect(eMSSQL_DBTYPE dbName)
        {
            if (IsExistDBServer(dbName) && MssqlConfigManager.IsExist(dbName))
            {
                var connection = new SqlConnection(MssqlConfigManager.msDBConfigMap[dbName].GetConnectionString());
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                return connection;
            }

            return null;
        }

        private async Task<SqlConnection> ConnectAsync(eMSSQL_DBTYPE dbName)
        {
            if (IsExistDBServer(dbName) && MssqlConfigManager.IsExist(dbName))
            {
                var connection = new SqlConnection(MssqlConfigManager.msDBConfigMap[dbName].GetConnectionString());
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                return connection;
            }

            return null;
        }

        private bool IsOverQueryLengthMax(int length) => length > MAX_QUERY_STRING_LENGTH;

        private bool IsExistDBServer(eMSSQL_DBTYPE dbName) => eMSSQL_DBTYPE._MIN_NO_TYPE < dbName && dbName < eMSSQL_DBTYPE._MAX_NO_TYPE;


        #region "DB Query (SYNC)"
        public sDBResultModify ExecuteQueryModify(eMSSQL_DBTYPE dbName, string query, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
               return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        command.Prepare();
                        var affectedRows = command.ExecuteNonQuery();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultModify ExecuteQueryModify(eMSSQL_DBTYPE dbName, string query, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                            command.Parameters.Add(parameter);

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        command.Prepare();
                        var affectedRows = command.ExecuteNonQuery();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultModify ExecuteQueryModify(eMSSQL_DBTYPE dbName, string query, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        command.Prepare();
                        var affectedRows = command.ExecuteNonQuery();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultSelect ExecuteQuerySelect(eMSSQL_DBTYPE dbName, string query, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                command.Prepare();
                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public sDBResultSelect ExecuteQuerySelect(eMSSQL_DBTYPE dbName, string query, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                        {
                            parameter.Direction = ParameterDirection.Input;
                            command.Parameters.Add(parameter);
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                command.Prepare();
                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public sDBResultSelect ExecuteQuerySelect(eMSSQL_DBTYPE dbName, string query, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                command.Prepare();
                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }
        #endregion

        #region "DB Query (ASYNC)"
        public async Task<sDBResultModify> ExecuteQueryModifyAsync(eMSSQL_DBTYPE dbName, string query, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        await command.PrepareAsync();
                        var affectedRows = await command.ExecuteNonQueryAsync();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultModify> ExecuteQueryModifyAsync(eMSSQL_DBTYPE dbName, string query, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                            command.Parameters.Add(parameter);

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        await command.PrepareAsync();
                        var affectedRows = await command.ExecuteNonQueryAsync();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultModify> ExecuteQueryModifyAsync(eMSSQL_DBTYPE dbName, string query, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultModify(false, 0, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        await command.PrepareAsync();
                        var affectedRows = await command.ExecuteNonQueryAsync();

                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultSelect> ExecuteQuerySelectAsync(eMSSQL_DBTYPE dbName, string query, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                await command.PrepareAsync();
                                var affectedRows = await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public async Task<sDBResultSelect> ExecuteQuerySelectAsync(eMSSQL_DBTYPE dbName, string query, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                        {
                            parameter.Direction = ParameterDirection.Input;
                            command.Parameters.Add(parameter);
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                await command.PrepareAsync();
                                var affectedRows = await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public async Task<sDBResultSelect> ExecuteQuerySelectAsync(eMSSQL_DBTYPE dbName, string query, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            if (IsOverQueryLengthMax(query.Length))
                return new sDBResultSelect(false, 0, null, 0);

            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                await command.PrepareAsync();
                                var affectedRows =  await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }
        #endregion

        #region "DB StoredProcedure (SYNC)"
        public sDBResultModify ExecuteSPModify(eMSSQL_DBTYPE dbName, string procName, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = command.ExecuteNonQuery();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultModify ExecuteSPModify(eMSSQL_DBTYPE dbName, string procName, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                            command.Parameters.Add(parameter);

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = command.ExecuteNonQuery();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultModify ExecuteSPModify(eMSSQL_DBTYPE dbName, string procName, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = command.ExecuteNonQuery();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public sDBResultSelect ExecuteSPSelect(eMSSQL_DBTYPE dbName, string procName, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public sDBResultSelect ExecuteSPSelect(eMSSQL_DBTYPE dbName, string procName, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                        {
                            parameter.Direction = ParameterDirection.Input;
                            command.Parameters.Add(parameter);
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public sDBResultSelect ExecuteSPSelect(eMSSQL_DBTYPE dbName, string procName, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = command.ExecuteNonQuery();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }
        #endregion

        #region "DB StoredProcedure (ASYNC)"
        public async Task<sDBResultModify> ExecuteSPModifyAsync(eMSSQL_DBTYPE dbName, string procName, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = await command.ExecuteNonQueryAsync();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultModify> ExecuteSPModifyAsync(eMSSQL_DBTYPE dbName, string procName, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                            command.Parameters.Add(parameter);

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = await command.ExecuteNonQueryAsync();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultModify> ExecuteSPModifyAsync(eMSSQL_DBTYPE dbName, string procName, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        var affectedRows = await command.ExecuteNonQueryAsync();
                        return output == null ? new sDBResultModify(true, affectedRows) : new sDBResultModify(true, affectedRows, (int)command.Parameters[output.ParameterName].Value);               
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultModify(false, 0, 0);
        }

        public async Task<sDBResultSelect> ExecuteSPSelectAsync(eMSSQL_DBTYPE dbName, string procName, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public async Task<sDBResultSelect> ExecuteSPSelectAsync(eMSSQL_DBTYPE dbName, string procName, SqlParameter parameter, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameter != null)
                        {
                            parameter.Direction = ParameterDirection.Input;
                            command.Parameters.Add(parameter);
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output);
                        }

                        try
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }

        public async Task<sDBResultSelect> ExecuteSPSelectAsync(eMSSQL_DBTYPE dbName, string procName, IEnumerable<SqlParameter> parameters = null, SqlParameter output = null, int timeout = 15)
        {
            try
            {
                using(var connection = await ConnectAsync(dbName))
                {
                    using(var command = new SqlCommand(procName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = timeout;

                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                parameter.Direction = ParameterDirection.Input;
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (output != null)
                        {
                            output.Direction = ParameterDirection.Output;
                            command.Parameters.Add(output); 
                        }

                        try
                        {
                            using(var adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                var affectedRows = await command.ExecuteNonQueryAsync();

                                return output == null ? new sDBResultSelect(true, affectedRows, dt) : new sDBResultSelect(true, affectedRows, dt, (int)command.Parameters[output.ParameterName].Value);
                            }
                        }
                        catch (SqlException) { throw; }
                        catch (Exception) { throw; }
                        finally
                        {
                            if (command.Parameters.Count > 0)
                                command.Parameters.Clear();
                        }
                    }    
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }

            return new sDBResultSelect(false, 0, null, 0);
        }
        #endregion

        #region "DB InsertBulk (SYNC)"
        public void ExecuteBulkInsertTR(eMSSQL_DBTYPE dbName, DataTable dataTable, string tableName, int timeout = 15)
        {
            try
            {
                using (var connection = Connect(dbName))
                {
                    using (var tran = connection.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, tran))
                        {
                            bool result = false;
                            try
                            {
                                bulkCopy.DestinationTableName = tableName;
                                bulkCopy.WriteToServer(dataTable);

                                tran.Commit();
                                result = true;
                            }
                            catch (SqlException) { throw; }
                            catch (Exception) { throw; }
                            finally
                            {
                                if (!result)
                                    tran.Rollback();
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }
        #endregion

        #region "DB InsertBulk (ASYNC)"
        public async Task ExecuteBulkInsertTRAsync(eMSSQL_DBTYPE dbName, DataTable dataTable, string tableName, int timeout = 15)
        {
            try
            {
                using (var connection = await ConnectAsync(dbName))
                {
                    using (var tran = connection.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, tran))
                        {
                            bool result = false;
                            try
                            {
                                bulkCopy.DestinationTableName = tableName;
                                await bulkCopy.WriteToServerAsync(dataTable);

                                tran.Commit();
                                result = true;
                            }
                            catch (SqlException) { throw; }
                            catch (Exception) { throw; }
                            finally
                            {
                                if (!result)
                                    tran.Rollback();
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), sqlEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }
        #endregion
    }
}
