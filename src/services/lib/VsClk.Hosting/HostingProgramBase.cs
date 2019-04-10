using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.VsClk.Hosting
{
    public class SampleProgramBase<STARTUP> 
        where STARTUP : class
    {
        public static void Run(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                // TEMP ENDPOINT
                .Configure(app => app.Map("", EchoHandler))
                .Build();

            host.Run();
        }

        private static void EchoHandler(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.ContentType = context.Request.ContentType;
                await context.Response.WriteAsync(
                    JsonConvert.SerializeObject(new
                    {
                        Server = typeof(STARTUP).Assembly.GetName().Name,
                        StatusCode = context.Response.StatusCode.ToString(),
                        PathBase = context.Request.PathBase.Value.Trim('/'),
                        Path = context.Request.Path.Value.Trim('/'),
                        Method = context.Request.Method,
                        Scheme = context.Request.Scheme,
                        ContentType = context.Request.ContentType,
                        ContentLength = (long?)context.Request.ContentLength,
                        Content = new StreamReader(context.Request.Body).ReadToEnd(),
                        QueryString = context.Request.QueryString.ToString(),
                        Query = context.Request.Query
                            .ToDictionary(
                                item => item.Key,
                                item => item.Value,
                                StringComparer.OrdinalIgnoreCase)
                    })
                );
            });
        }
    }
}
