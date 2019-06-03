using Microsoft.AspNetCore.Http;
using VsClk.EnvReg.Models.DataStore;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Provider
{
    public class HttpContextProfileCache : IProfileCache
    {
        public readonly static string HttpContext_CurrentProfileKey = "VsClk-Profile";

        public HttpContextProfileCache(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        private IHttpContextAccessor ContextAccessor { get; }

        public void SetProfile(Profile profile)
        {
            ContextAccessor.HttpContext.Items[BuildKey(profile.Id)] = profile;
        }

        public Profile GetProfile(string profileId)
        {
            return ContextAccessor.HttpContext.Items[BuildKey(profileId)] as Profile;
        }

        private static string BuildKey(string id)
        {
            return $"{HttpContext_CurrentProfileKey}__{id}";
        }
    }
}
