using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ChangeStatus;

internal sealed class ChangeUserStatusCommandHandler(
    IApplicationDbContext context
) : ICommandHandler<ChangeUserStatusCommand>
{
    public async Task<Result> Handle(ChangeUserStatusCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        user.UserStatus = command.NewStatus;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
