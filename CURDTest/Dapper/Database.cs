/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Dapper;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using DataModels;
using System.Threading;

namespace Dapper
{
    /// <summary>
    /// A container for a database, assumes all the tables have an Id column named Id
    /// </summary>
    /// <typeparam name="TDatabase"></typeparam>
    public abstract class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
#if DEBUG
        System.Diagnostics.Stopwatch sw;
#endif
        public class Table<T, TId>
        {
            Database<TDatabase> database;
            string tableName;
            string likelyTableName;
            string identityFieldName = "Id"; //預設 identityField 欄位名稱為 "Id"

            public Table(Database<TDatabase> database, string likelyTableName)
            {
                this.database = database;
                this.likelyTableName = likelyTableName;

                //抓取identityFieldName 
            }

            public string TableName
            {
                set
                {
                    tableName = value;
                }
                get
                {
                    tableName = tableName ?? database.DetermineTableName<T>(likelyTableName);
                    return tableName;
                }
            }

            public string IdentityFieldName
            {

                set
                {

                    identityFieldName = value;
                }
                get
                {
                    identityFieldName = identityFieldName ?? database.DetermineIdentityFieldName<T>(identityFieldName);
                    return identityFieldName;
                }
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public int Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove(identityFieldName);

                string cols = string.Join(",", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = "set nocount on insert " + TableName + " (" + cols + ") values (" + cols_params + ") select cast(scope_identity() as int)";

                return database.Query<int>(sql, o).Single();
            }

            /// <summary>
            /// Update a record in the DB
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public int Update(dynamic data) //(TId id, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                //var id2 = data.Id;
                var id = (object)data.GetType().GetProperty(identityFieldName).GetValue((object)data, null);

                var builder = new StringBuilder();
                builder.Append("UPDATE ").Append(TableName).Append(" SET ");
                builder.AppendLine(string.Join(",", paramNames.Where(n => n != identityFieldName).Select(p => p + "= @" + p)));
                builder.Append(" WHERE " + identityFieldName + " = @" + identityFieldName + "");

                DynamicParameters parameters = new DynamicParameters(data);

                parameters.Add(identityFieldName, id);

                return database.Execute(builder.ToString(), parameters);
            }


            public int Update(string guid, string field, dynamic data) //(TId id, dynamic data)
            {


                var builder = new StringBuilder();
                builder.Append("UPDATE ").Append(TableName).Append(" SET ");
                builder.AppendLine(field + "= @" + field);
                builder.Append(" WHERE GUID = @GUID");

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("GUID", guid);
                parameters.Add(field, data);

                return database.Execute(builder.ToString(), parameters);
            }

            public int Update(TId id, string field, dynamic data) //(TId id, dynamic data)
            {


                var builder = new StringBuilder();
                builder.Append("UPDATE ").Append(TableName).Append(" SET ");
                builder.AppendLine(field + "= @" + field);
                builder.Append(" WHERE " + identityFieldName + " = @" + identityFieldName);

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add(identityFieldName, id);
                parameters.Add(field, data);

                return database.Execute(builder.ToString(), parameters);
            }

            /// <summary>
            /// Insert a row into the db or update when key is duplicated
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            //public int InsertOrUpdate(dynamic data)
            //{
            //    var o = (object)data;
            //    List<string> paramNames = GetParamNames(o);

            //    string cols = string.Join(",", paramNames);
            //    string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
            //    string cols_update = string.Join(",", paramNames.Select(p =>  p + " = @" + p));
            //    var sql = @"INSERT INTO `" + TableName + " (" + cols + ") VALUES (" + cols_params +
            //        ") ON DUPLICATE KEY UPDATE " + cols_update + "; SELECT LAST_INSERT_ID()";

            //    return database.Query<int>(sql, o).Single();
            //}

