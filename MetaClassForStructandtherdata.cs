using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb
{
    [Serializable]
    public class MetaClassForStructandtherdata
    {
        object struct_which_need_transfer { get; set; }
        string type_struct { get; set; }

        public MetaClassForStructandtherdata(string txt)
        {
            struct_which_need_transfer = txt;
            type_struct = "string";
        }


        public MetaClassForStructandtherdata(object txt)
        {
            struct_which_need_transfer = txt;
            //type_struct = txt.;
        }

    }
}
