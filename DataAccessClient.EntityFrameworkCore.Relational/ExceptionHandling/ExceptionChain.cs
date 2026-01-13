using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    /// <summary>
    /// Kleine helper om door de InnerException-keten te lopen.
    /// </summary>
    public static class ExceptionChain
    {
        public static IEnumerable<Exception> Traverse(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
                yield return e;
        }
    }
}
