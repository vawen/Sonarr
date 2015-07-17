using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Profiles;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(100)]
    public class update_profile_language : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Profiles").AddColumn("Languages").AsString().NotNullable().WithDefaultValue("");
            Alter.Table("Profiles").AddColumn("CutoffLanguage").AsInt32().NotNullable().WithDefaultValue(0);
            Alter.Table("Profiles").AddColumn("AllowLanguageUpgrade").AsBoolean().NotNullable().WithDefaultValue(false);
            Alter.Table("Profiles").AddColumn("LanguageOverQuality").AsBoolean().NotNullable().WithDefaultValue(false);
            Execute.WithConnection(ConvertLanguage);
            Delete.Column("Language").FromTable("Profiles");
        }

        private void ConvertLanguage(IDbConnection conn, IDbTransaction tran)
        {
            var LanguageConverter = new EmbeddedDocumentConverter(new LanguageIntConverter());

            var langs = GetLanguages(conn, tran);

            foreach (var lang in langs)
            {

                using (IDbCommand updateProfileCmd = conn.CreateCommand())
                {
                    var itemsJson = LanguageConverter.ToDB(lang.Languages);
                    updateProfileCmd.Transaction = tran;
                    updateProfileCmd.CommandText = "UPDATE Profiles SET Languages = ? WHERE Id = ?";
                    updateProfileCmd.AddParameter(itemsJson);
                    updateProfileCmd.AddParameter(lang.Id);

                    updateProfileCmd.ExecuteNonQuery();
                }

                using (IDbCommand updateProfileCmd = conn.CreateCommand())
                {
                    var langDB = LanguageConverter.ToDB(lang.cutoff);
                    updateProfileCmd.Transaction = tran;
                    updateProfileCmd.CommandText = "UPDATE Profiles SET CutoffLanguage = ? WHERE Id = ?";
                    updateProfileCmd.AddParameter(langDB);
                    updateProfileCmd.AddParameter(lang.Id);

                    updateProfileCmd.ExecuteNonQuery();
                }
            }
        }

        private List<LangProfile> GetLanguages(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = new List<LangProfile>();

            using (IDbCommand getProfilesCmd = conn.CreateCommand())
            {
                getProfilesCmd.Transaction = tran;
                getProfilesCmd.CommandText = @"SELECT Id, Language FROM Profiles";

                using (IDataReader profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        var id = profileReader.GetInt32(0);
                        var lang = profileReader.GetInt32(1);

                        var languages = Language.All
                                .OrderByDescending(l => l.Name)
                                .Select(v => new ProfileLanguageItem { Language = v, Allowed = v.Id == lang })
                                .ToList();

                        profiles.Add(new LangProfile { Id = id, cutoff = Language.FindById(lang), Languages = languages });
                    }
                }
            }

            return profiles;
        }

        private class LangProfile
        {
            public int Id { get; set; }
            public Language cutoff { get; set; }
            public List<ProfileLanguageItem> Languages { get; set; }
        }
    }
}
