using System.Net;
using System.Net.Http.Json;
using Domain.Branches;
using Domain.Organizations;
using Domain.Users;
using FluentAssertions;
using Infrastructure.Database;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel;
using Xunit;

namespace IntegrationTests.EclAnalysis;

[Collection("Integration Tests")]
public class CalculateThresholdSummaryTests : BaseIntegrationTest
{
    public CalculateThresholdSummaryTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithValidData_ReturnsCorrectClassification()
    {
        // Arrange
        (User user, Branch branch) = await SetupTestUserWithBranchAsync();
        await SeedLoanDataAsync(branch.BranchName, new[]
        {
            ("CUST001", 150000m),  // Individual (>= 100000)
            ("CUST002", 200000m),  // Individual
            ("CUST003", 50000m),   // Collective (< 100000)
            ("CUST004", 75000m),   // Collective
            ("CUST005", 100000m)   // Individual (exactly at threshold)
        });

        var request = new
        {
            IndividualSignificantThreshold = 100000m
        };

        // Update test user authentication
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ThresholdSummaryResponse? result = await response.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        result.Should().NotBeNull();
        result!.BranchCode.Should().Be(branch.BranchCode);
        result.BranchName.Should().Be(branch.BranchName);

        // Individual: CUST001 (150k), CUST002 (200k), CUST005 (100k)
        result.Individual.CustomerCount.Should().Be(3);
        result.Individual.AmortizedCost.Should().Be(450000m);

        // Collective: CUST003 (50k), CUST004 (75k)
        result.Collective.CustomerCount.Should().Be(2);
        result.Collective.AmortizedCost.Should().Be(125000m);

        // Grand Total
        result.GrandTotal.CustomerCount.Should().Be(5);
        result.GrandTotal.AmortizedCost.Should().Be(575000m);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithMultipleLoansPerCustomer_AggregatesCorrectly()
    {
        // Arrange
        (User user, Branch branch) = await SetupTestUserWithBranchAsync();

        // Customer with multiple loans
        await SeedLoanDataAsync(branch.BranchName, new[]
        {
            ("CUST001", 40000m),   // Loan 1
            ("CUST001", 35000m),   // Loan 2
            ("CUST001", 30000m),   // Loan 3
            // Total for CUST001 = 105000 (should be Individual)
            
            ("CUST002", 25000m),   // Loan 1
            ("CUST002", 20000m),   // Loan 2
            // Total for CUST002 = 45000 (should be Collective)
        });

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ThresholdSummaryResponse? result = await response.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        result.Should().NotBeNull();
        result!.Individual.CustomerCount.Should().Be(1);
        result.Individual.AmortizedCost.Should().Be(105000m);
        result.Collective.CustomerCount.Should().Be(1);
        result.Collective.AmortizedCost.Should().Be(45000m);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithNegativeThreshold_ReturnsBadRequest()
    {
        // Arrange
        (User user, Branch _) = await SetupTestUserWithBranchAsync();

        var request = new { IndividualSignificantThreshold = -100m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithZeroThreshold_ReturnsBadRequest()
    {
        // Arrange
        (User user, Branch _) = await SetupTestUserWithBranchAsync();

        var request = new { IndividualSignificantThreshold = 0m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateThresholdSummary_UserWithoutBranch_ReturnsError()
    {
        // Arrange
        User userWithoutBranch = await CreateUserWithoutBranchAsync();

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", userWithoutBranch.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithNoLoanData_ReturnsError()
    {
        // Arrange
        (User user, _) = await SetupTestUserWithBranchAsync();
        // No loan data seeded

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithoutPermission_ReturnsUnauthorized()
    {
        // Arrange
        (User user, Branch branch) = await SetupTestUserWithBranchAsync();
        await SeedLoanDataAsync(branch.BranchName, new[] { ("CUST001", 150000m) });

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        // No permissions added

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CalculateThresholdSummary_FiltersByBranch_DoesNotIncludeOtherBranches()
    {
        // Arrange
        (User user, Branch userBranch) = await SetupTestUserWithBranchAsync();
        Branch otherBranch = await CreateBranchAsync("Other Branch", "OTH002");

        // Seed data for user's branch
        await SeedLoanDataAsync(userBranch.BranchName, new[]
        {
            ("CUST001", 150000m),
            ("CUST002", 75000m)
        });

        // Seed data for other branch (should not be included)
        await SeedLoanDataAsync(otherBranch.BranchName, new[]
        {
            ("CUST003", 200000m),
            ("CUST004", 100000m)
        });

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ThresholdSummaryResponse? result = await response.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        result.Should().NotBeNull();
        result!.BranchCode.Should().Be(userBranch.BranchCode);
        result.GrandTotal.CustomerCount.Should().Be(2); // Only user's branch data
        result.GrandTotal.AmortizedCost.Should().Be(225000m);
    }

    [Fact]
    public async Task CalculateThresholdSummary_WithDifferentThresholds_ClassifiesCorrectly()
    {
        // Arrange
        (User user, Branch branch) = await SetupTestUserWithBranchAsync();
        await SeedLoanDataAsync(branch.BranchName, new[]
        {
            ("CUST001", 250000m),
            ("CUST002", 150000m),
            ("CUST003", 100000m),
            ("CUST004", 50000m)
        });

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Test with threshold = 100000
        var request1 = new { IndividualSignificantThreshold = 100000m };
        HttpResponseMessage response1 = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary", request1);
        ThresholdSummaryResponse? result1 = await response1.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        result1.Should().NotBeNull();
        result1!.Individual.CustomerCount.Should().Be(3); // 250k, 150k, 100k
        result1.Collective.CustomerCount.Should().Be(1); // 50k

        // Test with threshold = 200000
        var request2 = new { IndividualSignificantThreshold = 200000m };
        HttpResponseMessage response2 = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary", request2);
        ThresholdSummaryResponse? result2 = await response2.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        result2.Should().NotBeNull();
        result2!.Individual.CustomerCount.Should().Be(1); // 250k only
        result2.Collective.CustomerCount.Should().Be(3); // 150k, 100k, 50k
    }

    [Fact]
    public async Task CalculateThresholdSummary_SecondCall_UsesCachedResult()
    {
        // Arrange
        (User user, Branch branch) = await SetupTestUserWithBranchAsync();
        await SeedLoanDataAsync(branch.BranchName, new[] { ("CUST001", 150000m) });

        var request = new { IndividualSignificantThreshold = 100000m };

        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions", PermissionRegistry.EclAnalysisThresholdCalculation);

        // Act - First call
        HttpResponseMessage response1 = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary", request);
        ThresholdSummaryResponse? result1 = await response1.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        // Act - Second call (should use cache)
        HttpResponseMessage response2 = await HttpClient.PostAsJsonAsync(
            "/api/impairment/ecl/threshold-summary", request);
        ThresholdSummaryResponse? result2 = await response2.Content.ReadFromJsonAsync<ThresholdSummaryResponse>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        result1.Should().BeEquivalentTo(result2);
    }

    // Helper Methods

    private async Task<(User user, Branch branch)> SetupTestUserWithBranchAsync()
    {
        Organization organization = await CreateOrganizationAsync();
        Branch branch = await CreateBranchAsync("Test Branch", "TST001", organization.Id);
        User user = await CreateUserAsync(branch.Id);

        return (user, branch);
    }

    private async Task<Organization> CreateOrganizationAsync()
    {
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Test Organization",
            Code = $"ORG{Guid.NewGuid().ToString()[..8]}",
            Email = $"test{Guid.NewGuid().ToString()[..8]}@test.com",
            ContactNumber = "1234567890",
            Address = "Test Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await TestConnectionResiliency.ExecuteWithRetryAsync(async () =>
        {
            DbContext.Organizations.Add(organization);
            await DbContext.SaveChangesAsync();
        });

        return organization;
    }

    private async Task<Branch> CreateBranchAsync(string name, string code, Guid? organizationId = null)
    {
        if (organizationId == null)
        {
            Organization org = await CreateOrganizationAsync();
            organizationId = org.Id;
        }

        var branch = new Branch
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId.Value,
            BranchName = name,
            BranchCode = code,
            Email = $"{code.ToLower(System.Globalization.CultureInfo.CurrentCulture)}@test.com",
            ContactNumber = "1234567890",
            Address = "Test Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await TestConnectionResiliency.ExecuteWithRetryAsync(async () =>
        {
            DbContext.Branches.Add(branch);
            await DbContext.SaveChangesAsync();
        });

        return branch;
    }

    private async Task<User> CreateUserAsync(Guid? branchId = null)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = $"test{Guid.NewGuid().ToString()[..8]}@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "DummyHash",
            UserStatus = UserStatus.Active,
            IsTemporaryPassword = false,
            IsWizardComplete = true,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await TestConnectionResiliency.ExecuteWithRetryAsync(async () =>
        {
            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
        });

        return user;
    }

    private async Task<User> CreateUserWithoutBranchAsync()
    {
        return await CreateUserAsync(branchId: null);
    }

    private async Task SeedLoanDataAsync(string branchName, (string customerNumber, decimal totalOs)[] loans)
    {
        await TestConnectionResiliency.ExecuteWithRetryAsync(async () =>
        {
            using var connection = new NpgsqlConnection(DbContext.Database.GetConnectionString());
            await connection.OpenAsync();

            foreach ((string customerNumber, decimal totalOs) in loans)
            {
                string sql = @"
                    INSERT INTO loan_details (
                        id, file_details_id, customer_number, facility_number, branch,
                        product_category, segment, industry, earning_type, nature,
                        grant_date, maturity_date, interest_rate, installment_type,
                        days_past_due, limit, total_os, undisbursed_amount,
                        interest_in_suspense, collateral_type, collateral_value,
                        rescheduled, restructured, no_of_times_restructured,
                        upgraded_to_delinquency_bucket, individually_impaired,
                        bucketing_in_individual_assessment, period,
                        remaining_maturity_years, bucket_label, final_bucket
                    ) VALUES (
                        gen_random_uuid(), gen_random_uuid(), @customerNumber, @facilityNumber, @branch,
                        'Test Category', 'Test Segment', 'Test Industry', 'Test Type', 'Test Nature',
                        @grantDate, @maturityDate, 5.5, 'Monthly',
                        0, @totalOs, @totalOs, 0,
                        0, 'Test Collateral', 0,
                        false, false, 0,
                        false, false,
                        'Test Bucket', '2024-12',
                        5, 'Test Label', 'Test Final'
                    )";

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                command.Parameters.AddWithValue("@customerNumber", customerNumber);
                command.Parameters.AddWithValue("@facilityNumber", $"FAC{Guid.NewGuid().ToString()[..8]}");
                command.Parameters.AddWithValue("@branch", branchName);
                command.Parameters.AddWithValue("@grantDate", DateTime.UtcNow.AddYears(-2));
                command.Parameters.AddWithValue("@maturityDate", DateTime.UtcNow.AddYears(3));
                command.Parameters.AddWithValue("@totalOs", totalOs);

                await command.ExecuteNonQueryAsync();
            }
        });
    }

    protected override async Task CleanupAsync()
    {
        await base.CleanupAsync();

        // Clean up loan_details
        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"loan_details\" RESTART IDENTITY CASCADE;");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Ignore if table does not exist
        }
    }
}

// Response DTOs for deserialization
public class ThresholdSummaryResponse
{
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public CategoryResponse Individual { get; set; } = new();
    public CategoryResponse Collective { get; set; } = new();
    public CategoryResponse GrandTotal { get; set; } = new();
}

public class CategoryResponse
{
    public int CustomerCount { get; set; }
    public decimal AmortizedCost { get; set; }
}
