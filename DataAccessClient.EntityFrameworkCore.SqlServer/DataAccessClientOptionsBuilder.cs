using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class DataAccessClientOptionsBuilder
    {
        private readonly DataAccessClientOptions _options;

        public DataAccessClientOptionsBuilder()
            : this(new DataAccessClientOptions())
        {
        }

        public DataAccessClientOptionsBuilder(DataAccessClientOptions options)
        {
            _options = options;
        }

        public virtual DataAccessClientOptions Options => _options;

        public virtual DataAccessClientOptionsBuilder ConfigureDbContextOptions(Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
        {
            _options.DbContextOptionsBuilder = dbContextOptionsBuilder;
            return this;
        }

        public virtual DataAccessClientOptionsBuilder ConfigureEntityTypes(Type[] entityTypes)
        {
            _options.EntityTypes = entityTypes;
            return this;
        }

        public virtual DataAccessClientOptionsBuilder UsePooling(bool usePooling, int? poolSize = null)
        {
            _options.UsePooling = usePooling;
            _options.PoolSize = poolSize;
            return this;
        }
    }
}
