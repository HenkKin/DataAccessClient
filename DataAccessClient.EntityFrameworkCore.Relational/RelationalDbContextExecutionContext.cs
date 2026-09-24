using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    public class RelationalDbContextExecutionContext
    {
        private readonly Dictionary<string, dynamic> _context;

        internal RelationalDbContextExecutionContext(Dictionary<string, dynamic> context)
        {
            _context = context;
        }

        public T Get<T>()
        {
            return Get<T>(typeof(T).Name);
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
            if (!_context.TryGetValue(name, out var value))
            {
                throw new InvalidOperationException(
                    $"'{name}' is not available in the execution context, because it is not registered in DependencyInjection with Scoped or Singleton lifetime.");
            }

            return (T)value;
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