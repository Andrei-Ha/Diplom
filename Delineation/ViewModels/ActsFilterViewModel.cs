using Delineation.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Delineation.ViewModels
{
    public class ActsFilterViewModel
    {
        public SelectList Reses { get; private set; }
        public int? SelectedRes { get; private set; }
        public string SelectedFIO { get; private set; }
        public ActsFilterViewModel(List<SelList> reses,int? res,string fio)
        {
            reses.Insert(0, new SelList() { Id = "0", Text = "Все РЭСы" });
            Reses = new SelectList(reses, "Id", "Text", res);
            SelectedRes = res;
            SelectedFIO = fio;
        }
    }
}
