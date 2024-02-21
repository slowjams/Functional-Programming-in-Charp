﻿using static System.Linq.Enumerable;
using static System.Console;
using System.Threading.Tasks;
using System;

namespace Examples.Chapter11
{
   class Product
   {
      public int Inventory { get; private set; }

      public void ReplenishInventory(int units)
         => Inventory += units;

      public void ProcessSale(int units)
         => Inventory -= units;
   }

   class Product_
   {
      int inventory;

      public bool IsLowOnInventory { get; private set; }
      public int Inventory
      {
         get => inventory;
         private set
         {
            inventory = value;

            IsLowOnInventory = inventory <= 5;
         }
      }
   }

   static class Product_FunctionalAlternative
   {
      record Product(int Inventory);

      static Product RetrieveProduct(Guid id) => throw new NotImplementedException();

      static Product ReplenishInventory(Guid id, int units)
      {
         Product original = RetrieveProduct(id);
         Product updated = new Product(original.Inventory + units);
         return updated;
      }
   }

   class LocalMutationIsOk
   {
      int Sum(int[] ints)
      {
         var result = 0;
         foreach (int i in ints) result += i;
         return result;
      }
   }
}
