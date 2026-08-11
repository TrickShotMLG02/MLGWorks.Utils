using System;
using MLGWorks.Utils.Helpers.SceneManagement.Core;

namespace MLGWorks.Utils.Helpers.SceneManagement.Transitions
{
    /// <summary>Defines the visual or interactive stages surrounding a scene load.</summary>
    public interface ISceneTransition
    {
        /// <summary>Plays the outgoing transition and invokes the callback when loading may begin.</summary>
        /// <param name="scene">The scene that will be loaded.</param>
        /// <param name="onCompleted">The callback that starts the load.</param>
        void PlayOut(SceneReference scene, Action onCompleted);

        /// <summary>Plays the incoming transition and invokes the callback when the transition is complete.</summary>
        /// <param name="scene">The scene that was loaded.</param>
        /// <param name="onCompleted">The callback that completes the operation.</param>
        void PlayIn(SceneReference scene, Action onCompleted);
    }
}
