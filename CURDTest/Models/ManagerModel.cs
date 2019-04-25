using System;
using System.ComponentModel.DataAnnotations;
namespace DataModels
{
    public class Manager 
    {
        public int Id { get; set; }
        public string GUID { get; set; }
        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱")]        
        public string Name { get; set; }

        /// <summary>
        /// 客戶ID
        /// </summary>
        [Display(Name = "客戶ID")]
        public string UserID { get; set; }

        /// <summary>
        /// 年齡
        /// </summary>
        [Display(Name = "年齡")]
        public int Age { get; set; }

        /// <summary>
        /// 客戶住址
        /// </summary>
        [Display(Name = "客戶住址")]
        public string Address { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; }
    }
}
