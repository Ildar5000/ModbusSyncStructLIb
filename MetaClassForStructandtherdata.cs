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
        public object struct_which_need_transfer { get; set; }
        public Type type { get; set; }
        public DateTime dateTime { get; set; }

        public int type_archv { get; set; }

        public MetaClassForStructandtherdata(string txt)
        {
            struct_which_need_transfer = txt;
            type = txt.GetType();
            dateTime = DateTime.Now;
        }


        public MetaClassForStructandtherdata(object txt)
        {
            struct_which_need_transfer = txt;
            type = txt.GetType();
            dateTime = DateTime.Now;
        }

        public MetaClassForStructandtherdata()
        {
        }
    }
}
