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
    }
}
