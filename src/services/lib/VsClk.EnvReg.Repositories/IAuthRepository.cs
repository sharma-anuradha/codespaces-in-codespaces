using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace VsClk.EnvReg.Repositories
{
    public interface IAuthRepository
    {
        Task<string> ExchangeToken(string externalToken);
    }
}
