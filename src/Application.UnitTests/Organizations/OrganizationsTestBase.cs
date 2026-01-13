using Application.Abstractions.Data;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public abstract class OrganizationsTestBase
{
    protected static Mock<IApplicationDbContext> CreateMockContext()
    {
        return new Mock<IApplicationDbContext>();
    }

    protected static Organization CreateTestOrganization(
        Guid? id = null,
        string name = "Test Organization",
        string code = "TESTORG",
        string email = "test@organization.com",
        bool isActive = true)
    {
        return new Organization
        {
            Id = id ?? Guid.CreateVersion7(),
            Name = name,
            Code = code,
            Email = email,
            ContactNumber = "+1-555-123-4567",
            Address = "123 Test Street, Test City",
            IsActive = isActive,
            FinancialYearEnd = new DateOnly(2024, 12, 31),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    protected static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return mockSet.Object;
    }
}