            /// <summary>
            /// Delete all record for the DB
            /// </summary>
            /// <param name=""></param>
            /// <returns></returns>
            public int DeleteAll()
            {
                return database.Execute("DELETE FROM " + TableName);
            }

            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public int Delete(TId id)
            {
                return database.Execute("DELETE FROM " + TableName + " WHERE " + identityFieldName + " = @id", new { id });
            }
            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="guid"></param>
            /// <returns></returns>
            public int Delete(string guid)
            {
                return database.Execute("DELETE FROM " + TableName + " WHERE GUID = @guid", new { guid });
            }
            /// <summary>
            /// Grab a record with a particular Id from the DB 
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public T Get(TId id)
            {
                return database.Query<T>("SELECT * FROM " + TableName + " WHERE " + identityFieldName + " = @id", new { id }).FirstOrDefault();
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB 
            /// </summary>
            /// <param name="guid"></param>
            /// <returns></returns>
            public T Get(string guid)
            {
                return database.Query<T>("SELECT * FROM " + TableName + " WHERE GUID = @guid", new { guid }).FirstOrDefault();
            }


            /// <summary>
            /// Query records with dedicate field name from the DB 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public IEnumerable<T> GetByField(string name, string value)
            {
                return database.Query<T>("SELECT * FROM " + TableName + " WHERE " + name + " = @value", new { value });
            }


            //<summary>
            //Query records with Dictionary by field's Name and value )
            //</summary>
            //<param name="dis"></param>
            //<returns></returns>
            public IEnumerable<T> Query(Dictionary<string, string> dis)
            {
                string sql = "SELECT * FROM " + TableName + " WHERE ";
                var builder = new StringBuilder();
                builder.Append("SELECT * FROM ").Append(TableName).Append(" WHERE ");
                builder.AppendLine(string.Join(" AND ", dis.Select(p => p.Key + "= @" + p.Key)));

                var pr = new DynamicParameters();

                foreach (var di in dis)
                {
                    //      sql += di.Key + " = @" + di.Key + " and ";
                    pr.Add("@" + di.Key, di.Value);

                }

                //  sql = sql.Substring(0, sql.Length - 4);
                return database.Query<T>(builder.ToString(), pr);
                //  return database.Query<T>(sql , pr);

            }

            //<summary>
            //Query records with Dictionary by field's Name and value )
            //</summary>
            //<param name="dis"></param>
            //<returns></returns>
            public IEnumerable<T> QueryByOr(Dictionary<string, string> dis)
            {
                string sql = "SELECT * FROM " + TableName + " WHERE ";
                var builder = new StringBuilder();
                builder.Append("SELECT * FROM ").Append(TableName).Append(" WHERE ");
                builder.AppendLine(string.Join(" OR ", dis.Select(p => p.Key + "= @" + p.Key)));

                var pr = new DynamicParameters();

                foreach (var di in dis)
                {
                    //      sql += di.Key + " = @" + di.Key + " and ";
                    pr.Add("@" + di.Key, di.Value);

                }

                //  sql = sql.Substring(0, sql.Length - 4);
                return database.Query<T>(builder.ToString(), pr);
                //  return database.Query<T>(sql , pr);

            }


            //<summary>
            //Query records with Dictionary by field's Name and value )
            //</summary>
            //<param name=""></param>
            //<returns></returns>
            public IEnumerable<T> QueryByLike(string name, string value)
            {

                return database.Query<T>("SELECT * FROM " + TableName + " WHERE " + name + " LIKE %@value%", new { value });
            }

            //<summary>
            //Query records with Dictionary by field's Name and value )
            //</summary>
            //<param name="dis"></param>
            //<returns></returns>
            public IEnumerable<T> QueryByLike(Dictionary<string, string> dis)
            {
                string sql = "SELECT * FROM " + TableName + " WHERE ";
                var builder = new StringBuilder();
                builder.Append("SELECT * FROM ").Append(TableName).Append(" WHERE ");
                builder.AppendLine(string.Join(" OR ", dis.Select(p => p.Key + " LIKE '%' + @" + p.Key + " +'%'")));

                var pr = new DynamicParameters();

                foreach (var di in dis)
                {
                    //      sql += di.Key + " = @" + di.Key + " and ";
                    pr.Add("@" + di.Key, di.Value);

                }

                //  sql = sql.Substring(0, sql.Length - 4);
                return database.Query<T>(builder.ToString(), pr);
                //  return database.Query<T>(sql , pr);

            }
            //<summary>
            //Query records with T-SQL and param
            //</summary>
            //<param name="sql"></param>
            //<param name="param"></param>
            //<returns></returns>
            public IEnumerable<T> Query(string sql, dynamic param = null)
            {
                return database.Query<T>(sql, param);
            }


            public T First()
            {
                return database.Query<T>("SELECT * FROM " + TableName + " LIMIT 1").FirstOrDefault();
            }

            public IEnumerable<T> All()
            {
                return database.Query<T>("SELECT * FROM " + TableName);
            }

            public Page<T> Page(int page = 1, int itemsPerPage = 10)
            {
                return database.Page<T>("SELECT * FROM " + TableName + " ", page, itemsPerPage: itemsPerPage);
            }

            static ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>>();
            private static List<string> GetParamNames(object o)
            {
                if (o is DynamicParameters)
                {
                    return (o as DynamicParameters).ParameterNames.ToList();
                }

                List<string> paramNames;
                if (!paramNameCache.TryGetValue(o.GetType(), out paramNames))
                {
                    paramNames = new List<string>();
                    foreach (var prop in o.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public))
                    {

                        if (prop.GetCustomAttributes(typeof(NoFieldAttribute), false).Length > 0) { continue; } //跳過[NoField]
                        paramNames.Add(prop.Name);
                    }
                    paramNameCache[o.GetType()] = paramNames;
                }
                return paramNames;
            }
        }

