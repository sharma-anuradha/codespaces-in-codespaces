using Microsoft.Identity.Client;
using Microsoft.VsCloudKernel.ApplicationRegistrations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopNetFxMsal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Set this to true to use First Party application registrations
        /// </summary>
        private static bool UseFirstPartyApp = false;

        /// <summary>
        ///  Configuration using DEV-test AppIds
        /// </summary>
        private static readonly PublicClientApplicationOptions AppOptions = new PublicClientApplicationOptions
        {
            AadAuthorityAudience = AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount,
            AzureCloudInstance = AzureCloudInstance.AzurePublic,
            ClientId = DevelopmentAppIds.VisualStudioServicesNativeClient,
            ClientName = typeof(MainWindow).Assembly.GetName().Name,
            ClientVersion = typeof(MainWindow).Assembly.GetName().Version.ToString(),
            LogLevel = LogLevel.Verbose,
            RedirectUri = "urn:ietf:wg:oauth:2.0:oob",
        };

        /// <summary>
        ///  Configuration using First-Party AppIds
        /// </summary>
        private static readonly PublicClientApplicationOptions FirstPartyAppOptions = new PublicClientApplicationOptions
        {
            Instance = "https://login.windows-ppe.net/common/v2.0",
            ClientId = FirstPartyAppIds.VisualStudioServicesNativeClient,
            ClientName = typeof(MainWindow).Assembly.GetName().Name,
            ClientVersion = typeof(MainWindow).Assembly.GetName().Version.ToString(),
            LogLevel = LogLevel.Verbose,
            RedirectUri = "http://localhost/callback",
        };

        private static readonly List<string> GraphScopes = new List<string>
        {
                // offline, opendid, profile automatically added by MSAL
                "User.Read",
                "email"
        };

        private static readonly List<string> WebApiScopes = new List<string>
        {
            FirstPartyAppIds.VisualStudioServicesWebClient
        };

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IPublicClientApplication _app;

        // Button strings
        const string SignInString = "Sign In";
        const string SignOutString = "Sign Out";

        public MainWindow()
        {
            InitializeComponent();
            var appOptions = UseFirstPartyApp ? FirstPartyAppOptions : AppOptions;
            var logLevel = UseFirstPartyApp ? LogLevel.Verbose : LogLevel.Warning;
            _app = PublicClientApplicationBuilder.CreateWithApplicationOptions(appOptions)
                .WithLogging(LogCallback)
                .Build();

            TokenCacheHelper.EnableSerialization(_app.UserTokenCache);
            var task = CallWebApi(); // do not await
        }


        /// <summary>
        /// "Invoke" button handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void InvokeWebApi(object sender = null, RoutedEventArgs args = null)
        {
            await CallWebApi(interactive: false);
        }

        /// <summary>
        /// Sign-in button handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CallWebApiInteractive(object sender = null, RoutedEventArgs args = null)
        {
            ClearOutput();

            // Manage sign-out
            if (SignInButton.Content.ToString() == SignOutString)
            {
                var accounts = (await _app.GetAccountsAsync()).ToList();

                // clear the cache
                while (accounts.Any())
                {
                    await _app.RemoveAsync(accounts.First());
                    accounts = (await _app.GetAccountsAsync()).ToList();
                }

                // Also clear cookies from the browser control.
                // How ???

                UserNotSignedIn();
                return;
            }

            await CallWebApi(interactive: true);
        }

        /// <summary>
        /// CallWebApi uses AcquireTokenSilent, using cached token from prior call to SignIn
        /// </summary>
        /// <returns></returns>
        private async Task CallWebApi(bool interactive = false)
        {
            var mode = interactive ? "interactively" : "non-interactively";
            AppendOutput(new string('*', 80));
            AppendOutput($"Calling web api {mode} using client id {_app.AppConfig.ClientId}");

            var accessToken = await GetApiTokenAsync(interactive);

            if (accessToken != null)
            {
                // Once the token has been returned by AAD, add it to the http authorization header, before making the call to access the To Do list service.
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                AppendOutput($"Invoke web api with {_httpClient.DefaultRequestHeaders.Authorization}");
            }
        }

        private async Task<string> GetApiTokenAsync(bool interactive = false)
        {
            var accounts = (await _app.GetAccountsAsync()).ToList();

            // Not already signed in, not interactive
            if (!accounts.Any() && !interactive)
            {
                UserNotSignedIn();
                AppendOutput("Please sign in");
                return null;
            }

            try
            {
                //
                // Get an access token interactively
                //
                if (interactive)
                {
                    // Force a sign-in (PromptBehavior.Always), as the MSAL web browser might contain cookies for the current user, and using .Auto
                    // would re-sign-in the same user
                    var result = await _app.AcquireTokenInteractive(GraphScopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    UserSigned(result.Account);
                    return result.AccessToken;
                }
                else
                {
                    var result = await _app.AcquireTokenSilent(GraphScopes, accounts.FirstOrDefault())
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    UserSigned(result.Account);
                    return result.AccessToken;
                }
            }
            catch (MsalUiRequiredException)
            {
                AppendOutput("Please sign in");
            }
            catch (MsalException ex)
            {
                string message = $"Error: {ex.Message}: {ex.ErrorCode}";
                if (ex.InnerException != null)
                {
                    message += "\nInner Exception: " + ex.InnerException.Message;
                }
                AppendOutput(message);
            }

            UserNotSignedIn();
            return null;
        }


        // Attempt to serialize output strings via chained async tasks
        private string outputBuffer;
        Task currentSetOutputTask;

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
            SetOutput(string.Empty);
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

        private void UserSigned(IAccount userInfo)
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

        public void LogCallback(LogLevel level, string message, bool containsPii)
        {
            AppendOutput($"{level}: {message}");
        }

    }
}
