using Application.Organizations;
using Application.Organizations.GetOrganization;
using Domain.Organizations;
using FluentAssertions;
using SharedKernel;

namespace Application.UnitTests.Organizations.GetOrganization;

public sealed class GetOrganizationQueryHandlerTests : OrganizationsTestBase
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly GetOrganizationQueryHandler _handler;

    public GetOrganizationQueryHandlerTests()
    {
        _mockContext = CreateMockContext();
        _handler = new GetOrganizationQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldReturnOrganization()
    {   
        // Arrange
        var organizationId = Guid.CreateVersion7();
        var organization = CreateTestOrganization(
            id: organizationId,
            name: "Test Organization",
            code: "TEST123"
        );

        var organizations = new List<Organization> { organization };
        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var query = new GetOrganizationQuery(organizationId);

        // Act
        Result<OrganizationResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(organizationId);
        result.Value.Name.Should().Be("Test Organization");
        result.Value.Code.Should().Be("TEST123");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFoundError()
    {
        // Arrange
        var organizations = new List<Organization>();
        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var nonExistentId = Guid.CreateVersion7();
        var query = new GetOrganizationQuery(nonExistentId);

        // Act
        Result<OrganizationResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Organization.NotFound");
    }
}
