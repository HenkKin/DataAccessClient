using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class SqlServerDbContextExecutionContext
    {
        private readonly Dictionary<string, dynamic> _context;

        internal SqlServerDbContextExecutionContext(Dictionary<string, dynamic> context)
        {
            _context = context;
        }

        public T Get<T>()
        {
            return (T)_context[typeof(T).Name];
        }

        public T TryGet<T>()
        {
            if (_context.TryGetValue(typeof(T).Name, out var value))
            {
                return (T)value;
            }
            return default;
        }
        public T Get<T>(string name)
        {
            return (T)_context[name];
        }

        public T TryGet<T>(string name)
        {
            if (_context.TryGetValue(name, out var value))
            {
                return (T)value;
            }
            return default;
        }
    }
}