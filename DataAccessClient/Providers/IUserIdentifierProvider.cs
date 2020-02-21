namespace DataAccessClient.Providers
{
    public interface IUserIdentifierProvider<TIdentifierType>
        where TIdentifierType : struct
    {
        TIdentifierType? Execute();
    }
}
