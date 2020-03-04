using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class SqlServerRepository<TEntity> : IRepository<TEntity> 
        where TEntity : class
    {
        protected readonly SqlServerDbContext DbContext;
        protected readonly DbSet<TEntity> DbSet;
        private readonly PropertyInfo _primaryKeyProperty;
        // private readonly object _lock = new object();


        public SqlServerRepository(IServiceProvider scopedServiceProvider, IEnumerable<IDataAccessClientDbContextType> dataAccessClientDbContextTypes)
        {
            foreach (var dataAccessClientDbContextType in dataAccessClientDbContextTypes)
            {
                var dbContextType = dataAccessClientDbContextType.Execute();
                var dbContextResolverType = typeof(ISqlServerDbContextResolver<>).MakeGenericType(dbContextType);
                var executeMethod = dbContextResolverType.GetMethod(nameof(ISqlServerDbContextResolver<SqlServerDbContext>.Execute));
                DbContext = executeMethod.Invoke(scopedServiceProvider.GetService(dbContextResolverType), new object[0]) as SqlServerDbContext ??
                            throw new ArgumentNullException(nameof(SqlServerDbContext));
                if (DbContext != null && DbContext.Model.FindEntityType(typeof(TEntity)) != null)
                {
                    DbSet = DbContext.Set<TEntity>();
                    if (DbSet != null)
                    {
                        break;
                    }
                }

            }

            _primaryKeyProperty = DbContext.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties?.SingleOrDefault()?.PropertyInfo;
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