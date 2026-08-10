using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ECommerceManagement.Infrastructure.Security
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Önce varsayılan .NET policy'lerinde var mı diye bak
            var policy = await base.GetPolicyAsync(policyName);

            if (policy == null)
            {
                // Yoksa, dinamik olarak bizim PermissionRequirement'i ekle
                policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
            }

            return policy;
        }
    }
}