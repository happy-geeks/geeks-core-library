using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Services;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Modules.GclReplacements.Services;

public class StringReplacementsServiceTests
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IStringReplacementsService stringReplacementsService;
    private Mock<IObjectsService> objectsServiceMock;
    private Mock<ILanguagesService> languagesServiceMock;
    private Mock<IAccountsService> accountsServiceMock;
    private Mock<IHttpContextAccessor> httpContextAccessorMock;
    private Mock<IDatabaseConnection> databaseConnectionMock;

    private IOptions<GclSettings> gclSettingsMock;
    private IReplacementsMediator replacementsMediator;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [SetUp]
    public void Setup()
    {
        // Create mocks.
        gclSettingsMock = Options.Create(new GclSettings());
        objectsServiceMock = new Mock<IObjectsService>();
        languagesServiceMock = new Mock<ILanguagesService>();
        accountsServiceMock = new Mock<IAccountsService>();
        httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        databaseConnectionMock = new Mock<IDatabaseConnection>();

        // Create the service that we're testing.
        replacementsMediator = new ReplacementsMediator(databaseConnectionMock.Object, httpContextAccessorMock.Object);
        stringReplacementsService = new StringReplacementsService(gclSettingsMock, objectsServiceMock.Object, languagesServiceMock.Object, accountsServiceMock.Object, replacementsMediator, httpContextAccessorMock.Object);

        // Setup the mocks.
        var context = new DefaultHttpContext();
        context.Request.Headers["HeaderVariable1"] = "HeaderValue1";
        context.Request.Headers["HeaderVariable2"] = "HeaderValue2";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("happyhorizon.com", 443);
        context.Request.PathBase = "/test";
        httpContextAccessorMock.Setup(httpContextAccessor => httpContextAccessor.HttpContext).Returns(context);

        accountsServiceMock.Setup(x => x.DoAccountReplacementsAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string input, bool forQuery) =>
            {
                var replaceData = new Dictionary<string, object>
                {
                    {"UserId", 10},
                    {"MainUserId", 20},
                    {"EntityType", "account"},
                    {"MainUserEntityType", "account"},
                    {"Roles", new List<RoleModel> {new() {Id = 1, Name = "Role1"}, new() {Id = 2, Name = "Role2"}}},
                    {"LoginDate", DateTime.Now},
                    {"Expires", DateTime.Now.AddMonths(1)},
                    {"IpAddress", "127.0.0.1"},
                    {"UserAgent", "UnitTest"}
                };

                input = replacementsMediator.DoReplacements(input, replaceData, forQuery: forQuery, prefix: "{Account_");
                input = replacementsMediator.DoReplacements(input, replaceData, forQuery: forQuery, prefix: "{AccountWiser2_");
                return input;
            });
    }

    [Test]
    [TestCase("")]
    [TestCase("String without variables")]
    [TestCase("String with {invalid variable")]
    [TestCase("String with invalid} variable")]
    [TestCase("String with invalid {} variable")]
    [TestCase("String with valid but not existing {test} variable")]
    public async Task DoAllReplacementsAsync_StringWithoutReplacements_ReturnsInput(string input)
    {
        // Act
        var actual = await stringReplacementsService.DoAllReplacementsAsync(input, null, false, true, false, false, "HtmlEncode", false);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any replacement variables");
    }
}