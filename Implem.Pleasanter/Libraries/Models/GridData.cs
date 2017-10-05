﻿using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Settings;
using Implem.Pleasanter.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Implem.Pleasanter.Libraries.Models
{
    public class GridData
    {
        public Databases.AccessStatuses AccessStatus = Databases.AccessStatuses.Initialized;
        public EnumerableRowCollection<DataRow> DataRows;
        public Aggregations Aggregations = new Aggregations();

        public GridData(
            SiteSettings ss,
            View view,
            SqlWhereCollection where = null,
            Sqls.TableTypes tableType = Sqls.TableTypes.Normal,
            int top = 0,
            int offset = 0,
            int pageSize = 0,
            bool countRecord = false,
            IEnumerable<Aggregation> aggregations = null,
            bool get = true)
        {
            Get(
                ss: ss,
                view: view,
                where: where,
                tableType: tableType,
                top: top,
                offset: offset,
                pageSize: pageSize,
                countRecord: countRecord,
                aggregations: aggregations);
        }

        private void Get(
            SiteSettings ss,
            View view,
            SqlWhereCollection where = null,
            Sqls.TableTypes tableType = Sqls.TableTypes.Normal,
            int top = 0,
            int offset = 0,
            int pageSize = 0,
            bool history = false,
            bool countRecord = false,
            IEnumerable<Aggregation> aggregations = null)
        {
            var column = Column(ss);
            var join = Join(ss);
            where = view.Where(ss, where);
            var orderBy = view.OrderBy(ss);
            if (pageSize > 0 && orderBy?.Any() != true)
            {
                orderBy = new SqlOrderByCollection().Add(
                    tableName: ss.ReferenceType,
                    columnBracket: "[UpdatedTime]",
                    orderType: SqlOrderBy.Types.desc);
            }
            var statements = new List<SqlStatement>
            {
                Rds.Select(
                    tableName: ss.ReferenceType,
                    dataTableName: "Main",
                    column: SqlColumnCollection(column),
                    join: join,
                    where: where,
                    orderBy: orderBy,
                    tableType: tableType,
                    top: top,
                    offset: offset,
                    pageSize: pageSize,
                    countRecord: countRecord)
            };
            if (aggregations != null)
            {
                SetAggregations(
                    ss: ss,
                    aggregations: aggregations,
                    join: join,
                    where: where,
                    statements: statements);
            }
            var dataSet = Rds.ExecuteDataSet(
                transactional: false,
                statements: statements.ToArray());
            Aggregations.Set(dataSet, aggregations, ss);
            DataRows = dataSet.Tables["Main"].AsEnumerable();
        }

        private static SqlColumnCollection SqlColumnCollection(IEnumerable<Column> columns)
        {
            return new SqlColumnCollection(columns
                .SelectMany(column => column.SqlColumnCollection())
                .GroupBy(o => o.ColumnBracket + o.As)
                .Select(o => o.First())
                .ToArray());
        }

        private static List<Column> Column(SiteSettings ss)
        {
            var columns = ss.GetGridColumns(checkPermission: true).ToList();
            columns
                .GroupBy(o => o.SiteId)
                .Select(o => o.First())
                .ToList()
                .ForEach(o => AddDefaultColumns(
                    o.Joined
                        ? o.TableAlias + ","
                        : string.Empty,
                    o.SiteSettings,
                    columns));
            return columns
                .Where(o => o != null)
                .ToList();
        }

        private static void AddDefaultColumns(
            string tableAlias, SiteSettings ss, List<Column> columns)
        {
            if (ss.ColumnHash.ContainsKey("SiteId"))
            {
                columns.Add(ss.GetColumn(tableAlias + "SiteId"));
            }
            ss.TitleColumns
                .Where(o => ss.ColumnHash.ContainsKey(o))
                .ForEach(name =>
                    columns.Add(ss.GetColumn(tableAlias + name)));
            columns.Add(ss.GetColumn(tableAlias + Rds.IdColumn(ss.ReferenceType)));
            columns.Add(ss.GetColumn(tableAlias + "Creator"));
            columns.Add(ss.GetColumn(tableAlias + "Updator"));
        }

        private static SqlJoinCollection Join(SiteSettings ss)
        {
            return ss.SqlJoinCollection(Arrays.Concat(ss.GridColumns, ss.FilterColumns)
                .Where(o => o.Contains(","))
                .Select(o => ss.GetColumn(o))
                .ToList());
        }

        private static void SetAggregations(
            SiteSettings ss,
            IEnumerable<Aggregation> aggregations,
            SqlJoinCollection join,
            SqlWhereCollection where,
            List<SqlStatement> statements)
        {
            switch (ss.ReferenceType)
            {
                case "Depts":
                    statements.AddRange(Rds.DeptsAggregations(aggregations, join, where));
                    break;
                case "Groups":
                    statements.AddRange(Rds.GroupsAggregations(aggregations, join, where));
                    break;
                case "Users":
                    statements.AddRange(Rds.UsersAggregations(aggregations, join, where));
                    break;
                case "Issues":
                    statements.AddRange(Rds.IssuesAggregations(aggregations, join, where));
                    break;
                case "Results":
                    statements.AddRange(Rds.ResultsAggregations(aggregations, join, where));
                    break;
                case "Wikis":
                    statements.AddRange(Rds.WikisAggregations(aggregations, join, where));
                    break;
            }
        }

        public void TBody(
            HtmlBuilder hb, SiteSettings ss, IEnumerable<Column> columns, bool checkAll)
        {
            var idColumn = Rds.IdColumn(ss.ReferenceType);
            DataRows.ForEach(dataRow =>
            {
                var dataId = dataRow.Long(idColumn).ToString();
                hb.Tr(
                    attributes: new HtmlAttributes()
                        .Class("grid-row")
                        .DataId(dataId),
                    action: () =>
                    {
                        hb.Td(action: () => hb
                            .CheckBox(
                                controlCss: "grid-check",
                                _checked: checkAll,
                                dataId: dataId));
                        var depts = new Dictionary<string, DeptModel>();
                        var groups = new Dictionary<string, GroupModel>();
                        var users = new Dictionary<string, UserModel>();
                        var issues = new Dictionary<string, IssueModel>();
                        var results = new Dictionary<string, ResultModel>();
                        var wikis = new Dictionary<string, WikiModel>();
                        columns.ForEach(column =>
                        {
                            var key = column.TableName();
                            switch (column.SiteSettings?.ReferenceType)
                            {
                                case "Depts":
                                    if (!depts.ContainsKey(key))
                                    {
                                        depts.Add(key, new DeptModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        deptModel: depts.Get(key));
                                    break;
                                case "Groups":
                                    if (!groups.ContainsKey(key))
                                    {
                                        groups.Add(key, new GroupModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        groupModel: groups.Get(key));
                                    break;
                                case "Users":
                                    if (!users.ContainsKey(key))
                                    {
                                        users.Add(key, new UserModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        userModel: users.Get(key));
                                    break;
                                case "Issues":
                                    if (!issues.ContainsKey(key))
                                    {
                                        issues.Add(key, new IssueModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        issueModel: issues.Get(key));
                                    break;
                                case "Results":
                                    if (!results.ContainsKey(key))
                                    {
                                        results.Add(key, new ResultModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        resultModel: results.Get(key));
                                    break;
                                case "Wikis":
                                    if (!wikis.ContainsKey(key))
                                    {
                                        wikis.Add(key, new WikiModel(
                                            column.SiteSettings, dataRow, column.TableAlias));
                                    }
                                    hb.TdValue(
                                        ss: column.SiteSettings,
                                        column: column,
                                        wikiModel: wikis.Get(key));
                                    break;
                            }
                        });
                    });
            });
        }
    }
}