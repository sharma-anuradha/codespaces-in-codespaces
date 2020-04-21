using Microsoft.VsCloudKernel.Services.Portal.WebSite.Models;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public interface ICookieEncryptionUtils
    {
        PortForwardingAuthCookiePayload DecryptCookie(string encryptedCookie);
        string GetEncryptedCookieContent(string cascadeToken, string environmentId = null, string connectionSessionId = null);
    }
}