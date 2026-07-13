using Microsoft.EntityFrameworkCore;
using Security.Application.Abstractions.Persistence;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Repositories;

public sealed class EmailChangeTokenRepository(SecurityDbContext dbContext) : IEmailChangeTokenRepository
{
    public Task AddAsync(EmailChangeToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        return dbContext.EmailChangeTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public async Task<EmailChangeToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailChangeTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}
