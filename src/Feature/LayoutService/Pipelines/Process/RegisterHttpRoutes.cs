using System.Web.Http;
using Sitecore.Pipelines;

namespace DeanOBrien.Feature.LayoutService.Pipelines.Process
{
    public class RegisterHttpRoutes
    {
        public void Process(PipelineArgs args)
        {
            HttpConfiguration config = GlobalConfiguration.Configuration;
            config.Routes.MapHttpRoute("LayoutService", "sitecore/api/layoutservice/get", new
            {
                controller = "LayoutApi",
                action = "Index"
            });
            config.Routes.MapHttpRoute("SecureLayoutService", "sitecore/api/layoutservice/secure", new
            {
                controller = "LayoutApi",
                action = "Secure"
            });
            config.Routes.MapHttpRoute("StaticPaths", "sitecore/api/staticpaths/get", new
            {
                controller = "LayoutApi",
                action = "StaticPaths"
            });
        }
    }
}