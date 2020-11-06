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

        public bool this_is_file { get; set; }
        public Type type { get; set; }
        public DateTime dateTime { get; set; }

        public int type_archv { get; set; }

        public string name_file { get; set; }
        public MetaClassForStructandtherdata(string txt)
        {
            struct_which_need_transfer = txt;
            type = txt.GetType();
            dateTime = DateTime.Now;
        }


        public MetaClassForStructandtherdata(object txt)
        {
            struct_which_need_transfer = txt;
            if (txt.GetType()!=null)
            {
                type = txt.GetType();
            }
            dateTime = DateTime.Now;
        }

        public MetaClassForStructandtherdata(object txt,bool is_file, string name_file)
        {
            struct_which_need_transfer = txt;
            if (txt.GetType() != null)
            {
                type = txt.GetType();
            }
            dateTime = DateTime.Now;
            this.this_is_file = is_file;
            this.name_file = name_file;
        }

        public MetaClassForStructandtherdata()
        {
        }
    }
}
