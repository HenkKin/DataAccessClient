using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class SqlServerRepository<TDbContext, TEntity, TIdentifierType> : IRepository<TEntity> 
        where TDbContext : SqlServerDbContext<TIdentifierType>
        where TEntity : class 
        where TIdentifierType : struct
    {
        protected readonly TDbContext DbContext;
        protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

        public SqlServerRepository(TDbContext dbContext)
        {
            DbContext = dbContext;
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
            if (id?.GetType() != typeof(TIdentifierType))
            {
                throw new NotSupportedException($"FindByIdAsync only can handle id of type '{id?.GetType().FullName}', expected id of type '{typeof(TIdentifierType).FullName}'");
            }

            return await DbSet.FindAsync(id);
        }
    }
}