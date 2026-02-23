using YamlDotNet.Serialization;

namespace Neko.Configuration
{
    public class NotFoundConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = "Page not found";

        [YamlMember(Alias = "message")]
        public string Message { get; set; } = "Sorry, we couldn’t find the page you’re looking for.";

        [YamlMember(Alias = "homeText")]
        public string HomeText { get; set; } = "Go back home";

        [YamlMember(Alias = "homeLink")]
        public string HomeLink { get; set; } = "/";

        [YamlMember(Alias = "contactText")]
        public string ContactText { get; set; }

        [YamlMember(Alias = "contactLink")]
        public string ContactLink { get; set; }

        [YamlMember(Alias = "image")]
        public string BackgroundImage { get; set; }
    }
}
