using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Parser.Analyzers;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Framework
{
    public abstract class CoreTest : TestBase
    {
        protected string ReadAllText(params string[] path)
        {
            return File.ReadAllText(Path.Combine(path));
        }

        protected void UseRealHttp()
        {
            Mocker.SetConstant<IHttpProvider>(new HttpProvider(TestLogger));
            Mocker.SetConstant<IHttpClient>(new HttpClient(new IHttpRequestInterceptor[0], Mocker.Resolve<CacheManager>(), Mocker.Resolve<RateLimitService>(), TestLogger));
            Mocker.SetConstant<IDroneServicesRequestBuilder>(new DroneServicesHttpRequestBuilder());
        }

        protected void UseAnalyzers()
        {
            Mocker.SetConstant<IEnumerable<IAnalyzeContent>>(new List<IAnalyzeContent> 
            { 
                new AnalyzeAudio(TestLogger), 
                new AnalyzeCodec(TestLogger),
                new AnalyzeDaily(TestLogger), 
                new AnalyzeHash(TestLogger),
                new AnalyzeLanguage(TestLogger),
                new AnalyzeResolution(TestLogger),
                new AnalyzeSeason(TestLogger),
                new AnalyzeSource(TestLogger),
                new AnalyzeSpecial(TestLogger),
                new AnalyzeYear(TestLogger),
                new AnalyzeAbsoluteEpisodeNumber(TestLogger),
                new AnalyzeFileExtension(TestLogger),
                new AnalyzeProper(TestLogger),
                new AnalyzerawHD(TestLogger),
                new Analyzereal(TestLogger)
            });
        }
    }

    public abstract class CoreTest<TSubject> : CoreTest where TSubject : class
    {
        private TSubject _subject;

        [SetUp]
        public void CoreTestSetup()
        {
            _subject = null;
        }

        protected TSubject Subject
        {
            get
            {
                if (_subject == null)
                {
                    _subject = Mocker.Resolve<TSubject>();
                }

                return _subject;
            }

        }
    }
}
