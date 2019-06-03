using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore;

namespace VsClk.EnvReg.Repositories
{
    public interface ICurrentUserProvider
    {
        void SetBearerToken(string token);

        string GetBearerToken();

        Profile GetProfile();

        void SetProfile(Profile profile);

        string GetProfileId();
    }
}
