using Application.Organizations.CreateOrganization;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Organizations.CreateOrganization;

public sealed class CreateOrganizationCommandValidatorTests
{
    private readonly CreateOrganizationCommandValidator _validator;

    public CreateOrganizationCommandValidatorTests()
    {
        _validator = new CreateOrganizationCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {   
        // Arrange
        var command = new CreateOrganizationCommand(
            Name: "Valid Organization",
            Code: "VALID123",
            Email: "valid@organization.com",
            ContactNumber: "+1-555-123-4567",
            Address: "123 Valid Street, Valid City"
        );

        // Act & Assert
        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithInvalidName_ShouldFail(string invalidName)
    {
        // Arrange
        var command = new CreateOrganizationCommand(
            Name: invalidName!,
            Code: "VALID123",
            Email: "valid@organization.com",
            ContactNumber: "+1-555-123-4567",
            Address: "123 Valid Street"
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("invalid-code")]
    [InlineData("lower")]
    [InlineData("WITH SPACES")]
    [InlineData("")]
    public void Validate_WithInvalidCode_ShouldFail(string invalidCode)
    {
        // Arrange
        var command = new CreateOrganizationCommand(
            Name: "Valid Organization",
            Code: invalidCode,
            Email: "valid@organization.com",
            ContactNumber: "+1-555-123-4567",
            Address: "123 Valid Street"
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("")]
    [InlineData("@invalid.com")]
    public void Validate_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var command = new CreateOrganizationCommand(
            Name: "Valid Organization",
            Code: "VALID123",
            Email: invalidEmail,
            ContactNumber: "+1-555-123-4567",
            Address: "123 Valid Street"
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
