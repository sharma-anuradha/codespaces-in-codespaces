using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.ControllerAccess
{
    /// <inheritdoc/>
    public class ControllerProvider : IControllerProvider
    {
        private readonly IServiceProvider serviceProvider;

        public ControllerProvider(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public T Create<T>(ControllerContext context)
            where T : Controller
        {
            var controller = this.serviceProvider.GetService(typeof(T)) as T;
            controller.ControllerContext = context;

            return controller;
        }
    }
}
