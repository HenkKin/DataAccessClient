﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure
{
    public class DataAccessClientOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public Type UserIdentifierType { get; private set; }
        public Type TenantIdentifierType { get; private set; }

        public DataAccessClientOptionsExtension WithUserIdentifierType(Type userIdentifierType)
        {
            var clone = Clone();
            clone.UserIdentifierType = userIdentifierType;
            return clone;
        }
        public DataAccessClientOptionsExtension WithTenantIdentifierType(Type tenantIdentifierType)
        {
            var clone = Clone();
            clone.TenantIdentifierType = tenantIdentifierType;
            return clone;
        }

        public DataAccessClientOptionsExtension()
        {
        }

        protected DataAccessClientOptionsExtension([NotNull] DataAccessClientOptionsExtension copyFrom)
        {
            UserIdentifierType = copyFrom.UserIdentifierType;
            TenantIdentifierType = copyFrom.TenantIdentifierType;
        }

        public virtual DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected virtual DataAccessClientOptionsExtension Clone() => new DataAccessClientOptionsExtension(this);

        public virtual void ApplyServices(IServiceCollection services)
        {
        }

        public virtual void Validate(IDbContextOptions options)
        {
            if (UserIdentifierType == null)
            {
                throw new InvalidOperationException($"{nameof(UserIdentifierType)} is not set");
            }

            if (TenantIdentifierType == null)
            {
                throw new InvalidOperationException($"{nameof(TenantIdentifierType)} is not set");
            }
        }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long? _serviceProviderHash;
            private string _logFragment;

            public ExtensionInfo(DataAccessClientOptionsExtension extension)
                : base(extension)
            {
            }

            private new DataAccessClientOptionsExtension Extension
                => (DataAccessClientOptionsExtension) base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        var builder = new StringBuilder();

                        if (Extension.UserIdentifierType != null)
                        {
                            builder.Append($"{nameof(Extension.UserIdentifierType)}={Extension.UserIdentifierType.FullName}; ");
                        }

                        if (Extension.TenantIdentifierType != null)
                        {
                            builder.Append($"{nameof(Extension.TenantIdentifierType)}={Extension.TenantIdentifierType.FullName}; ");
                        }

                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["DataAccessClient:" + nameof(UserIdentifierType)] =
                    (Extension.UserIdentifierType?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
                debugInfo["DataAccessClient:" + nameof(TenantIdentifierType)] =
                    Extension.TenantIdentifierType.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }

            public override long GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = Extension.UserIdentifierType?.GetHashCode() ?? 0L;
                    hashCode = (hashCode * 3) ^ Extension.TenantIdentifierType.GetHashCode();

                    _serviceProviderHash = hashCode;
                }

                return _serviceProviderHash.Value;
            }
        }
    }
}