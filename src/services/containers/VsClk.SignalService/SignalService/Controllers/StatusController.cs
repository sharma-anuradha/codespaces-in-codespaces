using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class StatusController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly PresenceService presenceService;
        private readonly IStartup startup;

        public StatusController(
            IOptions<AppSettings> appSettingsProvider,
            PresenceService presenceService,
            IStartup startup)
        {
            this.appSettings = appSettingsProvider.Value;
            this.presenceService = presenceService;
            this.startup = startup;
        }

        private ITokenValidationProvider TokenValidationProvider => this.startup.TokenValidationProvider;

        // GET: version
        [HttpGet]
        public object Get()
        {
            var securityKeys = TokenValidationProvider?.SecurityKeys.Select(k =>
            {
                if (k is X509SecurityKey securityKey)
                {
                    return new
                    {
                        securityKey.Certificate.Version,
                        IssuerName = securityKey.Certificate.IssuerName.Name
                    };
                }
                return null;
            }).ToArray();

            dynamic versionObj = new
            {
                this.presenceService.ServiceId,
                Name = "Livshare Presence Service",
                ContactsStatistics = this.presenceService.GetContactStatistics(),
                this.appSettings.BuildVersion,
                this.appSettings.GitCommit,
                this.appSettings.AuthenticateMetadataServiceUri,
                Startup.AzureSignalREnabled,
                this.startup.UseAzureSignalR,
                TokenValidatorType = TokenValidationProvider?.GetType().Name,
                TokenValidationProvider?.Audience,
                TokenValidationProvider?.Issuer,
                BackplaneProviderTypes = this.presenceService.BackplaneProviders.Select(f => f.GetType().Name).ToArray(),
                SecurityKeys = securityKeys
            };

            return versionObj;
        }
    }
}
