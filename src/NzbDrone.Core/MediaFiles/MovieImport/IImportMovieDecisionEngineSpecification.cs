using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.MovieImport
{
    public interface IImportMovieDecisionEngineSpecification
    {
        Decision IsSatisfiedBy(LocalMovie localMovie);
    }
}
