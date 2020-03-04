using System;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    public interface IDataAccessClientDbContextType
    {
        Type Execute();
    }
}