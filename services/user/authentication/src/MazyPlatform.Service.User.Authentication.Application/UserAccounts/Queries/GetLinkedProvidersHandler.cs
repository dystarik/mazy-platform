namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal sealed partial class GetLinkedProvidersHandler(
    IReadOnlyApplicationDbContext dbContext,
    ILogger<GetLinkedProvidersHandler> logger) : IQueryHandler<GetLinkedProvidersQuery, GetLinkedProvidersResult>
{
    public async Task<Result<GetLinkedProvidersResult>> HandleAsync(GetLinkedProvidersQuery query, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(query.UserAccountId);

        var linkedProviders = await dbContext.LinkedProviders.SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);
        if (linkedProviders is not null)
            return new GetLinkedProvidersResult([.. linkedProviders.LinkedProviders.Select(lp => lp.Type)]);

        LinkedProvidersNotFound(query.UserAccountId);
        return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
    }

    [LoggerMessage(1, LogLevel.Error, "Не найдена сущность UserLinkedProviders для пользователя с ID '{UserAccountId}'")]
    private partial void LinkedProvidersNotFound(string userAccountId);
}
