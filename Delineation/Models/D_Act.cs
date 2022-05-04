using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    public enum Stat
    {
        Edit,
        InAgreement,
        Rework,
        Accepted,
        Completed
    }
    public class D_Act
    {
        public int Id { get; set; }

        [Display(Name = "дата выдачи акта"), DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public D_Tc Tc { get; set; }
        [Display(Name = "ТУ")]
        public int? TcId { get; set; }

        [Display(Name = "юр. лицо")]
        public bool IsEntity { get; set; }

        [Display(Name = "действующ. на основании (доверенности, устава) №"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string EntityDoc { get; set; }

        [Display(Name = "На балансе потребителя"), Column(TypeName = "nvarchar(250)"), StringLength(250)]
        public string ConsBalance { get; set; }

        [Display(Name = "Граница баланс. принадлежн. находится на "), Column(TypeName = "nvarchar(150)"), StringLength(150)]
        public string DevBalance { get; set; }

        [Display(Name = "Эксплутационная ответственность потребителя"), Column(TypeName = "nvarchar(250)"), StringLength(250)]
        public string ConsExpl { get; set; }

        [Display(Name = "Граница эксплутационной отв. находится на "), Column(TypeName = "nvarchar(150)"), StringLength(150)]
        public string DevExpl { get; set; }

        [Display(Name = "транзитные сети")]
        public bool IsTransit { get; set; }

        [Display(Name = "ФИО представителя владельца транзитных электрических сетей"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string FIOtrans { get; set; }

        [Display(Name = "Срок действия акта"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string Validity { get; set; }

        [Display(Name = "статус")]
        public int State { get; set; }

        public List<D_Agreement> Agreements { get; set; }

        [Display(Name = "Основная питающая линия 10кВ")]
        [NotMapped]
        public string Temp { get; set; }

        public string Info { get; set; }

        public string StrPSline10 { get; set; }

        //метод определения статуса
        public static string CyrStat(int istat) => istat switch
        {
            0 => "редактирование",
            1 => "на согласовании",
            2 => "на доработку",
            3 => "согласован",
            4 => "действующий",
            _ => throw new ArgumentException("Недопустимый код операции")
        };
        public static string CssClassStat(int istat) => istat switch
        {
            0 => "edit",
            1 => "inagreement",
            2 => "rework",
            3 => "accepted",
            4 => "completed",
            _ => throw new ArgumentException("Недопустимый код операции")
        };
    }
}
