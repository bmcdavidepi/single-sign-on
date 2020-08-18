# Single Sign-On (SSO) Example

## Getting started

To run this project locally after cloning or downloading as a zip, follow these steps on initial setup:

### To run demo site

* Create a SQL db in AlloyWeb\App_Data with the name 'EPiServerDB_d95c272a'
* Follow section 'Setting Up ADFS for Azure AD'
* If needed: Right-click AlloyWeb in the Solution Explorer to set as startup project.
* Build and Run the AlloyWeb project.
* Optionally test ADFS SSO on /episerver login.

## Important Files

The below files are imporant to this demonstration:

* AlloyWeb\AzureGraphService.cs - used to map Azure AD Groups to Roles on login.
* AlloyWeb\AzureGraphServiceOptions.cs - used to map AppSetting values to a configuration class.
* AlloyWeb\Startup.cs - uses Owin for federated logins with Azure AD.

## Setting Up ADFS for Azure AD

The following steps are for setting up Azure AD to allow Episerver logins.

* Login to [Azure](https://portal.azure.com) with an account you have access to create app registrations in Azure AD.
* Select Azure Active Directory -> App Registrations 
* Select Endpoints
  * Copy the FEDERATION METADATA DOCUMENT value and set as the value in the web.config appSetting 'MetadataAddress'.
* Close and then select +New application registration.
* Enter the following
  * Any name such as SSO Demo
  * Leave application type Web app / API
  * Sign-on URL: http://localhost:58954/ (matches our IIS express in the Alloy Web project)
  * Select Create
* Copy the Application ID GUID and set as the value in the web.config appSetting 'ClientId'.
* Select settings -> Keys
  * Under Passwords, enter a description and select a duration.
  * Click Save.
  * Copy the value of the now shown and set as the value in the web.config appSetting 'ClientSecret'. Important: This value will no longer be visible one screen is closed! (ex value: +BzJV7KthrwzGGf73UeVxyYh7kZ21Xz8g2xqj9ikuIw= )
* Select Required permissions
  * Select Windows Azure Active Directory
  * Check Read directory data for Application Permissions
  * Check Sign in and read user profile and Read Directory data for Delegated Permissions.
  * Click Save.
  * Optional, ensure settings are saved.
* Select Grant permissions to ensure the new permissions are set.
* Close and select Properties
  * Copy the App ID URI value and set as the value in the web.config appSetting 'Wtrealm'.
  * Copy the fully qualified domain name portion of the AppID URI and set as the value in the web.config appSetting 'TenantName'. (ex value: bradmcdavidgmail.onmicrosoft.com)
  * Set Logout URL to be 'http://localhost:58954/util/logout.aspx'.
  * Optionally set a logo.
  * Click save.
* Create/Invite users to the Active Directory.
* Create security groups for WebAdmins, WebEditors in Azure AD.
* Add users to appropriate groups in Azure AD. Important: Episerver will have no users or groups, all is controlled using Azure AD.
* Azure setup should now be completed.
