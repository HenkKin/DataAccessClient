using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    internal static class RelationalDbContextInitializer
    {
        internal static void Initialize(RelationalDbContext dbContext, IServiceProvider scopedServiceProvider)
        {
            var context = new Dictionary<string, dynamic>();

            foreach (var entityBehaviorConfiguration in dbContext.DataAccessClientOptionsExtension.EntityBehaviors)
            {
                foreach (var entityBehaviorContext in entityBehaviorConfiguration.OnExecutionContextCreating(scopedServiceProvider))
                {
                    context.TryAdd(entityBehaviorContext.Key, entityBehaviorContext.Value);
                }
            }

            dbContext.Initialize(new RelationalDbContextExecutionContext(context));
        }
    }
}
