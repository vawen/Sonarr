using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(110)]
    public class history_language : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("History")
                .AddColumn("Language").AsInt32().NotNullable().WithDefaultValue(0);
        }     
    }
}
