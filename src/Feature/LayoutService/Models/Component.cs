using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace DeanOBrien.Feature.LayoutService.Models
{
    public class Component
    {
        public string Id { get; set; }
        public string ComponentName { get; set; }
        public string Title { get; set; }
        public string PH { get; set; }
        public string DS { get; set; }
        public string UID { get; set; }
        public string PAR { get; set; }
        public string PAFTER { get; set; }
        public ExpandoObject Fields { get; set; }
        public object CustomViewModel { get; set; }
    }
}