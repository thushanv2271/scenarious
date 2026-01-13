using Application.Organizations.DeleteOrganization;
using Domain.Branches;
using Domain.Organizations;
using Domain.Users;
using FluentAssertions;
using SharedKernel;

namespace Application.UnitTests.Organizations.DeleteOrganization;

public sealed class DeleteOrganizationCommandHandlerTests : OrganizationsTestBase
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly DeleteOrganizationCommandHandler _handler;

    public DeleteOrganizationCommandHandlerTests()
    {
        _mockContext = CreateMockContext();
        _handler = new DeleteOrganizationCommandHandler(_mockContext.Object);
    }
        
    [Fact]
    public async Task Handle_WithValidId_ShouldDeleteOrganization()
    {
        // Arrange
        var organizationId = Guid.CreateVersion7();
        var organization = CreateTestOrganization(id: organizationId);

        var organizations = new List<Organization> { organization };
        var branches = new List<Branch>();
        var users = new List<User>();

        var mockOrganizationSet = CreateMockDbSet(organizations);
        var mockBranchSet = CreateMockDbSet(branches);
        var mockUserSet = CreateMockDbSet(users);

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);
        _mockContext.Setup(c => c.Branches).Returns(mockBranchSet);
        _mockContext.Setup(c => c.Users).Returns(mockUserSet);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteOrganizationCommand(organizationId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockContext.Verify(c => c.Organizations.Remove(organization), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFoundError()
    {
        // Arrange
        var organizations = new List<Organization>();
        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var command = new DeleteOrganizationCommand(Guid.CreateVersion7());

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.NotFound");
        _mockContext.Verify(c => c.Organizations.Remove(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingBranches_ShouldReturnHasBranchesError()
    {
        // Arrange
        var organizationId = Guid.CreateVersion7();
        var organization = CreateTestOrganization(id: organizationId);
        var branch = new Branch 
        { 
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            BranchName = "Test Branch",
            BranchCode = "TB001",
            Email = "branch@test.com",
            ContactNumber = "+1-555-111-2222",
            Address = "Branch Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var organizations = new List<Organization> { organization };
        var branches = new List<Branch> { branch };
        var users = new List<User>();

        var mockOrganizationSet = CreateMockDbSet(organizations);
        var mockBranchSet = CreateMockDbSet(branches);
        var mockUserSet = CreateMockDbSet(users);

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);
        _mockContext.Setup(c => c.Branches).Returns(mockBranchSet);
        _mockContext.Setup(c => c.Users).Returns(mockUserSet);

        var command = new DeleteOrganizationCommand(organizationId);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.HasBranches");
        _mockContext.Verify(c => c.Organizations.Remove(It.IsAny<Organization>()), Times.Never);
    }
}
