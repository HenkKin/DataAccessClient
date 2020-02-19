namespace DataAccessClient
{
    public interface IMultiTenancyConfiguration<TTenantIdentifierType> where TTenantIdentifierType : struct
    {
        bool IsEnabled { get; }
        TTenantIdentifierType? CurrentTenantId { get; }

        RestoreAction Enable();
        RestoreAction Disable();
    }
}