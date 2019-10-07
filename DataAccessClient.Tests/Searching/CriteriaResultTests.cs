using System.Linq;
using DataAccessClient.Searching;
using Xunit;

namespace DataAccessClient.Tests.Searching
{
    public class CriteriaResultTests
    {
        [Fact]
        public void WhenCriteriaResultIsCreated_ItShouldHaveDefaults()
        {
            // Act
            var criteriaResult = new CriteriaResult<int>();

            // Assert
            Assert.Null(criteriaResult.Records);
            Assert.Equal(0, criteriaResult.TotalRecordCount);
        }

        [Fact]
        public void WhenCriteriaResultIsCreatedWithValues_ItShouldHaveTheseValues()
        {
            // Act
            var criteria = new CriteriaResult<int>()
            {
                TotalRecordCount = 10,
                Records = new []{1,2,3,4,5,6,7,8,9,10}
            };

            // Assert
            Assert.Equal(10, criteria.TotalRecordCount);
            Assert.Equal(10, criteria.Records.Count());
            Assert.Equal(1, criteria.Records.First());
            Assert.Equal(10, criteria.Records.Last());
        }
    }
}
