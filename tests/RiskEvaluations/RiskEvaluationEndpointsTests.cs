using System.Net;
using System.Net.Http.Json;
using Domain.RiskEvaluations;
using FluentAssertions;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests.RiskEvaluations;

public class RiskEvaluationEndpointsTests : BaseIntegrationTest
{
    private const string BaseUrl = "risk-evaluations";
    private const string IndicatorsUrl = "risk-indicators";

    public RiskEvaluationEndpointsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    #region POST /risk-evaluations Tests

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnOk_WhenValidData()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();
        await SeedTestCustomerAsync("CUST001");

        List<RiskIndicator> indicators = await DbContext.RiskIndicators
            .Where(r => r.IsActive)
            .Take(3)
            .ToListAsync();

        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST001",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: indicators.Select(i => new IndicatorEvaluationRequest(
                IndicatorId: i.IndicatorId,
                Value: "Yes"
            )).ToList()
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        CreateEvaluationResponse? result = await response.Content.ReadFromJsonAsync<CreateEvaluationResponse>();
        result.Should().NotBeNull();
        result!.EvaluationId.Should().NotBeEmpty();

        // Verify in database
        CustomerRiskEvaluation? evaluation = await DbContext.CustomerRiskEvaluations
            .Include(e => e.IndicatorEvaluations)
            .FirstOrDefaultAsync(e => e.EvaluationId == result.EvaluationId);

        evaluation.Should().NotBeNull();
        evaluation!.CustomerNumber.Should().Be("CUST001");
        evaluation.IndicatorEvaluations.Should().HaveCount(3);
        evaluation.IndicatorEvaluations.Should().AllSatisfy(ie => ie.Value.Should().Be("Yes"));
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnOk_WithMixedIndicatorValues()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();
        await SeedTestCustomerAsync("CUST002");

        List<RiskIndicator> indicators = await DbContext.RiskIndicators
            .Where(r => r.IsActive)
            .Take(3)
            .ToListAsync();

        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST002",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>
            {
                new(indicators[0].IndicatorId, "Yes"),
                new(indicators[1].IndicatorId, "No"),
                new(indicators[2].IndicatorId, "N/A")
            }
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        CreateEvaluationResponse? result = await response.Content.ReadFromJsonAsync<CreateEvaluationResponse>();
        result.Should().NotBeNull();

        // Verify indicator values
        CustomerRiskEvaluation? evaluation = await DbContext.CustomerRiskEvaluations
            .Include(e => e.IndicatorEvaluations)
            .FirstOrDefaultAsync(e => e.EvaluationId == result!.EvaluationId);

        evaluation!.IndicatorEvaluations.Should().Contain(ie => ie.Value == "Yes");
        evaluation.IndicatorEvaluations.Should().Contain(ie => ie.Value == "No");
        evaluation.IndicatorEvaluations.Should().Contain(ie => ie.Value == "N/A");
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnBadRequest_WhenCustomerNumberIsEmpty()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        RiskIndicator indicator = await DbContext.RiskIndicators.FirstAsync();

        CreateEvaluationRequest request = new(
            CustomerNumber: "",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>
            {
                new(indicator.IndicatorId, "Yes")
            }
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnBadRequest_WhenIndicatorEvaluationsIsEmpty()
    {
        // Arrange
        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST003",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>()
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnBadRequest_WhenIndicatorValueIsInvalid()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();
        await SeedTestCustomerAsync("CUST004");

        RiskIndicator indicator = await DbContext.RiskIndicators.FirstAsync();

        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST004",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>
            {
                new(indicator.IndicatorId, "InvalidValue")
            }
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnBadRequest_WhenIndicatorIdIsEmpty()
    {
        // Arrange
        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST005",
            EvaluationDate: DateTime.UtcNow.Date,
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>
            {
                new(Guid.Empty, "Yes")
            }
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRiskEvaluation_ShouldReturnBadRequest_WhenEvaluationDateIsInFuture()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();
        await SeedTestCustomerAsync("CUST008");

        RiskIndicator indicator = await DbContext.RiskIndicators.FirstAsync();

        CreateEvaluationRequest request = new(
            CustomerNumber: "CUST008",
            EvaluationDate: DateTime.UtcNow.Date.AddDays(2),
            IndicatorEvaluations: new List<IndicatorEvaluationRequest>
            {
                new(indicator.IndicatorId, "Yes")
            }
        );

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /risk-evaluations/customer/{customerNumber} Tests

    [Fact]
    public async Task GetCustomerEvaluations_ShouldReturnOk_WithEmptyList_WhenNoEvaluationsExist()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}/customer/NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerEvaluationResponse>? result = await response.Content.ReadFromJsonAsync<List<CustomerEvaluationResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerEvaluations_ShouldReturnOk_WithAllEvaluationsForCustomer()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        List<RiskIndicator> indicators = await DbContext.RiskIndicators.Take(2).ToListAsync();
        string customerNumber = "CUST009";

        CustomerRiskEvaluation evaluation1 = new()
        {
            EvaluationId = Guid.CreateVersion7(),
            CustomerNumber = customerNumber,
            EvaluationDate = DateTime.UtcNow.Date.AddDays(-2),
            EvaluatedBy = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        evaluation1.IndicatorEvaluations.Add(new CustomerRiskIndicatorEvaluation
        {
            EvalDetailId = Guid.CreateVersion7(),
            IndicatorId = indicators[0].IndicatorId,
            Value = "Yes",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });

        CustomerRiskEvaluation evaluation2 = new()
        {
            EvaluationId = Guid.CreateVersion7(),
            CustomerNumber = customerNumber,
            EvaluationDate = DateTime.UtcNow.Date.AddDays(-1),
            EvaluatedBy = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        evaluation2.IndicatorEvaluations.Add(new CustomerRiskIndicatorEvaluation
        {
            EvalDetailId = Guid.CreateVersion7(),
            IndicatorId = indicators[1].IndicatorId,
            Value = "No",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        DbContext.CustomerRiskEvaluations.AddRange(evaluation1, evaluation2);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}/customer/{customerNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerEvaluationResponse>? result = await response.Content.ReadFromJsonAsync<List<CustomerEvaluationResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        // Verify ordered by evaluation date descending
        result![0].EvaluationDate.Should().BeAfter(result[1].EvaluationDate);
    }

    [Fact]
    public async Task GetCustomerEvaluations_ShouldNotReturnOtherCustomersEvaluations()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        CustomerRiskEvaluation evaluation1 = new()
        {
            EvaluationId = Guid.CreateVersion7(),
            CustomerNumber = "CUST010",
            EvaluationDate = DateTime.UtcNow.Date,
            EvaluatedBy = TestUserId,
            CreatedAt = DateTime.UtcNow
        };

        CustomerRiskEvaluation evaluation2 = new()
        {
            EvaluationId = Guid.CreateVersion7(),
            CustomerNumber = "CUST011",
            EvaluationDate = DateTime.UtcNow.Date,
            EvaluatedBy = TestUserId,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.CustomerRiskEvaluations.AddRange(evaluation1, evaluation2);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{BaseUrl}/customer/CUST010");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CustomerEvaluationResponse>? result = await response.Content.ReadFromJsonAsync<List<CustomerEvaluationResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].CustomerNumber.Should().Be("CUST010");
    }

    #endregion

    #region GET /risk-indicators Tests

    [Fact]
    public async Task GetRiskIndicators_ShouldReturnOk_WithAllActiveIndicators()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(IndicatorsUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RiskIndicatorResponse>? result = await response.Content.ReadFromJsonAsync<List<RiskIndicatorResponse>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(r => r.PossibleValues.Should().Contain("Yes"));
    }

    [Fact]
    public async Task GetRiskIndicators_ShouldReturnOk_FilteredByCategory_SICR()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{IndicatorsUrl}?category=SICR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RiskIndicatorResponse>? result = await response.Content.ReadFromJsonAsync<List<RiskIndicatorResponse>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(r => r.Category.Should().Be("SICR"));
    }

    [Fact]
    public async Task GetRiskIndicators_ShouldReturnOk_FilteredByCategory_OEIL()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"{IndicatorsUrl}?category=OEIL");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RiskIndicatorResponse>? result = await response.Content.ReadFromJsonAsync<List<RiskIndicatorResponse>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(r => r.Category.Should().Be("OEIL"));
    }

    [Fact]
    public async Task GetRiskIndicators_ShouldReturnOk_OrderedByCategoryAndDisplayOrder()
    {
        // Arrange
        await SeedRiskIndicatorsAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(IndicatorsUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RiskIndicatorResponse>? result = await response.Content.ReadFromJsonAsync<List<RiskIndicatorResponse>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        // Verify ordering
        for (int i = 1; i < result!.Count; i++)
        {
            if (result[i].Category == result[i - 1].Category)
            {
                result[i].DisplayOrder.Should().BeGreaterThanOrEqualTo(result[i - 1].DisplayOrder);
            }
        }
    }

    #endregion

    #region Helper Methods

    private async Task SeedRiskIndicatorsAsync()
    {
        if (await DbContext.RiskIndicators.AnyAsync())
        {
            return;
        }

        List<RiskIndicator> indicators = new()
        {
            new()
            {
                IndicatorId = Guid.CreateVersion7(),
                Category = RiskIndicatorCategory.SICR,
                Description = "Contractual payments are more than 30 days past due",
                PossibleValues = "Yes,No,N/A",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                IndicatorId = Guid.CreateVersion7(),
                Category = RiskIndicatorCategory.SICR,
                Description = "Risk rating downgraded to B+",
                PossibleValues = "Yes,No,N/A",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                IndicatorId = Guid.CreateVersion7(),
                Category = RiskIndicatorCategory.OEIL,
                Description = "Significant financial difficulty of issuer",
                PossibleValues = "Yes,No,N/A",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                IndicatorId = Guid.CreateVersion7(),
                Category = RiskIndicatorCategory.OEIL,
                Description = "Breach of contract or default",
                PossibleValues = "Yes,No,N/A",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        DbContext.RiskIndicators.AddRange(indicators);
        await DbContext.SaveChangesAsync();
    }

    private async Task SeedTestCustomerAsync(string customerNumber)
    {
        // Check if loan_details table exists and seed test customer
        int customerExists = await DbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) FROM loan_details WHERE customer_number = {customerNumber}")
            .FirstOrDefaultAsync();

        if (customerExists == 0)
        {
            await DbContext.Database.ExecuteSqlAsync(
                $@"INSERT INTO loan_details (customer_number, account_number, product_type, loan_amount, created_at) 
                   VALUES ({customerNumber}, {"ACC" + customerNumber}, 'Test Loan', 100000, NOW())");
        }
    }

    #endregion

    #region Response DTOs

    private sealed record CreateEvaluationRequest(
        string CustomerNumber,
        DateTime EvaluationDate,
        List<IndicatorEvaluationRequest> IndicatorEvaluations
    );

    private sealed record IndicatorEvaluationRequest(
        Guid IndicatorId,
        string Value
    );

    private sealed record CreateEvaluationResponse(
        Guid EvaluationId
    );

    private sealed record CustomerEvaluationResponse(
        Guid EvaluationId,
        string CustomerNumber,
        DateTime EvaluationDate,
        Guid EvaluatedBy,
        DateTime CreatedAt,
        List<IndicatorEvaluationDetail> IndicatorEvaluations
    );

    private sealed record IndicatorEvaluationDetail(
        Guid EvalDetailId,
        Guid IndicatorId,
        string IndicatorDescription,
        string Category,
        string Value
    );

    private sealed record RiskIndicatorResponse(
        Guid IndicatorId,
        string Category,
        string Description,
        List<string> PossibleValues,
        int DisplayOrder
    );

    #endregion
}
