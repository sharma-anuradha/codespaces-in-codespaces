using System;
using System.Collections.Generic;
using System.Text;
using VsClk.EnvReg.Models.DataStore;

namespace VsClk.EnvReg.Repositories
{
    public interface IProfileCache
    {
        Profile GetProfile(string profileId);
        
        void SetProfile(Profile profile);
    }
}
