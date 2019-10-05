using System.Threading.Tasks;

namespace DataAccessClient
{
    public interface IUnitOfWork
    {
        Task SaveAsync();
        void Reset();
    }
}