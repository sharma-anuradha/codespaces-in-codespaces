using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class ClientKeychainKeysController : Controller
    {

        public class KeychainKey
        {
            [JsonProperty("key")]
            public string Key { get; set; }
            
            [JsonProperty("id")]
            public string Id { get; set; }
            
            [JsonProperty("expiresOn")]
            public long ExpiresOn { get; set; }
        }

        private ConcurrentDictionary<string, HMACSHA256> Hashers = new ConcurrentDictionary<string, HMACSHA256>();

        private HMACSHA256 GetHasherForSecret(string base64Secret)
        {
            if (Hashers.TryGetValue(base64Secret, out HMACSHA256 existingHasher))
            {
                return existingHasher;
            }

            byte[] keyBytes = Convert.FromBase64String(base64Secret);
            var hasher = new HMACSHA256(keyBytes);

            Hashers.TryAdd(base64Secret, hasher);
 
            return hasher;
        }

        public string HmacSha256Digest(string message, string base64Secret)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] messageBytes = encoding.GetBytes(message);

            var hasher = GetHasherForSecret(base64Secret);
            byte[] bytes = hasher.ComputeHash(messageBytes);

            return Convert.ToBase64String(bytes.Take(16).ToArray());
        }

        private string CreateKeychainKey(string userId, bool isPrimary)
        {
            var hashKey = (isPrimary)
                ? RuntimeSecrets.KeychainHashKey1
                : RuntimeSecrets.KeychainHashKey2;

            if (string.IsNullOrEmpty(hashKey))
            {
                return null;                                                            
            }

            var key = HmacSha256Digest(userId, hashKey);

            return key;
        }

        private KeychainKey[] CreateKeys(string userId)
        {
            var keyString1 = CreateKeychainKey(userId, true);

            var key1 = new KeychainKey
            {
                Key = keyString1,
                Id = RuntimeSecrets.KeychainHashId1,
                ExpiresOn = new DateTimeOffset(RuntimeSecrets.KeychainHashExpiration1).ToUnixTimeMilliseconds()
            };

            var keyString2 = CreateKeychainKey(userId, false);
            if (!string.IsNullOrEmpty(keyString2))
            {
                var key2 = new KeychainKey
                {
                    Key = keyString2,
                    Id = RuntimeSecrets.KeychainHashId2,
                    ExpiresOn = new DateTimeOffset(RuntimeSecrets.KeychainHashExpiration2).ToUnixTimeMilliseconds()
                };

                return new KeychainKey[] { key1, key2 };
            }

            return new KeychainKey[] { key1 };
        }

        private async void SetAuthCookie()
        {
            var claimsIdentity = new ClaimsIdentity(
                HttpContext.User.Claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [HttpGet("~/keychain-keys")]
        public async Task<IActionResult> GetKeysAsync()
        {
            await RuntimeSecrets.WaitOnKeychainSettingsAsync();

            if (
                string.IsNullOrEmpty(RuntimeSecrets.KeychainHashKey1)
                || string.IsNullOrEmpty(RuntimeSecrets.KeychainHashId1)
                || RuntimeSecrets.KeychainHashExpiration1 == null
            )
            {
                return BadRequest("KeychainHashKey1, KeychainHashId1 or KeychainHashExpiration1 keys are not found in the app settings.");
            }

            var userId = HttpContext.User.Identities.First().GetLegacyUserId();
            var responsePayload = CreateKeys(userId);

            SetAuthCookie();

            return Ok(
                JsonConvert.SerializeObject(responsePayload)
            );
        }
        
        [Authorize]
        [HttpPost("~/keychain-keys")]
        public async Task<IActionResult> CreateKeychainKeysAsync ()
        {
            await RuntimeSecrets.WaitOnKeychainSettingsAsync();
            
            if (string.IsNullOrEmpty(RuntimeSecrets.KeychainHashKey1)
                || string.IsNullOrEmpty(RuntimeSecrets.KeychainHashId1)
                || RuntimeSecrets.KeychainHashExpiration1 == null
            )
            {
                return BadRequest("KeychainHashKey1, KeychainHashId1 or KeychainHashExpiration1 keys are not found in the app settings.");
            }

            var userId = HttpContext.User.Identities.First().GetLegacyUserId();

            var responsePayload = CreateKeys(userId);

            SetAuthCookie();

            return Ok(
                JsonConvert.SerializeObject(responsePayload)
            );
        }
    }
}
