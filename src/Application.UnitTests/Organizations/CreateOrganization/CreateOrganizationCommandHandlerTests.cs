using Application.Organizations.CreateOrganization;
using Domain.Organizations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations.CreateOrganization;

public sealed class CreateOrganizationCommandHandlerTests : OrganizationsTestBase
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly CreateOrganizationCommandHandler _handler;

    public CreateOrganizationCommandHandlerTests()
    {
        _mockContext = CreateMockContext();
        _handler = new CreateOrganizationCommandHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrganization()
    {
        // Arrange
        var command = new CreateOrganizationCommand(
            Name: "New Organization",
            Code: "NEWORG",
            Email: "contact@neworg.com",
            ContactNumber: "+1-555-987-6543",
            Address: "456 New Street, New City",
            IsActive: true,
            FinancialYearEnd: new DateOnly(2024, 12, 31)
        );

        var organizations = new List<Organization>();
        var mockOrganizationSet = CreateMockDbSet(organizations);

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _mockContext.Verify(c => c.Organizations.Add(It.Is<Organization>(o =>
            o.Name == command.Name &&
            o.Code == command.Code &&
            o.Email == command.Email &&
            o.ContactNumber == command.ContactNumber &&
            o.Address == command.Address &&
            o.IsActive == command.IsActive &&
            o.FinancialYearEnd == command.FinancialYearEnd
        )), Times.Once);

        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ShouldReturnFailure()
    {
        // Arrange
        var existingOrganization = CreateTestOrganization(code: "EXISTING");
        var organizations = new List<Organization> { existingOrganization };
        var mockOrganizationSet = CreateMockDbSet(organizations);

        var command = new CreateOrganizationCommand(
            Name: "New Organization",
            Code: "EXISTING", // Duplicate code
            Email: "different@email.com",
            ContactNumber: "+1-555-987-6543",
            Address: "456 New Street, New City"
        );

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.CodeExists");
        result.Error.Description.Should().Contain("EXISTING");

        _mockContext.Verify(c => c.Organizations.Add(It.IsAny<Organization>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var existingOrganization = CreateTestOrganization(email: "existing@email.com");
        var organizations = new List<Organization> { existingOrganization };
        var mockOrganizationSet = CreateMockDbSet(organizations);

        var command = new CreateOrganizationCommand(
            Name: "New Organization",
            Code: "NEWORG",
            Email: "existing@email.com", // Duplicate email
            ContactNumber: "+1-555-987-6543",
            Address: "456 New Street, New City"
        );

        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.EmailExists");
        result.Error.Description.Should().Contain("existing@email.com");
    }
}
