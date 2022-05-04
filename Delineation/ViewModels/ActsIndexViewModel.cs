using Delineation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Threading.Tasks;

namespace Delineation.ViewModels
{
    public class ActsIndexViewModel
    {
        public IEnumerable<D_Act> Acts { get; set; }
        public ActsPageViewModel ActsPageViewModel { get; set; }
        public ActsFilterViewModel ActsFilterViewModel { get; set; }
        public ActsSortViewModel ActsSortViewModel { get; set; }
    }
}
