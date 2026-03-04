using Markdig.Renderers;
using Markdig.Renderers.Html;
using Neko.Extensions;

namespace Neko.Extensions
{
    // Alias to avoid ambiguity with Neko.Extensions.AlertBlock
    using MarkdigAlertBlock = Markdig.Extensions.Alerts.AlertBlock;

    public class GitHubAlertRenderer : HtmlObjectRenderer<MarkdigAlertBlock>
    {
        protected override void Write(HtmlRenderer renderer, MarkdigAlertBlock obj)
        {
            var kind = obj.Kind.ToString().ToUpperInvariant();
            var variant = "primary";
            var title = "Info";

            switch (kind)
            {
                case "NOTE":
                    variant = "info";
                    title = "Note";
                    break;
                case "TIP":
                    variant = "tip";
                    title = "Tip";
                    break;
                case "IMPORTANT":
                    variant = "important";
                    title = "Important";
                    break;
                case "WARNING":
                    variant = "warning";
                    title = "Warning";
                    break;
                case "CAUTION":
                    variant = "caution";
                    title = "Caution";
                    break;
                default:
                    variant = "primary";
                    title = kind; // Or maybe "Info"?
                    break;
            }

            // Map variant to colors
            string borderClass = "border-l-4";
            string bgClass = "bg-primary-50 dark:bg-primary-900/20";
            string borderColor = "border-primary-500";
            string icon = "info";
            string titleColor = "text-primary-800 dark:text-primary-200";
            string iconColor = "text-primary-500";

            switch (variant)
            {
                case "primary":
                case "info":
                    bgClass = "bg-primary-50 dark:bg-primary-900/20";
                    borderColor = "border-primary-500";
                    titleColor = "text-primary-800 dark:text-primary-200";
                    iconColor = "text-primary-500";
                    icon = "info";
                    break;
                case "success":
                case "tip":
                    bgClass = "bg-green-50 dark:bg-green-900/20";
                    borderColor = "border-green-500";
                    titleColor = "text-green-800 dark:text-green-200";
                    iconColor = "text-green-500";
                    icon = "check-circle";
                    break;
                case "warning":
                    bgClass = "bg-yellow-50 dark:bg-yellow-900/20";
                    borderColor = "border-yellow-500";
                    titleColor = "text-yellow-800 dark:text-yellow-200";
                    iconColor = "text-yellow-500";
                    icon = "exclamation";
                    break;
                case "danger":
                case "caution":
                    bgClass = "bg-red-50 dark:bg-red-900/20";
                    borderColor = "border-red-500";
                    titleColor = "text-red-800 dark:text-red-200";
                    iconColor = "text-red-500";
                    icon = "cross-circle";
                    break;
                case "important":
                    bgClass = "bg-purple-50 dark:bg-purple-900/20";
                    borderColor = "border-purple-500";
                    titleColor = "text-purple-800 dark:text-purple-200";
                    iconColor = "text-purple-500";
                    icon = "question";
                    break;
            }

            renderer.Write($"<div class=\"my-4 p-4 {borderClass} {borderColor} {bgClass} rounded-r shadow-sm\">");

            // Header with Icon and Title
            renderer.Write("<div class=\"flex items-start\">");

            renderer.Write($"<div class=\"flex-shrink-0 text-xl mr-3 {iconColor}\">");
            renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)}\"></i>");
            renderer.Write("</div>");

            renderer.Write("<div class=\"flex-1\">");

            if (!string.IsNullOrEmpty(title))
            {
                 renderer.Write($"<h5 class=\"font-bold mb-2 {titleColor}\">{title}</h5>");
            }

            renderer.Write("<div class=\"prose dark:prose-invert max-w-none\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            renderer.Write("</div>"); // flex-1
            renderer.Write("</div>"); // flex
            renderer.Write("</div>");
        }
    }
}
