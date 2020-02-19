namespace DataAccessClient
{
    public interface ISoftDeletableConfiguration
    {
        bool IsEnabled { get; }
        bool IsQueryFilterEnabled { get; }

        RestoreAction Enable();
        RestoreAction Disable();
        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}