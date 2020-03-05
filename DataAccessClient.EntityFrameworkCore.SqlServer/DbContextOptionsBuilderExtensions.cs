using System;
using System.Collections.Generic;
using DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder WithOption(this DbContextOptionsBuilder builder, Func<DataAccessClientOptionsExtension, DataAccessClientOptionsExtension> withFunc)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
                withFunc(builder.Options.FindExtension<DataAccessClientOptionsExtension>() ?? new DataAccessClientOptionsExtension()));

            return builder;
        }

        internal static DbContextOptionsBuilder WithUserIdentifierType(this DbContextOptionsBuilder builder, Type userIdentifierType)
            => builder.WithOption(e => e.WithUserIdentifierType(userIdentifierType));

        internal static DbContextOptionsBuilder WithTenantIdentifierType(this DbContextOptionsBuilder builder,
            Type tenantIdentifierType)
            => builder.WithOption(e => e.WithTenantIdentifierType(tenantIdentifierType));
        internal static DbContextOptionsBuilder WithLocaleIdentifierType(this DbContextOptionsBuilder builder,
            Type localeIdentifierType)
            => builder.WithOption(e => e.WithLocaleIdentifierType(localeIdentifierType));
        internal static DbContextOptionsBuilder WithCustomEntityBehaviorTypes(this DbContextOptionsBuilder builder,
            IList<Type> customEntityBehaviorTypes)
            => builder.WithOption(e => e.WithCustomEntityBehaviorTypes(customEntityBehaviorTypes));
    }
}