﻿using Slowjams.Functional;
using SlowJams.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using Unit = System.ValueTuple;

namespace Slowjams.Functional;

using static F;

public static partial class F
{
   public static Validation<T> Valid<T>(T value)
      => new(value ?? throw new ArgumentNullException(nameof(value)));

   // create a Validation in the Invalid state
   public static Validation.Invalid Invalid(params Error[] errors) => new(errors);  // why we need this? maybe fits scenario when T cannot be determined in advance?
   public static Validation<T> Invalid<T>(params Error[] errors) => new Validation.Invalid(errors);
   public static Validation.Invalid Invalid(IEnumerable<Error> errors) => new(errors);
   public static Validation<T> Invalid<T>(IEnumerable<Error> errors) => new Validation.Invalid(errors);
}

public struct Validation<T>
{
   internal IEnumerable<Error> Errors { get; }
   internal T? Value { get; }

   public bool IsValid { get; }

   public static Validation<T> Fail(IEnumerable<Error> errors)
      => new(errors);

   public static Validation<T> Fail(params Error[] errors)
      => new(errors.AsEnumerable());

   private Validation(IEnumerable<Error> errors)
      => (IsValid, Errors, Value) = (false, errors, default);

   internal Validation(T t)
      => (IsValid, Errors, Value) = (true, Enumerable.Empty<Error>(), t);

   public static implicit operator Validation<T>(Error error)    // Single Error
      => new Validation<T>(new[] { error });

   public static implicit operator Validation<T>(Validation.Invalid left)  // IEnumerable<Error>
      => new Validation<T>(left.Errors);

   public static implicit operator Validation<T>(T right) => Valid(right);

   public R Match<R>(Func<IEnumerable<Error>, R> Invalid, Func<T, R> Valid)
      => IsValid ? Valid(this.Value!) : Invalid(this.Errors);

   public Unit Match(Action<IEnumerable<Error>> Invalid, Action<T> Valid)
      => Match(Invalid.ToFunc(), Valid.ToFunc());

   public IEnumerator<T> AsEnumerable()
   {
      if (IsValid) yield return Value!;
   }

   public override string ToString()
      => IsValid
         ? $"Valid({Value})"
         : $"Invalid([{string.Join(", ", Errors)}])";

   public override bool Equals(object? obj)
      => obj is Validation<T> other
         && this.IsValid == other.IsValid
         && (IsValid && this.Value!.Equals(other.Value)
            || this.ToString() == other.ToString());

   public override int GetHashCode() => Match
   (
      Invalid: errs => errs.GetHashCode(),
      Valid: t => t!.GetHashCode()
   );
}

public static class Validation
{
   public struct Invalid
   {
      internal IEnumerable<Error> Errors;
      public Invalid(IEnumerable<Error> errors) { Errors = errors; }
   }

   // There is no Valid struct, Validation can represent "Right"

   public static Validation<R> Map<T, R>
      (this Validation<T> @this, Func<T, R> f)
      => @this.Match
      (
         Valid: t => Valid(f(t)),
         Invalid: errs => Invalid(errs)
      );


   public static Validation<Unit> ForEach<R>
      (this Validation<R> @this, Action<R> act)
      => Map(@this, act.ToFunc());

   public static Validation<T> Do<T>
      (this Validation<T> @this, Action<T> action)
   {
      @this.ForEach(action);
      return @this;
   }

   public static Validation<R> Bind<T, R>
      (this Validation<T> val, Func<T, Validation<R>> f)
       => val.Match(
          Invalid: (err) => Invalid(err),
          Valid: (r) => f(r));

    // Traversable
    //public static Exceptional<Validation<R>> Traverse<T, R>(this Validation<T> valT, Func<T, Exceptional<R>> f)
    //{
    //    return valT.Match
    //    (
    //        Invalid: errs => Exceptional(Invalid<R>(errs)),
    //        Valid: t => f(t).Map(Valid)
    //    );
    //}

    // LINQ

    public static Validation<R> Select<T, R>(this Validation<T> @this
      , Func<T, R> map) => @this.Map(map);

   public static Validation<RR> SelectMany<T, R, RR>(this Validation<T> @this
      , Func<T, Validation<R>> bind, Func<T, R, RR> project)
      => @this.Match(
         Invalid: (err) => Invalid(err),
         Valid: (t) => bind(t).Match(
            Invalid: (err) => Invalid(err),
            Valid: (r) => Valid(project(t, r))));
}
