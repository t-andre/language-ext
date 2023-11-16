#nullable enable
using System;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LanguageExt.Effects;
using LanguageExt.Effects.Traits;
using LanguageExt.Transducers;

namespace LanguageExt
{
    /// <summary>
    /// Transducer based IO monad
    /// </summary>
    /// <typeparam name="RT">Runtime struct</typeparam>
    /// <typeparam name="E">Error value type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    public readonly struct IO<RT, E, A> : Transducer<RT, Sum<E, A>>
        where RT : struct, HasIO<RT, E>
    {
        /// <summary>
        /// Cached mapping of errors to a valid output for this type 
        /// </summary>
        static readonly Func<Error, Either<E, A>> errorMap = 
            e => default(RT).FromError(e); 
        
        /// <summary>
        /// Underlying transducer that captures all of the IO behaviour 
        /// </summary>
        readonly Transducer<RT, Sum<E, A>> thunk;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Constructors
        //
        
        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        internal IO(Transducer<RT, Sum<E, A>> thunk) =>
            this.thunk = thunk;

        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        IO(Func<RT, Sum<E, A>> thunk) =>
            this.thunk = Transducer.lift(thunk);

        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        IO(Func<RT, A> thunk) 
            : this(rt => Sum<E, A>.Right(thunk(rt)))
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        IO(Transducer<RT, A> thunk) 
            : this(Transducer.compose(thunk, Transducer.lift<A, Sum<E, A>>(x => Sum<E, A>.Right(x))))
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        IO(Func<RT, Either<E, A>> thunk) 
            : this(rt => thunk(rt).ToSum())
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        [MethodImpl(Opt.Default)]
        IO(Transducer<RT, Either<E, A>> thunk) 
            : this(Transducer.compose(thunk, Transducer.lift<Either<E, A>, Sum<E, A>>(x => x.ToSum())))
        { }
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Transducer
        //

        /// <summary>
        /// Access to the underlying transducer
        /// </summary>
        public Transducer<RT, Sum<E, A>> Morphism =>
            thunk ?? Transducer.Fail<RT, Sum<E, A>>(Errors.Bottom);
        
        /// <summary>
        /// Reduction of the underlying transducer
        /// </summary>
        /// <param name="reduce">Reducer </param>
        /// <typeparam name="S"></typeparam>
        /// <returns></returns>
        public Reducer<RT, S> Transform<S>(Reducer<Sum<E, A>, S> reduce) => 
            Morphism.Transform(reduce);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Invoking
        //
        
        /// <summary>
        /// All IO monads will catch exceptions at their point of invocation (i.e. in the `Run*` methods).
        /// But they do not catch exceptions elsewhere without explicitly using this `Try()` method.
        ///
        /// This wraps the `IO` monad in a try/catch that converts exceptional errors to an `E` and 
        /// therefore puts the `IO` in a `Fail` state the expression that can then be matched upon.
        /// </summary>
        /// <remarks>
        /// Useful when you want exceptions to be dealt with through matching/bi-mapping/etc.
        /// and not merely be caught be the exception handler in `Run*`.
        /// </remarks>
        /// <remarks>
        /// This is used automatically for the first argument in the coalescing `|` operator so that any
        /// exceptional failures, in the first argument, allow the second argument to be invoked. 
        /// </remarks>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> Try() =>
            new(Transducer.@try(Morphism, _ => true, Transducer.mkLeft<E, A>()));

        /// <summary>
        /// Invoke the effect
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Either<E, A> Run(RT env) =>
            Morphism.Invoke1(env, env.CancellationToken)
                    .ToEither(errorMap);

        /// <summary>
        /// Invoke the effect (which could produce multiple values).  Run a reducer for
        /// each value yielded.
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Fin<S> RunMany<S>(RT env, S initialState, Func<S, Either<E, A>, TResult<S>> reducer) =>
            Morphism.Invoke(
                        env,
                        initialState,
                        Reducer.from<Sum<E, A>, S>(
                            (_, s, sv) => sv switch
                            {
                                SumRight<E, A> r => reducer(s, Either<E, A>.Right(r.Value)),
                                SumLeft<E, A> l => reducer(s, Either<E, A>.Left(l.Value)),
                                _ => TResult.Complete(s)
                            }),
                        env.CancellationToken)
                    .ToFin();

        /// <summary>
        /// Invoke the effect (which could produce multiple values).  Collect those results in a `Seq`
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Either<E, Seq<A>> RunMany(RT env) =>
            RunMany(env,
                    Either<E, Seq<A>>.Right(Seq<A>()),
                    (s, v) =>
                        (s.IsRight, v.IsRight) switch
                        {
                            (true, true) => TResult.Continue(Either<E, Seq<A>>.Right(((Seq<A>)s).Add((A)v))),
                            (true, false) => TResult.Complete(Either<E, Seq<A>>.Left((E)v)),
                            _ => TResult.Complete(s),
                        })
                .Match(
                    Succ: v => v,
                    Fail: e => default(RT).FromError(e));

        /// <summary>
        /// Invoke the effect asynchronously
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Task<Either<E, A>> RunAsync(RT env) =>
            Morphism.Invoke1Async(env, env.CancellationToken)
                    .Map(r => r.ToEither(errorMap));

        /// <summary>
        /// Invoke the effect (which could produce multiple values).  Run a reducer for
        /// each value yielded.
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Task<Fin<S>> RunManyAsync<S>(RT env, S initialState, Func<S, Either<E, A>, TResult<S>> reducer) =>
            Morphism.InvokeAsync(
                        env,
                        initialState,
                        Reducer.from<Sum<E, A>, S>(
                            (_, s, sv) => sv switch
                            {
                                SumRight<E, A> r => reducer(s, Either<E, A>.Right(r.Value)),
                                SumLeft<E, A> l => reducer(s, Either<E, A>.Left(l.Value)),
                                _ => TResult.Complete(s)
                            }),
                        env.CancellationToken)
                    .Map(r => r.ToFin());

        /// <summary>
        /// Invoke the effect (which could produce multiple values).  Collect those results in a `Seq`
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public Task<Either<E, Seq<A>>> RunManyAsync(RT env) =>
            RunManyAsync(env,
                    Either<E, Seq<A>>.Right(Seq<A>()),
                    (s, v) =>
                        (s.IsRight, v.IsRight) switch
                        {
                            (true, true) => TResult.Continue(Either<E, Seq<A>>.Right(((Seq<A>)s).Add((A)v))),
                            (true, false) => TResult.Complete(Either<E, Seq<A>>.Left((E)v)),
                            _ => TResult.Complete(s)
                        })
                .Map(r => r.Match(
                    Succ: v => v,
                    Fail: e => default(RT).FromError(e)));
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Lifting
        //
        
        /// <summary>
        /// Lift a value into the IO monad 
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Pure(A value) =>
            new (_ => Sum<E, A>.Right(value));

        /// <summary>
        /// Lift a failure into the IO monad 
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Fail(E error) =>
            new (_ => Sum<E, A>.Left(error));

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Func<RT, Either<E, A>> f) =>
            new (f);

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Transducer<RT, Either<E, A>> f) =>
            new (f);

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Func<RT, Sum<E, A>> f) =>
            new (f);

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Transducer<RT, Sum<E, A>> f) =>
            new (f);

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Func<RT, A> f) =>
            new (f);

        /// <summary>
        /// Lift a synchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> Lift(Transducer<RT, A> f) =>
            new (f);

        /// <summary>
        /// Lift a asynchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> LiftIO(Func<RT, Task<A>> f) =>
            new (Transducer.liftIO<RT, Sum<E, A>>(
                async (_, rt) => Sum<E, A>.Right(await f(rt).ConfigureAwait(false))));

        /// <summary>
        /// Lift a asynchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> LiftIO(Func<RT, Task<Sum<E, A>>> f) =>
            new(Transducer.liftIO<RT, Sum<E, A>>(
                async (_, rt) => await f(rt).ConfigureAwait(false)));

        /// <summary>
        /// Lift a asynchronous effect into the IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> LiftIO(Func<RT, Task<Either<E, A>>> f) =>
            new (Transducer.liftIO<RT, Sum<E, A>>(
                async (_, rt) => (await f(rt).ConfigureAwait(false)).ToSum()));

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Memoisation and tail calls
        //
        
        /// <summary>
        /// Memoise the result, so subsequent calls don't invoke the side-IOect
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> Memo() =>
            new(Transducer.memo(Morphism));

        /// <summary>
        /// Wrap this around the final `from` call in a `IO` LINQ expression to void a recursion induced space-leak.
        /// </summary>
        /// <example>
        /// 
        ///     IO<RT, E, A> recursive(int x) =>
        ///         from x in writeLine(x)
        ///         from r in tail(recursive(x + 1))
        ///         select r;      <--- this never runs
        /// 
        /// </example>
        /// <remarks>
        /// This means the result of the LINQ expression comes from the final `from`, _not_ the `select.  If the
        /// type of the `final` from differs from the type of the `select` then this has no effect.
        /// </remarks>
        /// <remarks>
        /// Background: When making recursive LINQ expressions, the final `select` is problematic because it means
        /// there's code to run _after_ the final `from` expression.  This means there's you're guaranteed to have a
        /// space-leak due to the need to hold thunks to the final `select` on every recursive step.
        ///
        /// This function ignores the `select` altogether and says that the final `from` is where we get our return
        /// result from and therefore there's no need to hold the thunk. 
        /// </remarks>
        /// <returns>IO operation that's marked ready for tail recursion</returns>        
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> Tail() =>
            new(Transducer.tail(Morphism));
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // 
        // Forking
        //

        /// <summary>
        /// Queue this IO operation to run on the thread-pool. 
        /// </summary>
        /// <param name="timeout">Maximum time that the forked IO operation can run for. `None` for no timeout.</param>
        /// <returns>Returns a `ForkIO` data-structure that contains two IO effects that can be used to either cancel
        /// the forked IO operation or to await the result of it.
        /// </returns>
        [MethodImpl(Opt.Default)]
        public IO<RT, E, ForkIO<RT, E, A>> Fork(Option<TimeSpan> timeout = default) =>
            new(Transducer.fork(Morphism, timeout).Map(TFork.ToIO<RT, E, A>));
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Map and map-left
        //

        /// <summary>
        /// Maps the IO monad if it's in a success state
        /// </summary>
        /// <param name="f">Function to map the success value with</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, B> Map<B>(Func<A, B> f) =>
            new(Transducer.mapRight(Morphism, f));

        /// <summary>
        /// Maps the IO monad if it's in a success state
        /// </summary>
        /// <param name="f">Function to map the success value with</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, B> Select<B>(Func<A, B> f) =>
            new(Transducer.mapRight(Morphism, f));

        /// <summary>
        /// Maps the IO monad if it's in a success state
        /// </summary>
        /// <param name="f">Function to map the success value with</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, B> Map<B>(Transducer<A, B> f) =>
            new(Transducer.mapRight(Morphism, f));

        /// <summary>
        /// Maps the IO monad if it's in a success state
        /// </summary>
        /// <param name="f">Function to map the success value with</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public  IO<RT, E, A> MapFail(Func<E, E> f) =>
            new(Transducer.mapLeft(Morphism, f));

        /// <summary>
        /// Maps the IO monad if it's in a success state
        /// </summary>
        /// <param name="f">Function to map the success value with</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public  IO<RT, E, A> MapFail(Transducer<E, E> f) =>
            new(Transducer.mapLeft(Morphism, f));

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Bi-map
        //

        /// <summary>
        /// Mapping of either the Success state or the Failure state depending on what
        /// state this IO monad is in.  
        /// </summary>
        /// <param name="Succ">Mapping to use if the IO monad is in a success state</param>
        /// <param name="Fail">Mapping to use if the IO monad is in a failure state</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, B> BiMap<B>(Func<A, B> Succ, Func<E, E> Fail) =>
            new(Transducer.bimap(Morphism, Fail, Succ));

        /// <summary>
        /// Mapping of either the Success state or the Failure state depending on what
        /// state this IO monad is in.  
        /// </summary>
        /// <param name="Succ">Mapping to use if the IO monad is in a success state</param>
        /// <param name="Fail">Mapping to use if the IO monad is in a failure state</param>
        /// <returns>Mapped IO monad</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, B> BiMap<B>(Transducer<A, B> Succ, Transducer<E, E> Fail) =>
            new(Transducer.bimap(Morphism, Fail, Succ));

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Matching
        //

        /// <summary>
        /// Pattern match the success or failure values and collapse them down to a success value
        /// </summary>
        /// <param name="Succ">Success value mapping</param>
        /// <param name="Fail">Failure value mapping</param>
        /// <returns>IO in a success state</returns>
        [Pure]
        public IO<RT, E, B> Match<B>(Func<A, B> Succ, Func<E, B> Fail) =>
            Match(Transducer.lift(Succ), Transducer.lift(Fail));

        /// <summary>
        /// Pattern match the success or failure values and collapse them down to a success value
        /// </summary>
        /// <param name="Succ">Success value mapping</param>
        /// <param name="Fail">Failure value mapping</param>
        /// <returns>IO in a success state</returns>
        [Pure]
        public IO<RT, E, B> Match<B>(Transducer<A, B> Succ, Transducer<E, B> Fail) =>
            new(Transducer.compose(
                    Transducer.bimap(Morphism, Fail, Succ),
                    Transducer.lift<Sum<B, B>, Sum<E, B>>(s => s switch
                    {
                        SumRight<B, B> r => Sum<E, B>.Right(r.Value),
                        SumLeft<B, B> l => Sum<E, B>.Right(l.Value),
                        _ => throw new BottomException()
                    })));

        /// <summary>
        /// Map the failure to a success value
        /// </summary>
        /// <param name="f">Function to map the fail value</param>
        /// <returns>IO in a success state</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> IfFail(Func<E, A> Fail) =>
            IfFail(Transducer.lift(Fail));

        /// <summary>
        /// Map the failure to a success value
        /// </summary>
        /// <param name="f">Function to map the fail value</param>
        /// <returns>IO in a success state</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> IfFail(Transducer<E, A> Fail) =>
            Match(Transducer.identity<A>(), Fail);

        /// <summary>
        /// Map the failure to a success value
        /// </summary>
        /// <param name="f">Function to map the fail value</param>
        /// <returns>IO in a success state</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> IfFail(A Fail) =>
            Match(Transducer.identity<A>(), Transducer.constant<E, A>(Fail));

        /// <summary>
        /// Map the failure to a new IO effect
        /// </summary>
        /// <param name="f">Function to map the fail value</param>
        /// <returns>IO that encapsulates that IfFail</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> IfFailIO(Func<E, IO<RT, E, A>> Fail) =>
            new(Transducer.bimap(
                    Morphism,
                    e => Fail(e).Morphism,
                    x => Transducer.constant<RT, Sum<E, A>>(Sum<E, A>.Right(x)))
                .Flatten());

        /// <summary>
        /// Map the failure to a new IO effect
        /// </summary>
        /// <param name="f">Function to map the fail value</param>
        /// <returns>IO that encapsulates that IfFail</returns>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, A> IfFailIO(IO<RT, E, A> Fail) =>
            IfFailIO(_ => Fail);
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //  Filter
        //

        /// <summary>
        /// Only allow values through the effect if the predicate returns `true` for the bound value
        /// </summary>
        /// <param name="predicate">Predicate to apply to the bound value></param>
        /// <returns>Filtered IO</returns>
        public IO<RT, E, A> Filter(Func<A, bool> predicate) =>
            Filter(Transducer.lift(predicate));

        /// <summary>
        /// Only allow values through the effect if the predicate returns `true` for the bound value
        /// </summary>
        /// <param name="predicate">Predicate to apply to the bound value></param>
        /// <returns>Filtered IO</returns>
        public IO<RT, E, A> Filter(Transducer<A, bool> predicate) =>
            new(Transducer.filter(Morphism, predicate));

        /// <summary>
        /// Only allow values through the effect if the predicate returns `true` for the bound value
        /// </summary>
        /// <param name="predicate">Predicate to apply to the bound value></param>
        /// <returns>Filtered IO</returns>
        public IO<RT, E, A> Where(Func<A, bool> predicate) =>
            Filter(Transducer.lift(predicate));

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //  Monadic binding
        //

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, B> Bind<B>(Func<A, IO<RT, E, B>> f) =>
            new(Transducer.bind(Morphism, x => f(x).Morphism));

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// transducer provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the transducer provided</returns>
        public IO<RT, E, B> Bind<B>(Transducer<A, IO<RT, E, B>> f) =>
            Map(f).Flatten();

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, B> Bind<B>(Func<A, Pure<B>> f) =>
            Bind(x => f(x).ToIO<RT, E>());

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, A> Bind(Func<A, Fail<E>> f) =>
            Bind(x => f(x).ToIO<RT, A>());

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, B> Bind<B>(Func<A, Use<B>> f) =>
            Bind(x => f(x).ToIO<RT, E>());

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, Unit> Bind<B>(Func<A, Release<B>> f) =>
            Bind(x => f(x).ToIO<RT, E>());

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, B> Bind<B>(Func<A, LiftIO<B>> f) =>
            Bind(x => f(x).ToIO<RT, E>());

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="f">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, B> Bind<B>(Func<A, Many<B>> f) =>
            Bind(x => f(x).ToIO<RT, E>());

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //  Monadic binding and projection
        //

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, IO<RT, E, B>> bind, Func<A, B, C> project) =>
            new(Transducer.selectMany(Morphism, x => bind(x).Morphism, project));

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, Pure<B>> bind, Func<A, B, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, E>(), project);

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, Fail<E>> bind, Func<A, B, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, B>(), project);

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, Use<B>> bind, Func<A, B, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, E>(), project);

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, Release<B>> bind, Func<A, Unit, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, E>(), project);

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, LiftIO<B>> bind, Func<A, B, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, E>(), project);

        /// <summary>
        /// Monadic bind operation.  This runs the current IO monad and feeds its result to the
        /// function provided; which in turn returns a new IO monad.  This can be thought of as
        /// chaining IO operations sequentially.
        /// </summary>
        /// <param name="bind">Bind operation</param>
        /// <returns>Composition of this monad and the result of the function provided</returns>
        public IO<RT, E, C> SelectMany<B, C>(Func<A, Many<B>> bind, Func<A, B, C> project) =>
            SelectMany(x => bind(x).ToIO<RT, E>(), project);
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Folding
        //

        /// <summary>
        /// Fold the effect
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public IO<RT, E, S> Fold<S>(S initialState, Func<S, A, S> folder) =>
            new(Transducer.fold(Morphism, initialState, folder));
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Operators
        //

        /// <summary>
        /// Convert to an IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static implicit operator IO<RT, E, A>(Use<A> ma) =>
            ma.ToIO<RT, E>();

        /// <summary>
        /// Convert to an IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static implicit operator IO<RT, E, A>(Pure<A> ma) =>
            ma.ToIO<RT, E>();

        /// <summary>
        /// Convert to an IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static implicit operator IO<RT, E, A>(Fail<E> ma) =>
            ma.ToIO<RT, A>();

        /// <summary>
        /// Convert to an IO monad
        /// </summary>
        [Pure, MethodImpl(Opt.Default)]
        public static implicit operator IO<RT, E, A>(LiftIO<A> ma) =>
            ma.ToIO<RT, E>();

        [Pure, MethodImpl(Opt.Default)]
        public static implicit operator IO<RT, E, A>(Many<A> ma) =>
            ma.ToIO<RT, E>();
        
        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, IO<RT, E, A> mb) =>
            new(Transducer.choice(ma.Try().Morphism, mb.Morphism));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, CatchError<E> mb) =>
            new(Transducer.@try(
                ma.Morphism,
                mb.Match,
                Transducer.compose(Transducer.lift(mb.Value), Transducer.mkLeft<E, A>())));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, CatchValue<E, A> mb) =>
            new(Transducer.@try(
                ma.Morphism,
                mb.Match,
                Transducer.compose(Transducer.lift(mb.Value), Transducer.mkRight<E, A>())));
        
        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, IOCatch<RT, E, A> mb) =>
            ma.Try().Match(Succ: Pure, Fail: mb.Run).Flatten();        

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<RT, A> mb) =>
            ma.Try() | new IO<RT, E, A>(Transducer.compose(mb, Transducer.mkRight<E, A>()));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<RT, E> mb) =>
            ma.Try() | new IO<RT, E, A>(Transducer.compose(mb, Transducer.mkLeft<E, A>()));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<Unit, A> mb) =>
            ma.Try() | new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default), 
                    mb, 
                    Transducer.mkRight<E, A>()));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<Unit, E> mb) =>
            ma.Try() | new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default), 
                    mb, 
                    Transducer.mkLeft<E, A>()));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<RT, Sum<E, A>> mb) =>
            ma.Try() | new IO<RT, E, A>(mb);

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<Unit, Sum<E, A>> mb) =>
            ma.Try() | new IO<RT, E, A>(Transducer.compose(Transducer.constant<RT, Unit>(default), mb));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<RT, Sum<Error, A>> mb) =>
            ma.Try() | new IO<RT, E, A>(Transducer.mapLeft(mb, Transducer.lift<Error, E>(e => default(RT).FromError(e))));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(IO<RT, E, A> ma, Transducer<Unit, Sum<Error, A>> mb) =>
            ma.Try() | new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default), 
                    Transducer.mapLeft(mb, Transducer.lift<Error, E>(e => default(RT).FromError(e)))));

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<RT, A> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(Transducer.compose(ma, Transducer.mkRight<E, A>())) | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<RT, E> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(Transducer.compose(ma, Transducer.mkLeft<E, A>())).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<Unit, A> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default),
                    ma,
                    Transducer.mkRight<E, A>())).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<Unit, E> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default),
                    ma,
                    Transducer.mkLeft<E, A>())).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<RT, Sum<E, A>> ma, IO<RT, E, A>  mb) =>
            new IO<RT, E, A>(ma).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<Unit, Sum<E, A>> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(Transducer.compose(Transducer.constant<RT, Unit>(default), ma)).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<RT, Sum<Error, A>> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(Transducer.mapLeft(ma, Transducer.lift<Error, E>(e => default(RT).FromError(e)))).Try() | mb;

        /// <summary>
        /// Run the first IO operation; if it fails, run the second.  Otherwise return the
        /// result of the first without running the second.
        /// </summary>
        /// <param name="ma">First IO operation</param>
        /// <param name="mb">Alternative IO operation</param>
        /// <returns>Result of either the first or second operation</returns>
        [Pure, MethodImpl(Opt.Default)]
        public static IO<RT, E, A> operator |(Transducer<Unit, Sum<Error, A>> ma, IO<RT, E, A> mb) =>
            new IO<RT, E, A>(
                Transducer.compose(
                    Transducer.constant<RT, Unit>(default),
                    Transducer.mapLeft(ma, Transducer.lift<Error, E>(e => default(RT).FromError(e))))).Try() | mb;
    }
}
