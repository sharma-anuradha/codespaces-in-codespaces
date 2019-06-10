using System;

namespace VsClk.EnvReg.Models.Errors
{
    public static class UnauthorizedUtil
    {
        public static void IsRequired(object value)
        {
            if (value == null)
            {
                throw new UnauthorizedAccessException();
            }
        }

        public static void IsRequired(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new UnauthorizedAccessException();
            }
        }

        public static void IsTrue(bool value)
        {
            if (!value)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}
