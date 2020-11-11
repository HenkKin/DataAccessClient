Packages
=========================================
[![Build Status](https://ci.appveyor.com/api/projects/status/github/HenkKin/DataAccessClient?branch=master&svg=true)](https://ci.appveyor.com/project/HenkKin/DataAccessClient) 
[![BCH compliance](https://bettercodehub.com/edge/badge/HenkKin/DataAccessClient?branch=master)](https://bettercodehub.com/)

[DataAccessClient](https://github.com/HenkKin/DataAccessClient#dataaccessclient)

[DataAccessClient.EntityFrameworkCore.SqlServer](https://github.com/HenkKin/DataAccessClient#dataaccesscliententityframeworkcoresqlserver)



DataAccessClient
=========================================
[![NuGet](https://img.shields.io/nuget/dt/DataAccessClient.svg)](https://www.nuget.org/packages/DataAccessClient) 
[![NuGet](https://img.shields.io/nuget/vpre/DataAccessClient.svg)](https://www.nuget.org/packages/DataAccessClient)


### Summary

Provides interfaces for Data Access with IRepository<T>, IUnitOfWork and IQueryableSearcher<T>. Also provides haviorial interfaces for entities like IIdentifiable, ICreatable, IModifiable, ISoftDeletable, ITranslatable, IRowVersionable, ITenantScopable and ILocalizable. Last but not least provides some types for Exceptions and searching capabilities like Filtering, Paging, Sorting and Includes. The IRepostory contains some methods to support cloning based on EntityFrameworkCore configuration.

This library is Cross-platform, supporting `netstandard2.1`.


### Installing DataAccessClient

You should install [DataAccessClient with NuGet](https://www.nuget.org/packages/DataAccessClient):

    Install-Package DataAccessClient

Or via the .NET Core command line interface:

    dotnet add package DataAccessClient

Either commands, from Package Manager Console or .NET Core CLI, will download and install DataAccessClient and all required dependencies.

### Dependencies

No external dependencies

### Entity behaviors

The [DataAccessClient](https://github.com/HenkKin/DataAccessClient/) package provides you a set of EntityBehavior interfaces. These interfaces you can use to decorate your entites.

The implementation packages, like [DataAccessClient.EntityFrameworkCore.SqlServer](https://github.com/HenkKin/DataAccessClient.EntityFrameworkCore.SqlServer/) package, use these interface to apply the behavior automatically.

```csharp
...
using DataAccessClient;

public class ExampleEntity : 
	IIdentifiable<int>, 
	ICreatable<int>, 
	IModifiable<int>, 
	ISoftDeletable<int>, 
	IRowVersionable,
	ITranslatable<ExampleEntityTranslation, int, string>,
	ITenantScopable<int>
{
	// to identify an entity
	public int Id { get; set; }

	// to track creation
	public DateTime CreatedOn { get; set; }
	public int CreatedById { get; set; }

	// to track modification
	public DateTime? ModifiedOn { get; set; }
	public int? ModifiedById { get; set; }

	// to implement Soft Delete
	public bool IsDeleted { get; set; }
	public DateTime? DeletedOn { get; set; }
	public int? DeletedById { get; set; }

	// to implement optimistic concurrency control.
	public byte[] RowVersion { get; set; } 

	// to translate entity specific fields
	public ICollection<ExampleEntityTranslation> Translations { get; set; }

	// to scope multiple tenants in same database
	public int TenantId { get; set; }

	// your own fields
	public string Name { get; set; }
}

public class ExampleEntityTranslation : IEntityTranslation<ExampleEntity, int, string>
{
    public ExampleEntity TranslatedEntity { get; set; }
    public int TranslatedEntityId { get; set; }

	// language of translations, f.e. en-GB or nl-NL
    public string LocaleId { get; set; }

	// your custom translatable fields
	public string Description { get; set; }
	...
}

```

Alle entity behaviors are optional. No one is required.

All `struct` types are possible, also the `Identifier` type of package [Identifiers](https://www.nuget.org/packages/Identifiers). 
For LocaleId the type should by `IConvertible`, so `String` is allowed too.

### IUnitOfWork and IRepository<T>
	
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

		// start change tracking without querying database
		var exampleEntityAttach = _exampleEntityRepository.StartChangeTrackingById(10);
		// update properties to trigger changetracking
		exampleEntityAttach.Name =  "Updated DataAccessClient10";

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

### SoftDelete configuration

To implement SoftDelete into your application your softdeletable entities have to implement the `ISoftDeletable<TUserIdentifier>` interface. By default this is the only thing to do. If you want to control the SoftDelete behavior then you can inject `ISoftDeletableConfiguration` service into your logic classes.

The `ISoftDeletableConfiguration` allows you to 
 - Enable/Disable SoftDelete Behavior. Disabling SoftDelete behavior also disables the SoftDeleteQueryFilters.
 - EnableQueryFilter/DisableQueryFilter.

When using multipe DbContexts, there is only one `ISoftDeletableConfiguration` per ServiceScope. Disabling QueryFilter will disable QueryFilter for all your DbContexts.
 
```csharp
...
using DataAccessClient;

public class HomeController : Controller
{
	private readonly IRepository<ExampleEntity> _exampleEntityRepository;
	private readonly IQueryableSearcher<ExampleEntity> _exampleEntityQueryableSearcher;
	private readonly ISoftDeletableConfiguration _softDeletableConfiguration;
    
	public HomeController(
		IRepository<ExampleEntity> exampleEntityRepository, 
		IQueryableSearcher<ExampleEntity> exampleEntityQueryableSearcher, 
		ISoftDeletableConfiguration softDeletableConfiguration)
	{
		_exampleEntityRepository = exampleEntityRepository;
		_exampleEntityQueryableSearcher = exampleEntityQueryableSearcher;
		_softDeletableConfiguration = softDeletableConfiguration;
	}

	[HttpGet]
	public async Task<IActionResult> GetAllExampleEntities()
	{
		var criteria = new Criteria();
		criteria.OrderBy = "Id";
		criteria.OrderByDirection = OrderByDirection.Ascending;
		criteria.Page = 1;
		criteria.PageSize = 10;
		criteria.Search = "Data Access Client";

		// start a new scope with disabled query filter for SoftDelete
		using (_softDeletableConfiguration.DisableQueryFilter())
		{
			// all queries executed here, return soft deleted entities too.
			
			var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
			return Json(new{ exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
		}		

		// here the SoftDeleteFilter is reset to previous state.
	}
}

```


### Multitenancy configuration

To implement Multitenancy into your application your multitenant entities have to implement the `ITenantScopable<TTenantIdentifier>` interface. By default this is the only thing to do. If you want to control the Multitenancy behavior then you can inject `IMultiTenancyConfiguration` service into your logic classes.

The `IMultiTenancyConfiguration` allows you to 
 - EnableQueryFilter/DisableQueryFilter.

Because of required TenantId property on the MultiTenancy entities, the MultiTenancy cannot be disabled, only the QueryFiltering can be disabled.

When using multipe DbContexts, there is only one `IMultiTenancyConfiguration` per ServiceScope. Disabling QueryFilter will disable QueryFilter for all your DbContexts.

```csharp
...
using DataAccessClient;

public class HomeController : Controller
{
	private readonly IRepository<ExampleEntity> _exampleEntityRepository;
	private readonly IQueryableSearcher<ExampleEntity> _exampleEntityQueryableSearcher;
	private readonly IMultiTenancyConfiguration _multiTenancyConfiguration;
	
	public HomeController(
		IRepository<ExampleEntity> exampleEntityRepository, 
		IQueryableSearcher<ExampleEntity> exampleEntityQueryableSearcher, 
		IMultiTenancyConfiguration multiTenancyConfiguration)
	{
		_exampleEntityRepository = exampleEntityRepository;
		_exampleEntityQueryableSearcher = exampleEntityQueryableSearcher;
		_multiTenancyConfiguration = multiTenancyConfiguration;
	}

	[HttpGet]
	public async Task<IActionResult> GetAllExampleEntitiesOfAllTenants()
	{
		var criteria = new Criteria();
		criteria.OrderBy = "Id";
		criteria.OrderByDirection = OrderByDirection.Ascending;
		criteria.Page = 1;
		criteria.PageSize = 10;
		criteria.Search = "Data Access Client";

		// start a new scope with disabled query filter for MultiTenancy
		using (_multiTenancyConfiguration.DisableQueryFilter())
		{
			// all queries executed here, return entities of all tenants.
			
			var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
			return Json(new{ exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
		}		

		// here the MultiTenancy is reset to previous state.
	}
}

```


### Providers (Required)

When using this package, there are two required implementation you have to provide for the following interfaces:

 - IUserIdentifierProvider<TUserIdentifierType>
 - ITenantIdentifierProvider<TTenantIdentifierType>
 - ILocaleIdentifierProvider<TLocaleIdentifierType>

These three providers have to be registered with Scoped Lifetime in DependencyInjection. They are only required when it is needed for the EntityBehaviors you have implemented.

#### IUserIdentifierProvider<TUserIdentifierType>

Providing an implementation for interface `IUserIdentifierProvider<TUserIdentifierType>`. This provider should try to return an user identifier of the current context.

```csharp
...
using  DataAccessClient.Providers;

public class YourUserIdentifierProvider : IUserIdentifierProvider<int>
{
	public int? Execute()
	{
		// f.e. in Asp.NET Core it could use IHttpContextAccessor.HttpContext.User.Identity to get user identifier via claims or your own implementation;

		// return the current user id
		return 10;
	}
}
...
```

#### ITenantIdentifierProvider<TTenantIdentifierType>

Providing an implementation for interface `ITenantIdentifierProvider<TTenantIdentifierType>`. This provider should try to return a tenant identifier of the current context.

```csharp
...
using  DataAccessClient.Providers;

public class YourTenantIdentifierProvider : ITenantIdentifierProvider<int>
{
	public int? Execute()
	{
		// f.e. in Asp.NET Core it could use IHttpContextAccessor.HttpContext.User.Identity to get tenant identifier via claims or your own implementation;

		// return the current tenant id
		return 1;
	}
}
...
```

#### ILocaleIdentifierProvider<TLocaleIdentifierType>

Providing an implementation for interface `ILocaleIdentifierProvider<TLocaleIdentifierType>`. This provider should try to return a locale identifier of the current context.

```csharp
...
using  DataAccessClient.Providers;

public class YourLocaleIdentifierProvider : ILocaleIdentifierProvider<string>
{
	public string Execute()
	{
		// f.e. in Asp.NET Core it could use IHttpContextAccessor.HttpContext.User.Identity to get locale identifier via claims or your own implementation;

		// return the current locale id
		return "nl-NL";
	}
}
...
```

### Exceptions

The package provides you two types of exceptions

1) DuplicateKeyException

This exception is throw when an implementation package detects an duplicate key.

2) RowVersioningException

This exception is thrown when an entity is changed during your change.


### Searching

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
	public async Task Main(IYourService yourService)
	{
		var criteria = new Criteria();
		criteria.OrderBy = "Id";
		criteria.OrderByDirection = OrderByDirection.Ascending;
		criteria.Page = 1;
		criteria.PageSize = 10;
		criteria.Search = "Data Access Client";

		var criteriaResult = await yourService.SearchAsync(criteria);

		// criteriaResult.Records // of type YourEntity
		// criteriaResult.TotalRecordCount // integer
	}
}
```


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
- [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/)
- [EntityCloner.Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/EntityCloner.Microsoft.EntityFrameworkCore)

### Usage

If you're using EntityFrameworkCore.SqlServer and you want to use the DataAccessClient, then you can use [DataAccessClient.EntityFrameworkCore.SqlServer](https://github.com/HenkKin/DataAccessClient#dataaccesscliententityframeworkcoresqlserver) package which includes the following registration options via extensions method:

- `IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> dataAccessClientOptionsBuilderAction)`

This extension method supports you to register all needed DbContexts, IUnitOfWorks and IRepositories for provided entity types. Calling AddDbContext or AddDbContextPool of EntityFrameworkCore is not needed and not recommended when you are using this library.

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
        
        services.AddScoped<IUserIdentifierProvider<int>, ExampleUserIdentifierProvider>();
        services.AddScoped<ITenantIdentifierProvider<int>, ExampleTenantIdentifierProvider>();
        services.AddScoped<ILocaleIdentifierProvider<string>, ExampleLocaleIdentifierProvider>();
        
        // register as DataAccessClient
        services.AddDataAccessClient<ExampleDbContext>(conf => conf
            .UsePooling(true)
            .AddCustomEntityBehavior<YourCustomEntityBehaviorConfigurationType>() // optional extensible
            .ConfigureDbContextOptions(builder => builder
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .UseSqlServer("[Your connectionstring]")
            )
        );
                
        ...
    }
    
    ...
```

Using the base class `SqlServerDbContext` on your own DbContext implementation:

```csharp
...
using DataAccessClient.EntityFrameworkCore.SqlServer;

internal class YourDbContext : SqlServerDbContext
{
	public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Register your entities to the DbContext using EntityTypeBuilder
		modelBuilder.Entity<ExampleEntity>()
				.ToTable("ExampleEntities");
		// OR
		// Register your entities to the DbContext using EntityTypeConfiguration class
		modelBuilder.ApplyConfiguration(new ExampleEntityEntityTypeConfiguration());

		// OR
		// Register your entities to the DbContext using IEntityTypeConfiguration 
 		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyInfo).Assembly);

		base.OnModelCreating(modelBuilder);
	}
}
    ...
```

### Extensibility

To add your custom EntityBehavior, you have to implement the `IEntityBehaviorConfiguration` interface.

To see a working implementation of an EntityBehavior, have a look at: [TenantScopeableEntityBehaviorConfiguration](https://github.com/HenkKin/DataAccessClient/blob/master/DataAccessClient.EntityFrameworkCore.SqlServer/Configuration/EntityBehaviors/TenantScopeableEntityBehaviorConfiguration.cs)

```csharp
...
using DataAccessClient.EntityFrameworkCore.SqlServer;

public class Startup
{
    ...
    
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
		// register as DataAccessClient
		services.AddDataAccessClient<ExampleDbContext>(conf => conf
		    ...
			.AddCustomEntityBehavior<YourCustomEntityBehavior1ConfigurationType>() 
			.AddCustomEntityBehavior<YourCustomEntityBehavior2ConfigurationType>() 
            
		);
                
		...
	}
}



public class YourCustomEntityBehaviorConfigurationType : IEntityBehaviorConfiguration
{
	public void OnRegistering(IServiceCollection serviceCollection)
	{
		// please register here dependencies you need for your custom entity behavior, it is also allowed to register them elsewhere in your applicatie
	}
	
	public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
	{
		// if you need some context information for query filters, like tenantIdentifier or LocaleIdentifier, then you van provide it into this dictionary. 
		return new Dictionary<string, dynamic>();
	}
	
	public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext sqlServerDbContext, Type entityType)
	{
		// configure the Entities if needed
	}
	
	public void OnBeforeSaveChanges(SqlServerDbContext sqlServerDbContext, DateTime onSaveChangesTime)
	{
		// optional you van provide some logic before save. You can you use here the `ChangeTracker`
	}
	
	public void OnAfterSaveChanges(SqlServerDbContext sqlServerDbContext)
	{
		// optional you van provide some logic after save. You can you use here the `ChangeTracker`
	}
}
    ...
```
#### HasQueryFilter issue, please use AppendQueryFilter!

When configuring an QueryFilter for an entity, you normally use `EntityTypeBuilder.HasQueryFilter(LambdaExpression filter)` or the generic variant of it.
The downside of this method is, that is overwrite the current QueryFilter.
Especially when we have multiple entity behaviors, which each specify its own filter.

To solve this issue an extension method is provided:

`EntityTypeBuilder<TEntity> AppendQueryFilter<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, bool>> expression) where TEntity : class`

It lives in the namespace `DataAccessClient.EntityFrameworkCore.SqlServer` and class `EntityTypeBuilderExtensions`.

This method concatenates the queryies provided via `AppendQueryFilter(...)`.

### Supporting migrations using `dotnet ef` tooling

First, open a command prompt and navigate to your migrations project folder

`cd [path-to-your-project-folder]`

When version of `dotnet ef` tooling is updated, uninstall `dotnet ef` tooling 

`dotnet tool uninstall --global dotnet-ef `

Install `dotnet ef` tooling (only needed first time or when version is updated)

`dotnet tool install --global dotnet-ef --add-source https://api.nuget.org/v3/index.json --ignore-failed-sources`

Adding migrations for specific DbContext

`dotnet ef migrations add [migrationname] --context YourDbContext --output-dir Migrations/YourDatabase`

Removing latest migration for specific DbContext

`dotnet ef migrations remove --context YourDbContext`

Updating database to latest migration

`dotnet ef database update --context YourDbContext`

Updating database to target migration (up or down)

`dotnet ef database update [migrationname] --context YourDbContext`

Note: when only one DbContext exists in your project, you can skip te --context and the --output-dir (default folder will be: Migrations)


### Debugging

If you want to debug the source code, thats possible. [SourceLink](https://github.com/dotnet/sourcelink) is enabled. To use it, you  have to change Visual Studio Debugging options:

Debug => Options => Debugging => General

Set the following settings:

[&nbsp;&nbsp;] Enable Just My Code

[X] Enable .NET Framework source stepping

[X] Enable source server support

[X] Enable source link support


Now you can use 'step into' (F11).
