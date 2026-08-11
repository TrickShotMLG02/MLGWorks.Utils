namespace MLGWorks.Utils.Helpers.Pooling.Core
{
    /// <summary>Provides reset hooks for objects managed by an object pool.</summary>
    public interface IPoolable
    {
        /// <summary>Prepares the object after acquisition.</summary>
        void OnPoolAcquire();

        /// <summary>Resets the object before release.</summary>
        void OnPoolRelease();
    }
}
