﻿using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase
{
    public abstract class DbContextTestBase : IClassFixture<DatabaseFixture>
    {
        protected DatabaseFixture DatabaseFixture { get; }

        protected DbContextTestBase(string testName, DatabaseFixture databaseFixture)
        {
            DatabaseFixture = databaseFixture;
        }
    }
}
