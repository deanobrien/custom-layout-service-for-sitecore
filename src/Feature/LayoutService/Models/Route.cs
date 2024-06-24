using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using DeanOBrien.Feature.LayoutService.Controllers;

namespace DeanOBrien.Feature.LayoutService.Models
{
    public class Route
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ItemId { get; set; }
        public string TemplateName { get; set; }
        public string TemplateID { get; set; }
        public string Path { get; set; }
        public List<SimpleNavItem> Parents { get; set; }
        public List<SimpleNavItem> Children { get; set; }
        public List<SimpleNavItem> Siblings { get; set; }
        public ExpandoObject Fields { get; set; }
        public List<Component> Components { get; set; }
        public Component Component { get; set; }
    }
}