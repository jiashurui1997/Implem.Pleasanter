﻿using Implem.DefinitionAccessor;
using Implem.Libraries.Utilities;
namespace Implem.CodeDefiner.Functions.SqlServer
{
    internal static class DbConfigurator
    {
        internal static void Configure()
        {
            Consoles.Write(Environments.ServiceName, Consoles.Types.Info);
            Def.SqlIoBySysem().ExecuteNonQuery(
                Def.Sql.CreateDatabase.Replace("#InitialCatalog#", Environments.ServiceName));
        }
    }
}
