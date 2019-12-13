using System;
using Moq;
using Xunit;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Tests.Extensions.ProcedureBuilderTests.EfContexts;
using Snickler.EFCore;

namespace Tests.Extensions.ProcedureBuilderTests
{
    public class ProcedureBuilderLoadStoredProcWithSaltWaterFishingContextTests
    {
        private SaltWaterFishingContext Context { get; set; }
        private Mock<DbCommand> MockDbCommand { get; set; }

        private Mock<ProcedureBuilder> MockProcedureBuilder { get; set; }

        public ProcedureBuilderLoadStoredProcWithSaltWaterFishingContextTests()
        {
            var options = new DbContextOptionsBuilder<SaltWaterFishingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            Context = new SaltWaterFishingContext(options);
            MockDbCommand = new Mock<DbCommand>();
            MockDbCommand.SetupAllProperties();
            MockProcedureBuilder = new Mock<ProcedureBuilder>();
            MockProcedureBuilder.Setup(x => x.CreateDbCommand()).Returns(MockDbCommand.Object);
        }

        [Fact]
        public void GivenPrependDefaultSchemaIsTrueAndNoDefaultSchemaIsSetInContentReturnsWarningAboutHowToCorrect()
        {
            var procName = "GetStateRecord";
            MockProcedureBuilder.CallBase = true;
            Assert.Throws<ArgumentException>(
               () => MockProcedureBuilder.Object.LoadStoredProc(
                   Context,
                   procName, true));
        }

    }
}
