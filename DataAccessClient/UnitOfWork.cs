using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessClient
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly IEnumerable<IUnitOfWorkPart> _unitofWorkParts;

        public UnitOfWork(IEnumerable<IUnitOfWorkPart> unitofWorkParts)
        {
            _unitofWorkParts = unitofWorkParts;
        }
        public async Task SaveAsync()
        {
            // TODO: add transacion management
            // https://docs.microsoft.com/en-gb/azure/architecture/patterns/compensating-transaction
            // https://docs.microsoft.com/en-us/ef/core/saving/transactions
            var saveTasks = _unitofWorkParts.Select(part => part.SaveAsync()).ToList();

            await Task.WhenAll(saveTasks);
        }

        public void Reset()
        {
            // TODO: add transacion management
            _unitofWorkParts.ToList().ForEach(part => part.Reset());
        }
    }
}
