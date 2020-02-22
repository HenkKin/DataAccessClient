using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class SqlServerRepository<TDbContext, TEntity, TUserIdentifierType, TTenantIdentifierType> : IRepository<TEntity> 
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TEntity : class
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        protected readonly TDbContext DbContext;
        protected readonly DbSet<TEntity> DbSet;
        private readonly PropertyInfo _primaryKeyProperty;


        public SqlServerRepository(ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType> sqlServerDbContextResolver)
        {
            DbContext = sqlServerDbContextResolver.Execute() ?? throw new ArgumentNullException(nameof(sqlServerDbContextResolver));
        
            DbSet = DbContext.Set<TEntity>();
            _primaryKeyProperty = DbContext.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties.Single().PropertyInfo;
        }

        public IQueryable<TEntity> GetReadOnlyQuery()
        {
            return DbSet.AsNoTracking();
        }

        public IQueryable<TEntity> GetChangeTrackingQuery()
        {
            return DbSet.AsTracking();
        }

        public void Add(TEntity newEntity)
        {
            DbSet.Add(newEntity);
        }

        public void AddRange(IEnumerable<TEntity> newEntities)
        {
            var enumerable = newEntities as TEntity[] ?? newEntities.ToArray();
            DbSet.AddRange(enumerable);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            var entitiesList = entities.ToList();

            DbSet.RemoveRange(entitiesList);
        }

        public async Task<TEntity> FindByIdAsync(object id)
        {
            if (id?.GetType() != _primaryKeyProperty.PropertyType)
            {
                throw new NotSupportedException($"FindByIdAsync only can handle id of type '{_primaryKeyProperty.PropertyType.FullName}', passed id of type '{id?.GetType().FullName}'");
            }
            return await DbSet.FindAsync(id);
        }

        public TEntity StartChangeTrackingById(object id)
        {
            if (id?.GetType() != _primaryKeyProperty.PropertyType)
            {
                throw new NotSupportedException($"StartChangeTrackingById only can handle id of type '{_primaryKeyProperty.PropertyType.FullName}', passed id of type '{id?.GetType().FullName}'");
            }
            var entity = Activator.CreateInstance<TEntity>();
            _primaryKeyProperty.SetValue(entity, id);
            DbSet.Attach(entity);
            return entity;
        }
    }
}