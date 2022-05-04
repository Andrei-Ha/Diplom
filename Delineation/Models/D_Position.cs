using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Delineation.Models
{
    public class D_Position
    {
        public int Id { get; set; }
        [Display(Name = "Должность")]
        public string Name { get; set; }
    }
}
