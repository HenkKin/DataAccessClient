using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DataAccessClient.Tests")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.Relational")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.Relational.Tests")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.SqlServer")]
[assembly: InternalsVisibleTo("DataAccessClient.EntityFrameworkCore.SqlServer.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DataAccessClient
{
    public class AssemblyInfo { }
}