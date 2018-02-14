using System;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Pages;
using System.Net;

private static readonly string ADMIN_USER_CONFIG_KEY = "SharePointAdminUser";
private static readonly string ADMIN_PASSWORD_CONFIG_KEY = "SharePointAdminPassword";

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    // collect site/page details from request body..
    dynamic data = await req.Content.ReadAsAsync<object>();
    string siteUrl = data.SiteUrl;
    string pageName = data.PageName;
    string pageText = data.PageText;

    // fetch auth credentials from config - N.B. consider use of app authentication for production code!
    string ADMIN_USER_CONFIG_KEY = "SharePointAdminUser";
    string ADMIN_PASSWORD_CONFIG_KEY = "SharePointAdminPassword";
    string adminUserName = System.Environment.GetEnvironmentVariable(ADMIN_USER_CONFIG_KEY, EnvironmentVariableTarget.Process);
    string adminPassword = System.Environment.GetEnvironmentVariable(ADMIN_PASSWORD_CONFIG_KEY, EnvironmentVariableTarget.Process);

    log.Info($"Will attempt to authenticate to SharePoint with username {adminUserName}");

    // auth to SharePoint and get ClientContext..
    ClientContext siteContext = AuthenticationManager.GetSharePointOnlineAuthenticatedContextTenant(siteUrl, adminUserName, adminPassword);
    

    /*SecureString securePwd = new SecureString();
    foreach (char c in adminPassword.ToCharArray()) 
    {
        securePwd.AppendChar(c);
    }
        ClientContext siteContext = new ClientContext(siteUrl);
        siteContext.Credentials = new SharePointOnlineCredentials(adminUserName, securePwd);
*/

    
    Site site = siteContext.Site;
    siteContext.Load(site);
    siteContext.ExecuteQueryRetry();

    log.Info($"Successfully authenticated to site {siteContext.Url}..");

    log.Info($"Will attempt to create page with name {data.PageName}");

    ClientSidePage page = new ClientSidePage(siteContext);
    ClientSideText txt1 = new ClientSideText() { Text = pageText };
    page.AddControl(txt1, 0);

    // page will be created if it doesn't exist, otherwise overwritten if it does..
    page.Save(pageName);

    return pageName == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass site URL, page name and page text in request body!")
        : req.CreateResponse(HttpStatusCode.OK, "Created page " + pageName);
}
