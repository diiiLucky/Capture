using System.Security.Cryptography;
using System.Text;

namespace CodexCapture.Services;

public sealed class SecretService
{
    public string? Protect(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string? Unprotect(string? protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(protectedText);
            var plainBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }
}
