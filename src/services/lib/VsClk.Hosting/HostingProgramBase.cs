using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.VsCloudKernel.Services.VsClk.Hosting
{
    public class SampleProgramBase<STARTUP> 
        where STARTUP : class
    {
        public static void Run(string[] args)
        {
        }
    }
}
