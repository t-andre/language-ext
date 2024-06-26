﻿using System;
using System.Diagnostics.Contracts;
using LanguageExt.Traits;

namespace LanguageExt;

public static partial class Prelude
{
    [Pure]
    public static NUMTYPE bind<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value, Func<T, NUMTYPE> bind)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        value.Bind(bind);

    public static Unit iter<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value, Action<T> f)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        value.Iter(f);

    [Pure]
    public static int count<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        1;

    [Pure]
    public static bool exists<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value, Func<T, bool> predicate)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        predicate((T)value);

    [Pure]
    public static bool forall<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value, Func<T, bool> predicate)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        predicate((T)value);

    [Pure]
    public static NUMTYPE map<NUMTYPE, NUM, T, PRED>(NumType<NUMTYPE, NUM, T, PRED> value, Func<T, T> map)
        where NUM     : Num<T>
        where PRED    : Pred<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T, PRED> =>
        value.Map(map);

    /// <summary>
    /// Add the bound values of x and y, uses an Add trait to provide the add
    /// operation for type A.  For example x.Add<Metres, TInt, int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>An NewType with y added to x</returns>
    [Pure]
    public static NUMTYPE add<NUMTYPE, NUM, A, PRED>(NumType<NUMTYPE, NUM, A, PRED> x, NUMTYPE y)
        where NUM     : Num<A>
        where PRED    : Pred<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A, PRED> =>
        from a in x
        from b in y
        select NUM.Add(a, b);

    /// <summary>
    /// Find the subtract between the two bound values of x and y, uses a Subtract trait 
    /// to provide the subtract operation for type A.  For example x.Subtract<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>An NewType with the subtract between x and y</returns>
    [Pure]
    public static NUMTYPE subtract<NUMTYPE, NUM, A, PRED>(NumType<NUMTYPE, NUM, A, PRED> x, NUMTYPE y)
        where NUM     : Num<A>
        where PRED    : Pred<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A, PRED> =>
        from a in x
        from b in y
        select NUM.Subtract(a, b);

    /// <summary>
    /// Find the product between the two bound values of x and y, uses a Product trait 
    /// to provide the product operation for type A.  For example x.Product<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>Product of x and y</returns>
    [Pure]
    public static NUMTYPE product<NUMTYPE, NUM, A, PRED>(NumType<NUMTYPE, NUM, A, PRED> x, NUMTYPE y)
        where NUM     : Num<A>
        where PRED    : Pred<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A, PRED> =>
        from a in x
        from b in y
        select NUM.Multiply(a, b);

    /// <summary>
    /// Find the product between the two bound values of x and y, uses a Product trait 
    /// to provide the product operation for type A.  For example x.Product<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>Product of x and y</returns>
    [Pure]
    public static NUMTYPE divide<NUMTYPE, NUM, A, PRED>(NumType<NUMTYPE, NUM, A, PRED> x, NUMTYPE y)
        where NUM     : Num<A>
        where PRED : Pred<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A, PRED> =>
        from a in x
        from b in y
        select NUM.Divide(a, b);

    public static A sum<NUMTYPE, NUM, A, PRED>(NumType<NUMTYPE, NUM, A, PRED> self)
        where NUMTYPE : NumType<NUMTYPE, NUM, A, PRED>
        where PRED : Pred<A>
        where NUM     : Num<A> =>
        (A)self;


    [Pure]
    public static NUMTYPE bind<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value, Func<T, NUMTYPE> bind)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        value.Bind(bind);

    public static Unit iter<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value, Action<T> f)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        value.Iter(f);

    [Pure]
    public static int count<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        1;

    [Pure]
    public static bool exists<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value, Func<T, bool> predicate)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        predicate((T)value);

    [Pure]
    public static bool forall<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value, Func<T, bool> predicate)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        predicate((T)value);

    [Pure]
    public static NUMTYPE map<NUMTYPE, NUM, T>(NumType<NUMTYPE, NUM, T> value, Func<T, T> map)
        where NUM : Num<T>
        where NUMTYPE : NumType<NUMTYPE, NUM, T> =>
        value.Map(map);

    /// <summary>
    /// Add the bound values of x and y, uses an Add trait to provide the add
    /// operation for type A.  For example x.Add<Metres, TInt, int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>An NewType with y added to x</returns>
    [Pure]
    public static NUMTYPE add<NUMTYPE, NUM, A>(NumType<NUMTYPE, NUM, A> x, NUMTYPE y)
        where NUM : Num<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A> =>
        from a in x
        from b in y
        select NUM.Add(a, b);

    /// <summary>
    /// Find the subtract between the two bound values of x and y, uses a Subtract trait 
    /// to provide the subtract operation for type A.  For example x.Subtract<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>An NewType with the subtract between x and y</returns>
    [Pure]
    public static NUMTYPE subtract<NUMTYPE, NUM, A>(NumType<NUMTYPE, NUM, A> x, NUMTYPE y)
        where NUM : Num<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A> =>
        from a in x
        from b in y
        select NUM.Subtract(a, b);

    /// <summary>
    /// Find the product between the two bound values of x and y, uses a Product trait 
    /// to provide the product operation for type A.  For example x.Product<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>Product of x and y</returns>
    [Pure]
    public static NUMTYPE product<NUMTYPE, NUM, A>(NumType<NUMTYPE, NUM, A> x, NUMTYPE y)
        where NUM     : Num<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A> =>
        from a in x
        from b in y
        select NUM.Multiply(a, b);

    /// <summary>
    /// Find the product between the two bound values of x and y, uses a Product trait 
    /// to provide the product operation for type A.  For example x.Product<TInteger,int>(y)
    /// </summary>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <param name="x">Left hand side of the operation</param>
    /// <param name="y">Right hand side of the operation</param>
    /// <returns>Product of x and y</returns>
    [Pure]
    public static NUMTYPE divide<NUMTYPE, NUM, A>(NumType<NUMTYPE, NUM, A> x, NUMTYPE y)
        where NUM     : Num<A>
        where NUMTYPE : NumType<NUMTYPE, NUM, A> =>
        from a in x
        from b in y
        select NUM.Divide(a, b);

    public static A sum<NUMTYPE, NUM, A>(NumType<NUMTYPE, NUM, A> self)
        where NUMTYPE : NumType<NUMTYPE, NUM, A>
        where NUM     : Num<A> =>
        (A)self;

}
