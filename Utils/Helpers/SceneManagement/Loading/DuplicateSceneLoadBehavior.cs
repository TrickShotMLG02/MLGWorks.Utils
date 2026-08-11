namespace MLGWorks.Utils.Helpers.SceneManagement.Loading
{
    /// <summary>Defines how a coordinator handles a scene load that is already in progress.</summary>
    public enum DuplicateSceneLoadBehavior
    {
        /// <summary>Return the existing operation to all callers.</summary>
        ReturnExisting,

        /// <summary>Throw an exception for the duplicate request.</summary>
        Throw
    }
}
