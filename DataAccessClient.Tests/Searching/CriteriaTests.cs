using System.Linq;
using DataAccessClient.Searching;
using Xunit;

namespace DataAccessClient.Tests.Searching
{
    public class CriteriaTests
    {
        [Fact]
        public void WhenCriteriaIsCreated_ItShouldHaveDefaults()
        {
            // Act
            var criteria = new Criteria();

            // Assert
            Assert.Null(criteria.Search);
            Assert.Null(criteria.OrderBy);
            Assert.Equal(OrderByDirection.Ascending, criteria.OrderByDirection);
            Assert.Null(criteria.Page);
            Assert.Null(criteria.PageSize);
            Assert.Empty(criteria.Includes);
            Assert.Empty(criteria.KeyFilters);
            Assert.False(criteria.UsePaging());
        }


        [Fact]
        public void WhenCriteriaIsCreatedWithValues_ItShouldHaveTheseValues()
        {
            // Act
            var criteria = new Criteria
            {
                PageSize = 20,
                Page = 2,
                OrderByDirection = OrderByDirection.Descending,
                OrderBy = "OrderedColumn",
                Search = "Search value",
                KeyFilters = {{"key", "value"}},
                Includes = {"Included"}
            };

            // Assert
            Assert.Equal("Search value",criteria.Search);
            Assert.Equal("OrderedColumn", criteria.OrderBy);
            Assert.Equal(OrderByDirection.Descending, criteria.OrderByDirection);
            Assert.Equal(2, criteria.Page);
            Assert.Equal(20, criteria.PageSize);
            Assert.Equal("Included", criteria.Includes.Single());
            Assert.Equal("key",criteria.KeyFilters.First().Key);
            Assert.Equal("value", criteria.KeyFilters.First().Value);
            Assert.True(criteria.UsePaging());
        }

        [Theory]
        [InlineData(null,null, false)]
        [InlineData(null, 10, false)]
        [InlineData(1,null, false)]
        [InlineData(1,10, true)]
        public void WhenPagingIsSet_ItShouldHaveUsePagingIsTrue(int? page, int? pageSize, bool expectedResult)
        {
            // Act
            var criteria = new Criteria();
            criteria.Page = page;
            criteria.PageSize = pageSize;

            // Assert
            Assert.Equal(expectedResult, criteria.UsePaging());
        }
    }
}
