using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class IdentityMapEntityTest
    {
        public static readonly string TenantId = AuthenticationConstants.MsaPseudoTenantId;

        // MakeId Tests
        public static readonly TheoryData<string, string> UserNameData = new TheoryData<string, string>
        {
            { @"person@hotmail.com", $"person@hotmail.com:{TenantId}" },
            { @"person/@hotmail.com", $"person\u2044@hotmail.com:{TenantId}" },
            { @"person\@hotmail.com", $"person\u2215@hotmail.com:{TenantId}" },
            { @"person?@hotmail.com", $"person\u2202@hotmail.com:{TenantId}" },
            { @"person#@hotmail.com", $"person\u20bc@hotmail.com:{TenantId}" },
            { @"person/\?#@hotmail.com", $"person\u2044\u2215\u2202\u20bc@hotmail.com:{TenantId}" },
            { @"per?son??@hotmail.com", $"per\u2202son\u2202\u2202@hotmail.com:{TenantId}" },
        };

        [Theory]
        [MemberData(nameof(UserNameData))]
        public void IdentityMapEntity_Ctor_OK(string userName, string id)
        {
            var entity = new IdentityMapEntity(userName, TenantId);
            Assert.Equal(id, entity.Id);
        }


        [Theory]
        [MemberData(nameof(UserNameData))]
        public void IdentityMapEntity_Set_UserName_TenantId_OK(string userName, string id)
        {
            var entity = new IdentityMapEntity
            {
                UserName = userName,
                TenantId = TenantId,
            };

            Assert.Equal(id, entity.Id);
        }

        [Theory]
        [MemberData(nameof(UserNameData))]
        public void IdentityMapEntity_MakeId_OK(string userName, string id)
        {
            var newId = IdentityMapEntity.MakeId(userName, TenantId);
            Assert.Equal(id, newId);
        }
    }
}
