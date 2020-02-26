using System;
using System.Diagnostics.CodeAnalysis;
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

        public DataAccessClientOptionsBuilder([NotNull] DataAccessClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        public virtual DataAccessClientOptions Options => _options;


        public DataAccessClientOptionsBuilder WithDbContextOptions(Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
        {
            _options.DbContextOptionsBuilder = dbContextOptionsBuilder;
            return this;
        }

        public DataAccessClientOptionsBuilder WithUserIdentifierType<TUserIdentifierType>()
            where TUserIdentifierType : struct
        {
            _options.UserIdentifierType = typeof(TUserIdentifierType);
            return this;
        }

        public DataAccessClientOptionsBuilder WithUserIdentifierType(Type userIdentifierType)
        {
            _options.UserIdentifierType = userIdentifierType;
            return this;
        }

        public DataAccessClientOptionsBuilder WithTenantIdentifierType<TTenantIdentifierType>()
            where TTenantIdentifierType : struct
        {
            _options.TenantIdentifierType = typeof(TTenantIdentifierType);
            return this;
        }

        public DataAccessClientOptionsBuilder WithTenantIdentifierType(Type tenantIdentifierType)
        {
            _options.TenantIdentifierType = tenantIdentifierType;
            return this;
        }

        public DataAccessClientOptionsBuilder WithEntityTypes(Type[] entityTypes)
        {
            _options.EntityTypes = entityTypes;
            return this;
        }

        public DataAccessClientOptionsBuilder WithPooling(bool usePooling, int? poolSize = null)
        {
            _options.UsePooling = usePooling;
            _options.PoolSize = poolSize;
            return this;
        }
    }
}