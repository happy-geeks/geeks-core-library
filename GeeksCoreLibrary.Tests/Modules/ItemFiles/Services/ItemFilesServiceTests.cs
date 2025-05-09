using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.ItemFiles.Services;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Modules.ItemFiles.Services;

public class ItemFilesServiceTests
{
    // Byte arrays for the smallest possible image files of each type.
    private readonly byte[] jpegDummyBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x01, 0x3F, 0x10];
    private readonly byte[] pngDummyBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x37, 0x6E, 0xF9, 0x24, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x01, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x73, 0x75, 0x01, 0x18, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
    private readonly byte[] gifDummyBytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x21, 0xF9, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x00, 0x00];
    private readonly byte[] webpDummyBytes = [0x52, 0x49, 0x46, 0x46, 0x1A, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x4C, 0x0D, 0x00, 0x00, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x10, 0x07, 0x10, 0x11, 0x11, 0x88, 0x88, 0xFE, 0x07, 0x00];

    private const string MainQueryPart = "SELECT id, item_id, content_type, file_name, extension, added_on, added_by, property_name, protected, itemlink_id, content, content_url, extra_data, title";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ItemFilesService itemFilesService;
    private Mock<ILogger<ItemFilesService>> loggerMock;
    private Mock<IDatabaseConnection> databaseConnectionMock;
    private Mock<IFileCacheService> fileCacheServiceMock;
    private Mock<IObjectsService> objectsServiceMock;
    private Mock<IWiserItemsService> wiserItemsServiceMock;
    private Mock<IHttpClientService> httpClientServiceMock;
    private Mock<IAmazonS3Service> amazonS3ServiceMock;
    private Mock<IHttpContextAccessor> httpContextAccessorMock;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [SetUp]
    public void Setup()
    {
        // Create mocks.
        loggerMock = new Mock<ILogger<ItemFilesService>>();
        databaseConnectionMock = new Mock<IDatabaseConnection>();
        fileCacheServiceMock = new Mock<IFileCacheService>();
        objectsServiceMock = new Mock<IObjectsService>();
        wiserItemsServiceMock = new Mock<IWiserItemsService>();
        httpClientServiceMock = new Mock<IHttpClientService>();
        amazonS3ServiceMock = new Mock<IAmazonS3Service>();
        httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        // Create the service that we're testing.
        itemFilesService = new ItemFilesService(loggerMock.Object, databaseConnectionMock.Object, fileCacheServiceMock.Object, objectsServiceMock.Object, wiserItemsServiceMock.Object, httpClientServiceMock.Object, amazonS3ServiceMock.Object, httpContextAccessorMock.Object);

        // Setup the mocks.
        var context = new DefaultHttpContext();
        context.Request.Headers["HeaderVariable1"] = "HeaderValue1";
        context.Request.Headers["HeaderVariable2"] = "HeaderValue2";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("happyhorizon.com", 443);
        context.Request.PathBase = "/test";
        httpContextAccessorMock.Setup(httpContextAccessor => httpContextAccessor.HttpContext).Returns(context);

        var dataTable = new DataTable("wiser_itemfile");
        dataTable.Columns.Add("id", typeof(long));
        dataTable.Columns.Add("item_id", typeof(ulong));
        dataTable.Columns.Add("content_type", typeof(string));
        dataTable.Columns.Add("content", typeof(byte[]));
        dataTable.Columns.Add("content_url", typeof(string));
        dataTable.Columns.Add("file_name", typeof(string));
        dataTable.Columns.Add("extension", typeof(string));
        dataTable.Columns.Add("added_on", typeof(DateTime));
        dataTable.Columns.Add("added_by", typeof(string));
        dataTable.Columns.Add("title", typeof(string));
        dataTable.Columns.Add("property_name", typeof(string));
        dataTable.Columns.Add("itemlink_id", typeof(ulong));
        dataTable.Columns.Add("protected", typeof(bool));
        dataTable.Columns.Add("ordering", typeof(int));
        dataTable.Columns.Add("extra_data", typeof(string));

        #region Fake database data
        // Create some dummy data.
        dataTable.Rows.Add(1L, 123UL, "image/jpeg", jpegDummyBytes, null, "image_1.jpg", ".jpg", DateTime.UtcNow, "user", "Test Image", "images", 0UL, false, 1, null);
        dataTable.Rows.Add(2L, 123UL, "image/png", pngDummyBytes, null, "image_2.png", ".png", DateTime.UtcNow, "user", "Test Image 2", "images", 0UL, false, 2, null);
        dataTable.Rows.Add(3L, 123UL, "image/gif", gifDummyBytes, null, "image_3.gif", ".gif", DateTime.UtcNow, "user", "Test Image 3", "images", 0UL, false, 3, null);
        dataTable.Rows.Add(4L, 123UL, "image/webp", webpDummyBytes, null, "image_4.webp", ".webp", DateTime.UtcNow, "user", "Test Image 4", "images", 0UL, false, 4, null);

        dataTable.Rows.Add(5L, 0UL, "image/jpeg", jpegDummyBytes, null, "image_5.jpg", ".jpg", DateTime.UtcNow, "user", "Test Image 5", "images", 456UL, false, 1, null);
        dataTable.Rows.Add(6L, 0UL, "image/png", pngDummyBytes, null, "image_6.png", ".png", DateTime.UtcNow, "user", "Test Image 6", "images", 456UL, false, 2, null);
        dataTable.Rows.Add(7L, 0UL, "image/gif", gifDummyBytes, null, "image_7.gif", ".gif", DateTime.UtcNow, "user", "Test Image 7", "images", 456UL, false, 3, null);
        dataTable.Rows.Add(8L, 0UL, "image/webp", webpDummyBytes, null, "image_8.webp", ".webp", DateTime.UtcNow, "user", "Test Image 8", "images", 456UL, false, 4, null);

        dataTable.Rows.Add(9L, 321UL, "text/plain", Array.Empty<byte>(), null, "file_1.txt", ".txt", DateTime.UtcNow, "user", "Test File 1", "files", 0UL, false, 1, null);
        dataTable.Rows.Add(10L, 321UL, "text/xml", Array.Empty<byte>(), null, "file_2.xml", ".xml", DateTime.UtcNow, "user", "Test File 2", "files", 0UL, false, 2, null);
        dataTable.Rows.Add(11L, 321UL, "application/pdf", Array.Empty<byte>(), null, "file_3.pdf", ".pdf", DateTime.UtcNow, "user", "Test File 3", "files", 0UL, false, 3, null);
        dataTable.Rows.Add(12L, 321UL, "application/json", Array.Empty<byte>(), null, "file_4.json", ".json", DateTime.UtcNow, "user", "Test File 4", "files", 0UL, false, 4, null);

        dataTable.Rows.Add(13L, 0UL, "text/plain", Array.Empty<byte>(), null, "file_5.txt", ".txt", DateTime.UtcNow, "user", "Test File 5", "files", 654UL, false, 1, null);
        dataTable.Rows.Add(14L, 0UL, "text/xml", Array.Empty<byte>(), null, "file_6.xml", ".xml", DateTime.UtcNow, "user", "Test File 6", "files", 654UL, false, 2, null);
        dataTable.Rows.Add(15L, 0UL, "application/pdf", Array.Empty<byte>(), null, "file_7.pdf", ".pdf", DateTime.UtcNow, "user", "Test File 7", "files", 654UL, false, 3, null);
        dataTable.Rows.Add(16L, 0UL, "application/json", Array.Empty<byte>(), null, "file_8.json", ".json", DateTime.UtcNow, "user", "Test File 8", "files", 654UL, false, 4, null);
        #endregion

        databaseConnectionMock.Setup(x => x.GetAsync(It.Is<string>(query => query.Contains(MainQueryPart) && query.Contains("itemid = ?id")), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() =>
            {
                var testArguments = TestContext.CurrentContext.Test.Arguments;

                var result = dataTable.Clone();
                var rows = dataTable.Select($"item_id = {testArguments[0]} AND property_name = '{testArguments[1]}'");
                foreach (var row in rows)
                {
                    result.ImportRow(row);
                }

                return result;
            });

        databaseConnectionMock.Setup(x => x.GetAsync(It.Is<string>(query => query.Contains(MainQueryPart) && query.Contains("id = ?id")), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() =>
            {
                var testArguments = TestContext.CurrentContext.Test.Arguments;

                var result = dataTable.Clone();
                var rows = dataTable.Select($"id = {testArguments[0]}");
                foreach (var row in rows)
                {
                    result.ImportRow(row);
                }

                return result;
            });

        databaseConnectionMock.Setup(x => x.GetAsync(It.Is<string>(query => query.Contains(MainQueryPart) && query.Contains("itemlink_id = ?id")), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() =>
            {
                var testArguments = TestContext.CurrentContext.Test.Arguments;

                var result = dataTable.Clone();
                var rows = dataTable.Select($"itemlink_id = {testArguments[0]} AND property_name = '{testArguments[1]}'");
                foreach (var row in rows)
                {
                    result.ImportRow(row);
                }

                return result;
            });
    }

    #region Image tests

    [Test]
    [TestCase(123UL, "images", 800U, 600U, "image_1.jpg", 1, "image_1.jpg")]
    [TestCase(123UL, "images", 800U, 600U, "image_2.png", 2, "image_2.png")]
    [TestCase(123UL, "images", 800U, 600U, "image_3.gif", 3, "image_3.gif")]
    [TestCase(123UL, "images", 800U, 600U, "image_4.webp", 4, "image_4.webp")]
    public async Task GetWiserImageAsync_GetImage_ReturnsFileResultModel(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserItemImageAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    [Test]
    [TestCase(456UL, "images", 800U, 600U, "image_5.jpg", 1, "image_5.jpg")]
    [TestCase(456UL, "images", 800U, 600U, "image_6.png", 2, "image_6.png")]
    [TestCase(456UL, "images", 800U, 600U, "image_7.gif", 3, "image_7.gif")]
    [TestCase(456UL, "images", 800U, 600U, "image_8.webp", 4, "image_8.webp")]
    public async Task GetWiserItemLinkImageAsync_GetImage_ReturnsFileResultModel(ulong itemLinkId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserItemLinkImageAsync(itemLinkId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    [Test]
    [TestCase(1UL, 800U, 600U, "image_1.jpg", "image_1.jpg")]
    [TestCase(3UL, 800U, 600U, "image_3.gif", "image_3.gif")]
    [TestCase(5UL, 800U, 600U, "image_5.jpg", "image_5.jpg")]
    [TestCase(7UL, 800U, 600U, "image_7.gif", "image_7.gif")]
    public async Task GetWiserDirectImageAsync_GetImage_ReturnsFileResultModel(ulong itemId, uint preferredWidth, uint preferredHeight, string fileName, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserDirectImageAsync(itemId, preferredWidth, preferredHeight, fileName);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    [Test]
    [TestCase(123UL, "images", 800U, 600U, "image_1.jpg", "image_1.jpg")]
    [TestCase(123UL, "images", 800U, 600U, "image_2.png", "image_2.png")]
    [TestCase(123UL, "images", 800U, 600U, "image_3.gif", "image_3.gif")]
    [TestCase(123UL, "images", 800U, 600U, "image_4.webp", "image_4.webp")]
    public async Task GetWiserImageByFileNameAsync_GetImage_ReturnsFileResultModel(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserImageByFileNameAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    #endregion

    #region File tests

    [Test]
    [TestCase(321UL, "files", "file_1.txt", 1, "file_1.txt")]
    [TestCase(321UL, "files", "file_2.xml", 2, "file_2.xml")]
    [TestCase(321UL, "files", "file_3.pdf", 3, "file_3.pdf")]
    [TestCase(321UL, "files", "file_4.json", 4, "file_4.json")]
    public async Task GetWiserItemFileAsync_GetFile_ReturnsFileResultModel(ulong itemId, string propertyName, string fileName, int fileNumber, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserItemFileAsync(itemId, propertyName, fileName, fileNumber);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    [Test]
    [TestCase(654UL, "files", "file_5.txt", 1, "file_5.txt")]
    [TestCase(654UL, "files", "file_6.xml", 2, "file_6.xml")]
    [TestCase(654UL, "files", "file_7.pdf", 3, "file_7.pdf")]
    [TestCase(654UL, "files", "file_8.json", 4, "file_8.json")]
    public async Task GetWiserItemLinkFileAsync_GetFile_ReturnsFileResultModel(ulong itemLinkId, string propertyName, string fileName, int fileNumber, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserItemLinkFileAsync(itemLinkId, propertyName, fileName, fileNumber);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    [Test]
    [TestCase(9UL, "file_1.txt", "file_1.txt")]
    [TestCase(11UL, "file_3.pdf", "file_3.pdf")]
    [TestCase(13UL, "file_5.txt", "file_5.txt")]
    [TestCase(15UL, "file_7.pdf", "file_7.pdf")]
    public async Task GetWiserDirectFileAsync_GetFile_ReturnsFileResultModel(ulong itemId, string fileName, string expectedFileName)
    {
        // Act
        var actual = await itemFilesService.GetWiserDirectFileAsync(itemId, fileName);

        // Assert
        actual.WiserItemFile.FileName.Should().NotBeNullOrEmpty("because we expect a non-empty file name");
        actual.WiserItemFile.FileName.Should().Be(expectedFileName, "because we expect the returned file name to match the expected value");
    }

    #endregion
}