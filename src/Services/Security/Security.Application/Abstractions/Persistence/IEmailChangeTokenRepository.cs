using Security.Domain.Tokens;

namespace Security.Application.Abstractions.Persistence;

public interface IEmailChangeTokenRepository
{
    Task AddAsync(EmailChangeToken token, CancellationToken cancellationToken = default);

    Task<EmailChangeToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
