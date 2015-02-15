using System;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Newpct
{
    public class Newpct : HttpIndexerBase<NewpctSettings>
    {
        public override DownloadProtocol Protocol { get { return DownloadProtocol.Torrent; } }
        public override Int32 PageSize { get { return 0; } }
        public override bool SupportsRss { get { return false; } }


        public Newpct(IHttpClient httpClient, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, configService, parsingService, logger)
        {

        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NewpctRequestGenerator() { Settings = Settings};
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NewpctParser();
        }
    }
}