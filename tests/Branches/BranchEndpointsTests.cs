using Domain.Branches;
using Domain.Organizations;
using FluentAssertions;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests.Branches;

public class BranchEndpointsTests : BaseIntegrationTest
{
    private const string BaseUrl = "branches";

    public BranchEndpointsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    #region Create Tests

    [Fact]
    public async Task CreateBranch_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "Main Branch",
            BranchCode: "MB001",
            Email: "main@branch.com",
            ContactNumber: "+94771234567",
            Address: "123 Main Street, Colombo"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateBranchResponse? result = await response.Content.ReadFromJsonAsync<CreateBranchResponse>();
        result.Should().NotBeNull();
        result!.BranchId.Should().NotBeEmpty();

        // Verify in database
        Branch? branch = await DbContext.Branches
            .FirstOrDefaultAsync(b => b.Id == result.BranchId);
        branch.Should().NotBeNull();
        branch!.BranchName.Should().Be("Main Branch");
        branch.BranchCode.Should().Be("MB001");
        branch.Email.Should().Be("main@branch.com");
        branch.OrganizationId.Should().Be(organization.Id);
        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBranch_ShouldReturnBadRequest_WhenOrganizationDoesNotExist()
    {
        // Arrange
        var request = new CreateBranchRequest(
            OrganizationId: Guid.NewGuid(),
            BranchName: "Test Branch",
            BranchCode: "TB001",
            Email: "test@branch.com",
            ContactNumber: "+94771234567",
            Address: "Test Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBranch_ShouldReturnConflict_WhenBranchCodeAlreadyExists()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        await CreateTestBranchAsync(organization.Id, "Existing Branch", "EB001");

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "New Branch",
            BranchCode: "EB001", // Duplicate code
            Email: "new@branch.com",
            ContactNumber: "+94771234567",
            Address: "New Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBranch_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        await CreateTestBranchAsync(organization.Id, "Existing Branch", "EB001", "existing@branch.com");

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "New Branch",
            BranchCode: "NB001",
            Email: "existing@branch.com", // Duplicate email
            ContactNumber: "+94771234567",
            Address: "New Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBranch_ShouldReturnBadRequest_WhenBranchNameIsEmpty()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "",
            BranchCode: "TB001",
            Email: "test@branch.com",
            ContactNumber: "+94771234567",
            Address: "Test Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBranch_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "Test Branch",
            BranchCode: "TB001",
            Email: "invalid-email",
            ContactNumber: "+94771234567",
            Address: "Test Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAllBranches_ShouldReturnOk_WithEmptyList_WhenNoBranchesExist()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BranchResponse>? result = await response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllBranches_ShouldReturnOk_WithAllBranches()
    {
        // Arrange
        Organization org1 = await CreateTestOrganizationAsync("Org1", "ORG1");
        Organization org2 = await CreateTestOrganizationAsync("Org2", "ORG2");

        await CreateTestBranchAsync(org1.Id, "Branch 1", "BR001");
        await CreateTestBranchAsync(org1.Id, "Branch 2", "BR002");
        await CreateTestBranchAsync(org2.Id, "Branch 3", "BR003");

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BranchResponse>? result = await response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(b => b.BranchName == "Branch 1");
        result.Should().Contain(b => b.BranchName == "Branch 2");
        result.Should().Contain(b => b.BranchName == "Branch 3");
    }

    [Fact]
    public async Task GetAllBranches_ShouldReturnOk_FilteredByOrganization()
    {
        // Arrange
        Organization org1 = await CreateTestOrganizationAsync("Org1", "ORG1");
        Organization org2 = await CreateTestOrganizationAsync("Org2", "ORG2");

        await CreateTestBranchAsync(org1.Id, "Org1 Branch 1", "O1BR001");
        await CreateTestBranchAsync(org1.Id, "Org1 Branch 2", "O1BR002");
        await CreateTestBranchAsync(org2.Id, "Org2 Branch 1", "O2BR001");

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}?organizationId={org1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BranchResponse>? result = await response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.OrganizationId.Should().Be(org1.Id));
        result.Should().Contain(b => b.BranchName == "Org1 Branch 1");
        result.Should().Contain(b => b.BranchName == "Org1 Branch 2");
    }

    [Fact]
    public async Task GetAllBranches_ShouldReturnBranchesOrderedByName()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();

        await CreateTestBranchAsync(organization.Id, "Zebra Branch", "ZB001");
        await CreateTestBranchAsync(organization.Id, "Alpha Branch", "AB001");
        await CreateTestBranchAsync(organization.Id, "Middle Branch", "MB001");

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BranchResponse>? result = await response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result![0].BranchName.Should().Be("Alpha Branch");
        result[1].BranchName.Should().Be("Middle Branch");
        result[2].BranchName.Should().Be("Zebra Branch");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetBranchById_ShouldReturnOk_WhenBranchExists()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}/{branch.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BranchResponse? result = await response.Content.ReadFromJsonAsync<BranchResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(branch.Id);
        result.BranchName.Should().Be("Test Branch");
        result.BranchCode.Should().Be("TB001");
        result.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public async Task GetBranchById_ShouldReturnNotFound_WhenBranchDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateBranch_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Old Name", "OB001");

        var request = new UpdateBranchRequest(
            BranchName: "Updated Name",
            Email: "updated@branch.com",
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Detach the entity from the context to force a fresh query
        DbContext.Entry(branch).State = EntityState.Detached;

        // Verify in database
        Branch? updatedBranch = await DbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == branch.Id);
        updatedBranch.Should().NotBeNull();
        updatedBranch!.BranchName.Should().Be("Updated Name");
        updatedBranch.Email.Should().Be("updated@branch.com");
        updatedBranch.ContactNumber.Should().Be("+94779999999");
        updatedBranch.Address.Should().Be("Updated Address");
        updatedBranch.BranchCode.Should().Be("OB001"); // Should not change
    }

