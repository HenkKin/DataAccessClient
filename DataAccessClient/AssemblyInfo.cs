using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DataAccessClient.Tests")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.SqlServer")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.SqlServer.Tests")]

namespace DataAccessClient
{
    public class AssemblyInfo { }
}