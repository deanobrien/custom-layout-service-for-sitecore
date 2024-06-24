using System.Web.Http;
using Sitecore.Sites;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using Sitecore.Data;
using System.Text.RegularExpressions;
using System.Dynamic;
using System;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.Data.Fields;
using Sitecore.Links;
using DeanOBrien.Feature.LayoutService.Helper;
using System.Security.Claims;
using DeanOBrien.Feature.LayoutService.Models;
using Sitecore.Services.Infrastructure.Web.Http;

namespace DeanOBrien.Feature.LayoutService.Controllers
{
    public class LayoutApiController : ServicesApiController
    {
        public static Database _database { get; set; }

        
        private static string[] ignoreList = { "__createdby", "__updatedby", "__finalrenderings", "__revision", "__lock", "__created", "__updated", "__sortorder", "__basetemplate", "__icon", "__standardvalues", "__masters", "__renderings", "__validfrom", "__owner" };
        private static string[] pageTemplates = { "{2A10A69C-D78C-49CC-8CE6-B2C760B34CB1}", "{E2A9ED18-95B5-4247-84A3-3823D102EB45}","{C2E6F5C2-478A-4C1A-9DD6-F5EBD638E378}", "{A9D9D9C8-7E45-4BA0-8345-9ABA38F7C8A4}", "{9B426CFE-D9E3-4DB9-8B63-06209EF7534E}", "{7AF87EB4-423B-4F15-98F3-C5BED2497531}", "{EB5B9E5B-9AAB-4B06-AA8B-D60F53E227CF}", "{D49A718D-F5FB-442E-B602-935968BC9E62}", "{8AC4B7CA-DE49-48E3-9B1F-C40274E4BD40}", "{957385D3-EA3B-499E-8E7D-0C0773165F71}" };
        private const string NewsFeedComponentId = "{34067097-6710-4070-986D-05ED07E688BF}";
        private const string CaseListComponentId = "{1926CC22-B709-4BB5-9B6A-94A53622F9F6}";
        private SiteContext _siteContext;
        private string _root;
        private string _toDiscard;
        private Item _rootItem;

        private string _email { 
            get
            {
                if (Sitecore.Context.User.Identity is ClaimsIdentity)
                {
                    var claimsIdentity = Sitecore.Context.User.Identity as ClaimsIdentity;
                    return claimsIdentity.Claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").FirstOrDefault().Value;
                }
                return string.Empty;
            }
        }

        public LayoutApiController() {

        }
        [HttpGet]
        public IHttpActionResult StaticPaths(string lang, string site, string apiKey)
        {
            Initialize(site);

            var result = new List<string>();

            _toDiscard = _root.Replace("/sitecore/content", "");

            var allPages = _rootItem.Axes.GetDescendants().Where(x => pageTemplates.Contains(x.TemplateID.ToString()));
            foreach (var child in allPages)
            {
                result.Add(child.Paths.ContentPath.Replace(_toDiscard, ""));
            }
            return Json(result);
        }

        private void Initialize(string site)
        {
            _siteContext = SiteContextFactory.GetSiteContext(site);
            _database = _siteContext.Database;
            _root = _siteContext.StartPath;
            _rootItem = _database.GetItem(_root);
        }

        [HttpPost]
        [Authorize]
        public IHttpActionResult Secure(string path, string lang, string site, string apiKey, string componentId = null, string componentDataSourceId = null, bool includeFields = true, bool includeParents = false, bool includeChildren = false, bool includeSiblings = false)
        {
            var result = new LayoutServiceResponse();

            Initialize(site);

            path = path.Replace("-", " ");

            var item = _database.GetItem($"{_root}/{path}");

            AddContext(lang, site, result);
            AddRoute(path, result, item);
            if (includeParents) AddParents(result, item);
            if (includeChildren) AddChildren(result, item);
            if (includeSiblings) AddSiblings(result, item);

            using (new Sitecore.SecurityModel.SecurityDisabler())
            {
                AddFieldsAndComponentsFromBaseTemplates(includeFields, result, item);
                AddFieldsAndComponentsFromStandardValues(includeFields, result, item);
            }
            AddFieldsAndComponentsFromItem(includeFields, result, item);

            foreach (var component in result.Route.Components)
            {
                AddFieldsToComponent(component);
            }

            if (!string.IsNullOrWhiteSpace(componentId)) SelectSingleComponent(componentId, componentDataSourceId, result);

            return Json(result);
        }


