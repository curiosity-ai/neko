namespace Neko.Builder
{
    public static class IconHelper
    {
        public static string GetIconClass(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return string.Empty;

            if (iconName.StartsWith("brands-"))
            {
                return $"fi fi-{iconName}";
            }

            return $"fi fi-rr-{iconName}";
        }
    }
}
