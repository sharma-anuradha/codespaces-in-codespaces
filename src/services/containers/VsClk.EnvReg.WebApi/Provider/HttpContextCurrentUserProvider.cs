using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Provider
{
    public class HttpContextCurrentUserProvider : ICurrentUserProvider
    {
        public readonly static string HttpContext_CurrentUserIdKey = "VsClk-UserId";
        public readonly static string HttpContext_CurrentUserTokenKey = "VsClk-UserToken";

        public HttpContextCurrentUserProvider(
            IProfileCache profileCache,
            IHttpContextAccessor contextAccessor)
        {
            ProfileCache = profileCache;
            ContextAccessor = contextAccessor;
        }

        private IProfileCache ProfileCache { get; }

        private IHttpContextAccessor ContextAccessor { get; }

        public void SetBearerToken(string token)
        {
            ContextAccessor.HttpContext.Items[HttpContext_CurrentUserTokenKey] = token;
        }

        public string GetBearerToken()
        {
            return ContextAccessor.HttpContext.Items[HttpContext_CurrentUserTokenKey] as string;
        }

        public Profile GetProfile()
        {
            var profileId = GetProfileId();
            return !string.IsNullOrEmpty(profileId) ? ProfileCache.GetProfile(profileId) : null;
        }

        public void SetProfile(Profile profile)
        {
            ProfileCache.SetProfile(profile);
            ContextAccessor.HttpContext.Items[HttpContext_CurrentUserIdKey] = profile.Id;
        }

        public string GetProfileId()
        {
            return ContextAccessor.HttpContext.Items[HttpContext_CurrentUserIdKey] as string;
        }
    }
}
