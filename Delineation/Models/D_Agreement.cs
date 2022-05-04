using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    public class D_Agreement
    {
        public int Id { get; set; }
        public D_Act Act { get; set; }
        public int ActId { get; set; }
        public D_Person Person { get; set; }
        public int PersonId { get; set; }
        public bool Accept { get; set; }
        [Display(Name = "Замечания"), Column(TypeName = "nvarchar(250)"), StringLength(250)]
        public string Note { get; set; }
        [Display(Name = "дата выдачи акта"), DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Display(Name = "информация"), Column(TypeName = "nvarchar(100)"), StringLength(100)]
        public string Info { get; set; }
        public bool Notice { get; set; }
    }
}
