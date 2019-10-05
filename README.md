DataAccessClient
=========================================
[![Build Status](https://ci.appveyor.com/api/projects/status/github/HenkKin/DataAccessClient?branch=master&svg=true)](https://ci.appveyor.com/project/HenkKin/DataAccessClient) 
[![NuGet](https://img.shields.io/nuget/dt/DataAccessClient.svg)](https://www.nuget.org/packages/DataAccessClient) 
[![NuGet](https://img.shields.io/nuget/vpre/DataAccessClient.svg)](https://www.nuget.org/packages/DataAccessClient)
[![BCH compliance](https://bettercodehub.com/edge/badge/HenkKin/DataAccessClient?branch=master)](https://bettercodehub.com/)

### Summary

Provides interfaces for Data Access with IRepository<T>, IUnitOfWork and IQueryableSearcher<T>. Also provides haviorial interfaces for entities like IIdentifiable, ICreatable, IModifiable, ISoftDeletable and IRowVersioned. Last but not least provides some types for Exceptions and searching capabilities like Filtering, Paging, Sorting and Includes.

This library is Cross-platform, supporting `netstandard2.1`.


### Installing DataAccessClient

You should install [DataAccessClient with NuGet](https://www.nuget.org/packages/DataAccessClient):

    Install-Package DataAccessClient

Or via the .NET Core command line interface:

    dotnet add package DataAccessClient

Either commands, from Package Manager Console or .NET Core CLI, will download and install DataAccessClient and all required dependencies.

### Dependencies

No external dependencies

### Usage

If you're using EntityFrameworkCore and you want to use this Identifier type in your entities, then you can use [DataAccessClient](https://github.com/HenkKin/DataAccessClient/) package which includes a `DbContextOptionsBuilder.UseIdentifiers<[InternalClrType:short|int|long|Guid]>()` extension method, allowing you to register all needed IValueConverterSelectors and IMigrationsAnnotationProviders. 
It also includes a `PropertyBuilder<Identifier>.IdentifierValueGeneratedOnAdd()` extension method, allowing you to register all needed configuration to use `SqlServerValueGenerationStrategy.IdentityColumn`. 

Entity behaviors
================
The [DataAccessClient](https://github.com/HenkKin/DataAccessClient/) package provides you a set of EntityBehavior interfaces. These interfaces you can use to decorate your entites.

The implementation packages, like [DataAccessClient.EntityFrameworkCore.SqlServer](https://github.com/HenkKin/DataAccessClient.EntityFrameworkCore.SqlServer/) package, use these interface to apply the behavior automaticalle.

```csharp
...
using DataAccessClient;

 public class ExampleEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersioned
{
	// to identify an entity
    public int Id { get; set; }

	// to track creation
    public DateTime CreatedOn { get; set; }
    public int CreatedById { get; set; }

	// to track modification
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedById { get; set; }

	//  to implement Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public int? DeletedById { get; set; }

	//  to implement optimistic concurrency control.
    public byte[] RowVersion { get; set; } 

	// your own fields
    public string Name { get; set; }
}

```

Alle entity behaviours are optional. No one is required.

The generic parameter int defines the IdentifierType of your identifier fiels (primary and foreign keys).


IUnitOfWork and IRepository<T>
=========================
To use Repository and UnitOfWork, see example below.

```csharp
...
using DataAccessClient;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<ExampleEntity> _exampleEntityRepository;
    private readonly IRepository<ExampleSecondEntity> _exampleSecondEntityRepository;
    private readonly IQueryableSearcher<ExampleEntity> _exampleEntityQueryableSearcher;
    private readonly IQueryableSearcher<ExampleSecondEntity> _exampleSecondEntityQueryableSearcher;

    public HomeController(
		IUnitOfWork unitOfWork, 
		IRepository<ExampleEntity> exampleEntityRepository, 
		IRepository<ExampleSecondEntity> exampleSecondEntityRepository, 
		IQueryableSearcher<ExampleEntity> exampleEntityQueryableSearcher, 
		IQueryableSearcher<ExampleSecondEntity> exampleSecondEntityQueryableSearcher)
    {
        _unitOfWork = unitOfWork;
        _exampleEntityRepository = exampleEntityRepository;
        _exampleSecondEntityRepository = exampleSecondEntityRepository;
        _exampleEntityQueryableSearcher = exampleEntityQueryableSearcher;
        _exampleSecondEntityQueryableSearcher = exampleSecondEntityQueryableSearcher;
    }

    [HttpGet]
    public async Task<IActionResult> Test()
    {
        var exampleEntity1 = new ExampleEntity
        {
            Name = "DataAccessClient1"
        };

        var exampleEntity2 = new ExampleEntity
        {
            Name = "DataAccessClient2"
        };

        _exampleEntityRepository.Add(exampleEntity1);
        _exampleEntityRepository.Add(exampleEntity2);

        var exampleSecondEntity1 = new ExampleSecondEntity
        {
            Name = "SecondDataAccessClient1"
        };

        var exampleSecondEntity2 = new ExampleSecondEntity
        {
            Name = "SecondDataAccessClient2"
        };

        _exampleSecondEntityRepository.Add(exampleSecondEntity1);
        _exampleSecondEntityRepository.Add(exampleSecondEntity2);

        await _unitOfWork.SaveAsync();

        exampleEntity2.Name = "Updated DataAccessClient2";
        exampleSecondEntity2.Name = "Updated SecondDataAccessClient2";

        await _unitOfWork.SaveAsync();

        var exampleEntities = await _exampleEntityRepository.GetChangeTrackingQuery()
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        var exampleSecondEntities = await _exampleSecondEntityRepository.GetChangeTrackingQuery()
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        _exampleEntityRepository.RemoveRange(exampleEntities);
        _exampleSecondEntityRepository.RemoveRange(exampleSecondEntities);

        await _unitOfWork.SaveAsync();

        var criteria = new Criteria();
        criteria.OrderBy = "Id";
        criteria.OrderByDirection = OrderByDirection.Ascending;
        criteria.Page = 1;
        criteria.PageSize = 10;
        criteria.Search = "Data Access Client";

        var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
        var exampleSecondEntitiesSearchResults = await _exampleSecondEntityQueryableSearcher.ExecuteAsync(_exampleSecondEntityRepository.GetReadOnlyQuery(), criteria);

        return Json(new{ exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
    }
}

```

Exceptions
==========

The package provides you two types of exceptions

1) DuplicateKeyException
This exception is throw when an implementation package detects an duplicate key.

2) RowVersioningException
This exception is thrown when an entity is changed during your change.

Searching
=========
To support easy searching with filtering, includes, ordering and paging, an IQueryableSearcher<ExampleEntity> interface is provided. It requires an IQueryable<ExampleEntity parameter and a Criteria parameter.

```csharp
...
using DataAccessClient;

public class YourService : IYourService
{
	private readonly IRepository<YourEntity> _repository;
	private readonly IQueryableSearcher<YourEntity> _queryableSearcher;

	public YourService(IRepository<YourEntity> repository, IQueryableSearcher<YourEntity> queryableSearcher)
	{
		_repository = repository;
		_queryableSearcher = queryableSearcher;
	}

	public async Task<CriteriaResult<YourEntity>> SearchAsync(Criteria criteria)
	{
		var queryable = _repository.GetReadOnlyQuery();
		... 
		// do some extra filtering on queryable
		// queryable = queryable.Where(x => x.Name  = "DataAccessClient");
		...
		return await _queryableSearcher.ExecuteAsync(queryable, criteria);
	}
}

public class Client
{
	public void Main(IYourService yourService)
	{
		var criteria = new Criteria();
        criteria.OrderBy = "Id";
        criteria.OrderByDirection = OrderByDirection.Ascending;
        criteria.Page = 1;
        criteria.PageSize = 10;
        criteria.Search = "Data Access Client";

		var criteriaResult = yourService.SearchAsync(criteria);

		// criteriaResult.Records // of type YourEntity
		// criteriaResult.TotalRecordCount // integer
	}
}
```


=========================================



DataAccessClient.EntityFrameworkCore.SqlServer
=========================================
[![NuGet](https://img.shields.io/nuget/dt/DataAccessClient.EntityFrameworkCore.SqlServer.svg)](https://www.nuget.org/packages/DataAccessClient.EntityFrameworkCore.SqlServer) 
[![NuGet](https://img.shields.io/nuget/vpre/DataAccessClient.EntityFrameworkCore.SqlServer.svg)](https://www.nuget.org/packages/DataAccessClient.EntityFrameworkCore.SqlServer)

### Summary

The DataAccessClient.EntityFrameworkCore.SqlServer library is an Microsoft.EntityFrameworkCore.SqlServer implementation for [DataAccessClient](https://github.com/HenkKin/DataAccessClient/).

This library is Cross-platform, supporting `netstandard2.1`.


### Installing DataAccessClient.EntityFrameworkCore.SqlServer

You should install [DataAccessClient.EntityFrameworkCore.SqlServer with NuGet](https://www.nuget.org/packages/DataAccessClient.EntityFrameworkCore.SqlServer):

    Install-Package DataAccessClient.EntityFrameworkCore.SqlServer

Or via the .NET Core command line interface:

    dotnet add package DataAccessClient.EntityFrameworkCore.SqlServer

Either commands, from Package Manager Console or .NET Core CLI, will download and install DataAccessClient.EntityFrameworkCore.SqlServer and all required dependencies.

### Dependencies

- [DataAccessClient](https://www.nuget.org/packages/DataAccessClient/)
- [Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/)
- [LinqKit.Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/LinqKit.Microsoft.EntityFrameworkCore/)

### Usage

IIf you're using EntityFrameworkCore.SqlServer and you want to use this Identifier type in your entities, then you can use [DataAccessClient.EntityFrameworkCore.SqlServer](https://github.com/HenkKin/DataAccessClient.EntityFrameworkCore.SqlServer/) package which includes the following registration options via extensions method on the DataAccessSqlServerServiceCollectionExtensions class:
- `IServiceCollection AddDataAccessClient<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)`
- `IServiceCollection AddDataAccessClient<TDbContext, TIdentifierType, TUserIdentifierProvider>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)` 
- `IServiceCollection AddDataAccessClientPool<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)`
- `IServiceCollection AddDataAccessClientPool<TDbContext, TIdentifierType, TUserIdentifierProvider>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)`

These extension methods supporting you to register all needed DbContexts, IUnitOfWorks and IRepositories for provided entity types. 

To use it:

```csharp
...
using DataAccessClient.EntityFrameworkCore.SqlServer;

public class Startup
{
    ...
    
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
		var entityTypes = new [] { typeof(Entity1), typeof(Entity2) }; // can also done by using reflection
        ...

		// regist IUserIdentifierProvider standalone, usefull in n-layer architectures
		services.AddSingleton<IUserIdentifierProvider<int>, YourUserIdentifierProviderType>();
		// register as AddDbContext (without pooling)
        services.AddDataAccessClient<YourDbContext, int>(
				builder => ... , // f.e. builder.UseSqlServer(...)
				entityTypes
            );
                
        // or
        // register IUserIdentifierProvider standalone, usefull in n-layer architectures
		// register as AddDbContextPool (with pooling)
	    services.AddSingleton<IUserIdentifierProvider<int>, YourUserIdentifierProviderType>();
        services.AddDataAccessClientPool<YourDbContext, int>(
				builder => ... , // f.e. builder.UseSqlServer(...)
				entityTypes
            );
                
        // or
                
		// register IUserIdentifierProvider within extension method
		// register as AddDbContext (without pooling)
        services.AddDataAccessClient<YourDbContext, int, YourUserIdentifierProvider>(
				builder => ... , // f.e. builder.UseSqlServer(...)
				entityTypes
            );
                
        // or
                
		// register IUserIdentifierProvider within extension method
		// register as AddDbContext (with pooling)
        services.AddDataAccessClientPool<YourDbContext, int, YourUserIdentifierProvider>(
				builder => ... , // f.e. builder.UseSqlServer(...)
				entityTypes
            );
		...
    }
    
    ...
```

Using the base class `SqlServerDbContext<TIdentifierType>` on your own DbContext implementation:

```csharp
...
using DataAccessClient.EntityFrameworkCore.SqlServer;

internal class YourDbContext : SqlServerDbContext<int>
{
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
    {
    }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
	    // Register your entities to the DbContext using EntityTypeBuilder
		modelBuilder.Entity<ExampleEntity>()
            .ToTable("ExampleEntities");

	    // Register your entities to the DbContext using EntityEntityTypeConfiguration class
		modelBuilder.ApplyConfiguration(new ExampleEntityEntityTypeConfiguration());

		base.OnModelCreating(modelBuilder);
	}
}
    ...
```

Providing an implementation for interface `IUserIdentifierProvider<TIdentifierType>`. This provider should be a singleton, so the implementation should try to return an user identifier of the current context

```csharp
...
using DataAccessClient.EntityFrameworkCore.SqlServer;

public class YourUserIdentifierProvider : IUserIdentifierProvider<int>
{
    public async Task<int> ExecuteAsync()
    {
		// f.e. in Asp.NET Core it could use IHttpContextAccessor.HttpContext.User.Identity to get user identifier via claims or your own implementation;

		// return the current user id
        return await Task.FromResult(10);
    }
}
...
```

### Supporting migrations using `dotnet ef` tooling

First navigate to your migrations project folder
`cd [path-to-your-project-folder]`

Install `dotnet ef` tooling (only needed first time)
'dotnet tool install --global dotnet-ef --version 3.0.0 --add-source https://api.nuget.org/v3/index.json --ignore-failed-sources'

Adding migrations for specific DbContext

`dotnet ef migrations add [migrationname] --context YourDbContext --output-dir Migrations/YourDatabase`

Removing latest migration for specific DbContext

`dotnet ef migrations remove --context YourDbContext`

Updating database to latest migration

`dotnet ef database update --context YourDbContext`

Updating database to target migration (up or down)

`dotnet ef database update [migrationname] --context YourDbContext`

Note: when only one DbContext exists in your project, you can skip te --context and the --output-dir (default folder will be: Migrations)