using System;
using System.Collections.Generic;
using System.Text;

namespace VsClk.EnvReg.Models.DataStore
{
    public class Profile
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string AvatarUri { get; set; }

        public string UserName { get; set; }

        public string Provider { get; set; }

        public string ProviderId { get; set; }

        public string Status { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public Dictionary<string, object> Programs { get; set; }
    }
}
