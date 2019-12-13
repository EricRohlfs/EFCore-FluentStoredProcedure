using System;
using Moq;
using Xunit;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Tests.Extensions.ProcedureBuilderTests.EfContexts;
using Snickler.EFCore;

namespace Tests.Extensions.ProcedureBuilderTests
{
    public class ProcedureBuilderLoadStoredProcWithBassFishingContextTests
    {
        private BassFishingContext Context { get; set; }
        private Mock<DbCommand> MockDbCommand { get; set; }

        private Mock<ProcedureBuilder> MockProcedureBuilder { get; set; }

        private string StoredProcName = "dbo.GetCitationListByYear";


        public ProcedureBuilderLoadStoredProcWithBassFishingContextTests()
        {
            var options = new DbContextOptionsBuilder<BassFishingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            Context = new BassFishingContext(options);
            MockDbCommand = new Mock<DbCommand>();
            MockDbCommand.SetupAllProperties();
            MockProcedureBuilder = new Mock<ProcedureBuilder>();
            MockProcedureBuilder.Setup(x => x.CreateDbCommand()).Returns(MockDbCommand.Object);
        }

        [Fact]
        public void LoadStoredProcSetsStoredProcNameAndCommandType()
        {
            MockProcedureBuilder.CallBase = true;
            MockProcedureBuilder.Object.LoadStoredProc(Context, StoredProcName, false);
            Assert.Equal(StoredProcName, MockProcedureBuilder.Object.Command.CommandText);
            Assert.Equal(
                CommandType.StoredProcedure,
                MockProcedureBuilder.Object.Command.CommandType);
        }

        [Fact]
        public void GivenPrependSchemaIsTrueSetsDefaultSchemaFromContext()
        {
            const string procName = "GetStateRecord";
            MockProcedureBuilder.CallBase = true;
            MockProcedureBuilder.Object.LoadStoredProc(Context, procName, true);
            var expected =
                Context.Model.Relational().DefaultSchema + "." + procName;
            Assert.Equal(expected, MockDbCommand.Object.CommandText);
        }

    }
}
