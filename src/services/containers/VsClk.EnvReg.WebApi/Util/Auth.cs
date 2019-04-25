using Microsoft.AspNetCore.Http;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Util
{
    public class Auth
    {
        public static string GetAccessToken(HttpRequest request)
        {
            /* There is still a need to handle the cookie auth case when calling from the portal */
            return request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        }
    }
}
