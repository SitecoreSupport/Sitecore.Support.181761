using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Abstractions;
using Sitecore.ContentSearch.Client.Pipelines.Search;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Search;
using Sitecore.Search;
using Sitecore.Shell;
using Sitecore.StringExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sitecore.Support.ContentSearch.Client.Pipelines.Search
{
    public class SearchContentSearchIndex
    {
        private bool IsHidden(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            return item.Appearance.Hidden || (item.Parent != null && this.IsHidden(item.Parent));
        }
        public SearchContentSearchIndex()
        {
        }
        public SearchContentSearchIndex(ISettings settings)
        {
            this.settings = settings;
        }
        private ISettings settings;
        public virtual void Process(SearchArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.UseLegacySearchEngine)
            {
                return;
            }
            if (!ContentSearchManager.Locator.GetInstance<IContentSearchConfigurationSettings>().ItemBucketsEnabled())
            {
                args.UseLegacySearchEngine = true;
                return;
            }
            Item item = args.Root ?? args.Database.GetRootItem();
            //Sitecore.Support.181761
            while (item.ID != new ID("{11111111-1111-1111-1111-111111111111}"))
            {
                item = item.Parent;
            }
            //

            Assert.IsNotNull(item, "rootItem");
            if (args.TextQuery.IsNullOrEmpty())
            {
                return;
            }
            ISearchIndex index = ContentSearchManager.GetIndex(new SitecoreIndexableItem(item));
            if (this.settings == null)
            {
                this.settings = index.Locator.GetInstance<ISettings>();
            }
            using (IProviderSearchContext providerSearchContext = index.CreateSearchContext(SearchSecurityOptions.Default))
            {
                Func<SitecoreUISearchResultItem, bool> predicate = null;
                List<SitecoreUISearchResultItem> results = new List<SitecoreUISearchResultItem>();
                try
                {
                    IQueryable<SitecoreUISearchResultItem> queryable = null;
                    if (args.Type != Sitecore.Search.SearchType.ContentEditor)
                    {
                        queryable = new GenericSearchIndex().Search(args, providerSearchContext);
                    }
                    if (queryable == null || queryable.Count<SitecoreUISearchResultItem>() == 0)
                    {
                        if (args.ContentLanguage != null && !args.ContentLanguage.Name.IsNullOrEmpty())
                        {
                            queryable = from i in providerSearchContext.GetQueryable<SitecoreUISearchResultItem>()
                                        where i.Name.StartsWith(args.TextQuery) || (i.Content.Contains(args.TextQuery) && i.Language.Equals(args.ContentLanguage.Name))
                                        select i;
                        }
                        else
                        {
                            queryable = from i in providerSearchContext.GetQueryable<SitecoreUISearchResultItem>()
                                        where i.Name.StartsWith(args.TextQuery) || i.Content.Contains(args.TextQuery)
                                        select i;
                        }
                    }
                    if (args.Root != null && args.Type != SearchType.ContentEditor)
                    {
                        queryable = from i in queryable
                                    where i.Paths.Contains(args.Root.ID)
                                    select i;
                    }
                    if (predicate == null)
                    {
                        predicate = result => results.Count < args.Limit;
                    }
                    using (IEnumerator<SitecoreUISearchResultItem> enumerator = queryable.TakeWhile<SitecoreUISearchResultItem>(predicate).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            SitecoreUISearchResultItem result = enumerator.Current;
                            if (!UserOptions.View.ShowHiddenItems)
                            {
                                Item item2 = result.GetItem();
                                if (item2 != null && this.IsHidden(item2))
                                {
                                    continue;
                                }
                            }
                            SitecoreUISearchResultItem sitecoreUISearchResultItem = results.FirstOrDefault((SitecoreUISearchResultItem r) => r.ItemId == result.ItemId);
                            if (sitecoreUISearchResultItem == null)
                            {
                                results.Add(result);
                            }
                            else if (args.ContentLanguage != null && !args.ContentLanguage.Name.IsNullOrEmpty())
                            {
                                if ((sitecoreUISearchResultItem.Language != args.ContentLanguage.Name && result.Language == args.ContentLanguage.Name) || (sitecoreUISearchResultItem.Language == result.Language && sitecoreUISearchResultItem.Uri.Version.Number < result.Uri.Version.Number))
                                {
                                    results.Remove(sitecoreUISearchResultItem);
                                    results.Add(result);
                                }
                            }
                            else if (args.Type != SearchType.Classic)
                            {
                                if (sitecoreUISearchResultItem.Language == result.Language && sitecoreUISearchResultItem.Uri.Version.Number < result.Uri.Version.Number)
                                {
                                    results.Remove(sitecoreUISearchResultItem);
                                    results.Add(result);
                                }
                            }
                            else
                            {
                                results.Add(result);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("Invalid lucene search query: " + args.TextQuery, exception, this);
                    return;
                }
                foreach (SitecoreUISearchResultItem current in results)
                {
                    string title = current.DisplayName ?? current.Name;
                    object arg_77A_0;
                    if ((arg_77A_0 = current.Fields.Find((KeyValuePair<string, object> pair) => pair.Key == Sitecore.ContentSearch.BuiltinFields.Icon).Value) == null)
                    {
                        arg_77A_0 = (current.GetItem().Appearance.Icon ?? this.settings.DefaultIcon());
                    }
                    object obj = arg_77A_0;
                    string url = string.Empty;
                    if (current.Uri != null)
                    {
                        url = current.Uri.ToString();
                    }
                    args.Result.AddResult(new SearchResult(title, obj.ToString(), url));
                }
            }
        }
    }
}
