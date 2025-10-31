using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Abstractions.Emailing;
using Application.Abstractions.Messaging;
using Application.PasswordResetTokens.Create;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    ICommandHandler<CreatePasswordResetTokenCommand, string> createTokenHandler,
    IAppConfiguration appConfiguration
) : ICommandHandler<ForgotPasswordCommand, string>
{
    public async Task<Result<string>> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<string>(UserErrors.NotFoundByEmail);
        }

        Result<string> tokenResult = await createTokenHandler.Handle(
            new CreatePasswordResetTokenCommand(command.Email), cancellationToken);

        if (tokenResult.IsFailure)
        {
            return Result.Failure<string>(tokenResult.Error);
        }

        string resetToken = tokenResult.Value;

        string resetLink = $"{appConfiguration.FrontEndUrl}/email-verification?token={resetToken}";

        await emailService.SendEmailAsync(
            new[] { user.Email },
            "Password Reset",
            $"Click the following link to reset your password:\n\n{resetLink}",
            $"<p>Click <a href=\"{resetLink}\">here</a> to reset your password.</p>"
        );

        return Result.Success(resetLink);
    }
}
