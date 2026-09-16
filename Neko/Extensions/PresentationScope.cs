using System;

namespace Neko.Extensions
{
    /// <summary>
    /// Marks the window in which a Markdown fragment is being rendered as a
    /// presentation slide. Neko's `::: name` container is otherwise generic — the
    /// name becomes the div's class — so the presentation components
    /// (<c>cols</c>, <c>box</c>, <c>note</c>, <c>claim</c>, <c>lead</c>,
    /// <c>figure</c>) only take over those names inside a deck, and every other
    /// page keeps rendering them as plain containers.
    ///
    /// Thread-static because pages are rendered in parallel; a slide's whole
    /// render happens synchronously on the thread that set the flag.
    /// </summary>
    public static class PresentationScope
    {
        [ThreadStatic]
        private static bool _isRenderingSlide;

        public static bool IsRenderingSlide => _isRenderingSlide;

        /// <summary>
        /// Enters the slide-rendering scope. Dispose (or let the <c>using</c> end)
        /// to restore whatever was in force before.
        /// </summary>
        public static IDisposable Enter() => new Scope();

        private sealed class Scope : IDisposable
        {
            private readonly bool _previous;

            public Scope()
            {
                _previous = _isRenderingSlide;
                _isRenderingSlide = true;
            }

            public void Dispose() => _isRenderingSlide = _previous;
        }
    }
}