        [HttpGet]
        public IHttpActionResult Index(string path = "/", string lang="en", string site="website", string apiKey="notset", string componentId = null, string componentDataSourceId = null, bool includeFields = true, bool includeParents=false, bool includeChildren=false, bool includeSiblings=false)
        {
            var result = new LayoutServiceResponse();

            Initialize(site);

            path = path.Replace("-", " ");

            var item = _database.GetItem($"{_root}/{path}");

            AddContext(lang, site, result);
            AddRoute(path, result, item);
            if (includeParents) AddParents(result, item);
            if (includeChildren) AddChildren(result, item);
            if (includeSiblings) AddSiblings(result, item);

            using (new Sitecore.SecurityModel.SecurityDisabler())
            {
                AddFieldsAndComponentsFromBaseTemplates(includeFields, result, item);
                AddFieldsAndComponentsFromStandardValues(includeFields, result, item);
            }
            AddFieldsAndComponentsFromItem(includeFields, result, item);

            foreach (var component in result.Route.Components)
            {
                AddFieldsToComponent(component);
            }

            if (!string.IsNullOrWhiteSpace(componentId)) SelectSingleComponent(componentId, componentDataSourceId, result);

            return Json(result);
        }

        private void SelectSingleComponent(string componentId, string componentDataSourceId, LayoutServiceResponse result)
        {
            result.Route.Component = result.Route.Components.Where(component => component.UID == componentId).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(componentDataSourceId)) AddCustomViewModel(componentDataSourceId, result.Route.Component);
            result.Route.Components = null;
        }

        private void AddFieldsToComponent(Component component)
        {
            if (!string.IsNullOrWhiteSpace(component.DS))
            {
                var componentDataSourceItem = _database.GetItem(component.DS);
                if (componentDataSourceItem != null)
                {
                    Sitecore.Data.Items.Item componentDataSourceItemStandardValues;
                    using (new Sitecore.SecurityModel.SecurityDisabler())
                    {
                        componentDataSourceItemStandardValues = componentDataSourceItem?.Template?.StandardValues;
                    }

                    AddFields(componentDataSourceItemStandardValues, component.Fields);
                    AddFields(componentDataSourceItem, component.Fields);

                    AddCustomViewModel(component.DS, component);
                }
            }
        }

        private static void AddFieldsAndComponentsFromItem(bool includeFields, LayoutServiceResponse result, Item item)
        {
            if (item.Fields != null)
            {
                if (includeFields) AddFields(item, result.Route.Fields);

                var renderings = item.Fields["__Renderings"];
                if (renderings != null) ExtractComponentsFromXML(result, renderings);

                var finalRenderings = item.Fields["__Final Renderings"];
                if (finalRenderings != null) ExtractComponentsFromXML(result, finalRenderings);
            }
        }

        private static void AddFieldsAndComponentsFromStandardValues(bool includeFields, LayoutServiceResponse result, Item item)
        {
            Sitecore.Data.Items.Item itemStandardValues = item.Template.StandardValues;
            if (itemStandardValues != null && itemStandardValues.Fields != null)
            {
                if (includeFields) AddFields(itemStandardValues, result.Route.Fields);

                var renderingsStandardValues = itemStandardValues.Fields["__Renderings"];
                if (renderingsStandardValues != null) ExtractComponentsFromXML(result, renderingsStandardValues);

                var finalRenderingsStandardValues = itemStandardValues.Fields["__Final Renderings"];
                if (finalRenderingsStandardValues != null) ExtractComponentsFromXML(result, finalRenderingsStandardValues);
            }
        }

        private static void AddFieldsAndComponentsFromBaseTemplates(bool includeFields, LayoutServiceResponse result, Item item)
        {
            var baseTemplates = new TemplateItem(item.Template).BaseTemplates.ToList();
            foreach (var baseTemplate in baseTemplates)
            {
                var baseTemplateStandardValues = baseTemplate.StandardValues;
                if (baseTemplateStandardValues != null && baseTemplateStandardValues.Fields != null)
                {
                    if (includeFields) AddFields(baseTemplateStandardValues, result.Route.Fields);
                }
                if (baseTemplate != null && baseTemplate.Fields != null)
                {
                    if (includeFields) AddFields(baseTemplate, result.Route.Fields);
                }
            }
        }

