using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Snickler.EFCore
{
    /* Example of testing Procedure builder And the mocking out the result reader.
     * Mocking the callback was hard gotten information.
     * [Fact]
        public void ReturnsDistinctRecords()
        {
            const int bassGuitarId = 221;

            var dataWithDuplicates = new List<BassGuitar> {
                new BassGuitar {CapabilityID = 1},
                new BassGuitar {CapabilityID = 1},
                new BassGuitar {CapabilityID = 2},
                new BassGuitar {CapabilityID = 2}
            };
            var dbTool = new Mock<IProcedureBuilder>();

            var map = new Mock<IResultReader>();
            map.Setup(m => m.ReadToList<BassGuitar>())
                .Returns(dataWithDuplicates);
            //have to fill out all parameters, even the default ones for this to work.
            dbTool.Setup(
                t => t.Execute(
                    It.IsAny<DbCommand>(),
                    It.IsAny<Action<IResultReader>>(),
                    CommandBehavior.Default,
                    true)).Callback(
                (DbCommand cmd, Action<IResultReader> sr, CommandBehavior y, bool x) =>
                {
                    sr.Invoke(map.Object);
                });

            var actual = DAL.GetBassGuitar(bassGuitarId);
            Assert.Equal(2, actual.Data.Count());
        }
     */

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ProcedureBuilder : IProcedureBuilder
    {
        private DbContext Context { get; set; }
        public DbCommand Command { get; set; }

        public virtual void LoadStoredProc(
            DbContext context,
            string storedProcName,
            bool prependDefaultSchema = true)
        {
            Context = context;
            Command = CreateDbCommand();
            if (prependDefaultSchema)
            {
                var schemaName = context.Model.Relational().DefaultSchema;
                if (schemaName != null)
                {
                    storedProcName = $"{schemaName}.{storedProcName}";
                }
                else
                {
                    var msg =
                        "Trying to prepend a schema to the stored procedure when the context does not have a default schema can cause unwanted side effects.  Please change the argument (prependDefaultSchema = false), or add a default schema to your context.";
                    throw new ArgumentException(msg);
                }
            }
            Command.CommandText = storedProcName;
            Command.CommandType = CommandType.StoredProcedure;
        }

        //wrapped to support unit testing
        public virtual DbCommand CreateDbCommand()
        {
            return Context.Database.GetDbConnection().CreateCommand();
        }

        public virtual DbParameter CreateParameter()
        {
            return Command.CreateParameter();
        }

        public DbCommand AddParameter(
            string paramName,
            object paramValue,
            Action<DbParameter> configureParam = null)
        {
            if (string.IsNullOrEmpty(Command.CommandText) && Command.CommandType
                != CommandType.StoredProcedure)
                throw new InvalidOperationException(
                    "Call LoadStoredProc before using method");

            var param = CreateParameter();
            param.ParameterName = paramName;
            param.Value = paramValue ?? DBNull.Value;
            configureParam?.Invoke(param);
            Command.Parameters.Add(param);
            return Command;
        }

        //wraper to facilitate mocking in unit tests
        public virtual int ExecuteNonQuery()
        {
            return Command.ExecuteNonQuery();
        }

        public void Execute(
            Action<IResultReader> handleResults,
            CommandBehavior commandBehaviour =
                CommandBehavior.Default,
            bool manageConnection = true)
        {
            if (handleResults == null)
            {
                throw new ArgumentNullException(nameof(handleResults));
            }

            using (Command)
            {
                if (manageConnection && Command.Connection.State
                    == ConnectionState.Closed)
                    Command.Connection.Open();
                try
                {
                    using (var reader =
                        Command.ExecuteReader(commandBehaviour))
                    {
                        var sprocResults = new ResultReader(reader);
                        handleResults(sprocResults);
                    }
                }
                finally
                {
                    if (manageConnection)
                    {
                        Command.Connection.Close();
                    }
                }
            }
        }

    }
    public interface IProcedureBuilder
    {
        void LoadStoredProc(
            DbContext context,
            string storedProcedureName,
            bool prependDefaultSchema = true);

        DbCommand AddParameter(
            string paramName,
            object paramValue,
            Action<DbParameter> configureParam = null);

        void Execute(
            Action<IResultReader> handleResults,
            CommandBehavior commandBehaviour =
                System.Data.CommandBehavior.Default,
            bool manageConnection = true);
        int ExecuteNonQuery();

    }

    public class ResultReader : IResultReader
    {

        private readonly DbDataReader _reader;

        public ResultReader(DbDataReader reader)
        {
            _reader = reader;
        }

        public IList<T> ReadToList<T>()
        {
            return MapToList<T>(_reader);
        }

        public T? ReadToValue<T>() where T : struct
        {
            return MapToValue<T>(_reader);
        }

        public Task<bool> NextResultAsync()
        {
            return _reader.NextResultAsync();
        }

        public Task<bool> NextResultAsync(CancellationToken ct)
        {
            return _reader.NextResultAsync(ct);
        }

        public bool NextResult()
        {
            return _reader.NextResult();
        }

        public IList<T> MapToList<T>(DbDataReader dr)
        {
            var objList = new List<T>();
            var props = typeof(T).GetRuntimeProperties().ToList();

            var colMapping = dr.GetColumnSchema()
                .Where(
                    x => props.Any(
                        y => y.Name.ToLower() == x.ColumnName.ToLower()))
                .ToDictionary(key => key.ColumnName.ToLower());

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    foreach (var prop in props)
                    {
                        if (colMapping.ContainsKey(prop.Name.ToLower()))
                        {
                            var column = colMapping[prop.Name.ToLower()];

                            if (column?.ColumnOrdinal != null)
                            {
                                var val = dr.GetValue(
                                    column.ColumnOrdinal.Value);
                                prop.SetValue(
                                    obj,
                                    val == DBNull.Value ? null : val);
                            }

                        }
                    }

                    objList.Add(obj);
                }
            }

            return objList;
        }
        public T? MapToValue<T>(DbDataReader dr) where T : struct
        {
            if (dr.HasRows)
            {
                if (dr.Read())
                {
                    return dr.IsDBNull(0)
                        ? new T?()
                        : dr.GetFieldValue<T>(0);
                }
            }

            return new T?();
        }
    }

    public interface IResultReader
    {
        IList<T> ReadToList<T>();
        T? ReadToValue<T>() where T : struct;
        Task<bool> NextResultAsync();
        Task<bool> NextResultAsync(CancellationToken ct);
        bool NextResult();
        IList<T> MapToList<T>(DbDataReader dr);
        T? MapToValue<T>(DbDataReader dr) where T : struct;
    }

}
