using System.Collections.Generic;

namespace DataAccessClient.Searching
{
    public class Criteria
    {
        public string Search { get; set; }
        public string OrderBy { get; set; }
        public OrderByDirection OrderByDirection { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public IList<string> Includes { get; } = new List<string>();
        public IDictionary<string, string> KeyFilters { get; } = new Dictionary<string, string>();
        public bool UsePaging()
        {
            return Page.HasValue && PageSize.HasValue;
        }
    }
}