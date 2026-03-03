using Keycloak.AuthServices.Authorization;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Common.Infrastructure.Security;

public static class SecurityServiceExtensions
{
    /// <summary>
    /// MESNET security altyapısını DI container'a ekler:
    /// 1. ICurrentUserService — HttpContext claim → UserContext
    /// 2. Authorization policies — permission ve rol bazlı
    /// 3. Keycloak claims transformation — realm roles → ClaimTypes.Role
    /// 4. Custom permission handler + claims transformation
    /// </summary>
    public static IServiceCollection AddMesnetSecurity(
        this IServiceCollection services, IConfiguration configuration)
    {
        // HttpContextAccessor — ICurrentUserService için gerekli
        services.AddHttpContextAccessor();

        // ICurrentUserService — Scoped (request başına)
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Authorization policies
        services.AddAuthorization(options =>
        {
            // Her permission sabiti için policy oluştur
            foreach (var permission in Permissions.GetAll())
            {
                options.AddPolicy(permission, policy =>
                    policy.AddRequirements(new PermissionRequirement(permission)));
            }

            // Rol bazlı policy'ler (Keycloak realm roles)
            options.AddPolicy("RequireInstitutionManager", policy =>
                policy.RequireRealmRoles(MesnetRoles.InstitutionManager));

            options.AddPolicy("RequireTeacher", policy =>
                policy.RequireRealmRoles(MesnetRoles.Teacher));

            options.AddPolicy("RequireStudent", policy =>
                policy.RequireRealmRoles(MesnetRoles.Student));

            options.AddPolicy("RequireDepartmentHead", policy =>
                policy.RequireRealmRoles(MesnetRoles.DepartmentHead));

            options.AddPolicy("RequireCompanyManager", policy =>
                policy.RequireRealmRoles(MesnetRoles.CompanyManager));
        });

        // Keycloak Authorization — claims transformation + handler + policy provider
        services.AddKeycloakAuthorization(options =>
        {
            options.EnableRolesMapping = RolesClaimTransformationSource.Realm;
            options.RolesResource = "mesnet-api";
        });

        // Custom permission authorization handler
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Custom claims transformation — Marten'dan güncel permission'lar + institution_id enrichment
        services.AddTransient<IClaimsTransformation, PermissionClaimsTransformation>();

        // IMemoryCache — PermissionClaimsTransformation cache'i için
        services.AddMemoryCache();

        return services;
    }
}
