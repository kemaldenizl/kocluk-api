namespace Security.Application.Abstractions.Security;

public interface IPasswordChangeTokenGenerator
{
    (string PlainTextToken, string HashedToken) Generate();

    string Hash(string plainTextToken);
}
