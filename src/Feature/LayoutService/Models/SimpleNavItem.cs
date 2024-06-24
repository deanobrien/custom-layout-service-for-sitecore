using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DeanOBrien.Feature.LayoutService.Models
{
    public class SimpleNavItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Id { get; set; }
        public string TemplateName { get; set; }
    }
}