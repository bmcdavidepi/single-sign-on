using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;

[assembly: OwinStartup(typeof(AlloyWeb.Startup))]

namespace AlloyWeb
{
  public class Startup
  {
    private const string LogoutUrl = "/util/logout.aspx";
    private readonly IServiceLocator _serviceLocator;
    private readonly AzureGraphServiceOptions _azureGraphServiceOptions;
    private readonly Func<AzureGraphService> _azureGraphFactory;
    private readonly Func<ISynchronizingUserService> _syncUserServiceFactory;

    /// <summary>
    /// Empty constructor for Activator.CreateInstance used by Owin Startup
    /// </summary>
    public Startup() : this(null, null, null) { }

    public Startup(AzureGraphServiceOptions azureGraphServiceOptions, Func<AzureGraphService> azureGraphFactory, Func<ISynchronizingUserService> syncUserServiceFactory)
    {
      _serviceLocator = ServiceLocator.Current;
      _azureGraphServiceOptions = azureGraphServiceOptions ?? _serviceLocator.GetInstance<AzureGraphServiceOptions>();
      _azureGraphFactory = azureGraphFactory ?? (() => _serviceLocator.GetInstance<AzureGraphService>());
      _syncUserServiceFactory = syncUserServiceFactory ?? (() => _serviceLocator.GetInstance<ISynchronizingUserService>());
    }

    public void Configuration(IAppBuilder app)
    {
      // below are needed for groups to display in UI
      //app.CreatePerOwinContext<UIRoleProvider>(() => new TestRoleProvider(_azureGraphFactory()));
      //app.CreatePerOwinContext<UIUserProvider>(() => new TestUserProvider());

      // Enable cookie authentication, used to store the claims between requests 
      app.SetDefaultSignInAsAuthenticationType(WsFederationAuthenticationDefaults.AuthenticationType);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = WsFederationAuthenticationDefaults.AuthenticationType
      });

