using Neko.Configuration;
using System.Collections.Generic;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        private void RenderSidebar(StringBuilder sb, List<LinkConfig> sidebarLinks)
        {
            sb.AppendLine("        <div id=\"sidebar-overlay\" class=\"fixed inset-0 bg-black/50 z-40 hidden md:hidden glassmorphism\"></div>");
            sb.AppendLine("        <aside id=\"sidebar\" class=\"neko-no-anim w-64 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 overflow-y-auto flex flex-col shrink-0 fixed md:static inset-y-0 left-0 z-40 transform -translate-x-full md:translate-x-0 transition-transform duration-200 ease-in-out h-full\">");
            sb.AppendLine("            <nav class=\"flex-1\">");
            sb.AppendLine("                <div class=\"p-4 sticky top-0 z-10 bg-gray-50 dark:bg-gray-800\">");
            sb.AppendLine("                    <div class=\"relative\">");
            sb.AppendLine("                        <div class=\"absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none\">");
            sb.AppendLine("                            <i class=\"fi fi-rr-filter text-gray-400\"></i>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                        <input type=\"text\" id=\"sidebar-filter\" placeholder=\"Filter...\" class=\"w-full pl-10 pr-3 py-2 text-sm bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-sm\">");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"px-4 pb-4\">");
            sb.AppendLine("                    <ul class=\"space-y-1\" id=\"sidebar-list\">");

            RenderSidebarItems(sb, sidebarLinks ?? new List<LinkConfig>(), 0);

            sb.AppendLine("                    </ul>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </nav>");
            sb.AppendLine("        </aside>");
        }

        private void RenderSidebarItems(StringBuilder sb, List<LinkConfig> links, int level, string parentGroupId = "__root__", string parentSectionKey = "")
        {
            if (links == null || links.Count == 0) return;

            foreach (var link in links)
            {
                var iconHtml = "";
                if (!string.IsNullOrEmpty(link.Icon))
                {
                    iconHtml = Neko.Builder.IconHelper.RenderIcon(link.Icon);
                }
                else
                {
                     // Add invisible icon spacer to align with siblings
                     iconHtml = "<i class=\"fi fi-rr-circle opacity-0\"></i>";
                }

                string editHtml = "";
                string folderRelativePath = null;
                string folderYmlPath = null;
                if (!string.IsNullOrEmpty(link.FolderPath))
                {
                    folderRelativePath = link.FolderPath.Replace("\\", "/");
                    if (folderRelativePath.StartsWith(_config.Input.Replace("\\", "/")))
                    {
                         folderRelativePath = folderRelativePath.Substring(_config.Input.Replace("\\", "/").Length).TrimStart('/');
                    }
                    folderYmlPath = string.IsNullOrEmpty(folderRelativePath) ? "index.yml" : folderRelativePath + "/index.yml";
                }
                if (_isWatchMode && folderYmlPath != null)
                {
                    editHtml = $"<button onclick=\"nekoOpenEditorPath('{folderYmlPath}', event)\" class=\"text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors focus:outline-none p-1 rounded hover:bg-gray-300 dark:hover:bg-gray-600 mr-1\" title=\"Edit Folder Config\"><i class=\"fi fi-rr-pencil text-xs\"></i></button>";
                }

                string reorderAttrs = "";
                string groupIdForChildren = parentGroupId;
                if (_isWatchMode)
                {
                    string neonType;
                    string neonPath;
                    if (folderYmlPath != null)
                    {
                        neonType = "folder";
                        neonPath = folderYmlPath;
                        groupIdForChildren = folderYmlPath;
                    }
                    else
                    {
                        neonType = "file";
                        neonPath = link.Link ?? "";
                    }
                    var encPath = System.Net.WebUtility.HtmlEncode(neonPath);
                    var encParent = System.Net.WebUtility.HtmlEncode(parentGroupId ?? "__root__");
                    reorderAttrs = $" draggable=\"true\" data-neko-reorder=\"true\" data-neko-type=\"{neonType}\" data-neko-path=\"{encPath}\" data-neko-parent=\"{encParent}\"";
                }

                string effectivePassword = null;
                if (!string.IsNullOrEmpty(link.Password))
                {
                    if (!link.Password.Equals("none", System.StringComparison.OrdinalIgnoreCase))
                    {
                        effectivePassword = link.Password;
                    }
                }
                else if (!string.IsNullOrEmpty(_config.Password))
                {
                    effectivePassword = _config.Password;
                }

                string liClasses = "";
                string itemAttributes = "";
                string displayTitle = link.Text;

                if (!string.IsNullOrEmpty(effectivePassword))
                {
                    var encryptionResult = Neko.Encryption.PageEncryptor.Encrypt(link.Text ?? "", effectivePassword);
                    var payloadObj = new { salt = encryptionResult.Salt, iv = encryptionResult.Iv, data = encryptionResult.Data };
                    var payloadJson = System.Text.Json.JsonSerializer.Serialize(payloadObj);
                    var payloadBase64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson));

                    liClasses = "protected-sidebar-item hidden";
                    itemAttributes = $" data-protected-payload=\"{payloadBase64}\"";
                    displayTitle = "<span class=\"protected-text\">Protected</span>";
                }
                else
                {
                     // Ensure non-protected items don't accidentally contain HTML
                     displayTitle = System.Net.WebUtility.HtmlEncode(link.Text);
                }

                if (link.Items != null && link.Items.Count > 0)
                {
                    // Stable, hierarchical key (title path from the root) so the open/closed
                    // state of every collapsible section can be persisted across page loads.
                    var titleForKey = link.Text ?? "";
                    var sectionKeyRaw = string.IsNullOrEmpty(parentSectionKey) ? titleForKey : parentSectionKey + " / " + titleForKey;
                    var sectionKey = System.Net.WebUtility.HtmlEncode(sectionKeyRaw);

                    if (level == 0)
                    {
                        // Render as a collapsible section header, open by default.
                        // The active page's section is re-opened on load by the active-link script;
                        // any user-toggled state is restored by the section-state script.
                        sb.AppendLine($"                    <li class=\"first:mt-0 {liClasses}\"{itemAttributes}{reorderAttrs} style=\"margin-top:1.2rem;margin-bottom:0.5rem;\">");
                        sb.AppendLine($"                        <details class=\"sidebar-section group\" data-section-key=\"{sectionKey}\" open>");
                        sb.AppendLine($"                            <summary class=\"flex items-center justify-between w-full px-2 py-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer select-none\">");
                        sb.AppendLine($"                                <span class=\"text-xs font-bold text-gray-500 uppercase tracking-wider\">{displayTitle}</span>");
                        sb.AppendLine($"                                <div class=\"flex items-center shrink-0\">{editHtml}<i class=\"neko-chevron fi fi-rr-angle-small-right text-gray-400 transition-transform duration-200 ease-in-out group-open:rotate-90\"></i></div>");
                        sb.AppendLine($"                            </summary>");
                        sb.AppendLine($"                            <ul class=\"space-y-1 mt-1\">");

                        RenderSidebarItems(sb, link.Items, level + 1, groupIdForChildren, sectionKeyRaw);

                        sb.AppendLine($"                            </ul>");
                        sb.AppendLine($"                        </details>");
                        sb.AppendLine($"                    </li>");
                    }
                    else
                    {
                        // Render as a collapsible group
                        sb.AppendLine($"                    <li class=\"space-y-1 {liClasses}\"{itemAttributes}{reorderAttrs}>");
                        sb.AppendLine($"                        <details class=\"group\" data-section-key=\"{sectionKey}\" open>");
                        sb.AppendLine($"                            <summary class=\"flex items-center justify-between py-1 px-2 text-[13px] font-medium text-gray-700 dark:text-gray-200 rounded hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer select-none\">");
                        sb.AppendLine($"                                <span class=\"flex items-center gap-2 truncate\">{iconHtml} <span class=\"truncate\">{displayTitle}</span></span>");
                        sb.AppendLine($"                                <div class=\"flex items-center shrink-0\">{editHtml}<i class=\"neko-chevron fi fi-rr-angle-small-right transition-transform duration-200 ease-in-out group-open:rotate-90\"></i></div>");
                        sb.AppendLine($"                            </summary>");
                        sb.AppendLine($"                            <ul class=\"pl-0 space-y-1 mt-1 border-l border-gray-200 dark:border-gray-700 ml-3\">");

                        RenderSidebarItems(sb, link.Items, level + 1, groupIdForChildren, sectionKeyRaw);

                        sb.AppendLine($"                            </ul>");
                        sb.AppendLine($"                        </details>");
                        sb.AppendLine($"                    </li>");
                    }
                }
                else
                {
                    // Render as a leaf link
                    var href = link.Link ?? "#";
                    if (href.EndsWith(".md")) href = href.Substring(0, href.Length - 3);
                    if (href.EndsWith(".html")) href = href.Substring(0, href.Length - 5);

                    // Ensure absolute path for internal links if not already absolute/external
                    if (!href.StartsWith("/") && !href.Contains("://") && href != "#")
                    {
                        href = "/" + href;
                    }

                    sb.AppendLine($"                    <li class=\"{liClasses}\"{itemAttributes}{reorderAttrs}><a href=\"{href}\" class=\"block py-1 px-2 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2 text-[13px] text-gray-700 dark:text-gray-300 truncate\">{iconHtml} <span class=\"truncate\">{displayTitle}</span></a></li>");
                }
            }
        }
    }
}
