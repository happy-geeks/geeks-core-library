using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Services;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Moq;
using NUnit.Framework;
using FluentAssertions;

namespace GeeksCoreLibrary.Tests.Core.Services
{
    public class WiserItemsServiceTests
    {
        private Mock<IDatabaseConnection> databaseConnectionMock;
        private Mock<IObjectsService> objectsServiceMock;
        private Mock<IStringReplacementsService> stringReplacementsServiceMock;
        private Mock<IDataSelectorsService> dataSelectosServiceMock;
        private Mock<IDatabaseHelpersService> databaseHelpersServiceMock;
        private WiserItemsService wiserItemsService;
        
        [SetUp]
        public void Setup()
        {
            // Create mocks.
            databaseConnectionMock = new Mock<IDatabaseConnection>();
            objectsServiceMock = new Mock<IObjectsService>();
            stringReplacementsServiceMock = new Mock<IStringReplacementsService>();
            dataSelectosServiceMock = new Mock<IDataSelectorsService>();
            databaseHelpersServiceMock = new Mock<IDatabaseHelpersService>();
            
            // Create the service that we're testing.
            wiserItemsService = new WiserItemsService(databaseConnectionMock.Object, objectsServiceMock.Object, stringReplacementsServiceMock.Object, dataSelectosServiceMock.Object, databaseHelpersServiceMock.Object);
            
            // Setup the mocks.
            databaseConnectionMock.Setup(x => x.GetAsync(It.Is<string>(query => query.Contains("SELECT permission.permissions")), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(() =>
                {
                    var dataTable = new DataTable();
                    dataTable.Columns.Add("permissions", typeof(int));
                    dataTable.Rows.Add(15);
                    return dataTable;
                });
            
            databaseConnectionMock.Setup(x => x.GetAsync(It.Is<string>(query => query.Contains("SELECT readonly")), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(() =>
                {
                    var dataTable = new DataTable();
                    dataTable.Columns.Add("readonly", typeof(bool));
                    dataTable.Rows.Add(false);
                    return dataTable;
                });
        }

        [Test]
        [TestCase(EntityActions.Read, AccessRights.Read)]
        [TestCase(EntityActions.Create, AccessRights.Create)]
        [TestCase(EntityActions.Delete, AccessRights.Delete)]
        [TestCase(EntityActions.Update, AccessRights.Update)]
        public async Task CheckIfEntityActionIsPossibleAsync_ItemAndPermissions_ReturnsSuccess(EntityActions action, AccessRights expectedFlag)
        {
            var actual = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(1, action, 2, null, true, null);
            actual.ok.Should().BeTrue();
            actual.errorMessage.Should().BeNullOrEmpty();
            actual.permissions.Should().HaveFlag(expectedFlag, $"because we want to {expectedFlag.ToString()} an item");
        }
    }
}