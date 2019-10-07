using System.Threading.Tasks;
using Moq;
using Xunit;

namespace DataAccessClient.Tests
{
    public class UnitOfWorkTests
    {
        [Fact]
        public async Task SaveAsync_WhenCalled_ItShouldCallAllDependentIUnitOfWorkParts()
        {
            // Arrange
            var mockRepository = new MockRepository(MockBehavior.Strict);
            var unitOfWorkPart1 = mockRepository.Create<IUnitOfWorkPart>();
            var unitOfWorkPart2 = mockRepository.Create<IUnitOfWorkPart>();

            unitOfWorkPart1.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
            unitOfWorkPart2.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);

            var unitOfWork = new UnitOfWork(new[] { unitOfWorkPart1.Object, unitOfWorkPart2.Object });

            // Act
            await unitOfWork.SaveAsync();

            // Assert
            mockRepository.VerifyAll();
        }

        [Fact]
        public void Reset_WhenCalled_ItShouldCallAllDependentIUnitOfWorkParts()
        {
            // Arrange
            var mockRepository = new MockRepository(MockBehavior.Strict);
            var unitOfWorkPart1 = mockRepository.Create<IUnitOfWorkPart>();
            var unitOfWorkPart2 = mockRepository.Create<IUnitOfWorkPart>();

            unitOfWorkPart1.Setup(x => x.Reset()).Verifiable();
            unitOfWorkPart2.Setup(x => x.Reset()).Verifiable();

            var unitOfWork = new UnitOfWork(new[] { unitOfWorkPart1.Object, unitOfWorkPart2.Object });

            // Act
            unitOfWork.Reset();

            // Assert
            mockRepository.VerifyAll();
        }
    }
}
