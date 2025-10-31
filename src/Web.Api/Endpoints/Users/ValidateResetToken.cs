using Application.Abstractions.Messaging;
using Application.PasswordResetTokens.ValidateResetToken;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ValidateResetToken : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/validate-reset-token", async (
            [FromQuery] string token,
            ICommandHandler<ValidateResetTokenCommand, string> handler,
            IValidator<ValidateResetTokenCommand> validator,
            CancellationToken cancellationToken) =>
           {
               var command = new ValidateResetTokenCommand(token);

               ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

               if (!validationResult.IsValid)
               {
                   IEnumerable<string> errors = validationResult.Errors.Select(e => e.ErrorMessage);
                   return Results.BadRequest(new { Errors = errors });
               }

               Result<string> result = await handler.Handle(command, cancellationToken);
               return result.Match(Results.Ok, CustomResults.Problem);
           })
        .WithTags(Tags.Users);
    }
}
