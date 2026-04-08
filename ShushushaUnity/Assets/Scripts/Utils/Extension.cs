using Cysharp.Threading.Tasks;
using FairyGUI;
using UnityEngine;

namespace Utils
{
    public static class Extension
    {
        #region ToUniTask

        public static UniTask TweenMoveAsync(this GObject gObject, Vector2 endValue, float duration)
        {
            var task = new UniTaskCompletionSource();
            gObject.TweenMove(endValue, duration).OnComplete(() => task.TrySetResult());
            return task.Task;
        }

        public static UniTask TweenPlayAsync(this Transition transition)
        {
            var task = new UniTaskCompletionSource();
            transition.Play(() => task.TrySetResult());
            return task.Task;
        }

        public static UniTask TweenPlayReverseAsync(this Transition transition)
        {
            var task = new UniTaskCompletionSource();
            transition.PlayReverse(() => task.TrySetResult());
            return task.Task;
        }

        public static UniTask TweenScaleAsync(this GObject gObject, Vector2 endValue, float duration)
        {
            var task = new UniTaskCompletionSource();
            GTween.To(gObject.scale, endValue, duration).SetTarget(gObject, TweenPropType.Scale);
            return task.Task;
        }

        #endregion
    }
}