        private static void AddSiblings(LayoutServiceResponse result, Item item)
        {
            var siblings = GetNavItems(item.Parent.Children.ToList());
            result.Route.Siblings = siblings;
        }

        private static void AddChildren(LayoutServiceResponse result, Item item)
        {
            var children = GetNavItems(item.Children.ToList());
            result.Route.Children = children;
        }

        private static void AddParents(LayoutServiceResponse result, Item item)
        {
            var parents = GetParents(item);
            var parentsNavItems = GetNavItems(parents);
            result.Route.Parents = parentsNavItems;
        }

        private static void AddRoute(string path, LayoutServiceResponse result, Item item)
        {
            result.Route = new Route()
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                ItemId = item.ID.ToString(),
                TemplateName = item.TemplateName,
                TemplateID = item.TemplateID.ToString(),
                Path = path,
                Fields = new ExpandoObject(),
                Components = new List<Component>()
            };
        }

        private static void AddContext(string lang, string site, LayoutServiceResponse result)
        {
            result.Context = new Context()
            {
                Site = site,
                Language = lang
            };
        }

        private void AddCustomViewModel(string componentDataSourceId, Component component)
        {
            // Add custom view model
            if (component.Id == NewsFeedComponentId) component.CustomViewModel = ComponentHelper.GetNewsFeed(componentDataSourceId);

            // _email would only be populated if the user successfully authorizes
            if (component.Id == CaseListComponentId && !string.IsNullOrWhiteSpace(_email)) component.CustomViewModel = ComponentHelper.GetCaseList(_email);
            
        }

        private static void AddFields(Item item, ExpandoObject result)
        {
            if (item !=null && item.Fields != null)
            {
                var fields = result as IDictionary<string, Object>;
                for (int i = 0; i < item.Fields.Count; i++)
                {
                    var key = item.Fields[i].Key.Replace(" ", "");
                    if (ignoreList.Contains(key)) continue;

                    var value = item.Fields[i].Value;

                    if (item.Fields[i].Type == "Image" || item.Fields[i].Type == "Droplink" || item.Fields[i].Type == "General Link") {

                        var newValue = new ExpandoObject();
                        var f = newValue as IDictionary<string, Object>;
                        var newInnerFields = new ExpandoObject();

                        if (item.Fields[i].Type == "Image")
                        {
                            var imageFieldItem = (ImageField)item.Fields[i];
                            if (imageFieldItem?.MediaItem == null)
                                continue;

                            AddPropertyToExpando("Url", MediaManager.GetMediaUrl(imageFieldItem.MediaItem), newInnerFields);
                            AddPropertyToExpando("Alt", imageFieldItem.Alt, newInnerFields);
                            AddPropertyToExpando("Height", imageFieldItem.Height, newInnerFields);
                            AddPropertyToExpando("Width", imageFieldItem.Width, newInnerFields);
                        }
                        else if (item.Fields[i].Type == "General Link")
                        {
                            var linkField = (LinkField)item.Fields[i];
                            if (string.IsNullOrWhiteSpace(linkField?.Value))
                                continue;

                            AddPropertyToExpando("Url", (linkField.TargetItem != null) ? LinkManager.GetItemUrl(linkField.TargetItem) : linkField.Url, newInnerFields);
                            AddPropertyToExpando("Text", linkField.Text, newInnerFields);
                            AddPropertyToExpando("Target", linkField.Target, newInnerFields);                           
                        }
                        else if (item.Fields[i].Type == "Droplink")
                        {
                            var linkedItem = _database.GetItem(value);
                            AddFields(linkedItem, newInnerFields);
                        }

                        f.Add("Fields", newInnerFields);
                        newValue = f as ExpandoObject;
                        fields.Add(key, newValue);

                    } else {
                        value = CleanIfRichText(item, i, value);

                        if (((IDictionary<String, object>)fields).ContainsKey(key))
                        {
                            ((IDictionary<String, Object>)fields).Remove(key);
                        }
                        fields.Add(key, value);
                    }
                }
                result = fields as ExpandoObject;
            }
        }

        private static void AddPropertyToExpando(string key, string value, ExpandoObject newInnerFields)
        {
            var innerFields = newInnerFields as IDictionary<string, Object>;
            if (((IDictionary<String, object>)innerFields).ContainsKey(key))
            {
                ((IDictionary<String, Object>)innerFields).Remove(key);
            }
            innerFields.Add(key, value);
        }
        private static List<Item> GetParents(Item item)
        {
            var homePath = Sitecore.Context.Site.StartPath;
            var homeItem = Sitecore.Context.Database.GetItem(homePath);
            var items = item.Axes.GetAncestors()
                .SkipWhile(x => x.ID != homeItem.ID)
                .ToList();

            return items;
        }
        private static List<SimpleNavItem> GetNavItems(List<Item> items)
        {
            return items.Select(x => new SimpleNavItem() { Id = x.ID.ToString(), Title = x.DisplayName, Url = LinkManager.GetItemUrl(x).ToLower(), TemplateName = x.TemplateName }).ToList();
        }
        private static string CleanIfRichText(Item item, int i, string value)
        {
            if (item.Fields[i].Type == "Rich Text")
            {
                string html = item.Fields[i].Value;
                string cleaned = new Regex("style=\"[^\"]*\"").Replace(html, "");
                cleaned = new Regex("(?<=class=\")([^\"]*)\\babc\\w*\\b([^\"]*)(?=\")").Replace(cleaned, "$1$2");
                value = cleaned;
            }

            return value;
        }
        private static void ExtractComponentsFromXML(LayoutServiceResponse result, Sitecore.Data.Fields.Field renderings)
        {
            if (renderings != null)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(renderings.Value);

                string xpath = "r/d/r";
                var nodes = xmlDoc.SelectNodes(xpath);

                foreach (XmlNode childrenNode in nodes)
                {
                    var component = new Component();
                    var uid = childrenNode.Attributes["uid"];
                    if (uid != null)
                    {
                        component.UID = uid.Value;
                    }
                    var id = childrenNode.Attributes["id"];
                    if (id != null)
                    {
                        component.Id = id.Value;
                    }
                    var sid = childrenNode.Attributes["s:id"];
                    if (sid != null)
                    {
                        component.Id = sid.Value;
                    }
                    var ds = childrenNode.Attributes["ds"];
                    if (ds != null)
                    {
                        component.DS = ds.Value;
                    }
                    var sds = childrenNode.Attributes["s:ds"];
                    if (sds != null)
                    {
                        component.DS = sds.Value;
                    }
                    var ph = childrenNode.Attributes["ph"];
                    if (ph != null)
                    {
                        component.PH = ph.Value;
                    }
                    var sph = childrenNode.Attributes["s:ph"];
                    if (sph != null)
                    {
                        component.PH = sph.Value;
                    }
                    var par = childrenNode.Attributes["par"];
                    if (par != null)
                    {
                        component.PAR = par.Value;
                    }
                    var spar = childrenNode.Attributes["s:par"];
                    if (spar != null)
                    {
                        component.PAR = spar.Value;
                    }
                    var pafter = childrenNode.Attributes["p:after"];
                    if (pafter != null)
                    {
                        component.PAFTER = pafter.Value;
                    }
                    if (!string.IsNullOrWhiteSpace(component.Id)) {
                        using (new Sitecore.SecurityModel.SecurityDisabler())
                        {
                            var componentItem = _database.GetItem(component.Id);
                            if (componentItem != null)
                            {
                                component.ComponentName = componentItem.DisplayName.Replace(" ", "");
                                component.Title = component.ComponentName;
                            }
                        }
                    }
                    component.Fields = new ExpandoObject();
                    var existingComponent = result.Route.Components.Where(x => x.UID == component.UID).FirstOrDefault();
                    if (existingComponent == null)
                    {
                        result.Route.Components.Add(component);
                    }
                    else 
                    {
                        if (component.Id != null) existingComponent.Id = component.Id;
                        if (component.DS != null) existingComponent.DS = component.DS;
                        if (component.PH != null) existingComponent.PH = component.PH;
                        if (component.PAR != null) existingComponent.PAR = component.PAR;
                    }
                    if (!string.IsNullOrEmpty(component.PAFTER) && component.PAFTER.Length>36)
                    {
                        var oldComponent = result.Route.Components.Where(x => x.UID == component.UID).FirstOrDefault();
                        result.Route.Components.Remove(oldComponent);
                        var targetUid = component.PAFTER.Substring(8, 38);
                        var targetItem = result.Route.Components.Where(x => x.UID == targetUid).FirstOrDefault();
                        var targetIndex = result.Route.Components.IndexOf(targetItem);
                        result.Route.Components.Insert(targetIndex+1, oldComponent);
                    }

                }
            }
        }
    }
}