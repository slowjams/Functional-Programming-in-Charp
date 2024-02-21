using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
using Boc.Domain.Events;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Boc.Domain;

namespace ConsoleAppFP.zChapter_Practices
{
    public static class Program_Practices
    {       
        public static void Main_(string[] args)
        {
            List<string> list = new List<string>();
            
            Dictionary<int, string> dict = new Dictionary<int, string>()
            {
                { 1, "Tom"}, { 2, "Michael"}, { 5, "John"}
            };
        
            //--------------------------------------V
            if (dict.TryGetValue(3, out var value))
                list.Add(value);

            if (dict.TryGetValue(5, out var value2))
                list.Add(value2);

            if (dict.TryGetValue(7, out var value3))
                list.Add(value3);
            //--------------------------------------Ʌ

            Func<int, Unit> addRecord = key =>
            {
                if (dict.TryGetValue(key, out var value_))
                    list.Add(value_);

                return default;
            };

            addRecord(3);
            addRecord(5);
            addRecord(7);          
        }

        public static void AnotherMethod()
        {
            List<string> list2 = new List<string>();

            Dictionary<int, string> dict2 = new Dictionary<int, string>()
            {
                { 7, "Jen"}, { 8, "Alice"}, { 9, "Grace"}
            };
         
            // Func<List<string>, int, Unit> funcTakeList = addRecord2.Apply(dict);
            // Func<int, Unit> funcTakeKey = funcTakeList.Apply(list);

            Func<int, Unit> finalFunc = addRecord2.Apply(dict2).Apply(list2);

            finalFunc(7);
            finalFunc(8);
            finalFunc(9);
        }

        public static void AnotherMethod2()
        {
            List<int> list2 = new List<int>();

            Dictionary<string, int> dict2 = new Dictionary<string , int>()
            {
                { "Jen", 7 }, { "Alice", 8 }, { "Grace", 9 }
            };

            // Func<List<string>, int, Unit> funcTakeList = addRecord2.Apply(dict);
            // Func<int, Unit> funcTakeKey = funcTakeList.Apply(list);

            Func<string, Unit> finalFunc = addRecord3<string, int>().Apply(dict2).Apply(list2);

            finalFunc("Jen");
            finalFunc("Alice");
            finalFunc("Grace");
        }


        public static Func<IDictionary<int, string>, IList<string>, int, Unit> addRecord2 = (dict, list, key) =>
        {
            if (dict.TryGetValue(key, out var value_))
                list.Add(value_);

            return default;
        };


        public static Func<IDictionary<TKey, TValue>, IList<TValue>, TKey, Unit> addRecord3<TKey, TValue>()
        {

            return (dict, list, key) =>
            {
                if (dict.TryGetValue(key, out var value_))
                    list.Add(value_);

                return default;
            };
        }


        //public static Func<IDictionary<TKey, TValue>, IList<TValue>, TKey, Unit> addRecord3<TKey, TValue> = (IDictionary<TKey, TValue> a, IList<TValue> b, TKey k) =>
        //{
           
        //    return default;
        //};
    }
}