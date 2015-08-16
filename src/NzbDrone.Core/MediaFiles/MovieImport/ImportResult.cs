using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.MediaFiles.EpisodeImport;

namespace NzbDrone.Core.MediaFiles.MovieImport
{
    public class ImportMovieResult
    {
        public ImportMovieDecision ImportMovieDecision { get; private set; }
        public List<string> Errors { get; private set; }

        public ImportResultType Result
        {
            get
            {
                if (Errors.Any())
                {
                    if (ImportMovieDecision.Approved)
                    {
                        return ImportResultType.Skipped;
                    }

                    return ImportResultType.Rejected;
                }

                return ImportResultType.Imported;
            }
        }

        public ImportMovieResult(ImportMovieDecision importMovieDecision, params string[] errors)
        {
            Ensure.That(importMovieDecision, () => importMovieDecision).IsNotNull();

            ImportMovieDecision = importMovieDecision;
            Errors = errors.ToList();
        }
    }
}
