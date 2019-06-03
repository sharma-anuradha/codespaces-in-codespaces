namespace VsClk.EnvReg.Models.Errors
{
    public static class ErrorCodes
    {
        public const int CouldNotConnectToServer = -32000;

        public const int OlderThanServer = -32001;
        public const int NewerThanServer = -32002;

        public const int NonSuccessHttpStatusCodeReceived = -32030;
        public const int UnauthorizedHttpStatusCode = -32032;
        public const int ForbiddenHttpStatusCode = -32033;

        public const int InvocationException = -32098;
    }
}
