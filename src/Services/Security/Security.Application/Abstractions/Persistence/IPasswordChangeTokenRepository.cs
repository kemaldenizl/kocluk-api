using Security.Domain.Tokens;

namespace Security.Application.Abstractions.Persistence;

public interface IPasswordChangeTokenRepository
{
    Task AddAsync(PasswordChangeToken token, CancellationToken cancellationToken = default);

    Task<PasswordChangeToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
