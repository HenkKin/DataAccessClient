using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessClient
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWorkPart[] _unitofWorkParts;

        public UnitOfWork(IEnumerable<IUnitOfWorkPart> unitofWorkParts)
        {
            _unitofWorkParts = unitofWorkParts.ToArray();
        }
        public async Task SaveAsync()
        {
            if (_unitofWorkParts.Length > 1)
            {
                // System.PlatformNotSupportedException: 'This platform does not support distributed transactions.'
                // using var transactionScope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

                var saveTasks = _unitofWorkParts.Select(part => part.SaveAsync()).ToList();
                await Task.WhenAll(saveTasks);

                // transactionScope.Complete();
            }
            else
            {
                var saveTasks = _unitofWorkParts.Select(part => part.SaveAsync()).ToList();
                await Task.WhenAll(saveTasks);
            }
        }

        public void Reset()
        {
            _unitofWorkParts.ToList().ForEach(part => part.Reset());
        }
    }
}
