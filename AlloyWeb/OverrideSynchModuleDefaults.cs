using bmcdavid.Episerver.SynchronizedProviderExtensions;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;

namespace AlloyWeb
{
  [ModuleDependency(typeof(ServiceContainerInitialization))]
  [ModuleDependency(typeof(ExtendedRoleConfigurationModule))]
  public class OverrideSynchModuleDefaults : IConfigurableModule
  {
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
      // replaces lifetimes set in ExtendedRoleConfigurationModule
      context.Services.RemoveAll<UIUserProvider>();
      context.Services.RemoveAll<UIRoleProvider>();
      context.Services
          .AddScoped<EPiServer.Notification.IQueryableNotificationUsers, ExtendedUserProvider>()
          .AddScoped<UIUserProvider, ExtendedUserProvider>()
          .AddScoped<UIRoleProvider, ExtendedRoleProvider>()
          .AddTransient<SecurityEntityProvider, ExtendedSecurityProvider>();
    }

    public void Initialize(InitializationEngine context) { }

    public void Uninitialize(InitializationEngine context) { }
  }
}
