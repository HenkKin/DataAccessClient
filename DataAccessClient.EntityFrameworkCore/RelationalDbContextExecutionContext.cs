using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    internal class RelationalDbContextExecutionContext
    {
        private readonly Dictionary<string, dynamic> _context;

        public RelationalDbContextExecutionContext(Dictionary<string, dynamic> context)
        {
            _context = context;
        }

        public T Get<T>()
        {
            return (T)_context[typeof(T).Name];
        }

        public T Get<T>(string name)
        {
            return (T)_context[name];
        }
    }
}