using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;
using EntityCloner.Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    internal class RelationalRepository<TEntity> : IRepository<TEntity> 
        where TEntity : class
    {
        protected readonly DbSet<TEntity> DbSet;
        protected readonly RelationalDbContext DbContext;
        private readonly List<PropertyInfo> _primaryKeyProperties;
        
        public RelationalRepository(IRelationalDbContextForEntityResolver relationalDbContextForEntityResolver)
        {
            DbContext = relationalDbContextForEntityResolver.Execute<TEntity>();
            if (DbContext != null)
            {
                DbSet = DbContext.Set<TEntity>();
            }

            if (DbContext == null || DbSet == null)
            {
                throw new InvalidOperationException($"Can not find IRepository instance for type {typeof(TEntity).FullName}");
            }

            _primaryKeyProperties = DbContext.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties.Select(p=>p.PropertyInfo).ToList() ?? new List<PropertyInfo>();
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

        public async Task<TEntity> FindByIdAsync(params object[] id)
        {
            ThrowIfInvalidPrimaryKey(id, _primaryKeyProperties);

            return await DbSet.FindAsync(id);
        }

        public TEntity StartChangeTrackingById(params object[] id)
        {
            ThrowIfInvalidPrimaryKey(id, _primaryKeyProperties);

            var entity = Activator.CreateInstance<TEntity>();

            for (int i = 0; i < _primaryKeyProperties.Count; i++)
            {
                var primaryKeyProperty = _primaryKeyProperties[i];
                var idPart = id[i];
                primaryKeyProperty.SetValue(entity, idPart);
            }

            DbSet.Attach(entity);
            return entity;
        }

        public TEntity Update(TEntity entity)
        {
            return DbSet.Update(entity).Entity;
        }

        public async Task<TEntity> CloneAsync(params object[] id)
        {
            ThrowIfInvalidPrimaryKey(id, _primaryKeyProperties);

            var clonedEntity = await DbContext.CloneAsync<TEntity>(id);
            return clonedEntity;
        }

        public async Task<TEntity> CloneAsync(Func<IClonableQueryable<TEntity>, IClonableQueryable<TEntity>> includeQuery, params object[] id)
        {
            ThrowIfInvalidPrimaryKey(id, _primaryKeyProperties);

            var clonedEntity = await DbContext.CloneAsync(includeQuery, id);
            return clonedEntity;
        }

        private static void ThrowIfInvalidPrimaryKey(object[] id, List<PropertyInfo> primaryKeyProperties)
        {
            if (id.Length != primaryKeyProperties.Count)
            {
                throw new ArgumentException($"The primary key of entity {typeof(TEntity).Name} consist of {primaryKeyProperties.Count} properties. The value provided has {id.Length} values", nameof(id));
            }

            for (int i = 0; i < primaryKeyProperties.Count; i++)
            {
                var primaryKeyProperty = primaryKeyProperties[i];
                var idPart = id[i];

                if (idPart?.GetType() != primaryKeyProperty.PropertyType)
                {
                    throw new NotSupportedException(
                        $"The PrimaryKey part '{primaryKeyProperty.Name}' with type '{primaryKeyProperty.PropertyType.FullName}' is not same as provided value type '{idPart?.GetType().FullName}'");
                }
            }
        }
    }
}