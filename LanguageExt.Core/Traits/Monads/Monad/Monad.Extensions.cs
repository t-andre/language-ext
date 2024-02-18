﻿using System;

namespace LanguageExt.Traits;

/// <summary>
/// Monad module
/// </summary>
public static partial class Monad
{
    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="f">Monadic bind function</param>
    /// <typeparam name="A">Initial bound value type</typeparam>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <returns>M<B></returns>
    public static K<M, B> Bind<M, A, B>(
        this K<M, A> ma,
        Func<A, K<M, B>> f)
        where M : Monad<M> =>
        M.Bind(ma, f);
    
    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="A">Initial bound value type</typeparam>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>M<C></returns>
    public static K<M, C> SelectMany<M, A, B, C>(
        this K<M, A> ma,
        Func<A, K<M, B>> bind,
        Func<A, B, C> project)
        where M : Monad<M> =>
        M.Bind(ma, a => M.Map(b => project(a, b) , bind(a)));

    /// <summary>
    /// Monadic join operation
    /// </summary>
    /// <param name="mma"></param>
    /// <typeparam name="M">Monad trait</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>Joined monad</returns>
    public static K<M, A> Flatten<M, A>(this K<M, K<M, A>> mma)
        where M : Monad<M> =>
        M.Bind(mma, Prelude.identity);
}
