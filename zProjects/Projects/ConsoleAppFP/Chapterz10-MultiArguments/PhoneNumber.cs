using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;
using System.Collections.Immutable;
//using static Slowjams.Functional.F;
//using Slowjams.Functional;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Double = LaYumba.Functional.Double;


namespace ConsoleAppFP.Chapterz10_MultiArguments
{
   public static partial class Program9_2
   {
      private static ISet<string> ValidCountryCodes = new HashSet<string> { "au", "ch" };
      public static void Main_2(string[] args)
      {

      }

      //---------------------------------------------------------------------------V
      public static Validation<PhoneNumber> CreateValidPhoneNumber_ReturnValidation(string type, string countryCode, string number)
      {
         return Valid(PhoneNumber.Create)
                .Apply(validNumberType(type))
                .Apply(validCountryCode(countryCode))
                .Apply(validNumber(number));
      }

      //
      public static Func<string, Validation<NumberType>> validNumberType
        = str => LaYumba.Functional.Enum.Parse<NumberType>(str).Match(
           None: () => Error($"{str} is not a valid number type"),
           Some: n => Valid(n));

      public static Func<string, Validation<CountryCode>> validCountryCode
        = s => CountryCode.Create(ValidCountryCodes, s).Match(
           None: () => Error($"{s} is not a valid country code"),
           Some: c => Valid(c));

      public static Func<string, Validation<Number>> validNumber
         = str => Number.Create(str).Match(
            None: () => Error($"{str} is not a valid number"),
            Some: n => Valid(n));
      //
      //---------------------------------------------------------------------------Ʌ

      //--------------------------------------------------------------V
      public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseApply(string typeStr, string countryStr, string numberStr)
      {
         return Some(PhoneNumber.Create)
                .Apply(optNumberType(typeStr))
                .Apply(optCountryCode(countryStr))
                .Apply(Number.Create(numberStr));
      }

      //
      public static Func<string, Option<CountryCode>> optCountryCode
         = CountryCode.Create.Apply(ValidCountryCodes);

      public static Func<string, Option<NumberType>> optNumberType
         = LaYumba.Functional.Enum.Parse<NumberType>;
      //
      //--------------------------------------------------------------Ʌ

      public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseBind(string typeStr, string countryStr, string numberStr)
      {
         return optCountryCode(countryStr)                
                .Bind(country => optNumberType(typeStr)   // country -> Option<PhoneNumber>
                   .Bind(type => Number.Create(numberStr) 
                      .Bind<Number, PhoneNumber>(number => PhoneNumber.Create(type, country, number))));
      }

      public static Option<PhoneNumber> CreatePhoneNumber_ReturnOption_UseLinq(string typeStr, string countryStr, string numberStr)
      {
         return from country in optCountryCode(countryStr)
                from type in optNumberType(typeStr)
                from number in Number.Create(numberStr)
                select PhoneNumber.Create(type, country, number);
      }

      public static Validation<PhoneNumber> CreatePhoneNumber_ReturnValidation_UseLinq(string typeStr, string countryStr, string numberStr)
      {
         return from type in validNumberType(typeStr)
                from country in validCountryCode(countryStr)
                from number in validNumber(numberStr)
                select PhoneNumber.Create(type, country, number);
      }
   }

   //--------------------------------------------------------------------------------------------V
   public record PhoneNumber
   {   
      public NumberType Type { get; }
      public CountryCode Country { get; }
      public Number Nr { get; }

      public static Func<NumberType, CountryCode, Number, PhoneNumber> Create
         = (type, country, number) => new(type, country, number);

      private PhoneNumber(NumberType type, CountryCode country, Number number)
      {
         Type = type;
         Country = country;
         Nr = number;
      }

      public override string ToString() => $"{Type}: ({Country}) {Nr}";
   }

   public enum NumberType { Mobile, Home, Office }

   public struct Number
   {
      public static Func<string, Option<Number>> Create
         => s => Long.Parse(s)
                     .Map(_ => s)
                     .Where(_ => 5 < s.Length && s.Length < 11)
                     .Map(_ => new Number(s));

      string Value { get; }
      private Number(string value) { Value = value; }
      public static implicit operator string(Number c) => c.Value;
      public static implicit operator Number(string s) => new Number(s);
      public override string ToString() => Value;
   }

   public class CountryCode
   {
      public static Func<ISet<string>, string, Option<CountryCode>> Create
         = (validCodes, code) => validCodes.Contains(code) ? Some(new CountryCode(code)) : None;

      string Value { get; }
      // private ctor so that no invalid instances may be created
      private CountryCode(string value) { Value = value; }
      public override string ToString() => Value;
   }
   //--------------------------------------------------------------------------------------------Ʌ
}