using System;

namespace SharedResources
{
    public interface ITrackedEntity
    {
        DateTime Created { get; set; }
        DateTime Modified { get; set; }
    }
}
