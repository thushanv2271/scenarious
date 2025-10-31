using Domain.Files;
using FluentAssertions;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests.Files;
public class DeleteFileEndpointsTests : BaseIntegrationTest
{
    private const string BaseUrl = "files";

    public DeleteFileEndpointsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    #region Delete Tests

    [Fact]
    public async Task DeleteFile_ShouldReturnOk_WhenFileExists()
    {
        // Arrange
        var testFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "test-document.xlsx",
            StoredFileName = "test-document_20250107_120000.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = 1024,
            PhysicalPath = "/uploads/test-document_20250107_120000.xlsx",
            PublicUrl = "https://example.com/uploads/test-document_20250107_120000.xlsx",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        DbContext.UploadedFiles.Add(testFile);
        await DbContext.SaveChangesAsync();

        // Act
        var requestBody = new { Ids = new List<Guid> { testFile.Id } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DeleteFilesResponseDto result = await response.Content.ReadFromJsonAsync<DeleteFilesResponseDto>();
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        DeleteFileData deleted = result.Data[0];
        deleted.Id.Should().Be(testFile.Id);
        deleted.OriginalFileName.Should().Be("test-document.xlsx");
        deleted.StoredFileName.Should().Be("test-document_20250107_120000.xlsx");
        deleted.Size.Should().Be(1024);
        deleted.DeletedBy.Should().Be(TestUserId);
        deleted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify file is deleted from database
        UploadedFile? deletedFile = await DbContext.UploadedFiles.FirstOrDefaultAsync(f => f.Id == testFile.Id);
        deletedFile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFile_ShouldReturnNotFound_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentFileId = Guid.NewGuid();

        var requestBody = new { Ids = new List<Guid> { nonExistentFileId } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFile_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var testFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "test.xlsx",
            StoredFileName = "test_20250107.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = 512,
            PhysicalPath = "/uploads/test_20250107.xlsx",
            PublicUrl = "https://example.com/uploads/test_20250107.xlsx",
            UploadedBy = Guid.NewGuid(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        DbContext.UploadedFiles.Add(testFile);
        await DbContext.SaveChangesAsync();

        // Remove authentication
        await AuthenticateAsUserWithoutPermissionsAsync();

        var requestBody = new { Ids = new List<Guid> { testFile.Id } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteFile_ShouldNotAffectOtherFiles()
    {
        // Arrange
        var file1 = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "file1.xlsx",
            StoredFileName = "file1_20250107.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = 1024,
            PhysicalPath = "/uploads/file1_20250107.xlsx",
            PublicUrl = "https://example.com/uploads/file1_20250107.xlsx",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        var file2 = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "file2.xlsx",
            StoredFileName = "file2_20250107.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = 2048,
            PhysicalPath = "/uploads/file2_20250107.xlsx",
            PublicUrl = "https://example.com/uploads/file2_20250107.xlsx",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        var file3 = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "file3.csv",
            StoredFileName = "file3_20250107.csv",
            ContentType = "text/csv",
            Size = 512,
            PhysicalPath = "/uploads/file3_20250107.csv",
            PublicUrl = "https://example.com/uploads/file3_20250107.csv",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        DbContext.UploadedFiles.AddRange(file1, file2, file3);
        await DbContext.SaveChangesAsync();

        var requestBody = new { Ids = new List<Guid> { file2.Id } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<UploadedFile> remainingFiles = await DbContext.UploadedFiles.ToListAsync();
        remainingFiles.Should().HaveCount(2);
        remainingFiles.Should().Contain(f => f.Id == file1.Id);
        remainingFiles.Should().Contain(f => f.Id == file3.Id);
        remainingFiles.Should().NotContain(f => f.Id == file2.Id);
    }

    [Fact]
    public async Task DeleteFile_ShouldDeleteMetadata_EvenIfPhysicalFileIsMissing()
    {
        // Arrange - File with non-existent physical path
        var testFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "missing-file.xlsx",
            StoredFileName = "missing-file_20250107.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = 1024,
            PhysicalPath = "/non/existent/path/missing-file.xlsx",
            PublicUrl = "https://example.com/uploads/missing-file.xlsx",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow
        };

        DbContext.UploadedFiles.Add(testFile);
        await DbContext.SaveChangesAsync();

        var requestBody = new { Ids = new List<Guid> { testFile.Id } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        UploadedFile? deletedFile = await DbContext.UploadedFiles.FirstOrDefaultAsync(f => f.Id == testFile.Id);
        deletedFile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFile_ShouldReturnCorrectResponseStructure()
    {
        // Arrange
        var testFile = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "structure-test.csv",
            StoredFileName = "structure-test_20250107.csv",
            ContentType = "text/csv",
            Size = 768,
            PhysicalPath = "/uploads/structure-test.csv",
            PublicUrl = "https://example.com/uploads/structure-test.csv",
            UploadedBy = TestUserId,
            UploadedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        DbContext.UploadedFiles.Add(testFile);
        await DbContext.SaveChangesAsync();

        var requestBody = new { Ids = new List<Guid> { testFile.Id } };
        using var request = new HttpRequestMessage(HttpMethod.Delete, BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DeleteFilesResponseDto result = await response.Content.ReadFromJsonAsync<DeleteFilesResponseDto>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("File deleted successfully");
        result.Data.Should().ContainSingle();
        result.Data[0].PhysicalPath.Should().Be("/uploads/structure-test.csv");
    }

    #endregion

    #region Response DTOs

    private sealed record DeleteFilesResponseDto(
        string Message,
        List<DeleteFileData> Data);

    private sealed record DeleteFileData(
        Guid Id,
        string OriginalFileName,
        string StoredFileName,
        long Size,
        string PhysicalPath,
        DateTime DeletedAt,
        Guid DeletedBy);

    #endregion
}
