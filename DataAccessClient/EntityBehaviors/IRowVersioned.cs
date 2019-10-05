namespace DataAccessClient.EntityBehaviors
{
    public interface IRowVersioned
    {
        byte[] RowVersion { get; set; }
    }
}