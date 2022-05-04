using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    public class D_Person
    {
        public int Id { get; set; }

        [Display(Name = "Фамилия"), Column(TypeName = "nvarchar(70)"), StringLength(70)]
        public string Surname { get; set; }

        [Display(Name = "Имя"), Column(TypeName = "nvarchar(70)"), StringLength(70)]
        public string Name { get; set; }

        [Display(Name = "Отчество"), Column(TypeName = "nvarchar(70)"), StringLength(70)]
        public string Patronymic { get; set; }

        [Column(TypeName = "nvarchar(7)"), StringLength(7)]
        public string Kod_long { get; set; }

        [Column(TypeName = "nvarchar(7)"), StringLength(7)]
        public string Linom { get; set; }

        public D_Position Position { get; set; }
        [Display(Name="Должность")]
        public int PositionId { get; set; }

        public string LineFIO()
        {
            return this.Surname + " " + this.Name + " " + this.Patronymic;
        }
    }
}
