using YamlDotNet.Serialization;

namespace Neko.Configuration
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
