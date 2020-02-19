namespace DataAccessClient
{
    public interface IMultiTenancyConfiguration<TTenantIdentifierType> where TTenantIdentifierType : struct
    {
        bool IsQueryFilterEnabled { get; }
        TTenantIdentifierType? CurrentTenantId { get; }

        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}