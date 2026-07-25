using Security.Infrastructure.Security;

namespace UnitTests.Security.Infrastructure;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_Verify_succeeds_for_correct_password()
    {
        var hash = _hasher.Hash("DummyPw123!");

        _hasher.Verify(hash, "DummyPw123!").ShouldBeTrue();
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hash = _hasher.Hash("DummyPw123!");

        _hasher.Verify(hash, "WrongPw123!").ShouldBeFalse();
    }

    [Fact]
    public void Hash_is_salted_so_two_hashes_differ()
        => _hasher.Hash("DummyPw123!").ShouldNotBe(_hasher.Hash("DummyPw123!"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Hash_rejects_blank(string? password)
        => Should.Throw<ArgumentException>(() => _hasher.Hash(password!));

    [Fact]
    public void Verify_rejects_blank_arguments()
    {
        Should.Throw<ArgumentException>(() => _hasher.Verify("", "pw"));
        Should.Throw<ArgumentException>(() => _hasher.Verify("hash", ""));
    }
}

public sealed class RecoveryCodeServiceTests
{
    private readonly RecoveryCodeService _service = new();

    [Fact]
    public void GenerateCodes_returns_requested_count()
        => _service.GenerateCodes(5).Count.ShouldBe(5);

    [Fact]
    public void GenerateCodes_defaults_to_ten()
        => _service.GenerateCodes().Count.ShouldBe(10);

    [Fact]
    public void Generated_codes_are_ten_hex_chars()
        => _service.GenerateCodes(20).ShouldAllBe(c => c.Length == 10 && c.All(Uri.IsHexDigit));

    [Fact]
    public void Generated_codes_are_unique()
    {
        var codes = _service.GenerateCodes(50);

        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    public void Hash_is_deterministic_and_uppercase_hex()
    {
        var first = _service.Hash("ABCD1234");
        var second = _service.Hash("ABCD1234");

        first.ShouldBe(second);
        first.Length.ShouldBe(64); // SHA-256 as hex
    }

    [Fact]
    public void Hash_differs_for_different_input()
        => _service.Hash("code-a").ShouldNotBe(_service.Hash("code-b"));
}

/// <summary>
/// The four opaque-token generators share the same SHA-256/base64 implementation. One parameterised suite
/// covers the invariants for every concrete generator (DRY) via a tuple of (generate, hash) delegates.
/// </summary>
public sealed class TokenGeneratorTests
{
    public static IEnumerable<object[]> Generators()
    {
        yield return Wrap(new RefreshTokenGenerator().Generate, new RefreshTokenGenerator().Hash);
        yield return Wrap(new EmailVerificationTokenGenerator().Generate, new EmailVerificationTokenGenerator().Hash);
        yield return Wrap(new PasswordResetTokenGenerator().Generate, new PasswordResetTokenGenerator().Hash);
        yield return Wrap(new PasswordChangeTokenGenerator().Generate, new PasswordChangeTokenGenerator().Hash);
        yield return Wrap(new EmailChangeTokenGenerator().Generate, new EmailChangeTokenGenerator().Hash);
    }

    private static object[] Wrap(Func<(string, string)> generate, Func<string, string> hash)
        => [generate, hash];

    [Theory]
    [MemberData(nameof(Generators))]
    public void Generate_returns_plaintext_and_matching_hash(Func<(string PlainText, string Hash)> generate, Func<string, string> hash)
    {
        var (plainText, hashed) = generate();

        plainText.ShouldNotBeNullOrWhiteSpace();
        hashed.ShouldBe(hash(plainText));
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void Generate_is_random_each_call(Func<(string PlainText, string Hash)> generate, Func<string, string> hash)
    {
        _ = hash;
        generate().Item1.ShouldNotBe(generate().Item1);
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void Hash_is_deterministic(Func<(string PlainText, string Hash)> generate, Func<string, string> hash)
    {
        _ = generate;
        hash("same-input").ShouldBe(hash("same-input"));
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public void Hash_rejects_blank(Func<(string PlainText, string Hash)> generate, Func<string, string> hash)
    {
        _ = generate;
        Should.Throw<ArgumentException>(() => hash(" "));
    }
}

public sealed class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_is_utc_and_close_to_now()
    {
        var before = DateTime.UtcNow;

        var provided = new DateTimeProvider().UtcNow;

        provided.Kind.ShouldBe(DateTimeKind.Utc);
        provided.ShouldBeInRange(before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }
}
