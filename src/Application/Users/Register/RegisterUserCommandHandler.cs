using System.Security.Cryptography;
using Application.Abstractions.Authentication;
using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Abstractions.Emailing;
using Application.Abstractions.Messaging;
using Application.Users.AssignRole;
using Domain.Branches;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Register;

/// <summary>
/// Handler for user registration
/// </summary>
internal sealed class RegisterUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICommandHandler<AssignRoleToUserCommand> assignRoleHandler,
    IAppConfiguration appConfiguration,
    IEmailService emailService
) : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        //Check if email already exists in the system
        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        // Get default branch if not provided
        Guid? branchId = command.BranchId;
        if (!branchId.HasValue)
        {
            // Get the first active branch as default
            Branch? defaultBranch = await context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            branchId = defaultBranch?.Id;
        }

        // Validate branch if provided
        if (branchId.HasValue)
        {
            bool branchExists = await context.Branches
                .AnyAsync(b => b.Id == branchId.Value, cancellationToken);

            if (!branchExists)
            {
                return Result.Failure<Guid>(BranchErrors.NotFound(branchId.Value));
            }
        }

        //Generate a random temporary password
        string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        string temporaryPassword = new string(Enumerable.Range(0, 10)
            .Select(_ =>
            {
                byte[] b = new byte[1];
                RandomNumberGenerator.Fill(b);
                return chars[b[0] % chars.Length];
            }).ToArray());

        //Create new user object
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            UserStatus = UserStatus.Active,
            IsTemporaryPassword = true,
            BranchId = branchId, // Use the resolved branch ID
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        // Assign roles
        if (command.RoleIds is { Count: > 0 })
        {
            var assignCommand = new AssignRoleToUserCommand(user.Id, command.RoleIds);
            Result assignResult = await assignRoleHandler.Handle(assignCommand, cancellationToken);

            if (assignResult.IsFailure)
            {
                return Result.Failure<Guid>(assignResult.Error);
            }
        }

        //Send welcome email with temporary credentials
        string websiteLink = $"{appConfiguration.FrontEndUrl}";

        await emailService.SendEmailAsync(
            new[] { user.Email },
            "Your Account Has Been Created - Temporary Credentials",
            $"""
            Hello {user.FirstName},

            Your account has been created successfully.  
            Please use the following credentials to log in:

            Email: {user.Email}
            Temporary Password: {temporaryPassword}

            Login here: {websiteLink}

            Important: For security reasons, you must change your password immediately after logging in.
            """
        );

        // Return the new user's ID
        return Result.Success(user.Id);
    }
}
