using GemBox.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Delineation.ViewModels
{
    public class ActsSortViewModel
    {
        public ASortState DateSort { get; private set; }
        public ASortState ResSort { get; private set; }
        public ASortState FioSort { get; private set; }
        public ASortState Current { get; private set; }
        public ActsSortViewModel(ASortState sortOrder)
        {
            DateSort = sortOrder == ASortState.DateAsc ? ASortState.DateDesc : ASortState.DateAsc;
            ResSort = sortOrder == ASortState.ResAsc ? ASortState.ResDesc : ASortState.ResAsc;
            FioSort = sortOrder == ASortState.FioAsc ? ASortState.FioDesc : ASortState.FioAsc;
            Current = sortOrder;
        }
    }
    public enum ASortState
    {
        DateAsc,
        DateDesc,
        ResAsc,
        ResDesc,
        FioAsc,
        FioDesc
    }
}
