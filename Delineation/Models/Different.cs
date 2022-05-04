using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Delineation.Models
{
    [NotMapped]
    public class Unit
    {
        public int Kod { get; set; }
        public int Kod_dop { get; set; }
        public string Naim { get; set; }
    }
    [NotMapped]
    public class SelList
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
}
