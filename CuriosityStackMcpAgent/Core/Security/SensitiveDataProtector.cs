using System.Security.Cryptography;
using System.Text;

namespace CuriosityStack.Mcp.Core.Security;

public interface ISensitiveDataProtector
{
    string Protect(string plaintext);
    string Unprotect(string cipherText);
}

public sealed class SensitiveDataProtector : ISensitiveDataProtector
{
    public string Protect(string plaintext)
    {
        if (OperatingSystem.IsWindows())
        {
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        throw new PlatformNotSupportedException("Non-Windows secure storage requires OS keychain integration.");
    }

    public string Unprotect(string cipherText)
    {
        if (OperatingSystem.IsWindows())
        {
            var bytes = Convert.FromBase64String(cipherText);
            var plaintextBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plaintextBytes);
        }

        throw new PlatformNotSupportedException("Non-Windows secure storage requires OS keychain integration.");
    }
}
