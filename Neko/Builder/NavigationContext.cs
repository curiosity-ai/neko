using System.Collections.Generic;

namespace Neko.Builder
{
    public class NavigationContext
    {
        public List<NavigationItem> Breadcrumbs { get; set; } = new List<NavigationItem>();
        public NavigationItem Prev { get; set; }
        public NavigationItem Next { get; set; }
    }

    public class NavigationItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
