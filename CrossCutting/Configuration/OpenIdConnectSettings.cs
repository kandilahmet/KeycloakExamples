using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Configuration
{
    public class OpenIdConnectSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }
        public string ResponseType { get; set; }
        public string CallbackPath { get; set; }
        public string SignedOutCallbackPath { get; set; }
        public string AccessDeniedPath { get; set; }
        public string SignedOutRedirectUri { get; set; }
        public string RemoteSignOutPath { get; set; }
        public List<string> Scopes { get; set; } = new();
        public TokenValidationParameters TokenValidationParameter { get; set; }  = new TokenValidationParameters();

        public class TokenValidationParameters
        {
            public string NameClaimType { get; set; }
            public string RoleClaimType { get; set; }
        }

    }
   
}
