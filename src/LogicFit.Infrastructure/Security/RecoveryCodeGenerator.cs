using System.Security.Cryptography;
using LogicFit.Application;

namespace LogicFit.Infrastructure.Security;

public sealed class RecoveryCodeGenerator : IRecoveryCodeGenerator
{
    public IReadOnlyList<string> Generate(int count)
    {
        if (count is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return Enumerable.Range(0, count)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant())
            .ToArray();
    }
}
