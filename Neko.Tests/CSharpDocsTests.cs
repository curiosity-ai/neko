using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class CSharpDocsTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        private const string DetailsListColumnSource = @"```csharp-docs
namespace Tesserae
{
    /// <summary>
    /// A typed column definition used to declare how a property of <typeparamref name=""T""/> is rendered inside a
    /// <see cref=""DetailsList{T}""/>.
    /// </summary>
    [H5.Name(""tss.DetailsListColumn"")]
    public class DetailsListColumn : IDetailsListColumn
    {
        private readonly Action      _onColumnClick;
        private readonly HTMLElement InnerElement;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public DetailsListColumn(string title, UnitSize width, UnitSize maxWidth, bool isRowHeader = false, bool enableColumnSorting = false, string sortingKey = null, Action onColumnClick = null)
        {
            InnerElement = TextBlock(Title).Regular().SemiBold().Render();
        }

        /// <summary>
        /// Gets or sets the sorting key.
        /// </summary>
        public string      SortingKey               { get; }
        /// <summary>
        /// Gets or sets the title of the component.
        /// </summary>
        public string      Title                    { get; }
        /// <summary>
        /// Registers a callback invoked when the column click event fires.
        /// </summary>
        public void        OnColumnClick()          => _onColumnClick?.Invoke();
        /// <summary>
        /// Renders the component's root HTML element.
        /// </summary>
        public HTMLElement Render()                 => InnerElement;
    }
}
```";

        [Test]
        public void DoesNotLeakClassBody()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;

            // The private fields should not appear in the rendered output: they had no XML docs
            // and the class signature should not contain the body.
            Assert.That(html, Does.Not.Contain("_onColumnClick"));
            Assert.That(html, Does.Not.Contain("InnerElement = TextBlock"));
            Assert.That(html, Does.Not.Contain("private readonly"));
        }

        [Test]
        public void RendersClassAsStickyHeader()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            Assert.That(doc.Html, Contains.Substring("csharp-type-header"));
            Assert.That(doc.Html, Contains.Substring("sticky"));
        }

        [Test]
        public void GroupsMembersByKind()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            Assert.That(html, Contains.Substring(">Constructors<"));
            Assert.That(html, Contains.Substring(">Properties<"));
            Assert.That(html, Contains.Substring(">Methods<"));
        }

        [Test]
        public void EachMemberHasAKindBadge()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            Assert.That(html, Contains.Substring(">Constructor<"));
            Assert.That(html, Contains.Substring(">Property<"));
            Assert.That(html, Contains.Substring(">Method<"));
        }

        [Test]
        public void MemberNamesArePrefixedWithClass()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            Assert.That(html, Contains.Substring("DetailsListColumn.SortingKey"));
            Assert.That(html, Contains.Substring("DetailsListColumn.OnColumnClick"));
        }

        [Test]
        public void NormalizesSignatureWhitespace()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            // The column-aligned whitespace in `public void        OnColumnClick()` must be collapsed.
            Assert.That(html, Does.Not.Contain("public void        OnColumnClick"));
            Assert.That(html, Contains.Substring("public void OnColumnClick()"));
        }

        [Test]
        public void LinkAnchorComesAfterTheName()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            // The anchor should appear after the class name within the h3, with no-underline.
            Assert.That(html, Contains.Substring("DetailsListColumn<a "));
            Assert.That(html, Contains.Substring("csharp-anchor"));
            Assert.That(html, Contains.Substring("no-underline"));
        }

        [Test]
        public void GenericClassSignatureDoesNotIncludeBody()
        {
            const string source = @"```csharp-docs
namespace Tesserae
{
    /// <summary>
    /// A virtualised, sortable, multi-column table for displaying large lists of typed items.
    /// </summary>
    public class DetailsList<TDetailsListItem> : IComponent, ISpecialCaseStyling where TDetailsListItem : class, IDetailsListItem<TDetailsListItem>
    {
        private readonly List<IDetailsListColumn> _columns;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public DetailsList(params IDetailsListColumn[] columns) { }
    }
}
```";
            var doc = _parser.Parse(source);
            Assert.That(doc.Html, Does.Not.Contain("_columns"));
            Assert.That(doc.Html, Contains.Substring("DetailsList&lt;TDetailsListItem&gt;"));
        }

        [Test]
        public void PropertySignatureShowsAccessors()
        {
            const string source = @"```csharp-docs
namespace Tesserae
{
    public class Sample
    {
        /// <summary>Read-only.</summary>
        public string ReadOnly { get; }
        /// <summary>Read-write.</summary>
        public int ReadWrite { get; set; }
        /// <summary>Init-only.</summary>
        public string InitOnly { get; init; }
    }
}
```";
            var doc = _parser.Parse(source);
            var html = doc.Html;
            Assert.That(html, Contains.Substring("public string ReadOnly { get; }"));
            Assert.That(html, Contains.Substring("public int ReadWrite { get; set; }"));
            Assert.That(html, Contains.Substring("public string InitOnly { get; init; }"));
        }

        [Test]
        public void RendersNamespaceAndImplementsInDefinition()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            Assert.That(html, Contains.Substring("csharp-definition"));
            // Namespace metadata derived from the enclosing namespace declaration.
            Assert.That(html, Contains.Substring(">Namespace<"));
            Assert.That(html, Contains.Substring("Tesserae"));
            // The interface in the base list is surfaced as "Implements".
            Assert.That(html, Contains.Substring(">Implements<"));
            Assert.That(html, Contains.Substring("IDetailsListColumn"));
        }

        [Test]
        public void RendersInheritanceChainForBaseClass()
        {
            const string source = @"```csharp-docs
namespace Demo
{
    /// <summary>A widget.</summary>
    public class Widget : Control, IDisposable
    {
        /// <summary>Does work.</summary>
        public void Run() { }
    }
}
```";
            var doc = _parser.Parse(source);
            var html = doc.Html;
            // The non-interface base type becomes the inheritance chain; the interface is listed under Implements.
            Assert.That(html, Contains.Substring(">Inheritance<"));
            Assert.That(html, Contains.Substring("Control → Widget"));
            Assert.That(html, Contains.Substring(">Implements<"));
            Assert.That(html, Contains.Substring("IDisposable"));
        }

        [Test]
        public void RendersMemberSummaryTable()
        {
            var doc = _parser.Parse(DetailsListColumnSource);
            var html = doc.Html;
            Assert.That(html, Contains.Substring("csharp-member-table"));
            // The summary table links each member name to its detail anchor.
            Assert.That(html, Contains.Substring("href=\"#DetailsListColumn.SortingKey\""));
            // Both the summary table row and the detail section carry the description text.
            Assert.That(html, Contains.Substring("Gets or sets the sorting key."));
        }

        [Test]
        public void StandaloneMemberStillRenders()
        {
            const string source = @"```csharp-docs
/// <summary>
/// Calculates the age.
/// </summary>
/// <param name=""dateOfBirth"">Date of birth.</param>
/// <returns>Age in years.</returns>
public static int AgeAt(this DateOnly dateOfBirth, DateOnly date)
{
    return 0;
}
```";
            var doc = _parser.Parse(source);
            var html = doc.Html;
            Assert.That(html, Contains.Substring("Calculates the age"));
            Assert.That(html, Contains.Substring("AgeAt"));
            Assert.That(html, Contains.Substring(">Method<"));
        }
    }
}
