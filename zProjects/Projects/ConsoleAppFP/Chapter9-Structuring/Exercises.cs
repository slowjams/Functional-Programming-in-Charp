using System;
using System.Collections.Generic;
using System.Linq;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Slowjams.Functional;
using static Slowjams.Functional.F;
using Unit = System.ValueTuple;
//using SlowJams.Functional;

namespace ConsoleAppFP.Chapter9_Structuring
{
   public static class Program
   {
      public static void Mainz(string[] args)
      {
         Func<int, int> RemainderWhenDividingBy5 = Remainder.ApplyR(5);


         var ss = CreateUkMobileNumber("1213");

      }

      public static Func<int, int, int> Remainder = 
         (dividend, divisor) => dividend - ((dividend / divisor) * divisor);

      public static Func<T1, R> ApplyR<T1, T2, R>(this Func<T1, T2, R> func, T2 t2) => t1 => func(t1, t2);

      public static Func<CountryCode, NumberType, string, PhoneNumber> CreatePhoneNumber = 
         (country, type, number) => new PhoneNumber(type, country, number);

      public static Func<NumberType, string, PhoneNumber> CreateUkNumber = CreatePhoneNumber.Apply((CountryCode)"uk");

      public static Func<string, PhoneNumber> CreateUkMobileNumber = CreateUkNumber.Apply(NumberType.Mobile);
   }

   public enum NumberType { Mobile, Home, Office }

   public class CountryCode
   {
      string Value { get; }
      public CountryCode(string value) { Value = value; }
      public static implicit operator string(CountryCode c) => c.Value;
      public static implicit operator CountryCode(string s) => new CountryCode(s);
      public override string ToString() => Value;
   }

   public class PhoneNumber
   {
      public NumberType Type { get; }
      public CountryCode Country { get; }
      public string Number { get; }

      public PhoneNumber(NumberType type, CountryCode country, string number)
      {
         Type = type;
         Country = country;
         Number = number;
      }
   }
}
