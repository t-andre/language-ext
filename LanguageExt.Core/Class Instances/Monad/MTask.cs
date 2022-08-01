﻿using System;
using LanguageExt.TypeClasses;
using System.Diagnostics.Contracts;
using static LanguageExt.Prelude;
using System.Threading.Tasks;

namespace LanguageExt.ClassInstances
{
    /// <summary>
    /// Class instance to give `Task<A>` the following traits: 
    ///     
    ///     MonadAsync
    ///     FoldableAsync
    ///     BiFoldableAsync
    ///     OptionalAsymc
    ///     OptionalUnsafeAsync
    /// </summary>
    /// <remarks>
    /// The `Plus` function will return `ma` if successful, `mb` otherwise
    /// </remarks>
    /// <typeparam name="A">Bound value type</typeparam>
    public struct MTask<A> :
        OptionalAsync<Task<A>, A>,
        OptionalUnsafeAsync<Task<A>, A>,
        MonadAsync<Task<A>, A>,
        FoldableAsync<Task<A>, A>,
        BiFoldableAsync<Task<A>, A, Unit>
    {
        public static readonly MTask<A> Inst = default(MTask<A>);

        [Pure]
        public Task<A> None =>
            default(MTaskFirst<A>).None;

        [Pure]
        public MB Bind<MONADB, MB, B>(Task<A> ma, Func<A, MB> f) where MONADB : struct, MonadAsync<Unit, Unit, MB, B> =>
            default(MTaskFirst<A>).Bind<MONADB, MB, B>(ma, f);

        [Pure]
        public MB BindAsync<MONADB, MB, B>(Task<A> ma, Func<A, Task<MB>> f) where MONADB : struct, MonadAsync<Unit, Unit, MB, B> =>
            default(MTaskFirst<A>).BindAsync<MONADB, MB, B>(ma, f);

        [Pure]
        public Task<A> Fail(object err = null) =>
            default(MTaskFirst<A>).Fail(err);

        /// <summary>
        /// The `Plus` function will return `ma` if successful, `mb` otherwise
        /// </summary>
        [Pure]
        public async Task<A> Plus(Task<A> ma, Task<A> mb)
        {
            try
            {
                return await ma.ConfigureAwait(false);
            }
            catch
            {
                return await mb.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Monad return
        /// </summary>
        /// <typeparam name="A">Type of the bound monad value</typeparam>
        /// <param name="x">The bound monad value</param>
        /// <returns>Monad of A</returns>
        [Pure]
        public Task<A> PureAsync(Task<A> x) =>
            default(MTaskFirst<A>).PureAsync(x);

        /// <summary>
        /// Monad return
        /// </summary>
        /// <typeparam name="A">Type of the bound monad value</typeparam>
        /// <returns>Monad of A</returns>
        [Pure]
        public Task<A> LiftAsync(Func<Unit, Task<A>> f) =>
            default(MTaskFirst<A>).LiftAsync(f);

        [Pure]
        public Task<A> Zero() =>
            default(MTaskFirst<A>).Zero();

        [Pure]
        public Task<bool> IsNone(Task<A> ma) =>
            default(MTaskFirst<A>).IsNone(ma);

        [Pure]
        public Task<bool> IsSome(Task<A> ma) =>
            default(MTaskFirst<A>).IsSome(ma);

        [Pure]
        public Task<B> Match<B>(Task<A> ma, Func<A, B> Some, Func<B> None) =>
            default(MTaskFirst<A>).Match(ma, Some, None);

        [Pure]
        public Task<A> Some(A value) =>
            default(MTaskFirst<A>).Some(value);

        [Pure]
        public Task<A> Optional(A value) =>
            default(MTaskFirst<A>).Optional(value);

        [Pure]
        public Task<A> BindReturn(Unit _, Task<A> mb) =>
            default(MTaskFirst<A>).BindReturn(_, mb);

        [Pure]
        public Task<A> RunAsync(Func<Unit, Task<Task<A>>> ma) =>
            default(MTaskFirst<A>).RunAsync(ma);

        [Pure]
        public Func<Unit, Task<S>> Fold<S>(Task<A> fa, S state, Func<S, A, S> f) => 
            default(MTaskFirst<A>).Fold(fa, state, f);

        [Pure]
        public Func<Unit, Task<S>> FoldAsync<S>(Task<A> fa, S state, Func<S, A, Task<S>> f) =>
            default(MTaskFirst<A>).FoldAsync(fa, state, f);

        [Pure]
        public Func<Unit, Task<S>> FoldBack<S>(Task<A> fa, S state, Func<S, A, S> f) =>
            default(MTaskFirst<A>).FoldBack(fa, state, f);

        [Pure]
        public Func<Unit, Task<S>> FoldBackAsync<S>(Task<A> fa, S state, Func<S, A, Task<S>> f) =>
            default(MTaskFirst<A>).FoldBackAsync(fa, state, f);

        [Pure]
        public Func<Unit, Task<int>> Count(Task<A> fa) =>
            default(MTaskFirst<A>).Count(fa);

        [Pure]
        public Task<A> Apply(Func<A, A, A> f, Task<A> fa, Task<A> fb) =>
            default(MTaskFirst<A>).Apply(f, fa, fb);

        [Pure]
        public Task<B> MatchAsync<B>(Task<A> ma, Func<A, Task<B>> SomeAsync, Func<B> None) =>
            default(MTaskFirst<A>).MatchAsync(ma, SomeAsync, None);

        [Pure]
        public Task<B> MatchAsync<B>(Task<A> ma, Func<A, B> Some, Func<Task<B>> NoneAsync) =>
            default(MTaskFirst<A>).MatchAsync(ma, Some, NoneAsync);

        [Pure]
        public Task<B> MatchAsync<B>(Task<A> ma, Func<A, Task<B>> SomeAsync, Func<Task<B>> NoneAsync) =>
            default(MTaskFirst<A>).MatchAsync(ma, SomeAsync, NoneAsync);

        [Pure]
        public Task<B> MatchUnsafe<B>(Task<A> ma, Func<A, B> Some, Func<B> None) =>
            default(MTaskFirst<A>).MatchUnsafe(ma, Some, None);

        [Pure]
        public Task<B> MatchUnsafeAsync<B>(Task<A> ma, Func<A, Task<B>> SomeAsync, Func<B> None) =>
            default(MTaskFirst<A>).MatchUnsafeAsync(ma, SomeAsync, None);

        [Pure]
        public Task<B> MatchUnsafeAsync<B>(Task<A> ma, Func<A, B> Some, Func<Task<B>> NoneAsync) =>
            default(MTaskFirst<A>).MatchUnsafeAsync(ma, Some, NoneAsync);

        [Pure]
        public Task<B> MatchUnsafeAsync<B>(Task<A> ma, Func<A, Task<B>> SomeAsync, Func<Task<B>> NoneAsync) =>
            default(MTaskFirst<A>).MatchUnsafeAsync(ma, SomeAsync, NoneAsync);

        [Pure]
        public Task<Unit> Match(Task<A> ma, Action<A> Some, Action None) =>
            default(MTaskFirst<A>).Match(ma, Some, None);

        [Pure]
        public Task<Unit> MatchAsync(Task<A> ma, Func<A, Task> SomeAsync, Action None) =>
            default(MTaskFirst<A>).MatchAsync(ma, SomeAsync, None);

        [Pure]
        public Task<Unit> MatchAsync(Task<A> ma, Action<A> SomeAsync, Func<Task> NoneAsync) =>
            default(MTaskFirst<A>).MatchAsync(ma, SomeAsync, NoneAsync);

        [Pure]
        public Task<Unit> MatchAsync(Task<A> ma, Func<A, Task> SomeAsync, Func<Task> NoneAsync) =>
            default(MTaskFirst<A>).MatchAsync(ma, SomeAsync, NoneAsync);

        [Pure]
        public Task<A> SomeAsync(Task<A> value) =>
            default(MTaskFirst<A>).SomeAsync(value);

        [Pure]
        public Task<A> OptionalAsync(Task<A> value) =>
            default(MTaskFirst<A>).OptionalAsync(value);

        [Pure]
        public Task<S> BiFold<S>(Task<A> ma, S state, Func<S, A, S> Some, Func<S, Unit, S> None) =>
            default(MTaskFirst<A>).BiFold(ma, state, Some, None);

        [Pure]
        public Task<S> BiFoldAsync<S>(Task<A> ma, S state, Func<S, A, Task<S>> SomeAsync, Func<S, Unit, S> None) =>
            default(MTaskFirst<A>).BiFoldAsync(ma, state, SomeAsync, None);

        [Pure]
        public Task<S> BiFoldAsync<S>(Task<A> ma, S state, Func<S, A, S> Some, Func<S, Unit, Task<S>> NoneAsync) =>
            default(MTaskFirst<A>).BiFoldAsync(ma, state, Some, NoneAsync);

        [Pure]
        public Task<S> BiFoldAsync<S>(Task<A> ma, S state, Func<S, A, Task<S>> SomeAsync, Func<S, Unit, Task<S>> NoneAsync) =>
            default(MTaskFirst<A>).BiFoldAsync(ma, state, SomeAsync, NoneAsync);

        [Pure]
        public Task<S> BiFoldBack<S>(Task<A> ma, S state, Func<S, A, S> Some, Func<S, Unit, S> None) =>
            default(MTaskFirst<A>).BiFoldBack(ma, state, Some, None);

        [Pure]
        public Task<S> BiFoldBackAsync<S>(Task<A> ma, S state, Func<S, A, Task<S>> SomeAsync, Func<S, Unit, S> None) =>
            default(MTaskFirst<A>).BiFoldBackAsync(ma, state, SomeAsync, None);

        [Pure]
        public Task<S> BiFoldBackAsync<S>(Task<A> ma, S state, Func<S, A, S> Some, Func<S, Unit, Task<S>> NoneAsync) =>
            default(MTaskFirst<A>).BiFoldBackAsync(ma, state, Some, NoneAsync);

        [Pure]
        public Task<S> BiFoldBackAsync<S>(Task<A> ma, S state, Func<S, A, Task<S>> SomeAsync, Func<S, Unit, Task<S>> NoneAsync) =>
            default(MTaskFirst<A>).BiFoldBackAsync(ma, state, SomeAsync, NoneAsync);
    }
}
