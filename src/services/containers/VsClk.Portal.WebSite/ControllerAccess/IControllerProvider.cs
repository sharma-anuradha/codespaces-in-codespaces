using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.ControllerAccess
{
    /// <summary>
    /// Allows creating new instances of <see cref="Controller"/> types with context properties properly initialized.
    /// </summary>
    public interface IControllerProvider
    {
        /// <summary>
        /// Creates a new instance of the requested <see cref="Controller"/> from the context of the source <see cref="Controller"/>.
        /// </summary>
        /// <typeparam name="T">The requested <see cref="Controller"/> type.</typeparam>
        /// <param name="context">The source <see cref="Controller"/>'s context.</param>
        /// <returns>The controller.</returns>
        T Create<T>(ControllerContext context) where T : Controller;
    }
}
