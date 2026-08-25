using LogicFit.Infrastructure.Security;
using LogicFit.Shared;
using Microsoft.Extensions.Options;

namespace LogicFit.UnitTests;

public sealed class SecurityPrimitivesTests
{
    [Fact]
    public void PasswordHashIsVerifiableAndDoesNotEqualThePassword()
    {
        var hasher = new Pbkdf2PasswordHasher();
        const string password = "A-Local-Only-Test-Password-2026!";

        var encoded = hasher.Hash(password);

        Assert.NotEqual(password, encoded);
        Assert.True(hasher.Verify(encoded, password));
        Assert.False(hasher.Verify(encoded, password + "-wrong"));
    }

    [Fact]
    public void TotpProvisioningUsesTheApprovedAuthenticatorUriShape()
    {
        var service = new TotpService(Options.Create(new LogicFitRuntimeOptions { MfaIssuer = "LogicFit Test" }));

        var provisioning = service.CreateProvisioning("user@example.test");

        Assert.StartsWith("otpauth://totp/", provisioning.ProvisioningUri, StringComparison.Ordinal);
        Assert.Contains("issuer=LogicFit%20Test", provisioning.ProvisioningUri, StringComparison.Ordinal);
        Assert.False(service.Verify(provisioning.Secret, "000000", DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void RecoveryCodesAreUniqueAndHaveTheApprovedLength()
    {
        var codes = new RecoveryCodeGenerator().Generate(10);

        Assert.Equal(10, codes.Count);
        Assert.Equal(10, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Equal(16, code.Length));
    }
}
