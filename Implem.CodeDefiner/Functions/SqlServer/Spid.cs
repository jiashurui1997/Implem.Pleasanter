﻿using Implem.DefinitionAccessor;
using System.Data;
using System.Linq;
namespace Implem.CodeDefiner.Functions.SqlServer
{
    internal static class Spids
    {
        internal static void Kill(string uid)
        {
            Get(uid).AsEnumerable().ForEach(spidDataRow =>
                Def.SqlIoBySysem().ExecuteNonQuery(
                    Def.Sql.KillSpid.Replace("#Spid#", spidDataRow["spid"].ToString())));
        }

        private static DataTable Get(string uid)
        {
            return Def.SqlIoBySysem().ExecuteTable(
                Def.Sql.SpWho.Replace("#Uid#", uid));
        }
    }
}
