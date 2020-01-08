// <copyright file="MainWindow.xaml.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Common.Identity;
using Newtonsoft.Json;

namespace DesktopNetFxMsal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Button strings
        private const string SignInString = "Sign In";

        private const string SignOutString = "Sign Out";

        private enum LegacySignIn
        {
            None,
            VisualStudio,
            VisualStudioServicesThirdParty,
            JohnsVsoClientNonMicrosoft,
        }

        /// <summary>
        /// Initializes static members of the <see cref="MainWindow"/> class.
        /// </summary>
        static MainWindow()
        {
            var legacySignIn = LegacySignIn.None;
            bool useVsoLocalHost = false;
            bool useLiveShareLocalHost = false;
            bool forceFirstPartyApiScope = false;

            CurrentAppConfig = new AppConfig();
            CurrentApiConfig = new ApiConfig();

            // Adjust various aspects to emulate sign-in from a legacy VS IDE client.
            if (legacySignIn == LegacySignIn.VisualStudio)
            {
                CurrentAppConfig.ClientId = AuthenticationConstants.VisualStudioClientAppId;
                CurrentAppConfig.AuthDomain = "organizations";
                CurrentAppConfig.RedirectUri = AppConfig.ClientRedirectUri;
                CurrentAppConfig.SignInScope = AuthenticationConstants.VisualStudioClientAppId + "/.default";
                CurrentApiConfig.ApiScope = AuthenticationConstants.VisualStudioClientAppId + "/.default";
            }
            else if (legacySignIn == LegacySignIn.VisualStudioServicesThirdParty)
            {
                const string VsServicesClient3rdPartyAppId = "4a1af908-0f4e-41cd-9542-75fa90175891";

                CurrentAppConfig.ClientId = VsServicesClient3rdPartyAppId;
                CurrentAppConfig.RedirectUri = AppConfig.ClientRedirectUri;
#pragma warning disable CS0618 // Type or member is obsolete
                CurrentAppConfig.SignInScope = $"api://{AuthenticationConstants.VisualStudioServicesDevApiAppId}/All";
                CurrentApiConfig.ApiScope = $"api://{AuthenticationConstants.VisualStudioServicesDevApiAppId}/All";
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else if (legacySignIn == LegacySignIn.JohnsVsoClientNonMicrosoft)
            {
                const string JohnsVsoClientAppId = "4a1af908-0f4e-41cd-9542-75fa90175891";
                CurrentAppConfig.ClientId = JohnsVsoClientAppId;
                CurrentAppConfig.SignInScope = $"api://{JohnsVsoClientAppId}/All";
            }

            if (useVsoLocalHost)
            {
                CurrentApiConfig.VsoBaseUri = "http://localhost:53760";
            }

            if (useLiveShareLocalHost)
            {
                CurrentApiConfig.LiveShareBaseUri = "https://local.dev.liveshare.vsengsaas.visualstudio.com";
            }

            if (forceFirstPartyApiScope)
            {
                CurrentApiConfig.ApiScope = new ApiConfig().ApiScope;
            }

            ApplicationOptions = new PublicClientApplicationOptions
            {
                Instance = CurrentAppConfig.AuthInstance,
                ClientId = CurrentAppConfig.ClientId,
                RedirectUri = CurrentAppConfig.RedirectUri,
                ClientName = typeof(MainWindow).Assembly.GetName().Name,
                ClientVersion = typeof(MainWindow).Assembly.GetName().Version.ToString(),
                LogLevel = LogLevel.Warning,
                EnablePiiLogging = true,
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            var json = JsonConvert.SerializeObject(ApplicationOptions, Formatting.Indented);
            AppendOutput(ApplicationOptions.GetType().Name);
            AppendOutput(json);

            _ = CallWebApi(interactive: false, requestUri: null); // do not await
        }

        private IPublicClientApplication publicClientApplication;

        private static AppConfig CurrentAppConfig { get; }

        private static ApiConfig CurrentApiConfig { get; }

        private static PublicClientApplicationOptions ApplicationOptions { get; }

        private HttpClient HttpClient { get; } = new HttpClient();

        private IPublicClientApplication PublicClientApplication
        {
            get
            {
                if (publicClientApplication == null)
                {
                    publicClientApplication = PublicClientApplicationBuilder.CreateWithApplicationOptions(ApplicationOptions)
                        .WithLogging(LogCallback)
                        .Build();

                    // TokenCacheHelper.EnableSerialization(PublicClientApplication.UserTokenCache);
                }

                return publicClientApplication;
            }
        }

        private async void InvokeWebApi1(object sender = null, RoutedEventArgs args = null)
        {
            await CallWebApi(interactive: false, CurrentApiConfig.LiveShareRequestUri);
        }

        private async void InvokeWebApi2(object sender = null, RoutedEventArgs args = null)
        {
            await CallWebApi(interactive: false, CurrentApiConfig.VsoRequestUri);
        }

        private async void CallWebApiInteractive(object sender = null, RoutedEventArgs args = null)
        {
            ClearOutput();

            // Manage sign-out
            if (SignInButton.Content.ToString() == SignOutString)
            {
                await SignOutAsync();
                return;
            }

            await CallWebApi(interactive: true, null);
        }

        private async Task SignOutAsync()
        {
            var accounts = (await PublicClientApplication.GetAccountsAsync()).ToList();

            // clear the cache
            while (accounts.Any())
            {
                await PublicClientApplication.RemoveAsync(accounts.First());
                accounts = (await PublicClientApplication.GetAccountsAsync()).ToList();
            }

            // Clear the application object
            publicClientApplication = null;

            /* Also clear cookies from the browser control. */

            UserNotSignedIn();
            return;
        }

        /// <summary>
        /// CallWebApi uses AcquireTokenSilent, using cached token from prior call to SignIn.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task CallWebApi(bool interactive, string requestUri)
        {
            var mode = interactive ? "interactively" : "non-interactively";
            AppendOutput(new string('*', 80));
            AppendOutput($"Calling web api {mode} using client id {PublicClientApplication.AppConfig.ClientId}");

            var (accessToken, idToken) = await GetApiTokenAsync(interactive);
            PrintJwt("idToken", idToken);
            PrintJwt("accessToken", accessToken);

            if (accessToken != null && !string.IsNullOrEmpty(requestUri))
            {
                try
                {
                    // Once the token has been returned by AAD, add it to the http authorization header, before making the call to access the To Do list service.
                    HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    AppendOutput($"Invoke {requestUri} with {HttpClient.DefaultRequestHeaders.Authorization}");
                    var parts = accessToken.Split('.');
                    var header = Base64Decode(parts[0]);
                    AppendOutput($"header: {header}, parts: {parts.Length}");

                    var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    var response = await HttpClient.SendAsync(request).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        body = PrettyJson(body);
                        AppendOutput(body);
                    }
                    else
                    {
                        AppendOutput(response.StatusCode.ToString());
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput(ex);
                }
            }
        }

        private void PrintJwt(string name, string jwt)
        {
            if (jwt != null)
            {
                var parts = jwt.Split('.');
                if (parts.Length == 3)
                {
                    string json = PrettyJson(Base64Decode(parts[1]));
                    AppendOutput($"{name}: {json}");
                }
            }
        }

        private static string PrettyJson(string json)
        {
            var obj = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        private string Base64Decode(string base64)
        {
            try
            {
                if (base64.Length % 4 > 0)
                {
                    base64 = base64.PadRight(base64.Length + 4 - (base64.Length % 4), '=');
                }

                var base64EncodedBytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                return base64;
            }
        }

        private async Task<IAccount> GetAppSsoAccount(bool interactive)
        {
            var accounts = (await PublicClientApplication.GetAccountsAsync()).ToList();
            if (!accounts.Any() && interactive)
            {
                // Prompt for client app sign in.
                var scopes = new string[] { CurrentAppConfig.SignInScope };
                AppendOutput($"Signing in with scopes {string.Join(",", scopes)}");
                var result = await PublicClientApplication.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.ForceLogin)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Get the account
                accounts = (await PublicClientApplication.GetAccountsAsync()).ToList();
                PrintJwt("idToken", result.IdToken);
                PrintJwt("accessToken", result.AccessToken);
            }

            return accounts.FirstOrDefault();
        }

        private async Task<(string, string)> GetApiTokenAsync(bool interactive = false)
        {
            IAccount ssoAccount;
            try
            {
                ssoAccount = await GetAppSsoAccount(interactive);
            }
            catch (Exception ex)
            {
                AppendOutput(ex);
                ssoAccount = null;
            }

            if (ssoAccount != null)
            {
                // Get access token for the required API scope.
                var scopes = new string[] { CurrentApiConfig.ApiScope };

                try
                {
                    AppendOutput($"Acquiring token for scopes {string.Join(",", scopes)}");
                    var result = await PublicClientApplication.AcquireTokenSilent(scopes, ssoAccount)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                    UserSignedIn(result.Account);
                    return (result.AccessToken, result.IdToken);
                }
                catch (MsalUiRequiredException)
                {
                    try
                    {
                        // Get an access token interactively
                        AppendOutput($"Acquiring token for scopes {string.Join(",", scopes)}");
                        var result = await PublicClientApplication.AcquireTokenInteractive(scopes)
                            .WithPrompt(Prompt.Consent)
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                        UserSignedIn(result.Account);
                        return (result.AccessToken, result.IdToken);
                    }
                    catch (Exception ex)
                    {
                        AppendOutput(ex);
                    }
                }
                catch (Exception ex)
                {
                    AppendOutput(ex);
                }
            }

            UserNotSignedIn();
            return (null, null);
        }

        // Attempt to serialize output strings via chained async tasks
        private string outputBuffer;
        private Task currentSetOutputTask;

        private void SetOutput(string text)
        {
            this.outputBuffer = text;
            var current = currentSetOutputTask ?? Task.CompletedTask;
            currentSetOutputTask = current.ContinueWith(async (t) => {
                await Dispatcher.InvokeAsync(() =>
                {
                    Output.Text = this.outputBuffer;
                    Output.ScrollToEnd();
                });
            });
        }

        private void ClearOutput()
        {
            // SetOutput(string.Empty);
        }

        private void AppendOutput(string message)
        {
            var output = this.outputBuffer ?? string.Empty;
            if (output.Length > 0)
            {
                output += "\n";
            }

            output += message;

            SetOutput(output);
        }

        private void AppendOutput(Exception ex)
        {
            string message = $"Error: {ex.GetType().Name} : {ex.Message}";
            if (ex.InnerException != null)
            {
                message += "\nInner Exception: " + ex.InnerException.Message;
            }

            AppendOutput(message);
        }

        private void UserSignedIn(IAccount userInfo)
        {
            Dispatcher.Invoke(() =>
            {
                SignInButton.Content = SignOutString;
                var userName = userInfo?.Username ?? Properties.Resources.UserNotIdentified;
                UserName.Content = $"Signed in as {userName}";
            });
        }

        private void UserNotSignedIn()
        {
            Dispatcher.Invoke(() =>
            {
                SignInButton.Content = SignInString;
                UserName.Content = Properties.Resources.UserNotSignedIn;
            });
        }

        private void LogCallback(LogLevel level, string message, bool containsPii)
        {
            // AppendOutput($"{level}: {message}");
        }

        private class AppConfig
        {
            public const string FirstPartyRedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";
            public const string ClientRedirectUri = "urn:ietf:wg:oauth:2.0:oob";

            public string AuthDomain { get; set; } = "common";

            public string ClientId { get; set; } = AuthenticationConstants.VisualStudioServicesClientAppId;

            public string RedirectUri { get; set; } = FirstPartyRedirectUri;

            public string AuthInstance => $"https://login.microsoftonline.com/{AuthDomain}/v2.0";

            public string SignInScope { get; set; } = AuthenticationConstants.VisualStudioServicesApiAppId + "/all";
        }

        private class ApiConfig
        {
            public string VsoBaseUri { get; set; } = "https://online.dev.core.vsengsaas.visualstudio.com";

            // public string VsoApiRoute { get; set; } = "api/v1/Environments";
            public string VsoApiRoute { get; set; } = "api/v1/Me";

            public string LiveShareBaseUri { get; set; } = "https://prod.liveshare.vsengsaas.visualstudio.com";

            public string LiveShareApiRoute { get; set; } = "api/v0.1/profile";

            public string VsoRequestUri => $"{VsoBaseUri}/{VsoApiRoute}";

            public string LiveShareRequestUri => $"{LiveShareBaseUri}/{LiveShareApiRoute}";

            public string ApiScope { get; set; } = AuthenticationConstants.VisualStudioServicesApiAppId + "/all";
        }
    }
}
