//using Domain.Branches;
//using Domain.Organizations;
//using Domain.Users;
//using FluentAssertions;
//using IntegrationTests.Common;
//using IntegrationTests.Helpers;
//using Microsoft.EntityFrameworkCore;
//using System.Net;
//using System.Net.Http.Json;
//using Xunit;
//using System.Globalization;

//namespace IntegrationTests.Users;

//public class UserWithBranchEndpointsTests : BaseIntegrationTest
//{
//    private const string BaseUrl = "users";

//    public UserWithBranchEndpointsTests(IntegrationTestWebAppFactory factory)
//        : base(factory)
//    {
//    }

//    #region Register User with Branch Tests

//    [Fact]
//    public async Task RegisterUser_ShouldReturnOk_WithValidBranch()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Main Branch", "MB001");

//        var request = new RegisterUserRequest(
//            Email: "user@test.com",
//            FirstName: "John",
//            LastName: "Doe",
//            RoleIds: new List<Guid>(),
//            BranchId: branch.Id
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);
//        RegisterUserResponse? result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
//        result.Should().NotBeNull();
//        result!.UserId.Should().NotBeEmpty();

//        // Verify in database
//        User? user = await DbContext.Users
//            .FirstOrDefaultAsync(u => u.Id == result.UserId);
//        user.Should().NotBeNull();
//        user!.Email.Should().Be("user@test.com");
//        user.BranchId.Should().Be(branch.Id);
//    }


//    [Fact]
//    public async Task RegisterUser_ShouldReturnNotFound_WhenBranchDoesNotExist()
//    {
//        // Arrange
//        var nonExistentBranchId = Guid.NewGuid();

//        var request = new RegisterUserRequest(
//            Email: "user@test.com",
//            FirstName: "John",
//            LastName: "Doe",
//            RoleIds: new List<Guid>(),
//            BranchId: nonExistentBranchId
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
//    }

//    [Fact]
//    public async Task RegisterUser_ShouldCreateMultipleUsersInSameBranch()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Shared Branch", "SB001");

//        var user1Request = new RegisterUserRequest(
//            Email: "user1@test.com",
//            FirstName: "User",
//            LastName: "One",
//            RoleIds: new List<Guid>(),
//            BranchId: branch.Id
//        );

//        var user2Request = new RegisterUserRequest(
//            Email: "user2@test.com",
//            FirstName: "User",
//            LastName: "Two",
//            RoleIds: new List<Guid>(),
//            BranchId: branch.Id
//        );

//        // Act
//        HttpResponseMessage response1 = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", user1Request);
//        HttpResponseMessage response2 = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", user2Request);

//        // Assert
//        response1.StatusCode.Should().Be(HttpStatusCode.OK);
//        response2.StatusCode.Should().Be(HttpStatusCode.OK);

//        List<User> usersInBranch = await DbContext.Users
//            .Where(u => u.BranchId == branch.Id)
//            .ToListAsync();
//        usersInBranch.Should().HaveCount(2);
//    }

//    #endregion

//    #region Update User with Branch Tests

//    [Fact]
//    public async Task UpdateUser_ShouldReturnOk_WhenChangingBranch()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch1 = await CreateTestBranchAsync(organization.Id, "Branch 1", "BR001");
//        Branch branch2 = await CreateTestBranchAsync(organization.Id, "Branch 2", "BR002");
//        User user = await CreateTestUserAsync("user@test.com", branch1.Id);

//        var request = new UpdateUserRequest(
//            UserId: user.Id,
//            FirstName: "Updated",
//            LastName: "User",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: branch2.Id
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);

//        // Detach and verify
//        DbContext.Entry(user).State = EntityState.Detached;
//        User? updatedUser = await DbContext.Users
//            .AsNoTracking()
//            .FirstOrDefaultAsync(u => u.Id == user.Id);
//        updatedUser!.BranchId.Should().Be(branch2.Id);
//    }

//    [Fact]
//    public async Task UpdateUser_ShouldReturnOk_WhenRemovingBranch()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Branch", "BR001");
//        User user = await CreateTestUserAsync("user@test.com", branch.Id);

//        var request = new UpdateUserRequest(
//            UserId: user.Id,
//            FirstName: "Updated",
//            LastName: "User",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: null // Removing branch assignment
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);

//        DbContext.Entry(user).State = EntityState.Detached;
//        User? updatedUser = await DbContext.Users
//            .AsNoTracking()
//            .FirstOrDefaultAsync(u => u.Id == user.Id);
//        updatedUser!.BranchId.Should().BeNull();
//    }

//    [Fact]
//    public async Task UpdateUser_ShouldReturnOk_WhenKeepingSameBranch()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Branch", "BR001");
//        User user = await CreateTestUserAsync("user@test.com", branch.Id);

//        var request = new UpdateUserRequest(
//            UserId: user.Id,
//            FirstName: "Updated",
//            LastName: "User",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: branch.Id // Same branch
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK);

//        DbContext.Entry(user).State = EntityState.Detached;
//        User? updatedUser = await DbContext.Users
//            .AsNoTracking()
//            .FirstOrDefaultAsync(u => u.Id == user.Id);
//        updatedUser!.BranchId.Should().Be(branch.Id);
//    }

//    [Fact]
//    public async Task UpdateUser_ShouldReturnNotFound_WhenBranchDoesNotExist()
//    {
//        // Arrange
//        User user = await CreateTestUserAsync("user@test.com", null);
//        var nonExistentBranchId = Guid.NewGuid();

//        var request = new UpdateUserRequest(
//            UserId: user.Id,
//            FirstName: "Updated",
//            LastName: "User",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: nonExistentBranchId
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"{BaseUrl}", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
//    }

//    #endregion





//    #region Branch Deletion Impact Tests

