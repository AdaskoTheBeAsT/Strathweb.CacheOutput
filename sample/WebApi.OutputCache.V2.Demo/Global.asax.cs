using System;
using System.Web;

namespace OwinWebApi
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Session_End(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }

        protected void Application_End(object sender, EventArgs e)
        {
            // global asax method without implementation - needed for Azure
        }
    }
}
