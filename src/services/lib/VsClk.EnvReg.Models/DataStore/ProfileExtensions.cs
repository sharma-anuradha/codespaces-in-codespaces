namespace VsClk.EnvReg.Models.DataStore
{
    public static class ProfileExtensions
    {
        public const string CloudeEvironementsPreviewuser = "vs.cloudenvironements.previewuser";

        public static bool IsCloudEnvironmentsPreviewUser(this Profile profile)
        {
            return profile.GetProgramsItem<bool>(CloudeEvironementsPreviewuser) 
                || (profile.Email?.EndsWith("@microsoft.com") ?? false);
        }

        public static T GetProgramsItem<T>(this Profile profile, string key)
        {
            if (profile.Programs == null || !profile.Programs.TryGetValue(key, out var value))
            {
                return default;
            }

            return value is T ? (T)value : default;
        }
    }
}
