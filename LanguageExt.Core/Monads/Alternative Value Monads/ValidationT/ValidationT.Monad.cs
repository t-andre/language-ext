﻿using System;
using LanguageExt.Traits;

namespace LanguageExt;

/// <summary>
/// Trait implementation for `ValidationT` 
/// </summary>
/// <typeparam name="M">Given monad trait</typeparam>
public partial class ValidationT<F, M> : 
    MonadT<ValidationT<F, M>, M>, 
    Fallible<F, ValidationT<F, M>>, 
    Alternative<ValidationT<F, M>>
    where M : Monad<M>
    where F : Monoid<F>
{
    static K<ValidationT<F, M>, B> Monad<ValidationT<F, M>>.Bind<A, B>(
        K<ValidationT<F, M>, A> ma, 
        Func<A, K<ValidationT<F, M>, B>> f) => 
        ma.As().Bind(f);

    static K<ValidationT<F, M>, B> Functor<ValidationT<F, M>>.Map<A, B>(
        Func<A, B> f, 
        K<ValidationT<F, M>, A> ma) => 
        ma.As().Map(f);

    static K<ValidationT<F, M>, A> Applicative<ValidationT<F, M>>.Pure<A>(A value) => 
        ValidationT<F, M, A>.Success(value);

    static K<ValidationT<F, M>, B> Applicative<ValidationT<F, M>>.Apply<A, B>(
        K<ValidationT<F, M>, Func<A, B>> mf,
        K<ValidationT<F, M>, A> ma) =>
        mf.As().Apply(ma.As());

    static K<ValidationT<F, M>, B> Applicative<ValidationT<F, M>>.Action<A, B>(
        K<ValidationT<F, M>, A> ma, 
        K<ValidationT<F, M>, B> mb) =>
        Prelude.fun((A _, B b) => b).Map(ma).Apply(mb).As();

    static K<ValidationT<F, M>, A> MonadT<ValidationT<F, M>, M>.Lift<A>(K<M, A> ma) => 
        ValidationT<F, M, A>.Lift(ma);
            
    static K<ValidationT<F, M>, A> Monad<ValidationT<F, M>>.LiftIO<A>(IO<A> ma) => 
        ValidationT<F, M, A>.Lift(M.LiftIO(ma));

    static K<ValidationT<F, M>, A> MonoidK<ValidationT<F, M>>.Empty<A>() =>
        ValidationT<F, M, A>.Fail(F.Empty);

    static K<ValidationT<F, M>, A> SemigroupK<ValidationT<F, M>>.Combine<A>(
        K<ValidationT<F, M>, A> ma,
        K<ValidationT<F, M>, A> mb) =>
        ma.As() | mb.As();

    static K<ValidationT<F, M>, A> Fallible<F, ValidationT<F, M>>.Fail<A>(F error) => 
        ValidationT<F, M, A>.Fail(error);

    static K<ValidationT<F, M>, A> Fallible<F, ValidationT<F, M>>.Catch<A>(
        K<ValidationT<F, M>, A> fa,
        Func<F, bool> Predicate,
        Func<F, K<ValidationT<F, M>, A>> Fail) =>
        fa.As().BindFail(e => Predicate(e) ? Fail(e).As() : ValidationT<F, M, A>.Fail(e));
}
