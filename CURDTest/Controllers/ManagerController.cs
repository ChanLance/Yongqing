using System.Web.Mvc;
using DataModels;
using Services;
using System.Collections.Generic;
using System.Linq;

namespace CURDTest.Controllers
{
    public class ManagerController : Controller
    {
        ManagerService ManagerSVC = new ManagerService();

        public ActionResult Index()
        {
            return View();
        }

        #region 新增

        [HttpPost]
        public JsonResult Insert(Manager model)
        {
            string rtnCode = "-1";
            string rtnMsg = "";

            try
            {
                //// 寫入 
                bool rtn = ManagerSVC.Add(model);
                if (rtn) { rtnCode = "1"; }
            }
            catch (System.Exception ex)
            {
                rtnCode = "0";
                rtnMsg = ex.ToString();
                throw;
            }

            var rtnvalue = new
            {
                rtnCode = rtnCode,
                rtnMsg = rtnMsg
            };
            return Json(rtnvalue, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 修改

        [HttpPost]
        public JsonResult Update(Manager model)
        {
            string rtnCode = "-1";
            string rtnMsg = "";

            try
            {
                //// 修改 
                bool rtn = ManagerSVC.Update(model);
                if (rtn) { rtnCode = "1"; }
            }
            catch (System.Exception ex)
            {
                rtnCode = "0";
                rtnMsg = ex.ToString();
                throw;
            }

            var rtnvalue = new
            {
                rtnCode = rtnCode,
                rtnMsg = rtnMsg
            };
            return Json(rtnvalue, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 刪除

        [HttpPost]
        public JsonResult Insert(string guid)
        {
            string rtnCode = "-1";
            string rtnMsg = "";

            try
            {
                //// 刪除 
                bool rtn = ManagerSVC.Delete(guid);
                if (rtn) { rtnCode = "1"; }
            }
            catch (System.Exception ex)
            {
                rtnCode = "0";
                rtnMsg = ex.ToString();
                throw;
            }

            var rtnvalue = new
            {
                rtnCode = rtnCode,
                rtnMsg = rtnMsg
            };
            return Json(rtnvalue, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 查詢

        [HttpPost]
        public JsonResult Search(string keyword)
        {
            string rtnCode = "-1";
            string rtnMsg = "";
            List<Manager> rtnmodel = null;

            try
            {
                //// 查詢
                rtnmodel = ManagerSVC.GetAllManager().Where(x => x.Name.Contains(keyword)).ToList();
                rtnCode = "-1";
            }
            catch (System.Exception ex)
            {
                rtnCode = "0";
                rtnMsg = ex.ToString();
                throw;
            }

            var rtnvalue = new
            {
                rtnCode = rtnCode,
                rtnMsg = rtnMsg
            };
            return Json(rtnvalue, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}