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
    public class SearchContentSearchIndex : Sitecore.ContentSearch.Client.Pipelines.Search.SearchContentSearchIndex
    {
        public override void Process(SearchArgs args)
        {
            Item item = args.Database.GetRootItem();
            args.Root = item;
            base.Process(args);
        } 
    }
}
