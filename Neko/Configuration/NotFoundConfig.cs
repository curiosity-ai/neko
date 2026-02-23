using YamlDotNet.Serialization;

namespace Neko.Configuration
{
    public class NotFoundConfig
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; } = "404";

        [YamlMember(Alias = "message")]
        public string Message { get; set; } = "Page not found";

        [YamlMember(Alias = "description")]
        public string Description { get; set; } = "Sorry, we couldn’t find the page you’re looking for.";

        [YamlMember(Alias = "linkText")]
        public string LinkText { get; set; } = "Go back home";

        [YamlMember(Alias = "link")]
        public string Link { get; set; } = "/";

        [YamlMember(Alias = "backgroundImage")]
        public string BackgroundImage { get; set; }
    }
}
