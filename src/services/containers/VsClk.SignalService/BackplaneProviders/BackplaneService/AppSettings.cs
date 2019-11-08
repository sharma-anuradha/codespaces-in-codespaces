
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    public class AppSettings : AppSettingsBase
    {
        public int WorkerThreads {get;set;}

        public int CompletionPortThreads { get; set; }
    }
}
