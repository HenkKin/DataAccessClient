using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class SqlServerDbContextExecutionContext
    {
        private readonly Dictionary<string, dynamic> _context;

        public SqlServerDbContextExecutionContext(Dictionary<string, dynamic> context)
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