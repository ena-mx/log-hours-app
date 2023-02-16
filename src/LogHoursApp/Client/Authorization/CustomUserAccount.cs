using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace LogHoursApp.Client.Authorization
{
    public class CustomUserAccount : RemoteUserAccount
    {
        [JsonPropertyName("groups")]
        public string[] Groups { get; set; } = new string[] { };

        [JsonPropertyName("roles")]
        public string[] Roles { get; set; } = new string[] { };
    }

    public class CustomAccountFactory
      : AccountClaimsPrincipalFactory<CustomUserAccount>
    {
        private readonly ILogger<CustomAccountFactory> logger;
        private readonly IServiceProvider serviceProvider;

        public CustomAccountFactory(IAccessTokenProviderAccessor accessor,
            IServiceProvider serviceProvider,
            ILogger<CustomAccountFactory> logger)
            : base(accessor)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async override ValueTask<ClaimsPrincipal> CreateUserAsync(
            CustomUserAccount account,
            RemoteAuthenticationUserOptions options)
        {
            var initialUser = await base.CreateUserAsync(account, options);

            if (initialUser.Identity.IsAuthenticated)
            {
                logger.LogInformation("User is authenticated");
                var userIdentity = (ClaimsIdentity)initialUser.Identity;

                foreach (var role in account.Roles)
                {
                    userIdentity.AddClaim(new Claim("role", role));
                    logger.LogInformation($"Role {role} was mapped to the user");
                }
            }

            return initialUser;
        }
    }
}
