using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Services;
using Moq;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Modules.GclReplacements.Services;

public class ReplacementsMediatorTests
{
    private IReplacementsMediator replacementsMediator;
    private Mock<IDatabaseConnection> databaseConnectionMock;

    [SetUp]
    public void Setup()
    {
        databaseConnectionMock = new Mock<IDatabaseConnection>();
        replacementsMediator = new ReplacementsMediator(databaseConnectionMock.Object);
    }
}