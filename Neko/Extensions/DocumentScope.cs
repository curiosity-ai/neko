using System;

namespace Neko.Extensions
{
    /// <summary>
    /// Marks the window in which a Markdown fragment is being rendered as a page of a
    /// paged document (<c>document:</c> front matter). Neko's <c>::: name</c> container
    /// is otherwise generic, so the document components (<c>cols</c>, <c>box</c>,
    /// <c>note</c>, <c>keypoint</c>, <c>flow</c>, <c>facts</c>, <c>entries</c>, …) only
    /// take over those names inside a document, and every other page keeps rendering
    /// them as plain containers. Thread-static for the same reason as
    /// <see cref="PresentationScope"/>: pages are rendered in parallel.
    /// </summary>
    public static class DocumentScope
    {
        [ThreadStatic]
        private static bool _isRenderingPage;

        public static bool IsRenderingPage => _isRenderingPage;

        public static IDisposable Enter() => new Scope();

        private sealed class Scope : IDisposable
        {
            private readonly bool _previous;

            public Scope()
            {
                _previous = _isRenderingPage;
                _isRenderingPage = true;
            }

            public void Dispose() => _isRenderingPage = _previous;
        }
    }
}
