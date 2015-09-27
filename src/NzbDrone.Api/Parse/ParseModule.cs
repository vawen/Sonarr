using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Api.Parse
{
    public class ParseModule : NzbDroneRestModule<ParseResource>
    {
        private readonly IParsingService _parsingService;
        private readonly IParseProvider _parseProvider;

        public ParseModule(IParsingService parsingService, IParseProvider parseProvider)
        {
            _parsingService = parsingService;
            _parseProvider = parseProvider;

            GetResourceSingle = Parse;
        }

        private ParseResource Parse()
        {
            var title = Request.Query.Title.Value;
            var parsedEpisodeInfo = _parseProvider.ParseTitle(title);

            if (parsedEpisodeInfo == null)
            {
                return null;
            }

            var remoteEpisode = _parsingService.Map(parsedEpisodeInfo);

            if (remoteEpisode == null)
            {
                remoteEpisode = new RemoteEpisode
                                {
                                    ParsedEpisodeInfo = parsedEpisodeInfo
                                };

                return new ParseResource
                       {
                           Title = title,
                           ParsedEpisodeInfo = parsedEpisodeInfo
                       };
            }

            var resource = ToResource(remoteEpisode);
            resource.Title = title;

            return resource;
        }
    }
}