using UnityEngine;

namespace MLGWorks.Utils.Helpers
{
    /// <summary>
    /// A utility class that prevents the GameObject it is attached to from being destroyed when loading a new scene.
    /// </summary>
    public class DontDestroyOnLoad : MonoBehaviour
    {
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
