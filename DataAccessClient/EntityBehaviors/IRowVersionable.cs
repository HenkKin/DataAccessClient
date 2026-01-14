using System;

namespace DataAccessClient.EntityBehaviors
{
    public interface IRowVersionable<TRowVersionableType>
    {
        TRowVersionableType RowVersion { get; set; }
    }
}