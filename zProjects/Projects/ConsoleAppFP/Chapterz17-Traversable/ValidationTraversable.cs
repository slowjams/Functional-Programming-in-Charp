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
using System.Threading.Tasks;

namespace ConsoleAppFP.Chapterz17_Traversable
{
    using static F;

    public static class ValidationTraversable_
    {
        // Exceptional
        public static Exceptional<Validation<R>> Traverse<T, R>(this Validation<T> @this, Func<T, Exceptional<R>> f)
        {
            return @this.Match
            (
                Invalid: errs => Exceptional(Invalid<R>(errs)),
                Valid: t => f(t).Map(Valid)  // Valid is F.Valid below
            );
        }

        /*
        public static partial class F
        {
            public static Validation<T> Valid<T>(T value) => new(value ?? throw new ArgumentNullException(nameof(value)));
            // ...
        }
        */


        // Task
        public static Task<Validation<R>> Traverse<T, R>(this Validation<T> @this, Func<T, Task<R>> func)
        {
            return @this.Match
            (
               Invalid: reasons => Async(Invalid<R>(reasons)),
               Valid: t => func(t).Map(Valid)   // Valid is F.Valid above
            );
        }

        // more, can be added in the future when needed
    }
}
