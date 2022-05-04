using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    public class D_Tc
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Display(Name = "№ ТУ")]
        [Column(TypeName = "nvarchar(20)"), StringLength(20)]
        public string Num { get; set; }

        [Display(Name = "Дата выдачи"), DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "РЭС")]
        public D_Res Res { get; set; }
        [Display(Name = "РЭС")]
        public int? ResId { get; set; }

        [Display(Name = "Наименование организации заявителя"), Column(TypeName = "nvarchar(500)"), StringLength(500)]
        public string Company { get; set; }

        [Display(Name = "ФИО заявителя"), Column(TypeName = "nvarchar(70)"), StringLength(70)]
        public string FIO { get; set; }

        [Display(Name = "№ абонента"), Column(TypeName = "nvarchar(7)"), StringLength(7)]
        public string AbonNum { get; set; }

        [Display(Name = "Наименование объекта"), Column(TypeName = "nvarchar(500)"), StringLength(500)]
        public string ObjName { get; set; }

        [Display(Name = "Адрес объекта строительства"), Column(TypeName = "nvarchar(250)"), StringLength(250)]
        public string Address { get; set; }

        [Display(Name = "Разрешенная мощность, кВт."), Column(TypeName = "nvarchar(7)"), StringLength(7, ErrorMessage = "допустимая длинна - 7 символов")]
        public string Pow { get; set; }

        [Display(Name = "Категория по надежн. эл. снабжен. эл. установок потребителя"), Column(TypeName = "nvarchar(5)"), StringLength(5)]
        public string Category { get; set; }

        [Display(Name = "Категория по надежн. эл. снабжен. внешней схемы"), Column(TypeName = "nvarchar(5)"), StringLength(5)]
        public string Category2 { get; set; }

        [Display(Name = "Название ПС"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string PS { get; set; }

        [Display(Name = "Инвентарный №")]
        public int PSInvNum { get; set; }

        [Display(Name = "Линия 10кВ"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string Line10 { get; set; }

        [Display(Name = "Инвентарный №")]
        public int Line10InvNum { get; set; }

        [Display(Name = "Название ТП"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string TP { get; set; }

        [Display(Name = "диспетчерский № ТП")]
        public int TPnum { get; set; }

        [Display(Name = "Инвентарный №")]
        public int TPInvNum { get; set; }

        [Display(Name = "Линия 0,4кВ"), Column(TypeName = "nvarchar(50)"), StringLength(50)]
        public string Line04 { get; set; }

        [Display(Name = "Инвентарный №")]
        public int Line04InvNum { get; set; }

        [Display(Name = "Опора №"), Column(TypeName = "nvarchar(10)"), StringLength(10)]
        public string Pillar { get; set; }
    }
}
