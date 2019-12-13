using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Snickler.EFCore;
using Tests.Extensions.ProcedureBuilderTests.EfContexts;
using Xunit;

namespace Tests.Extensions.ProcedureBuilderTests
{
    //https://github.com/ctigeek/CSharpQueryHelper/blob/master/CSharpQueryHelper/Testing/MockDatabase.cs
    public class ProcedureBuilderAddParameterTests
    {
        private BassFishingContext Context { get; set; }
        private Mock<IDbCommand> MockDbCommand { get; set; }

        private Mock<ProcedureBuilder> MockProcedureBuilder { get; set; }

        private string StoredProcName = "dbo.GetCitationListByYear";

        public ProcedureBuilderAddParameterTests()
        {
            var options = new DbContextOptionsBuilder<BassFishingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            Context = new BassFishingContext(options);
            MockProcedureBuilder = new Mock<ProcedureBuilder>();

            MockProcedureBuilder.Setup(
                x => x.LoadStoredProc(
                    It.IsAny<DbContext>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()));
        }
        [Fact]
        public void CanAddParameter()
        {
            var paramNameYear = "year";
            var paramValueYear = "1990";
            Expression<Func<DbParameterCollection, int>> ParametersAddTestExpression(string paramName, string paramValue1)
            {
                return x => x.Add(It.Is<DbParameter>(y => y.ParameterName == paramName && (string)y.Value == paramValue1));
            }

            MockDatabaseFactory.DbParameter = MockDatabaseFactory.CreateDbParameter();
            MockDatabaseFactory.Parameters = new Mock<DbParameterCollection>();
            MockDatabaseFactory.DbConnection = MockDatabaseFactory.CreateDbConnection();
            MockDatabaseFactory.DbCommand = MockDatabaseFactory.CreateDbCommand();

            MockDatabaseFactory.Parameters.Setup(
                ParametersAddTestExpression(paramNameYear, paramValueYear));

            var yearParam = MockDatabaseFactory.CreateDbParameter();
            MockProcedureBuilder.Setup(x => x.CreateParameter())
            .Returns(yearParam.Object);

            MockProcedureBuilder.Object.Command = MockDatabaseFactory.DbCommand.Object;
            MockProcedureBuilder.Object.Command.CommandText = StoredProcName;
            MockProcedureBuilder.Object.Command.CommandType = CommandType.StoredProcedure;

            MockProcedureBuilder.Object.LoadStoredProc(Context, StoredProcName, false);
            MockProcedureBuilder.Object.AddParameter(paramNameYear, paramValueYear);
            MockDatabaseFactory.Parameters.Verify(ParametersAddTestExpression(paramNameYear, paramValueYear), Times.Once);
        }

        [Fact]
        public void CallingAddParameterWithoutCallingLoadStoredProcFristThrowsError()
        {
            MockDatabaseFactory.DbParameter = MockDatabaseFactory.CreateDbParameter();
            MockDatabaseFactory.Parameters = new Mock<DbParameterCollection>();
            MockDatabaseFactory.DbConnection = MockDatabaseFactory.CreateDbConnection();
            MockDatabaseFactory.DbCommand = MockDatabaseFactory.CreateDbCommand();
            MockProcedureBuilder.Object.Command = MockDatabaseFactory.DbCommand.Object;
            Assert.Throws<InvalidOperationException>(() => MockProcedureBuilder.Object.AddParameter("year", "2000"));
        }
    }
}
