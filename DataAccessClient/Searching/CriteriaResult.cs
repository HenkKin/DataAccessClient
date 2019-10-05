using System.Collections.Generic;

namespace DataAccessClient.Searching
{
    public class CriteriaResult<T>
    {
        public IEnumerable<T> Records { get; set; }
        public int TotalRecordCount { get; set; }
    }
}