    [Fact]
    public async Task UpdateBranch_ShouldReturnNotFound_WhenBranchDoesNotExist()
    {
        // Arrange
        var request = new UpdateBranchRequest(
            BranchName: "Updated Name",
            Email: "updated@branch.com",
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBranch_ShouldReturnConflict_WhenEmailAlreadyExistsForDifferentBranch()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();

        Branch branch2 = await CreateTestBranchAsync(organization.Id, "Branch 2", "BR002", "branch2@test.com");

        var request = new UpdateBranchRequest(
            BranchName: "Branch 2 Updated",
            Email: "branch1@test.com", // Trying to use branch1's email
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch2.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateBranch_ShouldReturnOk_WhenUpdatingWithSameEmail()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001", "test@branch.com");

        var request = new UpdateBranchRequest(
            BranchName: "Updated Name",
            Email: "test@branch.com", // Same email
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateBranch_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

        var request = new UpdateBranchRequest(
            BranchName: "Updated Name",
            Email: "invalid-email",
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBranch_ShouldUpdateIsActiveStatus()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001", "test@branch.com");

        var request = new UpdateBranchRequest(
            BranchName: "Test Branch",
            Email: "test@branch.com", // Use the same email as created
            ContactNumber: "+94779999999",
            Address: "Test Address",
            IsActive: false // Deactivating
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Detach the entity from the context to force a fresh query
        DbContext.Entry(branch).State = EntityState.Detached;

        Branch? updatedBranch = await DbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == branch.Id);
        updatedBranch!.IsActive.Should().BeFalse();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteBranch_ShouldReturnOk_WhenBranchExists()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{branch.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted from database
        Branch? deletedBranch = await DbContext.Branches
            .FirstOrDefaultAsync(b => b.Id == branch.Id);
        deletedBranch.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBranch_ShouldReturnNotFound_WhenBranchDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBranch_ShouldNotAffectOtherBranches()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch1 = await CreateTestBranchAsync(organization.Id, "Branch 1", "BR001");
        Branch branch2 = await CreateTestBranchAsync(organization.Id, "Branch 2", "BR002");
        Branch branch3 = await CreateTestBranchAsync(organization.Id, "Branch 3", "BR003");

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{branch2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<Branch> remainingBranches = await DbContext.Branches.ToListAsync();
        remainingBranches.Should().HaveCount(2);
        remainingBranches.Should().Contain(b => b.Id == branch1.Id);
        remainingBranches.Should().Contain(b => b.Id == branch3.Id);
        remainingBranches.Should().NotContain(b => b.Id == branch2.Id);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task CreateBranch_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
    {
        // Arrange
        await AuthenticateAsUserWithoutPermissionsAsync();
        Organization organization = await CreateTestOrganizationAsync();

        var request = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "Test Branch",
            BranchCode: "TB001",
            Email: "test@branch.com",
            ContactNumber: "+94771234567",
            Address: "Test Address"
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllBranches_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
    {
        // Arrange
        await AuthenticateAsUserWithoutPermissionsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateBranch_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

        await AuthenticateAsUserWithoutPermissionsAsync();

        var request = new UpdateBranchRequest(
            BranchName: "Updated Name",
            Email: "updated@branch.com",
            ContactNumber: "+94779999999",
            Address: "Updated Address",
            IsActive: true
        );

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{branch.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteBranch_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
    {
        // Arrange
        Organization organization = await CreateTestOrganizationAsync();
        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

        await AuthenticateAsUserWithoutPermissionsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{branch.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Integration Workflow Tests

    [Fact]
    public async Task Branch_FullCRUD_Workflow()
    {
        // Create Organization
        Organization organization = await CreateTestOrganizationAsync();

        // Create Branch
        var createRequest = new CreateBranchRequest(
            OrganizationId: organization.Id,
            BranchName: "Workflow Branch",
            BranchCode: "WB001",
            Email: "workflow@branch.com",
            ContactNumber: "+94771234567",
            Address: "Workflow Address"
        );

        HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync(BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateBranchResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateBranchResponse>();
        created!.BranchId.Should().NotBeEmpty();

        // Get All Branches
        HttpResponseMessage getAllResponse = await HttpClient.GetAsync(BaseUrl);
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<BranchResponse>? allBranches = await getAllResponse.Content.ReadFromJsonAsync<List<BranchResponse>>();
        allBranches.Should().ContainSingle();

        // Get By Id
        HttpResponseMessage getByIdResponse = await HttpClient.GetAsync($"{BaseUrl}/{created.BranchId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        BranchResponse? retrievedBranch = await getByIdResponse.Content.ReadFromJsonAsync<BranchResponse>();
        retrievedBranch!.BranchName.Should().Be("Workflow Branch");

        // Update Branch
        var updateRequest = new UpdateBranchRequest(
            BranchName: "Updated Workflow Branch",
            Email: "updated.workflow@branch.com",
            ContactNumber: "+94779999999",
            Address: "Updated Workflow Address",
            IsActive: true
        );

        HttpResponseMessage updateResponse = await HttpClient.PutAsJsonAsync($"{BaseUrl}/{created.BranchId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Update
        HttpResponseMessage getAfterUpdate = await HttpClient.GetAsync($"{BaseUrl}/{created.BranchId}");
        BranchResponse? updatedBranch = await getAfterUpdate.Content.ReadFromJsonAsync<BranchResponse>();
        updatedBranch!.BranchName.Should().Be("Updated Workflow Branch");
        updatedBranch.Email.Should().Be("updated.workflow@branch.com");

        // Delete Branch
        HttpResponseMessage deleteResponse = await HttpClient.DeleteAsync($"{BaseUrl}/{created.BranchId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Delete
        HttpResponseMessage getAfterDelete = await HttpClient.GetAsync(BaseUrl);
        List<BranchResponse>? branchesAfterDelete = await getAfterDelete.Content.ReadFromJsonAsync<List<BranchResponse>>();
        branchesAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task Branch_MultipleOrganizations_Workflow()
    {
        // Create multiple organizations
        Organization org1 = await CreateTestOrganizationAsync("Org 1", "ORG1");
        Organization org2 = await CreateTestOrganizationAsync("Org 2", "ORG2");

        // Create branches for each organization
        await CreateTestBranchAsync(org1.Id, "Org1 Branch A", "O1BA");
        await CreateTestBranchAsync(org1.Id, "Org1 Branch B", "O1BB");
        await CreateTestBranchAsync(org2.Id, "Org2 Branch A", "O2BA");

        // Get all branches
        HttpResponseMessage getAllResponse = await HttpClient.GetAsync(BaseUrl);
        List<BranchResponse>? allBranches = await getAllResponse.Content.ReadFromJsonAsync<List<BranchResponse>>();
        allBranches.Should().HaveCount(3);

        // Filter by org1
        HttpResponseMessage org1Response = await HttpClient.GetAsync($"{BaseUrl}?organizationId={org1.Id}");
        List<BranchResponse>? org1Branches = await org1Response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        org1Branches.Should().HaveCount(2);
        org1Branches.Should().AllSatisfy(b => b.OrganizationId.Should().Be(org1.Id));

        // Filter by org2
        HttpResponseMessage org2Response = await HttpClient.GetAsync($"{BaseUrl}?organizationId={org2.Id}");
        List<BranchResponse>? org2Branches = await org2Response.Content.ReadFromJsonAsync<List<BranchResponse>>();
        org2Branches.Should().ContainSingle();
        org2Branches![0].OrganizationId.Should().Be(org2.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<Organization> CreateTestOrganizationAsync(
        string name = "Test Organization",
        string code = "TESTORG")
    {
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Code = code,
            Email = $"{code.ToLowerInvariant()}@test.com",
            ContactNumber = "+94771234567",
            Address = "Test Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Organizations.Add(organization);
        await DbContext.SaveChangesAsync();
        return organization;
    }

    private async Task<Branch> CreateTestBranchAsync(
        Guid organizationId,
        string branchName,
        string branchCode,
        string email = "")
    {
        Branch? branch = string.IsNullOrEmpty(email)
            ? new Branch
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            BranchName = branchName,
            BranchCode = branchCode,
            Email = $"{branchCode.ToLowerInvariant()}@test.com",
            ContactNumber = "+94771234567",
            Address = "Test Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
            : new Branch
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            BranchName = branchName,
            BranchCode = branchCode,
            Email = email,
            ContactNumber = "+94771234567",
            Address = "Test Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Branches.Add(branch);
        await DbContext.SaveChangesAsync();
        return branch;
    }

    #endregion

    #region Response DTOs

    private record CreateBranchRequest(
        Guid OrganizationId,
        string BranchName,
        string BranchCode,
        string Email,
        string ContactNumber,
        string Address);

    private record UpdateBranchRequest(
        string BranchName,
        string Email,
        string ContactNumber,
        string Address,
        bool IsActive);

    private record CreateBranchResponse(Guid BranchId);

    private record BranchResponse(
        Guid Id,
        Guid OrganizationId,
        string BranchName,
        string BranchCode,
        string Email,
        string ContactNumber,
        string Address,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    #endregion
}
