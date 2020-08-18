using EPiServer.ServiceLocation;
using System.Configuration;

namespace AlloyWeb
{
  // https://world.episerver.com/blogs/Kalle-Ljung/Dates/2014/11/using-azure-active-directory-as-identity-provider/
  // https://blog.nicolaayan.com/2017/10/episerver-with-azure-ad-authentication/
  // Key: OgfE6/XUK/CQUL7V2+jo+PeF8/ACGbo5RujER28w03k=
  //Install-Package Microsoft.Owin.Security.Cookies
  //Install-Package Microsoft.Owin.Security.WsFederation
  //Install-Package Microsoft.Owin.Host.SystemWeb
  //Install-Package Microsoft.Azure.ActiveDirectory.GraphClient
  //Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory

  [ServiceConfiguration(typeof(AzureGraphServiceOptions), Lifecycle = ServiceInstanceScope.Singleton)]
  public class AzureGraphServiceOptions
  {
    public AzureGraphServiceOptions() : this(clientId: null) { }

    protected AzureGraphServiceOptions(string clientId = "", string clientSecret = "", string graphUrl = "", string tenantName = "", string tenantId= "", string metaAddress = "", string wtRealm = "")
    {
      ClientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : ConfigurationManager.AppSettings["ClientId"];
      ClientSecret = !string.IsNullOrWhiteSpace(clientSecret) ? clientSecret : ConfigurationManager.AppSettings["ClientSecret"];
      GraphUrl = !string.IsNullOrWhiteSpace(graphUrl) ? graphUrl : ConfigurationManager.AppSettings["GraphUrl"];
      TenantName = !string.IsNullOrWhiteSpace(tenantName) ? tenantName : ConfigurationManager.AppSettings["TenantName"];
      TenantId = !string.IsNullOrWhiteSpace(tenantId) ? tenantId : ConfigurationManager.AppSettings["TenantId"];
      MetadataAddress = !string.IsNullOrWhiteSpace(metaAddress) ? metaAddress : ConfigurationManager.AppSettings["MetadataAddress"];
      WtRealm = !string.IsNullOrWhiteSpace(wtRealm) ? metaAddress : ConfigurationManager.AppSettings["Wtrealm"];
    }

    /// <summary>
    /// Application Id in Azure AD
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Application Secret created in Azure AD
    /// </summary>
    public string ClientSecret { get; }

    /// <summary>
    /// Default is https://graph.windows.net
    /// </summary>
    public string GraphUrl { get; }

    /// <summary>
    /// Trusted URL to federation server meta data 
    /// </summary>
    public string MetadataAddress { get; }

    /// <summary>
    /// Tenant Name of Azure AD in format of {name}.onmicrosoft.com
    /// </summary>
    public string TenantName { get; }

    /// <summary>
    /// Tenant ID, typically found in WtRealm address
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Value of Wtreal must *exactly* match what is configured in the federation server
    /// </summary>
    public string WtRealm { get; }
  }
}