        public class Table<T> : Table<T, int>
        {
            public Table(Database<TDatabase> database, string likelyTableName)
                : base(database, likelyTableName)
            {
            }
        }

        DbConnection connection;
        int commandTimeout;
        DbTransaction transaction;


        public static TDatabase Init(DbConnection connection, int commandTimeout)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, commandTimeout);
            return db;
        }

        private static Action<Database<TDatabase>> tableConstructor;

        private void InitDatabase(DbConnection connection, int commandTimeout)
        {
#if DEBUG
            //開始
            //Thread GetHashCode
            System.Diagnostics.Debug.WriteLine("ThreadFindSchedule Info: " + Thread.CurrentThread.GetHashCode());
            //記憶體使用量
            System.Diagnostics.Debug.WriteLine("Memory Info: " + (System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024).ToString() + "MB");
            //處理時間
            sw = new System.Diagnostics.Stopwatch();//引用stopwatch物件
            sw.Reset();//碼表歸零
            sw.Start();//碼表開始計時
#endif
            this.connection = connection;
            this.commandTimeout = commandTimeout;
            if (tableConstructor == null)
            {
                tableConstructor = CreateTableConstructor();
            }

            tableConstructor(this);
        }

        public void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
        {
            transaction = connection.BeginTransaction(isolation);
        }

        public void CommitTransaction()
        {
            transaction.Commit();
            transaction = null;
        }

        public void RollbackTransaction()
        {
            transaction.Rollback();
            transaction = null;
        }

        protected Action<Database<TDatabase>> CreateTableConstructor()
        {
            var dm = new DynamicMethod("ConstructInstances", null, new Type[] { typeof(Database<TDatabase>) }, true);
            var il = dm.GetILGenerator();

            var setters = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Table<>))
                .Select(p => Tuple.Create(
                        p.GetSetMethod(true),
                        p.PropertyType.GetConstructor(new Type[] { typeof(Database<TDatabase>), typeof(string) }),
                        p.Name,
                        p.DeclaringType
                 ));

            foreach (var setter in setters)
            {
                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Ldstr, setter.Item3);
                // [db, likelyname]

                il.Emit(OpCodes.Newobj, setter.Item2);
                // [table]

                var table = il.DeclareLocal(setter.Item2.DeclaringType);
                il.Emit(OpCodes.Stloc, table);
                // []

                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Castclass, setter.Item4);
                // [db cast to container]

                il.Emit(OpCodes.Ldloc, table);
                // [db cast to container, table]

                il.Emit(OpCodes.Callvirt, setter.Item1);
                // []
            }

            il.Emit(OpCodes.Ret);
            return (Action<Database<TDatabase>>)dm.CreateDelegate(typeof(Action<Database<TDatabase>>));
        }

        static ConcurrentDictionary<Type, string> tableNameMap = new ConcurrentDictionary<Type, string>();
        private string DetermineTableName<T>(string likelyTableName)
        {
            string name;

            if (!tableNameMap.TryGetValue(typeof(T), out name))
            {
                name = likelyTableName;
                if (!TableExists(name))
                {
                    name = "[" + typeof(T).Name + "]";
                }

                tableNameMap[typeof(T)] = name;
            }
            return name;
        }

        private string DetermineIdentityFieldName<T>(string identityFieldName)
        {
            //var obj = new T();		
            var pis = typeof(T).GetProperties();
            var pi = pis.Where(p => p.GetCustomAttributes(typeof(IdentityFieldAttribute), false).Length > 0).FirstOrDefault();
            if (pi != null)
            {

                return pi.Name;
            }
            else
            {
                return identityFieldName;
            }

        }

        private bool TableExists(string name)
        {
            string schemaName = null;

            name = name.Replace("[", "");
            name = name.Replace("]", "");

            if (name.Contains("."))
            {
                var parts = name.Split('.');
                if (parts.Count() == 2)
                {
                    schemaName = parts[0];
                    name = parts[1];
                }
            }

            var builder = new StringBuilder("select 1 from INFORMATION_SCHEMA.TABLES where ");
            if (!String.IsNullOrEmpty(schemaName)) builder.Append("TABLE_SCHEMA = @schemaName AND ");
            builder.Append("TABLE_NAME = @name");

            return connection.Query(builder.ToString(), new { schemaName, name }, transaction: transaction).Count() == 1;

            //return connection.Query("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name AND TABLE_SCHEMA = DATABASE()",
            //    new { name }, transaction: transaction).Count() == 1;
        }

        public int Execute(string sql, dynamic param = null)
        {
            return SqlMapper.Execute(connection, sql, param as object, transaction, commandTimeout: this.commandTimeout);
        }

        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true)
        {

            return SqlMapper.Query<T>(connection, sql, param as object, transaction, buffered, commandTimeout);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<dynamic> Query(string sql, dynamic param = null, bool buffered = true)
        {
            return SqlMapper.Query(connection, sql, param as object, transaction, buffered);
        }

        public Dapper.SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return SqlMapper.QueryMultiple(connection, sql, param, transaction, commandTimeout, commandType);
        }


        public void Dispose()
        {
            if (connection == null) return;
            if (connection.State != ConnectionState.Closed)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                connection.Close();
                connection = null;
            }
