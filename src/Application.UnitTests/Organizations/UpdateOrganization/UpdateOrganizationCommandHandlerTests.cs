using Application.Organizations.UpdateOrganization;
using Domain.Organizations;
using FluentAssertions;
using SharedKernel;

namespace Application.UnitTests.Organizations.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandlerTests : OrganizationsTestBase
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly UpdateOrganizationCommandHandler _handler;

    public UpdateOrganizationCommandHandlerTests()
    {
        _mockContext = CreateMockContext();
        _handler = new UpdateOrganizationCommandHandler(_mockContext.Object);
    }

    [Fact]  
    public async Task Handle_WithValidCommand_ShouldUpdateOrganization()
    {
        // Arrange
        var organizationId = Guid.CreateVersion7();
        var existingOrganization = CreateTestOrganization(
            id: organizationId,
            name: "Old Name",
            email: "old@email.com"
        );

        var organizations = new List<Organization> { existingOrganization };
        var mockOrganizationSet = CreateMockDbSet(organizations);

        var command = new UpdateOrganizationCommand(
            Id: organizationId,
            Name: "Updated Name",
            Email: "updated@email.com",
            ContactNumber: "+1-555-999-8888",
            Address: "Updated Address",
            IsActive: false,
            FinancialYearEnd: new DateOnly(2025, 3, 31)
        );

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        existingOrganization.Name.Should().Be("Updated Name");
        existingOrganization.Email.Should().Be("updated@email.com");
        existingOrganization.ContactNumber.Should().Be("+1-555-999-8888");
        existingOrganization.Address.Should().Be("Updated Address");
        existingOrganization.IsActive.Should().BeFalse();
        existingOrganization.FinancialYearEnd.Should().Be(new DateOnly(2025, 3, 31));

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFoundError()
    {
        // Arrange
        var organizations = new List<Organization>();
        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var command = new UpdateOrganizationCommand(
            Id: Guid.CreateVersion7(),
            Name: "Updated Name",
            Email: "updated@email.com",
            ContactNumber: "+1-555-999-8888",
            Address: "Updated Address",
            IsActive: true,
            FinancialYearEnd: null
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.NotFound");
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnEmailExistsError()
    {
        // Arrange
        var organizationId = Guid.CreateVersion7();
        var targetOrganization = CreateTestOrganization(
            id: organizationId,
            email: "target@email.com"
        );
        var otherOrganization = CreateTestOrganization(
            id: Guid.CreateVersion7(),
            email: "other@email.com"
        );

        var organizations = new List<Organization> { targetOrganization, otherOrganization };
        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var command = new UpdateOrganizationCommand(
            Id: organizationId,
            Name: "Updated Name",
            Email: "other@email.com", // Duplicate email
            ContactNumber: "+1-555-999-8888",
            Address: "Updated Address",
            IsActive: true,
            FinancialYearEnd: null
        );

        // Act
        Result result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.EmailExists");
    }
}
