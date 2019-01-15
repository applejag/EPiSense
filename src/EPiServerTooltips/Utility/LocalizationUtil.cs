using Microsoft.CodeAnalysis;

namespace EPiServerTooltips.Utility
{
    internal static class LocalizationUtil
    {
        public static LocalizableResourceString GetLocalizableString(this string nameOfLocalizableResource)
        {
            return new LocalizableResourceString(nameOfLocalizableResource, Resources.ResourceManager, typeof(Resources));
        }
    }
}