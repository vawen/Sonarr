using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.MovieImport
{
    public class ImportMovieDecision
    {
        public LocalMovie LocalMovie { get; private set; }
        public IEnumerable<Rejection> Rejections { get; private set; }

        public bool Approved
        {
            get
            {
                return Rejections.Empty();
            }
        }

        public ImportMovieDecision(LocalMovie localMovie, params Rejection[] rejections)
        {
            LocalMovie = localMovie;
            Rejections = rejections.ToList();
        }
    }
}
