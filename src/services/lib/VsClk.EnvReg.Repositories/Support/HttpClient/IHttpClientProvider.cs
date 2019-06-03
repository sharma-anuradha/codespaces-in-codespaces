namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public interface IHttpClientProvider
    {
        System.Net.Http.HttpClient ProfileServiceClient { get; }
    }
}
