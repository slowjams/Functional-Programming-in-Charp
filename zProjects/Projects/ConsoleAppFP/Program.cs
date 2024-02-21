using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ConsoleApp {
   class Program {
        static void Main1(string[] args)
        {
            Person p = new Person("John", "Citizen");

            Address2 addr = new Address2("chn", 10086);
            var (x, y) = addr;

            Console.WriteLine("");
        }

        static decimal CalculateDiscount(Order order) =>
         order switch 
         {
            {  Items: > 10, Cost: > 1000.00m } => 0.10m,
            (Items: > 5, Cost: > 500.00m) => 0.05m,
            Order { Cost: > 250.00m } => 0.02m,
            null => throw new ArgumentNullException(nameof(order), "Can't calculate discount on null order"),
            (var ds, var df) when ds>3 => 0.03m,
            _ => 0m
         };

      static bool CheckIfCanWalkIntoBank(Bank bank, bool isVip) {
         return bank switch {
            { Status: BankBranchStatus.Open } => true,
            { Status: BankBranchStatus.Closed } => false,
            { Status: BankBranchStatus.VIPCustomersOnly } => isVip
         };
      }

   }

   record Address2(string Country, int ee);

   public record Person(string FirstName, string LastName);

   public class Person2 {
      public string FirstName { get; init; } = default!;
      public string LastName { get; init; } = default!;
   }

   public record Person3(string FirstName, string LastName) {
      public string Middleware { get; init; } = default!;
   };

   public record Order(int Items, decimal Cost);

   class Bank {
      public BankBranchStatus Status { get; set; }
   }

   enum BankBranchStatus {
      Open,
      Closed,
      VIPCustomersOnly
   }
}
