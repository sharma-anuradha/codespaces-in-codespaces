using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class LiveShareConnectionDetails
    {
        public string CascadeToken { get; set; }
        public string SessionId { get; set; }
        public string LiveShareEndPoint { get; set; }
    }
}
