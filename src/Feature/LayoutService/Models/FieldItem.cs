using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace DeanOBrien.Feature.LayoutService.Models
{
    public class FieldItem
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public ExpandoObject Fields { get; set; }
    }
}