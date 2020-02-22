namespace DataAccessClient.Providers
{
    public interface IUserIdentifierProvider<TUserIdentifierType>
        where TUserIdentifierType : struct
    {
        TUserIdentifierType? Execute();
    }
}
