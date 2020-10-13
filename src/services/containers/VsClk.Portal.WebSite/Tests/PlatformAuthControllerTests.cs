using Xunit;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using System;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Test.Controllers
{
    public static class PortalWebSiteTest
    {
        static void Main(){}
    }

    public class PlatformAuthControllerGitHubTests
    {
        [Theory]
        // - prod Origin
        [InlineData("https://github.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://github.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://github.com", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://github.com/", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com/path/component", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com/path/component/", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.dev.github.dev", false, false)]
        // - review-lab prod
        [InlineData("https://subdomain1.review-lab.github.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain2.review-lab.github.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain3.review-lab.github.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain4.review-lab.github.com", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain1.review-lab.github.com/", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain1.review-lab.github.com/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.dev.github.dev", false, false)]
        // - local
        [InlineData("https://github.localhost", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://github.localhost", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://github.localhost", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://github.localhost/", "legomushroom-repo-id.github.dev", false, true)]
        [InlineData("https://github.localhost/path/component", "legomushroom-repo-id.github.dev", false, true)]
        [InlineData("https://github.localhost/path/component/", "legomushroom-repo-id.github.dev", false, true)]
        [InlineData("https://github.localhost/path/component/?param1=value2&qparam2=value1", "legomushroom-repo-id.github.dev", false, true)]
        [InlineData("https://github.localhost/path/component/?param1=value2&qparam2=value1#fragment", "legomushroom-repo-id.github.dev", false, true)]
        // - allow any subdomain locally
        [InlineData("https://subdomain1.github.dev", "id-8000.apps.codespaces.githubusercontent.com", false, true)]
        [InlineData("https://subdomain3.github.com", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain3.github.localhost", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain2.developer-subdomain.review-lab.github.com", "codespace-legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain1.ppe.github.dev", "id-8000.apps.codespaces.githubusercontent.com", false, true)]
        [InlineData("https://subdomain1.dev.github.dev", "id-8000.apps.codespaces.githubusercontent.com", false, true)]
        [InlineData("https://subdomain1.github.localhost", "id-8000.apps.codespaces.githubusercontent.com", false, true)]
        // - even Salesforce domains
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain.lightning.force.com", "legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("https://subdomain4.lightning.force.com", "legomushroom-repo-id.github.localhost", false, true)]
        public void valid_IsValidAuthRequestOrigin(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.True(
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal)
            );
        }

        [Theory]
        [InlineData("https://repos.github.com", "github.dev", true, false)]
        [InlineData("https://github.localhost", "legomushroom-repo-id.github.dev", true, false)]
        // Origin subdomain not valid
        // - prod
        [InlineData("https://subdomain0.github.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.github.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.github.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        // - review-lab prod
        [InlineData("https://subdomain.developer-subdomain.review-lab.github.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain.developer-subdomain.review-lab.github.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.developer-subdomain.review-lab.github.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        // - local
        [InlineData("https://subdomain.github.localhost", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain1.github.localhost", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.github.localhost", "legomushroom-repo-id.dev.github.dev", false, false)]

        // Host subdomains not valid
        [InlineData("https://subdomain4.review-lab.github.com", "subdomain1.codespace-legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain3.review-lab.github.com", "subdomain2.codespace-legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.review-lab.github.com", "subdomain3.codespace-legomushroom-repo-id.dev.github.dev", false, false)]

        // - restrict other subdomains when not locally
        [InlineData("https://subdomain1.github.dev", "id-8000.apps.codespaces.githubusercontent.com", false, false)]
        [InlineData("https://subdomain3.github.com", "codespace-legomushroom-repo-id.github.localhost", true, false)]
        [InlineData("https://subdomain3.github.localhost", "codespace-legomushroom-repo-id.github.localhost", false, false)]
        [InlineData("https://subdomain2.developer-subdomain.review-lab.github.com", "codespace-legomushroom-repo-id.github.localhost", false, false)]
        [InlineData("https://subdomain1.ppe.github.dev", "id-8000.apps.codespaces.githubusercontent.com", true, false)]
        [InlineData("https://subdomain1.dev.github.dev", "id-8000.apps.codespaces.githubusercontent.com", false, false)]
        [InlineData("https://subdomain1.github.localhost", "id-8000.apps.codespaces.githubusercontent.com", true, false)]

        // # Restrict FROM Salesforce domains
        // - prod
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain.lightning.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain4.lightning.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain5.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain5.dm1.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain5.salesforce.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain5.dm1.salesforce.com", "legomushroom-repo-id.github.dev", true, false)]
        // - ppe
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain.lightning.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain4.lightning.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain5.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain5.dm1.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain5.salesforce.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain5.dm1.salesforce.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        // - dev
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain.lightning.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain4.lightning.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain5.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain5.salesforce.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain5.dm1.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain5.dm1.salesforce.com", "legomushroom-repo-id.dev.github.dev", false, false)]

        // # Restrict FROM GitHub Codespaces
        // - prod
        [InlineData("https://id1.github.dev", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://id2.dev.github.dev", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://id3.ppe.github.dev", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://id4.github.localhost", "legomushroom-repo-id.github.dev", true, false)]
        // - ppe
        [InlineData("https://id41.github.dev", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://id32.dev.github.dev", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://id23.ppe.github.dev", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://id14.github.localhost", "legomushroom-repo-id.ppe.github.dev", false, false)]
        // - dev
        [InlineData("https://id41.github.dev", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://id32.dev.github.dev", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://id23.ppe.github.dev", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://id14.github.localhost", "legomushroom-repo-id.dev.github.dev", false, false)]

        // # Restrict FROM Salesforce Codespaces
        // - prod
        [InlineData("https://subdomain1.builder.code.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain2.ppe.builder.code.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain2.dev.builder.code.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain3.local.builder.code.com", "legomushroom-repo-id.github.dev", true, false)]
        // - ppe
        [InlineData("https://subdomain1.builder.code.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.ppe.builder.code.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain2.dev.builder.code.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain3.local.builder.code.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        // - dev
        [InlineData("https://subdomain1.builder.code.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain2.ppe.builder.code.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain2.dev.builder.code.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain3.local.builder.code.com", "legomushroom-repo-id.dev.github.dev", false, false)]

        // Restrict null Origin or Host
        // ## Origin not set
        // - prod
        [InlineData("   ", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData(null, "legomushroom-repo-id.github.dev", true, false)]
        // - ppe
        [InlineData("   ", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData(null, "legomushroom-repo-id.ppe.github.dev", false, false)]
        // - dev
        [InlineData("   ", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData(null, "legomushroom-repo-id.dev.github.dev", false, false)]
        // - local
        [InlineData("   ", "legomushroom-repo-id.github.localhost", false, true)]
        [InlineData("", "legomushroom-repo-id.github.localhost", false, true)]
        [InlineData(null, "legomushroom-repo-id.github.localhost", false, true)]
        // ## Host not set
        // - prod
        [InlineData("https://github.com", "  ", true, false)]
        [InlineData("https://github.com", "", true, false)]
        [InlineData("https://github.com", null, true, false)]
        // - review-lab prod
        [InlineData("https://subdomain31.review-lab.github.com", "  ", true, false)]
        [InlineData("https://subdomain32.review-lab.github.com", "", true, false)]
        [InlineData("https://subdomain33.review-lab.github.com", null, true, false)]
        // - unknown
        [InlineData("https://githubusercontent.io", "  ", true, false)]
        [InlineData("https://assets.github.com", "", true, false)]
        [InlineData("https://some-domain1.repos.xunit.com", null, true, false)]

        // # Misc not supported origins
        // - prod
        [InlineData("https://some-domain1.repos.github.com", "github.dev", true, false)]
        [InlineData("https://assets.github.com", "github.dev", true, false)]
        [InlineData("https://githubusercontent.io", "github.dev", true, false)]
        [InlineData("https://part.githubusercontent.io", "github.dev", true, false)]
        [InlineData("https://github.com.repos.com", "github.dev", true, false)]
        // - ppe
        [InlineData("https://some-domain1.repos.github.com", "ppe.github.dev", false, false)]
        [InlineData("https://assets.github.com", "ppe.github.dev", false, false)]
        [InlineData("https://githubusercontent.io", "ppe.github.dev", false, false)]
        [InlineData("https://part.githubusercontent.io", "ppe.github.dev", false, false)]
        [InlineData("https://github.com.repos.com", "ppe.github.dev", false, false)]
        // dev
        [InlineData("https://some-domain1.repos.github.com", "dev.github.dev", false, false)]
        [InlineData("https://assets.github.com", "dev.github.dev", false, false)]
        [InlineData("https://githubusercontent.io", "dev.github.dev", false, false)]
        [InlineData("https://part.githubusercontent.io", "dev.github.dev", false, false)]
        [InlineData("https://github.com.repos.com", "dev.github.dev", false, false)]
        public void notvalid_IsValidAuthRequestOrigin(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.False(
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal)
            );
        }

        [Theory]
        [InlineData("", "", true, true)]
        [InlineData("https://github.com", "legomushroom-repo-id.github.dev", true, true)]
        public void throws_ProdAndLocal(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal);
            });
        }
    }

    public class PlatformAuthControllerSalesforceTests
    {
        [Theory]
        [InlineData("https://subdomain.codebuilder.lightning.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://builder.lightning.force.com", "id.builder.code.com", true, false)]

        // - prod
        [InlineData("https://some-subdomain.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://some-subdomain2.repos.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://assets.force.com", "id.builder.code.com", true, false)]
        // - ppe
        [InlineData("https://some-subdomain5.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://some-subdomain6.repos.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://assets.force.com", "id.ppe.builder.code.com", false, false)]
        // - dev
        [InlineData("https://some-subdomain7.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://some-subdomain8.repos.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://assets.force.com", "id.dev.builder.code.com", false, false)]
        // - local
        [InlineData("https://some-subdomain3.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://some-subdomain4.repos.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://assets.force.com", "id.local.builder.code.com", false, true)]

        // - prod
        [InlineData("https://codebuilder.lightning.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com", "oneId.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com", "otherId.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com/", "id0.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component", "id1.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/", "id2.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1", "id3.builder.code.com", true, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.builder.code.com", true, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://some-domain1.force.com", "oneId.builder.code.com", true, false)]
        [InlineData("https://some-domain2.force.com", "otherId.builder.code.com", true, false)]
        [InlineData("https://some-domain3.force.com/", "id0.builder.code.com", true, false)]
        [InlineData("https://some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.builder.code.com", true, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://dm2.some-domain1.force.com", "oneId.builder.code.com", true, false)]
        [InlineData("https://dm2.some-domain2.force.com", "otherId.builder.code.com", true, false)]
        [InlineData("https://dm2.some-domain3.force.com/", "id0.builder.code.com", true, false)]
        [InlineData("https://dm2.some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.builder.code.com", true, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.builder.code.com", true, false)]
        [InlineData("https://webide-develop-dev-ed.my.salesforce.com", "id.builder.code.com", true, false)]
        [InlineData("https://dm0.my.salesforce.com", "id.builder.code.com", true, false)]
        [InlineData("https://webide-develop-dev-ed.my.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://dm0.my.force.com", "id.builder.code.com", true, false)]
        [InlineData("https://some-domain1.salesforce.com", "oneId.builder.code.com", true, false)]
        [InlineData("https://some-domain2.salesforce.com", "otherId.builder.code.com", true, false)]
        [InlineData("https://some-domain3.salesforce.com/", "id0.builder.code.com", true, false)]
        [InlineData("https://some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.builder.code.com", true, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.builder.code.com", true, false)]
        [InlineData("https://dm2.some-domain1.salesforce.com", "oneId.builder.code.com", true, false)]
        [InlineData("https://dm3.some-domain2.salesforce.com", "otherId.builder.code.com", true, false)]
        [InlineData("https://dm4.some-domain3.salesforce.com/", "id0.builder.code.com", true, false)]
        [InlineData("https://dm5.some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.builder.code.com", true, false)]
        // - ppe
        [InlineData("https://codebuilder.lightning.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com", "oneId.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com", "otherId.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/", "id0.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component", "id1.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/", "id2.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1", "id3.ppe.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.ppe.builder.code.com", false, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://webide-develop-dev-ed.my.salesforce.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://dm0.my.salesforce.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://webide-develop-dev-ed.my.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://dm0.my.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain1.force.com", "oneId.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain2.force.com", "otherId.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain3.force.com/", "id0.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.ppe.builder.code.com", false, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.force.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain1.force.com", "oneId.ppe.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain2.force.com", "otherId.ppe.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain3.force.com/", "id0.ppe.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.ppe.builder.code.com", false, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain1.salesforce.com", "oneId.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain2.salesforce.com", "otherId.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain3.salesforce.com/", "id0.ppe.builder.code.com", false, false)]
        [InlineData("https://some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.ppe.builder.code.com", false, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain1.salesforce.com", "oneId.ppe.builder.code.com", false, false)]
        [InlineData("https://dm3.some-domain2.salesforce.com", "otherId.ppe.builder.code.com", false, false)]
        [InlineData("https://dm4.some-domain3.salesforce.com/", "id0.ppe.builder.code.com", false, false)]
        [InlineData("https://dm5.some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.ppe.builder.code.com", false, false)]
        // - dev
        [InlineData("https://codebuilder.lightning.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com", "oneId.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com", "otherId.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/", "id0.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component", "id1.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/", "id2.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1", "id3.dev.builder.code.com", false, false)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.dev.builder.code.com", false, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain1.force.com", "oneId.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain2.force.com", "otherId.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain3.force.com/", "id0.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.dev.builder.code.com", false, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain1.force.com", "oneId.dev.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain2.force.com", "otherId.dev.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain3.force.com/", "id0.dev.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.dev.builder.code.com", false, false)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://webide-develop-dev-ed.my.salesforce.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://dm0.my.salesforce.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://webide-develop-dev-ed.my.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://dm0.my.force.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain1.salesforce.com", "oneId.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain2.salesforce.com", "otherId.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain3.salesforce.com/", "id0.dev.builder.code.com", false, false)]
        [InlineData("https://some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.dev.builder.code.com", false, false)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://dm2.some-domain1.salesforce.com", "oneId.dev.builder.code.com", false, false)]
        [InlineData("https://dm3.some-domain2.salesforce.com", "otherId.dev.builder.code.com", false, false)]
        [InlineData("https://dm4.some-domain3.salesforce.com/", "id0.dev.builder.code.com", false, false)]
        [InlineData("https://dm5.some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.dev.builder.code.com", false, false)]
        // - local
        [InlineData("https://codebuilder.lightning.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com", "oneId.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com", "otherId.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com/", "id0.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com/path/component", "id1.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/", "id2.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1", "id3.local.builder.code.com", false, true)]
        [InlineData("https://codebuilder.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.local.builder.code.com", false, true)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://some-domain1.force.com", "oneId.local.builder.code.com", false, true)]
        [InlineData("https://some-domain2.force.com", "otherId.local.builder.code.com", false, true)]
        [InlineData("https://some-domain3.force.com/", "id0.local.builder.code.com", false, true)]
        [InlineData("https://some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.local.builder.code.com", false, true)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://webide-develop-dev-ed.my.salesforce.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://dm0.my.salesforce.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://webide-develop-dev-ed.my.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://dm0.my.force.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://dm2.some-domain1.force.com", "oneId.local.builder.code.com", false, true)]
        [InlineData("https://dm2.some-domain2.force.com", "otherId.local.builder.code.com", false, true)]
        [InlineData("https://dm2.some-domain3.force.com/", "id0.local.builder.code.com", false, true)]
        [InlineData("https://dm2.some-domain4.lightning.force.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.local.builder.code.com", false, true)]
        [InlineData("https://dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://some-domain1.salesforce.com", "oneId.local.builder.code.com", false, true)]
        [InlineData("https://some-domain2.salesforce.com", "otherId.local.builder.code.com", false, true)]
        [InlineData("https://some-domain3.salesforce.com/", "id0.local.builder.code.com", false, true)]
        [InlineData("https://some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.local.builder.code.com", false, true)]
        [InlineData("https://dm1.dream-inspiration-24802-dev-ed.lightning.salesforce.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://dm2.some-domain1.salesforce.com", "oneId.local.builder.code.com", false, true)]
        [InlineData("https://dm3.some-domain2.salesforce.com", "otherId.local.builder.code.com", false, true)]
        [InlineData("https://dm4.some-domain3.salesforce.com/", "id0.local.builder.code.com", false, true)]
        [InlineData("https://dm5.some-domain4.lightning.salesforce.com/path/component/?param1=value2&qparam2=value1#fragment", "id4.local.builder.code.com", false, true)]
        public void valid_IsValidAuthRequestOrigin(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.True(
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal)
            );
        }

        [Theory]
        // Don't accept calls `FOR` GitHub Codesapces
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://codebuilder.lightning.force.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://codebuilder.lightning.salesforce.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://codebuilder.lightning.salesforce.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://codebuilder.lightning.salesforce.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://dm1.salesforce.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://dm2.salesforce.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://dm3.salesforce.com", "legomushroom-repo-id.dev.github.dev", false, false)]
        [InlineData("https://subdomain1.dm1.salesforce.com", "legomushroom-repo-id.github.dev", true, false)]
        [InlineData("https://subdomain2.dm2.salesforce.com", "legomushroom-repo-id.ppe.github.dev", false, false)]
        [InlineData("https://subdomain3.dm3.salesforce.com", "legomushroom-repo-id.dev.github.dev", false, false)]

        // # Don't accept calls `FROM` GitHub Codesapces
        // - prod
        [InlineData("https://legomushroom-repo-id.github.dev", "id.builder.code.com", true, false)]
        [InlineData("https://legomushroom-repo-id.ppe.github.dev", "id.builder.code.com", true, false)]
        [InlineData("https://legomushroom-repo-id.dev.github.dev", "id.builder.code.com", true, false)]
        [InlineData("https://codespace-legomushroom-repo-id.github.localhost", "id.builder.code.com", true, false)]
        // - ppe
        [InlineData("https://legomushroom-repo-id.github.dev", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://legomushroom-repo-id.ppe.github.dev", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://legomushroom-repo-id.dev.github.dev", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://codespace-legomushroom-repo-id.github.localhost", "id.ppe.builder.code.com", false, false)]
        // - dev
        [InlineData("https://legomushroom-repo-id.github.dev", "id.dev.builder.code.com", false, false)]
        [InlineData("https://legomushroom-repo-id.ppe.github.dev", "id.dev.builder.code.com", false, false)]
        [InlineData("https://legomushroom-repo-id.dev.github.dev", "id.dev.builder.code.com", false, false)]
        [InlineData("https://codespace-legomushroom-repo-id.github.localhost", "id.dev.builder.code.com", false, false)]
        // - local
        [InlineData("https://legomushroom-repo-id.github.dev", "id.local.builder.code.com", false, true)]
        [InlineData("https://legomushroom-repo-id.ppe.github.dev", "id.local.builder.code.com", false, true)]
        [InlineData("https://legomushroom-repo-id.dev.github.dev", "id.local.builder.code.com", false, true)]
        [InlineData("https://codespace-legomushroom-repo-id.github.localhost", "id.local.builder.code.com", false, true)]

        // # Host cannot be a subdomain of `id.builder.code.com`
        // - prod
        [InlineData("https://codebuilder.lightning.force.com", "subdomain.id.builder.code.com", true, false)]

        // # Ignore other domains
        // - prod
        [InlineData("https://githubusercontent.io", "id.builder.code.com", true, false)]
        [InlineData("https://github.com", "id.builder.code.com", true, false)]
        [InlineData("https://microsoft.com", "id.builder.code.com", true, false)]
        // - ppe
        [InlineData("https://githubusercontent.io", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://github.com", "id.ppe.builder.code.com", false, false)]
        [InlineData("https://microsoft.com", "id.ppe.builder.code.com", false, false)]
        // - dev
        [InlineData("https://githubusercontent.io", "id.dev.builder.code.com", false, false)]
        [InlineData("https://github.com", "id.dev.builder.code.com", false, false)]
        [InlineData("https://microsoft.com", "id.dev.builder.code.com", false, false)]
        // - local
        [InlineData("https://githubusercontent.io", "id.local.builder.code.com", false, true)]
        [InlineData("https://github.com", "id.local.builder.code.com", false, true)]
        [InlineData("https://microsoft.com", "id.local.builder.code.com", false, true)]

        // # Prevent null `Origins`
        // - prod
        [InlineData("   ", "id.builder.code.com", true, false)]
        [InlineData("", "id.builder.code.com", true, false)]
        [InlineData(null, "id.builder.code.com", true, false)]
        // - ppe
        [InlineData("   ", "id.ppe.builder.code.com", false, false)]
        [InlineData("", "id.ppe.builder.code.com", false, false)]
        [InlineData(null, "id.ppe.builder.code.com", false, false)]
        // - dev
        [InlineData("   ", "id.dev.builder.code.com", false, false)]
        [InlineData("", "id.dev.builder.code.com", false, false)]
        [InlineData(null, "id.dev.builder.code.com", false, false)]
        // - local
        [InlineData("   ", "id.local.builder.code.com", false, true)]
        [InlineData("", "id.local.builder.code.com", false, true)]
        [InlineData(null, "id.local.builder.code.com", false, true)]
        // ## no host
        // - prod
        [InlineData("https://codebuilder.lightning.force.com", "  ", true, false)]
        [InlineData("https://codebuilder.lightning.force.com", "", true, false)]
        [InlineData("https://codebuilder.lightning.force.com", null, true, false)]
        // - non-prod
        [InlineData("https://subdomain.lightning.force.com", "  ", false, false)]
        [InlineData("https://subdomain1.lightning.force.com", "", false, false)]
        [InlineData("https://subdomain2.lightning.force.com", null, false, true)]
        public void notvalid_IsValidAuthRequestOrigin(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.False(
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal)
            );
        }

        [Theory]
        [InlineData("", "", true, true)]
        [InlineData("https://codebuilder.lightning.force.com", "id.builder.code.com", true, true)]
        public void throws_ProdAndLocal(
            string origin,
            string host,
            bool isProduction,
            bool isLocal
        )
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Partners.IsValidAuthRequestOrigin(origin, host, isProduction, isLocal);
            });
        }
    }
}
