using Application.Organizations;
using Application.Organizations.GetOrganizations;
using Domain.Organizations;
using FluentAssertions;
using SharedKernel;

namespace Application.UnitTests.Organizations.GetOrganizations;

public sealed class GetOrganizationsQueryHandlerTests : OrganizationsTestBase
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly GetOrganizationsQueryHandler _handler;

    public GetOrganizationsQueryHandlerTests()
    {
        _mockContext = CreateMockContext();
        _handler = new GetOrganizationsQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WithActiveOrganizations_ShouldReturnFilteredResults()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            CreateTestOrganization(name: "Active Org 1", isActive: true),
            CreateTestOrganization(name: "Active Org 2", isActive: true),
            CreateTestOrganization(name: "Inactive Org", isActive: false)
        };

        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var query = new GetOrganizationsQuery(IsActive: true);

        // Act
        Result<List<OrganizationResponse>> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(o => o.IsActive.Should().BeTrue());
        result.Value.Should().Contain(o => o.Name == "Active Org 1");
        result.Value.Should().Contain(o => o.Name == "Active Org 2");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnMatchingResults()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            CreateTestOrganization(name: "Tech Company", code: "TECH"),
            CreateTestOrganization(name: "Finance Corp", code: "FIN", email: "tech@finance.com"),
            CreateTestOrganization(name: "Healthcare Inc", code: "HEALTH")
        };

        var mockOrganizationSet = CreateMockDbSet(organizations);
        _mockContext.Setup(c => c.Organizations).Returns(mockOrganizationSet);

        var query = new GetOrganizationsQuery(SearchTerm: "tech");

        // Act
        Result<List<OrganizationResponse>> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(o => o.Name == "Tech Company");
        result.Value.Should().Contain(o => o.Email == "tech@finance.com");
    }
}
