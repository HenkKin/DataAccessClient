//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore.Infrastructure;

//namespace DataAccessClient.EntityFrameworkCore.SqlServer
//{
//    public interface ISqlServerDbContextData<TUserIdentifierType, TTenantIdentifierType> : IResettableService
//        where TUserIdentifierType : struct 
//        where TTenantIdentifierType : struct
//    {
//        bool IsSoftDeletableEnabled { get; set; }
//        bool IsSoftDeletableQueryFilterEnabled { get; set; }
//        bool IsTenantScopableQueryFilterEnabled { get; set; }
//        TUserIdentifierType? UserIdentifier { get; set; }
//        TTenantIdentifierType? TenantIdentifier { get; set; }
//    }

//    public class SqlServerDbContextData<TUserIdentifierType, TTenantIdentifierType> : ISqlServerDbContextData<TUserIdentifierType, TTenantIdentifierType> where TUserIdentifierType : struct
//        where TTenantIdentifierType : struct
//    {
//        public bool IsSoftDeletableEnabled { get; set; }
//        public bool IsSoftDeletableQueryFilterEnabled { get; set; }
//        public bool IsTenantScopableQueryFilterEnabled { get; set; }
//        public TUserIdentifierType? UserIdentifier { get; set; }
//        public TTenantIdentifierType? TenantIdentifier { get; set; }

//        #region DbContextPooling

//        /////// <summary>
//        ///////     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
//        ///////     the same compatibility standards as public APIs. It may be changed or removed without notice in
//        ///////     any release. You should only use it directly in your code with extreme caution and knowing that
//        ///////     doing so can result in application failures when updating to a new Entity Framework Core release.
//        /////// </summary>
//        ////void IDbContextPoolable.Resurrect(DbContextPoolConfigurationSnapshot configurationSnapshot)
//        ////{
//        ////    ((IDbContextPoolable)this).Resurrect(configurationSnapshot);
//        ////}

//        /// <summary>
//        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
//        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
//        ///     any release. You should only use it directly in your code with extreme caution and knowing that
//        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
//        /// </summary>
//        void IResettableService.ResetState()
//        {
//            IsSoftDeletableEnabled = true;
//            IsSoftDeletableQueryFilterEnabled = true;
//            IsTenantScopableQueryFilterEnabled = true;
//            UserIdentifier = null;
//            TenantIdentifier = null;
//        }

//        /// <summary>
//        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
//        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
//        ///     any release. You should only use it directly in your code with extreme caution and knowing that
//        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
//        /// </summary>
//        /// <param name="cancellationToken"> A <see cref="CancellationToken" /> to observe while waiting for the task to complete. </param>
//        async Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
//        {
//            ((IResettableService)this).ResetState();
//            await Task.CompletedTask;
//        }

//        #endregion
//    }
//}