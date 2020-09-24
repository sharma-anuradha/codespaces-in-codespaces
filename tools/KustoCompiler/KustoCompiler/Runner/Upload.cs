using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Language;
using Kusto.Language.Syntax;
using Microsoft.Identity.Client;
using Microsoft.VsCloudKernel.Services.KustoCompiler.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Runner
{
    public class Upload
    {
        private PublicAppUsingDeviceCodeFlow DeviceCodeFlow { get; }

        private ICslQueryProvider QueryProvider { get; set; }

        private ICslAdminProvider AdminProvider { get; set; }

        private KustoClusterSettings Settings { get; }

        public Upload()
        {
            Settings = LoadAppSettings();
            var app = PublicClientApplicationBuilder
              .Create(Settings.AzureClientId)
              .WithAuthority(Settings.AzureAuthority)
              .WithDefaultRedirectUri()
              .Build();

            TokenCacheHelper.EnableSerialization(app.UserTokenCache);

            DeviceCodeFlow = new PublicAppUsingDeviceCodeFlow(app);
        }

        private async Task InitAsync()
        {
            if (QueryProvider == default || AdminProvider == default)
            {
                var authenticationResult = await DeviceCodeFlow.AcquireATokenFromCacheOrDeviceCodeFlowAsync(Settings.AadScopes);
                if (authenticationResult != default)
                {
                    DisplaySignedInAccount(authenticationResult.Account);
                }

                string accessToken = authenticationResult.AccessToken;

                var kcsb = new KustoConnectionStringBuilder("https://vsonline.kusto.windows.net", "VSOnlineService").WithAadUserTokenAuthentication(accessToken);
                QueryProvider = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(kcsb.ConnectionString);

                AdminProvider = Kusto.Data.Net.Client.KustoClientFactory.CreateCslAdminProvider(kcsb.ConnectionString);
            }
        }

            
        private static void DisplaySignedInAccount(IAccount account)
        {
            Console.WriteLine($"{account.Username} successfully signed-in");
        }

        private KustoClusterSettings LoadAppSettings()
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "appsettings.json");
            var jsonData = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<KustoClusterSettings>(jsonData);
        }

        private Dictionary<string, CslFile> nameToFunctionMap = new Dictionary<string, CslFile>();

        public async Task ExecuteAllControlQueriesAsync(string path)
        {
            await InitAsync();

            var files = Directory.EnumerateFiles(path, "*.csl", SearchOption.AllDirectories); 
            foreach (var file in files)
            {
                var parsedFile = CslFile.Create(file);
                nameToFunctionMap.Add(parsedFile.FunctionName, parsedFile);
            }

            var sortedlist = TopologicalSort(nameToFunctionMap);

            foreach (var item in sortedlist)
            {
                try
                {
                    ExecuteControlQuery(item.FileName);
                }
                catch
                {
                    Console.WriteLine($"Executing file {item.FileName} failed with exception.");
                    break;
                }
            }
        }

        private static List<CslFile> TopologicalSort(Dictionary<string, CslFile> nameMap)
        {
            List<Tuple<CslFile, CslFile>> edges = new List<Tuple<CslFile, CslFile>>();
            foreach (var item in nameMap.Values)
            {
                foreach (var dep in item.DependentFunction)
                {
                    var target = nameMap[dep];
                    edges.Add(new Tuple<CslFile, CslFile>(item, target));
                }
            }

            var result = new List<CslFile>();
            var sink = new HashSet<CslFile>(nameMap.Values.Where(n => edges.All(e => e.Item2.FunctionName != n.FunctionName)));
            while (sink.Any())
            {
                var n = sink.First();
                sink.Remove(n);

                result.Add(n);
                foreach (var e in edges.Where(e => e.Item1.FunctionName == n.FunctionName).ToList())
                {
                    var m = e.Item2;
                    edges.Remove(e);
                    if (edges.All(me => me.Item2.FunctionName != m.FunctionName))
                    {
                        sink.Add(m);
                    }
                }
            }

            if (edges.Any())
            {
                Console.WriteLine("Has loop");
                return null;
            }
            else
            {
                result.Reverse();
                return result;
            }

        }

        private void ExecuteControlQuery(string file)
        {
            try
            {
                var contents = File.ReadAllText(file);
                var result = AdminProvider.ExecuteControlCommand(contents);
                Console.WriteLine($"Running {file} successful.");

                // Following adds team as the admin, so we all have the ability to update stored functions.
                var functionName = Path.GetFileNameWithoutExtension(file);
                var permissionCommand = $".add function {functionName} admins ('aadgroup=76ed1206-72df-4116-8e6a-747439d31855;72f988bf-86f1-41af-91ab-2d7cd011db47') 'Team should have access to update the query.'";
                var permissionResult = AdminProvider.ExecuteControlCommand(permissionCommand);
                Console.WriteLine($"Adding permissions on {functionName} successful.");
            }
            catch
            {
                Console.WriteLine($"Failed when running {file}");
                Console.WriteLine($"Stopping execution.");
                throw;
            }
        }
    }
}
