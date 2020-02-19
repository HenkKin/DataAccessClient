namespace DataAccessClient
{
    public interface ISoftDeletableConfiguration
    {
        bool IsEnabled { get; }

        RestoreAction Enable();
        RestoreAction Disable();
    }
}