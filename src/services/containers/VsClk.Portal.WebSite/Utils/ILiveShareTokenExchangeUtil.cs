using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public interface ILiveShareTokenExchangeUtil
    {
        Task<string> ExchangeTokenAsync(string externalToken);
    }
}
