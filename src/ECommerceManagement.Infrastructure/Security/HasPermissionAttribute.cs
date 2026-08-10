using Microsoft.AspNetCore.Authorization;

namespace ECommerceManagement.Infrastructure.Security
{
    // Sadece Class (Controller) ve Method (Endpoint) üzerine yazılabilir.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        // Örn: [HasPermission("Catalog.Manage")] yazdığımızda "Catalog.Manage" buraya gelecek.
        public HasPermissionAttribute(string permission) : base(policy: permission)
        {
        }
    }
}