#if DEBUG
            //結束
            //Thread GetHashCode
            System.Diagnostics.Debug.WriteLine("ThreadFindSchedule Info: " + Thread.CurrentThread.GetHashCode());
            //記憶體使用量
            System.Diagnostics.Debug.WriteLine("Memory Info: " + (System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024).ToString() + "MB");
            sw.Stop();//碼錶停止
            //印出所花費的總秒數
            TimeSpan ts = sw.Elapsed;
            System.Diagnostics.Debug.WriteLine("程式執行秒數:" + String.Format("{0:000}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));//所耗費的時間
#endif

        }

        #region paging
        static readonly Regex rxColumns = new Regex(@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex rxOrderBy = new Regex(@"\bORDER\s+BY\s+(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?(?:\s*,\s*(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?)*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex rxDistinct = new Regex(@"\ADISTINCT\s", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        Page<T> Page<T>(string sql, int page, dynamic param, int itemsPerPage, out string sqlPage, out DynamicParameters pageParam)
        {
            const int totalPageDisplayed = 9;
            var s = page - totalPageDisplayed;
            if (s <= 0) s = 1;
            //replace SELECT <whatever> => SELECT count(*)
            var m = rxColumns.Match(sql);
            // Save column list and replace with COUNT(*)
            var g = m.Groups[1];
            var sqlSelectRemoved = sql.Substring(g.Index);
            var count = rxDistinct.IsMatch(sqlSelectRemoved) ? m.Groups[1].ToString().Trim() : "*";
            var sqlCount = string.Format("{0} COUNT({1}) {2}", sql.Substring(0, g.Index), count, sql.Substring(g.Index + g.Length));
            // Look for an "ORDER BY <whatever>" clause
            m = rxOrderBy.Match(sqlCount);
            if (m.Success)
            {
                g = m.Groups[0];
                sqlCount = sqlCount.Substring(0, g.Index) + sqlCount.Substring(g.Index + g.Length);
            }
            var total = SqlMapper.Query<int>(connection, sqlCount, param as object).FirstOrDefault();

            sqlPage = sql + "\n LIMIT @limit OFFSET @offset";
            pageParam = new DynamicParameters(param);
            pageParam.Add("@offset", (page - 1) * itemsPerPage);
            pageParam.Add("@limit", itemsPerPage);
            var totalPage = total / itemsPerPage;
            if (total % itemsPerPage != 0) totalPage++;
            int pageDisplayed = page + totalPageDisplayed;
            if (pageDisplayed > totalPage) pageDisplayed = totalPage;
            var p = new Page<T>
            {
                ItemsPerPage = itemsPerPage,
                CurrentPage = page,
                PageDisplayed = pageDisplayed,
                TotalPage = totalPage,
                Start = s,
                Numbering = (page - 1) * itemsPerPage,
                HasPrevious = page - 1 >= s,
                HasNext = page + 1 <= totalPage,
                TotalItems = total
            };
            return p;
        }

        public Page<T> Page<T>(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<T>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query<T>(connection, sqlPage, pageParam).ToList();
            return p;
        }

        public Page<dynamic> Page(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            return Page<dynamic>(sql, page, param as object, itemsPerPage);
        }

        public Page<TReturn> Page<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, int page, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }
        #endregion
    }

    public class Page<T>
    {
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int PageDisplayed { get; set; }
        public int TotalPage { get; set; }
        public int TotalItems { get; set; }
        public int Start { get; set; }
        public int Numbering { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public List<T> Items { get; set; }
    }
}