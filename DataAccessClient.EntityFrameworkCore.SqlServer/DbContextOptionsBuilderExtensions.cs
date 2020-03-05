using System;
using System.Collections.Generic;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder WithOption(this DbContextOptionsBuilder builder,
            Func<DataAccessClientOptionsExtension, DataAccessClientOptionsExtension> withFunc)
        {
            ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(
                withFunc(builder.Options.FindExtension<DataAccessClientOptionsExtension>() ??
                         new DataAccessClientOptionsExtension()));

            return builder;
        }

        internal static DbContextOptionsBuilder WithEntityBehaviors(this DbContextOptionsBuilder builder,
            IList<IEntityBehaviorConfiguration> customEntityBehaviors)
            => builder.WithOption(e => e.WithEntityBehaviors(customEntityBehaviors));
    }
}