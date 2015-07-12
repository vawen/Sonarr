using System.Data;
using System.IO;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(95)]
    public class add_language_to_episodeFiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Column("Language").OnTable("EpisodeFiles").AsInt32().WithDefaultValue(0);

            Execute.WithConnection(UpdateLanguage);
        }

        private void UpdateLanguage(IDbConnection conn, IDbTransaction tran)
        {
            var LanguageConverter = new EmbeddedDocumentConverter(new LanguageIntConverter());

            using (IDbCommand getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = @"SELECT Id, ProfileId FROM Series";
                using (IDataReader seriesReader = getSeriesCmd.ExecuteReader())
                {
                    while (seriesReader.Read())
                    {
                        var seriesId = seriesReader.GetInt32(0);
                        var seriesProfileId = seriesReader.GetInt32(1);


                        using (IDbCommand getProfileCmd = conn.CreateCommand())
                        {
                            getProfileCmd.Transaction = tran;
                            getProfileCmd.CommandText = "SELECT Language FROM Profiles WHERE Id = ?";
                            getProfileCmd.AddParameter(seriesProfileId);
                            IDataReader profilesReader = getProfileCmd.ExecuteReader();
                            while (profilesReader.Read())
                            {
                                var episodeLanguage = profilesReader.GetInt32(0);
                                var validJSON = LanguageConverter.ToDB(Language.FindById(episodeLanguage));

                                using (IDbCommand updateCmd = conn.CreateCommand())
                                {
                                    updateCmd.Transaction = tran;
                                    updateCmd.CommandText = "UPDATE EpisodeFiles SET Language = ? WHERE SeriesId = ?";
                                    updateCmd.AddParameter(validJSON);
                                    updateCmd.AddParameter(seriesId);

                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
