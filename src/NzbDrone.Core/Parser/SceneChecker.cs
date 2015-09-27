using System;

namespace NzbDrone.Core.Parser
{
    public static class SceneChecker
    {
        //This method should prefer false negatives over false positives.
        //It's better not to use a title that might be scene than to use one that isn't scene
        public static bool IsSceneTitle(string title, IParseProvider parseProvider)
        {
            if (!title.Contains(".")) return false;
            if (title.Contains(" ")) return false;

            var parsedTitle = parseProvider.ParseTitle(title);

            if (parsedTitle == null ||
                parsedTitle.ReleaseGroup == null ||
                parsedTitle.Quality.Quality == Qualities.Quality.Unknown ||
                String.IsNullOrWhiteSpace(parsedTitle.SeriesTitle))
            {
                return false;
            }

            return true;
        }
    }
}
