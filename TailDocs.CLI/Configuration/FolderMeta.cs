using YamlDotNet.Serialization;

namespace TailDocs.CLI.Configuration
{
    public class FolderMeta
    {
        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "order")]
        public int? Order { get; set; }
    }
}
