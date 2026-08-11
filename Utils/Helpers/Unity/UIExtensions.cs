using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Unity
{
    /// <summary>Provides conditional update helpers for common Unity UI components.</summary>
    public static class UIExtensions
    {
        /// <summary>Changes anchored position only when it differs.</summary>
        /// <param name="rectTransform">The RectTransform to update.</param>
        /// <param name="position">The desired anchored position.</param>
        /// <returns>True when the value changed; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rectTransform is null.</exception>
        public static bool SetAnchoredPositionIfChanged(this RectTransform rectTransform, Vector2 position)
        {
            if (rectTransform == null) throw new ArgumentNullException(nameof(rectTransform));
            if (rectTransform.anchoredPosition == position) return false;

            rectTransform.anchoredPosition = position;
            return true;
        }

        /// <summary>Changes size delta only when it differs.</summary>
        /// <param name="rectTransform">The RectTransform to update.</param>
        /// <param name="sizeDelta">The desired size delta.</param>
        /// <returns>True when the value changed; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rectTransform is null.</exception>
        public static bool SetSizeDeltaIfChanged(this RectTransform rectTransform, Vector2 sizeDelta)
        {
            if (rectTransform == null) throw new ArgumentNullException(nameof(rectTransform));
            if (rectTransform.sizeDelta == sizeDelta) return false;

            rectTransform.sizeDelta = sizeDelta;
            return true;
        }

        /// <summary>
        /// Sets CanvasGroup visibility and matching interaction state only when needed.
        /// </summary>
        /// <param name="canvasGroup">The CanvasGroup to update.</param>
        /// <param name="visible">The desired visibility.</param>
        /// <returns>True when at least one value changed; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when canvasGroup is null.</exception>
        public static bool SetVisibleIfChanged(this CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup == null) throw new ArgumentNullException(nameof(canvasGroup));

            float alpha = visible ? 1f : 0f;
            if (canvasGroup.alpha == alpha &&
                canvasGroup.interactable == visible &&
                canvasGroup.blocksRaycasts == visible)
            {
                return false;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            return true;
        }
    }
}
