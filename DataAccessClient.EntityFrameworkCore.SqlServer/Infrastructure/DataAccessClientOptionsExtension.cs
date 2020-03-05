using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure
{
    public class DataAccessClientOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public IList<IEntityBehaviorConfiguration> EntityBehaviors { get; } = new List<IEntityBehaviorConfiguration>();

        public DataAccessClientOptionsExtension WithEntityBehaviors(IList<IEntityBehaviorConfiguration> entityBehaviors)
        {
            var clone = Clone();
            foreach (var entityBehavior in entityBehaviors)
            {
                clone.EntityBehaviors.Add(entityBehavior);
            }

            return clone;
        }

        public DataAccessClientOptionsExtension()
        {
        }

        protected DataAccessClientOptionsExtension([NotNull] DataAccessClientOptionsExtension copyFrom)
        {
            foreach (var copyFromEntityBehavior in copyFrom.EntityBehaviors)
            {
                EntityBehaviors.Add(copyFromEntityBehavior);
            }
        }

        public virtual DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected virtual DataAccessClientOptionsExtension Clone() => new DataAccessClientOptionsExtension(this);

        public virtual void ApplyServices(IServiceCollection services)
        {
        }

        public virtual void Validate(IDbContextOptions options)
        {
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

                        builder.Append(
                            $"{nameof(Extension.EntityBehaviors)}[{Extension.EntityBehaviors.Count}]=[{string.Join(", ", Extension.EntityBehaviors.Select(eb => eb.GetType().Name))}]; ");

                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                var hashCode = Extension.EntityBehaviors?.GetHashCode() ?? 0L;
                if (Extension.EntityBehaviors != null)
                {
                    foreach (var entityBehaviorConfiguration in Extension.EntityBehaviors)
                    {
                        hashCode = (hashCode * 3) ^ entityBehaviorConfiguration.GetHashCode();
                    }
                }

                debugInfo["DataAccessClient:" + nameof(EntityBehaviors)] =
                    hashCode.ToString(CultureInfo.InvariantCulture);
            }

            public override long GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = Extension.EntityBehaviors?.GetHashCode() ?? 0L;
                    if (Extension.EntityBehaviors != null)
                    {
                        foreach (var entityBehaviorConfiguration in Extension.EntityBehaviors)
                        {
                            hashCode = (hashCode * 3) ^ entityBehaviorConfiguration.GetHashCode();
                        }
                    }

                    _serviceProviderHash = hashCode;
                }

                return _serviceProviderHash.Value;
            }
        }
    }
}