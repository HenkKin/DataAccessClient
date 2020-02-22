using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class DefaultSoftDeletableConfigurationTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public void Enable_WhenCalled_ItShouldDisableSoftDeletable(bool initialIsEnabled, bool expectextIsEnabled)
        {
            // Arrange
            var subject = new DefaultSoftDeletableConfiguration();
            if (initialIsEnabled)
            {
                subject.Enable();
            }
            else
            {
                subject.Disable();
            }

            Assert.Equal(initialIsEnabled, subject.IsEnabled);

            // Act
            var disposable = subject.Enable();

            // Assert
            Assert.Equal(expectextIsEnabled, subject.IsEnabled);

            disposable.Dispose();
            Assert.Equal(initialIsEnabled, subject.IsEnabled);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void Disable_WhenCalled_ItShouldDisableSoftDeletable(bool initialIsEnabled, bool expectextIsEnabled)
        {
            // Arrange
            var subject = new DefaultSoftDeletableConfiguration();
            if (initialIsEnabled)
            {
                subject.Enable();
            }
            else
            {
                subject.Disable();
            }

            Assert.Equal(initialIsEnabled, subject.IsEnabled);

            // Act
            var disposable = subject.Disable();

            // Assert
            Assert.Equal(expectextIsEnabled, subject.IsEnabled);

            disposable.Dispose();
            Assert.Equal(initialIsEnabled, subject.IsEnabled);
        }

        [Fact]
        public void EnableAndDisable_WhenCalledMultipleTimesWithNesting_ItShouldResetIsEnabledState()
        {
            // Arrange
            var subject = new DefaultSoftDeletableConfiguration();
            subject.Enable();

            Assert.True(subject.IsEnabled);

            // Act
            using (subject.Disable())
            {
                Assert.False(subject.IsEnabled);
                using (subject.Enable())
                {
                    Assert.True(subject.IsEnabled);
                    using (subject.Disable())
                    {
                        Assert.False(subject.IsEnabled);
                        using (subject.Enable())
                        {
                            Assert.True(subject.IsEnabled);
                            using (subject.Disable())
                            {
                                Assert.False(subject.IsEnabled);
                            }
                            Assert.True(subject.IsEnabled);
                        }
                        Assert.False(subject.IsEnabled);
                    }
                    Assert.True(subject.IsEnabled);
                }
                Assert.False(subject.IsEnabled);
            }

            // Assert
            Assert.True(subject.IsEnabled);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        public void EnableQueryFilter_WhenCalled_ItShouldEnableQueryFilter(bool initialIsQueryFilterEnabled, bool expectextIsQueryFilterEnabled)
        {
            // Arrange
            var subject = new DefaultSoftDeletableConfiguration();
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
            var subject = new DefaultSoftDeletableConfiguration();
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
            var subject = new DefaultSoftDeletableConfiguration();
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
