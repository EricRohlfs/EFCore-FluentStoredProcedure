using System;
using System.Data;
using System.Data.Common;
using Moq;
using Xunit;
using Snickler.EFCore;

namespace Tests.Extensions.ProcedureBuilderTests
{
    public class ProcedureBuilderExecuteTest
    {
        [Fact]
        public void ExecuteCallHandlerAndClosesConnection()
        {
            var reader = new Mock<DbDataReader>();
            MockDatabaseFactory.DbParameter = MockDatabaseFactory.CreateDbParameter();
            MockDatabaseFactory.Parameters = new Mock<DbParameterCollection>();
            MockDatabaseFactory.DbConnection = MockDatabaseFactory.CreateDbConnection();
            MockDatabaseFactory.DbCommand = MockDatabaseFactory.CreateDbCommand(reader.Object);

            var calledHandler = false;
            var handleResults =
                new Action<IResultReader>(x =>
                {
                    calledHandler = true;
                });

            var MockProcedureBuilder = new Mock<ProcedureBuilder>();
            MockProcedureBuilder.Object.Command = MockDatabaseFactory.DbCommand.Object;
            MockProcedureBuilder.Object.Execute(handleResults);
            MockDatabaseFactory.DbConnection.Verify(x => x.Close(), Times.Once);
            Assert.True(calledHandler);
        }


        [Fact]
        public void GivenManageConnectionFalseDoesNotTryToReOpenConnection()
        {
            var reader = new Mock<DbDataReader>();
            MockDatabaseFactory.DbParameter = MockDatabaseFactory.CreateDbParameter();
            MockDatabaseFactory.Parameters = new Mock<DbParameterCollection>();
            MockDatabaseFactory.DbConnection = MockDatabaseFactory.CreateDbConnection();
            MockDatabaseFactory.DbCommand = MockDatabaseFactory.CreateDbCommand(reader.Object);

            var calledHandler = false;
            var handleResults =
                new Action<IResultReader>(x =>
                {
                    calledHandler = true;
                });

            var MockProcedureBuilder = new Mock<ProcedureBuilder>();
            MockProcedureBuilder.Object.Command = MockDatabaseFactory.DbCommand.Object;
            MockProcedureBuilder.Object.Execute(handleResults, manageConnection: false);
            MockDatabaseFactory.DbConnection.Verify(x => x.Open(), Times.Never);
            Assert.True(calledHandler);
        }

        [Fact]
        public void GivenManageConnectionTrueAndConnectionIsOpenDoesNotTryToReOpenConnection()
        {
            var reader = new Mock<DbDataReader>();
            MockDatabaseFactory.DbParameter = MockDatabaseFactory.CreateDbParameter();
            MockDatabaseFactory.Parameters = new Mock<DbParameterCollection>();
            MockDatabaseFactory.DbConnection = MockDatabaseFactory.CreateDbConnection();
            MockDatabaseFactory.DbCommand = MockDatabaseFactory.CreateDbCommand(reader.Object);
            MockDatabaseFactory.DbConnection.SetupGet(x => x.State)
                .Returns(ConnectionState.Open);

            // SetupGet<ConnectionState>("State")
            // .Returns(ConnectionState.Open);
            var calledHandler = false;
            var handleResults =
                new Action<IResultReader>(x =>
                {
                    calledHandler = true;
                });

            var MockProcedureBuilder = new Mock<ProcedureBuilder>();
            MockProcedureBuilder.Object.Command = MockDatabaseFactory.DbCommand.Object;
            MockProcedureBuilder.Object.Execute(handleResults, manageConnection: true);
            MockDatabaseFactory.DbConnection.Verify(x => x.Open(), Times.Never);
            Assert.True(calledHandler);
        }
    }
}
