using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.DependencyInjection;
using Sitecore.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace DeanOBrien.Feature.LayoutService.Helper
{
    public static class ComponentHelper
    {
        public static Database _database { get; set; }

        public static object GetCaseList(string upn)
        {
            // This would be a custom view model from original MVC solution
            var viewModel = new object();
            
            return viewModel;
        }


        public static object GetNewsFeed(string dataSource)
        {
            // This would be a custom view model from original MVC solution
            var viewModel = new object();

            return viewModel;
        }
    }
}