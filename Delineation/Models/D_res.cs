using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    public class D_Res
    {
        [Key]
        [Display(Name = "Код")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Display(Name = "РЭС")]
        public string Name { get; set; }

        public D_Person Nach { get; set; }
        [Display(Name = "начальник")]
        public int? NachId { get; set; }
        public D_Person ZamNach { get; set; }
        [Display(Name = "зам.нач. по сбыту")]
        public int? ZamNachId { get; set; }
        public D_Person GlInzh { get; set; }
        [Display(Name = "гл. инженер")]
        public int? GlInzhId { get; set; }
        public D_Person Buh { get; set; }
        [Display(Name = "бухгалтер")]
        public int? BuhId { get; set; }

        [Display(Name = "город"), Column(TypeName = "nvarchar(30)"), StringLength(30)]
        public string City { get; set; }

        [Display(Name = "РЭСа"), Column(TypeName = "nvarchar(30)"), StringLength(30)]
        public string RESa { get; set; }

        [Display(Name = "РЭСом"), Column(TypeName = "nvarchar(30)"), StringLength(30)]
        public string RESom { get; set; }

        [Display(Name = "Ф.И.О. нач. в родит.падеже"), Column(TypeName = "nvarchar(30)"), StringLength(30)]
        public string FIOnachRod { get; set; }

        [Display(Name = "доверенность '№_ от _'"), Column(TypeName = "nvarchar(30)"), StringLength(30)]
        public string Dover { get; set; }
    }
}
