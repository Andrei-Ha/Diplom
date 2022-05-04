using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Delineation.Classes
{
    public static class StringExtension
    {
        public static string SmallFIO(this string str)
        {
            var fio = str.Split(' ');
            StringBuilder strb = new StringBuilder(string.Empty);
            if(fio.Length!=3)
                return str;
            else
                strb.Append(fio[0]).Append(" ").Append(fio[1].Substring(0, 1)).Append(". ").Append(fio[2].Substring(0, 1)).Append(".");
            return strb.ToString();
        }
    }
}
