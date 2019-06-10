using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VsClk.EnvReg.Models.Errors
{
    public static class ValidationUtil
    {
        public static void IsRequired(object value, string name = null)
        {
            if (value == null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ValidationException($"Input is invalid");
                }
                throw new ValidationException($"'{name}' is required");
            }
        }

        public static void IsRequired(string value, string name = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ValidationException($"Input is invalid");
                }
                throw new ValidationException($"'{name}' is required");
            }
        }

        public static void IsTrue(bool value, string message = null)
        {
            if (!value)
            {
                if (string.IsNullOrEmpty(message))
                {
                    throw new ValidationException($"Input is invalid");
                }
                throw new ValidationException(message);
            }
        }
    }
}
