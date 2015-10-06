using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Parser.Analizers;
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

        protected void UseAnalizers()
        {
            Mocker.SetConstant<IEnumerable<IAnalizeContent>>(new List<IAnalizeContent> 
            { 
                new AnalizeAudio(TestLogger), 
                new AnalizeCodec(TestLogger),
                new AnalizeDaily(TestLogger), 
                new AnalizeHash(TestLogger),
                new AnalizeLanguage(TestLogger),
                new AnalizeResolution(TestLogger),
                new AnalizeSeason(TestLogger),
                new AnalizeSource(TestLogger),
                new AnalizeSpecial(TestLogger),
                new AnalizeYear(TestLogger),
                new AnalizeAbsoluteEpisodeNumber(TestLogger),
                new AnalizeFileExtension(TestLogger),
                new AnalizeProper(TestLogger),
                new AnalizeRawHD(TestLogger),
                new AnalizeReal(TestLogger)
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
