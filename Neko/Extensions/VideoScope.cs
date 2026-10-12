using System;

namespace Neko.Extensions
{
    /// <summary>
    /// Marks the window in which a Markdown fragment is being rendered as a scene of
    /// a video page (<c>video:</c> front matter). Like <see cref="PresentationScope"/>
    /// and <see cref="DocumentScope"/>, the video components (<c>counter</c>,
    /// <c>chart</c>, <c>lower-third</c>, <c>cta</c>, …) only claim their container
    /// names inside a scene, so every other page keeps rendering them as plain
    /// containers.
    ///
    /// Thread-static because pages are rendered in parallel; a scene's whole render
    /// happens synchronously on the thread that set the flag.
    /// </summary>
    public static class VideoScope
    {
        [ThreadStatic]
        private static bool _isRenderingScene;

        public static bool IsRenderingScene => _isRenderingScene;

        /// <summary>Enters the scene-rendering scope until the returned handle is disposed.</summary>
        public static IDisposable Enter() => new Scope();

        private sealed class Scope : IDisposable
        {
            private readonly bool _previous;

            public Scope()
            {
                _previous = _isRenderingScene;
                _isRenderingScene = true;
            }

            public void Dispose() => _isRenderingScene = _previous;
        }
    }
}