      // Enable federated authentication 
      app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions
      {
        MetadataAddress = _azureGraphServiceOptions.MetadataAddress,
        Wtrealm = _azureGraphServiceOptions.WtRealm,
        Notifications = new WsFederationAuthenticationNotifications
        {
          RedirectToIdentityProvider = ctx =>
          {
            //  To avoid a redirect loop to the federation server send 403 when user is authenticated but does not have access 
            if (ctx.OwinContext.Response.StatusCode == 401 && ctx.OwinContext.Authentication.User.Identity.IsAuthenticated)
            {
              ctx.OwinContext.Response.StatusCode = 403;
              ctx.HandleResponse();
            }

            return Task.FromResult(0);
          },
          SecurityTokenValidated = async ctx =>
          {
            // Ignore scheme/host name in redirect Uri to make sure a redirect to HTTPS does not redirect back to HTTP 
            var redirectUri = new Uri(ctx.AuthenticationTicket.Properties.RedirectUri, UriKind.RelativeOrAbsolute);

            if (redirectUri.IsAbsoluteUri)
            {
              ctx.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
            }

            // Create claims for roles await 
            await _azureGraphFactory().AddGroupsAsRoleClaimsAsync(ctx.AuthenticationTicket.Identity);

            // Add other claims as needed
            ctx.AuthenticationTicket.Identity
              .AddClaim(new Claim(
                  ClaimTypes.Name,
                  ctx.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/displayname").Value,
                  ClaimValueTypes.String
            ));

            // Sync user and the roles to EPiServer in the background
            await _syncUserServiceFactory().SynchronizeAsync(ctx.AuthenticationTicket.Identity);
          }
        }
      });

      // Add stage marker to make sure WsFederation runs on Authenticate (before URL Authorization and virtual roles) 
      app.UseStageMarker(PipelineStage.Authenticate);

      // Remap logout to a federated logout 
      app.Map(LogoutUrl, map =>
      {
        map.Run(ctx =>
        {
          ctx.Authentication.SignOut();
          return Task.FromResult(0);
        });
      });

      // Tell antiforgery to use the name claim 
      AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
    }
  }

  [ModuleDependency(typeof(ServiceContainerInitialization))]
  internal class ConfigureProviders : IConfigurableModule
  {
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
      context.Services
        //.AddTransient<UIUserProvider>(s => HttpContext.Current.GetOwinContext().Get<UIUserProvider>())
        //.AddTransient<UIRoleProvider>(s => HttpContext.Current.GetOwinContext().Get<UIRoleProvider>())
        //.AddTransient<SecurityEntityProvider>(s => new TestSecurityProvider(s.GetInstance<UIRoleProvider>()))
        //.AddTransient<IQueryableNotificationUsers>(s => new TestSecurityProvider(s.GetInstance<UIRoleProvider>()))

        .AddTransient<AzureGraphService>()
        //.AddTransient<UIUserProvider, TestUserProvider>()
        //.AddTransient<UIRoleProvider, TestRoleProvider>()
        //.AddTransient<SecurityEntityProvider, AzureAdfsSecurityProvider>()
        // apply the synchronizing provider as the default
        .AddTransient<SecurityEntityProvider, SynchronizingRolesSecurityEntityProvider>()
        // wraps default with Azure AD graph client
        .Intercept<SecurityEntityProvider>((locator, defaultSecurityProvider) =>
          new AzureAdfsSecurityProvider(defaultSecurityProvider, locator.GetInstance<AzureGraphService>())
        )
      ;
    }

    public void Initialize(InitializationEngine context) { }

    public void Uninitialize(InitializationEngine context) { }
  }

  //EPiServer.Security.SynchronizingRolesSecurityEntityProvider
  //EPiServer.UI.Edit.MembershipBrowser
  //public Injected<EPiServer.Security.SecurityEntityProvider> SecurityEntityProvider { get; set; }
  //System.Security.Claims.ClaimsIdentity
  // IPrincipal: System.Security.Claims.ClaimsPrincipal

  public class AzureAdfsSecurityProvider : SecurityEntityProvider
  {
    private readonly SecurityEntityProvider _defaultProvider;
    private readonly AzureGraphService _azureGraphService;

    public AzureAdfsSecurityProvider(SecurityEntityProvider defaultProvider, AzureGraphService azureGraphService)
    {
      _defaultProvider = defaultProvider;
      _azureGraphService = azureGraphService;
    }

    public override IEnumerable<string> GetRolesForUser(string userName) => _defaultProvider.GetRolesForUser(userName);

    /// <summary>
    /// Called by UI to assign permissions
    /// </summary>
    /// <param name="partOfValue"></param>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public override IEnumerable<SecurityEntity> Search(string partOfValue, string claimType)
    {
      switch (claimType)
      {
        case ClaimTypes.Name:
          return _defaultProvider.Search(partOfValue, claimType);

        case ClaimTypes.Role:
        default:
          var hashSet = new HashSet<SecurityEntity>(_defaultProvider.Search(partOfValue, claimType), new SecurityNameComparer());
          var azureGroups = _azureGraphService.FindGroupsAsync(partOfValue).Result;

          foreach (var azGroup in azureGroups)
          {
            hashSet.Add(new SecurityEntity(azGroup, SecurityEntityType.Role));
          }

          return hashSet;
      }
    }

    public class SecurityNameComparer : IEqualityComparer<SecurityEntity>
    {
      public bool Equals(SecurityEntity x, SecurityEntity y) => x?.Name == y?.Name;

      public int GetHashCode(SecurityEntity obj) => obj.Name.GetHashCode();
    }

    public override IEnumerable<SecurityEntity> Search(string partOfValue, string claimType, int startIndex, int maxRows, out int totalCount)
    {
      var items = Search(partOfValue, claimType).ToList();
      totalCount = items.Count;

      return items;
    }
  }
}
