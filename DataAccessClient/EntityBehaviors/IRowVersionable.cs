namespace DataAccessClient.EntityBehaviors
{
    public interface IRowVersionable
    {
        byte[] RowVersion { get; set; }
    }
}