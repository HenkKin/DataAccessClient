using System;
using DataAccessClient.Providers;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels
{
    public class TestUserIdentifierProvider : IUserIdentifierProvider<int>
    {
        public Guid InstanceId { get; }
        public TestUserIdentifierProvider()
        {
            InstanceId = Guid.NewGuid();
        }

        public int? UserId { get; private set; } = 1;

        public int? Execute()
        {
            return UserId;
        }

        public void ChangeUserIdentifier(int? userIdentifier)
        {
            UserId = userIdentifier;
        }
    }
}