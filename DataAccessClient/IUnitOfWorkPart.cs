using System.Threading.Tasks;

namespace DataAccessClient
{
    internal interface IUnitOfWorkPart
    {
        Task SaveAsync();
        void Reset();
    }
}