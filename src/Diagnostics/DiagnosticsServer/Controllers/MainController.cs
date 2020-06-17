// <copyright file="MainController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Diagnostics;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DiagnosticsServer.Controllers
{
    /// <summary>
    /// The Main Controller.
    /// </summary>
    public class MainController : Controller
    {
        private readonly IHubContext<LogHub> logHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainController"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        public MainController(IHubContext<LogHub> logHub)
        {
            this.logHub = logHub;
        }

        /// <summary>
        /// The Index.
        /// </summary>
        /// <returns>An IActionResult.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// The Error Result.
        /// </summary>
        /// <returns>An IActionResult.</returns>
        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
