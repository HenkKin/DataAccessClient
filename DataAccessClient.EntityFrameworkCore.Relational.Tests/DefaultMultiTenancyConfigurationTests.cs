using Xunit;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests
{
    public class DefaultMultiTenancyConfigurationTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public void EnableQueryFilter_WhenCalled_ItShouldEnableQueryFilter(bool initialIsQueryFilterEnabled, bool expectextIsQueryFilterEnabled)
        {
            // Arrange
            var subject = new DefaultMultiTenancyConfiguration();
            if (initialIsQueryFilterEnabled)
            {
                subject.EnableQueryFilter();
            }
            else
            {
                subject.DisableQueryFilter();
            }

            Assert.Equal(initialIsQueryFilterEnabled, subject.IsQueryFilterEnabled);

            // Act
            var disposable = subject.EnableQueryFilter();

            // Assert
            Assert.Equal(expectextIsQueryFilterEnabled, subject.IsQueryFilterEnabled);

            disposable.Dispose();
            Assert.Equal(initialIsQueryFilterEnabled, subject.IsQueryFilterEnabled);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void DisableQueryFilter_WhenCalled_ItShouldEnableQueryFilter(bool initialIsQueryFilterEnabled, bool expectextIsQueryFilterEnabled)
        {
            // Arrange
            var subject = new DefaultMultiTenancyConfiguration();
            if (initialIsQueryFilterEnabled)
            {
                subject.EnableQueryFilter();
            }
            else
            {
                subject.DisableQueryFilter();
            }

            Assert.Equal(initialIsQueryFilterEnabled, subject.IsQueryFilterEnabled);

            // Act
            var disposable = subject.DisableQueryFilter();

            // Assert
            Assert.Equal(expectextIsQueryFilterEnabled, subject.IsQueryFilterEnabled);

            disposable.Dispose();
            Assert.Equal(initialIsQueryFilterEnabled, subject.IsQueryFilterEnabled);
        }

        [Fact]
        public void EnableAndDisableQueryFilter_WhenCalledMultipleTimesWithNesting_ItShouldResetQueryFilterState()
        {
            // Arrange
            var subject = new DefaultMultiTenancyConfiguration();
            subject.EnableQueryFilter();

            Assert.True(subject.IsQueryFilterEnabled);

            // Act
            using (subject.DisableQueryFilter())
            {
                Assert.False(subject.IsQueryFilterEnabled);
                using (subject.EnableQueryFilter())
                {
                    Assert.True(subject.IsQueryFilterEnabled);
                    using (subject.DisableQueryFilter())
                    {
                        Assert.False(subject.IsQueryFilterEnabled);
                        using (subject.EnableQueryFilter())
                        {
                            Assert.True(subject.IsQueryFilterEnabled);
                            using (subject.DisableQueryFilter())
                            {
                                Assert.False(subject.IsQueryFilterEnabled);
                            }
                            Assert.True(subject.IsQueryFilterEnabled);
                        }
                        Assert.False(subject.IsQueryFilterEnabled);
                    }
                    Assert.True(subject.IsQueryFilterEnabled);
                }
                Assert.False(subject.IsQueryFilterEnabled);
            }

            // Assert
            Assert.True(subject.IsQueryFilterEnabled);
        }
    }
}
