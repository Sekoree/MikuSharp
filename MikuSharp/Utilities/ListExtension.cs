using System;
using System.Collections.Generic;
using System.Text;

namespace MikuSharp.Utilities
{
    public class ListExtension
    {
        public static List<T> Swap<T>(List<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }
    }
}
