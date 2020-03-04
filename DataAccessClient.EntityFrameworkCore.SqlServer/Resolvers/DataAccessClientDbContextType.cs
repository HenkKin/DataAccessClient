using System;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    public class DataAccessClientDbContextType<TDbContext> : IDataAccessClientDbContextType
    {
        public Type Execute()
        {
            return typeof(TDbContext);
        }
    }
}