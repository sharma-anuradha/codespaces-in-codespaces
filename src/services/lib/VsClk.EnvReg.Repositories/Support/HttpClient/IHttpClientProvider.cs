namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public interface IHttpClientProvider
    {
        System.Net.Http.HttpClient ProfileServiceClient { get; }

        System.Net.Http.HttpClient ComputeServiceClient { get; }

        System.Net.Http.HttpClient WorkspaceServiceClient { get; }
    }
}
