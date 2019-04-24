using System;
using System.Collections.Generic;
using DataModels;
using System.Data.SqlClient;
using System.Web.Configuration;
using Dapper;

namespace Services
{
    public class ManagerService
    {
        Db db;

        public String TakeConn(String Name)
        {
            return WebConfigurationManager.OpenWebConfiguration("~/").ConnectionStrings.ConnectionStrings[Name].ToString();
        }

        public ManagerService()
        {
            var conn = new SqlConnection(TakeConn("ConnectionString"));
            db = Db.Init(conn, 30);
           
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Add(Manager model)
        {
            int result = 0;
            try
            {
                result = db.Manager.Insert(model);
            }
            catch (Exception ex)
            {
                result = -1;
            }
            return result > 0 ? true : false;
        }

        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool Delete(string guid)
        {
            int result;
            try
            {
                result = db.Manager.Delete(guid);
            }
            catch (Exception ex)
            {
                result = 2;
            }
            return result > 0 ? true : false;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Update(Manager model)
        {
            int result;
            try
            {
                result = db.Manager.Update(model);
            }
            catch (Exception ex)
            {
                result = 2;
            }
            return result > 0 ? true : false;
        }

        /// <summary>
        /// 取得所有管理員
        /// </summary>
        /// <param name="7"></param>
        /// <returns></returns>
        public IEnumerable<Manager> GetAllManager()
        {
            IEnumerable<Manager> models = null;
            models = db.Manager.All();
            return models;
        }
    }
}
