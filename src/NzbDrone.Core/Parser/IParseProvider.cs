using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public interface IParseProvider
    {
        ParsedEpisodeInfo ParsePath(string path);
        ParsedEpisodeInfo ParseTitle(string title);
        string ParseReleaseGroup(string title);
        Language ParseLanguage(string title);
    }
}
