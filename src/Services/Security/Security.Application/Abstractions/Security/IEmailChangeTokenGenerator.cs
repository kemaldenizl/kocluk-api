namespace Security.Application.Abstractions.Security;

public interface IEmailChangeTokenGenerator
{
    (string PlainTextToken, string HashedToken) Generate();

    string Hash(string plainTextToken);
}
