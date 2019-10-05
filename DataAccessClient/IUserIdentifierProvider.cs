using System.Threading.Tasks;

namespace DataAccessClient
{
    public interface IUserIdentifierProvider<TIdentifierType> where TIdentifierType : struct
    {
        Task<TIdentifierType> ExecuteAsync();
    }
}
