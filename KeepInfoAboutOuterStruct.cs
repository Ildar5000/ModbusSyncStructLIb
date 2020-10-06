using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructAllforTest;

namespace ModbusSyncStructLIb
{
    /// <summary>
    /// Класс который хранит в себе данные о всех подключаемых интерфейсов структур
    /// </summary>
    public class KeepInfoAboutOuterStruct
    {
        //хранение интерфейсовs
        public IStruct1K struct1K;
        public IStruct2K struct2K;
        public object obj;
        public string TypeStr;
        public string InterfaceDefinition(object ReciveStruct)
        {
            Type type = ReciveStruct.GetType();
            TypeStr = "Object";
            switch (type.ToString())
            {
                case "IStruct1K":
                    struct1K = (IStruct1K)ReciveStruct;
                    TypeStr = "struct1K";
                    break;
                case "IStruct2K":
                    struct2K = (IStruct2K)ReciveStruct;
                    TypeStr = "struct2K";
                    break;
                default:
                    obj = "Object";
                    break;
            }
            return TypeStr;
        }



    }
}
