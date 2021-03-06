﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb
{
    [Serializable]
    public class MetaClassForStructAndtherData
    {
        public object struct_which_need_transfer { get; set; }

        public bool this_is_file { get; set; }
        public Type type { get; set; }
        public DateTime dateTime { get; set; }

        public int type_archv { get; set; }

        public string name_file { get; set; }

        #region metaatributesdfiles

        public object metattributes { get; set; }

        public DateTime CreationTime_file { get; set; }

        public DateTime LastWriteTime { get; set; }

        #endregion




        public MetaClassForStructAndtherData(string txt)
        {
            struct_which_need_transfer = txt;
            type = txt.GetType();
            dateTime = DateTime.Now;
        }


        public MetaClassForStructAndtherData(object txt)
        {
            struct_which_need_transfer = txt;
            if (txt.GetType()!=null)
            {
                type = txt.GetType();
            }
            else
            {
            }
            dateTime = DateTime.Now;
        }

        public MetaClassForStructAndtherData(object txt,bool is_file, string name_file,object metattributes,DateTime CreationTime_file,DateTime LastWriteTime)
        {
            struct_which_need_transfer = txt;
            if (txt.GetType() != null)
            {
                type = txt.GetType();
            }
            dateTime = DateTime.Now;
            this.this_is_file = is_file;
            this.name_file = name_file;

            this.metattributes = metattributes;
            this.CreationTime_file = CreationTime_file;
            this.LastWriteTime = LastWriteTime;

        }

        public MetaClassForStructAndtherData()
        {
        }
    }
}