//    [Fact]
//    public async Task DeleteBranch_ShouldBePreventedWhenUsersExist()
//    {
//        // Arrange
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Branch with Users", "BU001");
//        await CreateTestUserAsync("user@test.com", branch.Id);

//        // Act
//        HttpResponseMessage response = await HttpClient.DeleteAsync($"branches/{branch.Id}");

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
//    }

//    #endregion

//    #region Integration Workflow Tests

//    [Fact]
//    public async Task UserBranch_FullWorkflow()
//    {
//        // Create Organization and Branches
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch1 = await CreateTestBranchAsync(organization.Id, "Branch 1", "BR001");
//        Branch branch2 = await CreateTestBranchAsync(organization.Id, "Branch 2", "BR002");

//        // Register User with Branch 1
//        var registerRequest = new RegisterUserRequest(
//            Email: "workflow@test.com",
//            FirstName: "Workflow",
//            LastName: "User",
//            RoleIds: new List<Guid>(),
//            BranchId: branch1.Id
//        );

//        HttpResponseMessage registerResponse = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", registerRequest);
//        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
//        RegisterUserResponse? registered = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();

//        // Verify User is in Branch 1
//        User? user = await DbContext.Users.FindAsync(registered!.UserId);
//        user!.BranchId.Should().Be(branch1.Id);

//        // Transfer User to Branch 2
//        var updateRequest = new UpdateUserRequest(
//            UserId: registered.UserId,
//            FirstName: "Workflow",
//            LastName: "User Updated",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: branch2.Id
//        );

//        HttpResponseMessage updateResponse = await HttpClient.PutAsJsonAsync($"{BaseUrl}", updateRequest);
//        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

//        // Verify User is now in Branch 2
//        DbContext.Entry(user).State = EntityState.Detached;
//        User? updatedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == registered.UserId);
//        updatedUser!.BranchId.Should().Be(branch2.Id);

//        // Remove Branch Assignment
//        var removeBranchRequest = new UpdateUserRequest(
//            UserId: registered.UserId,
//            FirstName: "Workflow",
//            LastName: "User Final",
//            UserStatus: UserStatus.Active,
//            RoleIds: new List<Guid>(),
//            BranchId: null
//        );

//        HttpResponseMessage removeBranchResponse = await HttpClient.PutAsJsonAsync($"{BaseUrl}", removeBranchRequest);
//        removeBranchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

//        // Verify User has no branch
//        DbContext.Entry(updatedUser).State = EntityState.Detached;
//        User? finalUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == registered.UserId);
//        finalUser!.BranchId.Should().BeNull();
//    }

//    #endregion

//    #region Authorization Tests with Branch Context

//    [Fact]
//    public async Task RegisterUser_WithBranch_ShouldRequirePermission()
//    {
//        // Arrange
//        await AuthenticateAsUserWithoutPermissionsAsync();
//        Organization organization = await CreateTestOrganizationAsync();
//        Branch branch = await CreateTestBranchAsync(organization.Id, "Test Branch", "TB001");

//        var request = new RegisterUserRequest(
//            Email: "user@test.com",
//            FirstName: "Test",
//            LastName: "User",
//            RoleIds: new List<Guid>(),
//            BranchId: branch.Id
//        );

//        // Act
//        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"{BaseUrl}/register", request);

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
//    }

//    #endregion

//    #region Helper Methods

//    private async Task<Organization> CreateTestOrganizationAsync(
//        string name = "Test Organization",
//        string code = "TESTORG")
//    {
//        var organization = new Organization
//        {
//            Id = Guid.CreateVersion7(),
//            Name = name,
//            Code = code,
//            Email = $"{code.ToUpperInvariant().ToLowerInvariant()}@test.com",
//            ContactNumber = "+94771234567",
//            Address = "Test Address",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        };

//        DbContext.Organizations.Add(organization);
//        await DbContext.SaveChangesAsync();
//        return organization;
//    }

//    private async Task<Branch> CreateTestBranchAsync(
//        Guid organizationId,
//        string branchName,
//        string branchCode,
//        string email = "")
//    {
//        var branch = new Branch
//        {
//            Id = Guid.CreateVersion7(),
//            OrganizationId = organizationId,
//            BranchName = branchName,
//            BranchCode = branchCode,
//            Email = string.IsNullOrEmpty(email) ? $"{branchCode.ToUpperInvariant().ToLowerInvariant()}@test.com" : email,
//            ContactNumber = "+94771234567",
//            Address = "Test Address",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        };

//        DbContext.Branches.Add(branch);
//        await DbContext.SaveChangesAsync();
//        return branch;
//    }

//    private async Task<User> CreateTestUserAsync(string email, Guid? branchId)
//    {
//        var user = new User
//        {
//            Id = Guid.CreateVersion7(),
//            Email = email,
//            FirstName = "Test",
//            LastName = "User",
//            PasswordHash = "hashed_password",
//            UserStatus = UserStatus.Active,
//            IsTemporaryPassword = false,
//            IsWizardComplete = true,
//            BranchId = branchId,
//            CreatedAt = DateTime.UtcNow,
//            ModifiedAt = DateTime.UtcNow
//        };

//        DbContext.Users.Add(user);
//        await DbContext.SaveChangesAsync();
//        return user;
//    }

//    #endregion

//    #region Request/Response DTOs

//    private record RegisterUserRequest(
//        string Email,
//        string FirstName,
//        string LastName,
//        List<Guid> RoleIds,
//        Guid? BranchId);

//    private record UpdateUserRequest(
//        Guid UserId,
//        string FirstName,
//        string LastName,
//        UserStatus UserStatus,
//        List<Guid> RoleIds,
//        Guid? BranchId);

//    private record RegisterUserResponse(Guid UserId);

//    #endregion
//}
