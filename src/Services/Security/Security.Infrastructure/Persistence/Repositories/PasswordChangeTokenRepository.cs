using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class PasswordChangeTokenRepository(SecurityDbContext dbContext) : IPasswordChangeTokenRepository
{
    public Task AddAsync(PasswordChangeToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return dbContext.PasswordChangeTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public async Task<PasswordChangeToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.PasswordChangeTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}
