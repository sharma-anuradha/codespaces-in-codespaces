using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public interface ILiveShareTokenExchangeUtil
    {
        Task<string> ExchangeToken(string path, string externalToken);
    }
}