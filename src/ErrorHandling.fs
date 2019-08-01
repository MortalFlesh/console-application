namespace MF.ConsoleApplication

[<AutoOpen>]
module internal ErrorHandling =
    module ResultOperators =
        /// Map
        let (<!>) r f = r |> Result.map f

        /// Map error
        let (<!!>) r fe = r |> Result.mapError fe

        /// Bind
        let (>>=) r f = r |> Result.bind f

    /// Functions for Result type (functor and monad).
    /// For applicatives, see Validation.
    [<RequireQualifiedAccess>]  // RequireQualifiedAccess forces the `Result.xxx` prefix to be used
    module Result =
        let tee f a =
            f a
            a

        let iter f r =
            r
            |> Result.map (tee f)
            |> ignore

        let ofOption onMissing = function
            | Some x -> Ok x
            | None -> Error onMissing

        let toOption = function
            | Ok x -> Some x
            | _ -> None

        /// Apply a Result<fn> to a Result<x> monadically
        let apply fR xR =
            match fR, xR with
            | Ok f, Ok x -> Ok (f x)
            | Error err1, Ok _ -> Error err1
            | Ok _, Error err2 -> Error err2
            | Error err1, Error _ -> Error err1

         // combine a list of results, monadically
        let sequence aListOfResults =
            let (<*>) = apply // monadic
            let (<!>) = Result.map
            let cons head tail = head::tail
            let consR headR tailR = cons <!> headR <*> tailR
            let initialValue = Ok [] // empty list inside Result

            // loop through the list, prepending each element
            // to the initial value
            List.foldBack consR aListOfResults initialValue

        let orFail = function
            | Ok option -> option
            | Error e -> failwithf "Error %A" e

    [<AutoOpen>]
    module ResultComputationExpression =
        // https://github.com/swlaschin/DomainModelingMadeFunctional/blob/master/src/OrderTaking/Result.fs#L178

        type ResultBuilder() =
            member __.Return(x) = Ok x
            member __.Bind(x, f) = Result.bind f x

            member __.ReturnFrom(x) = x
            member this.Zero() = this.Return ()

            member __.Delay(f) = f
            member __.Run(f) = f()

            member this.While(guard, body) =
                if not (guard())
                then this.Zero()
                else this.Bind( body(), fun () ->
                    this.While(guard, body))

            member this.TryWith(body, handler) =
                try this.ReturnFrom(body())
                with e -> handler e

            member this.TryFinally(body, compensation) =
                try this.ReturnFrom(body())
                finally compensation()

            member this.Using(disposable:#System.IDisposable, body) =
                let body' = fun () -> body disposable
                this.TryFinally(body', fun () ->
                    match disposable with
                        | null -> ()
                        | disp -> disp.Dispose())

            member this.For(sequence:seq<_>, body) =
                this.Using(sequence.GetEnumerator(),fun enum ->
                    this.While(enum.MoveNext,
                        this.Delay(fun () -> body enum.Current)))

            member this.Combine (a,b) =
                this.Bind(a, fun () -> b())

        let result = ResultBuilder()

    type Validation<'Success,'Failure> =
        Result<'Success,'Failure list>

    /// Functions for the `Validation` type (mostly applicative)
    [<RequireQualifiedAccess>]  // RequireQualifiedAccess forces the `Validation.xxx` prefix to be used
    module Validation =

        /// Apply a Validation<fn> to a Validation<x> applicatively
        let apply (fV:Validation<_,_>) (xV:Validation<_,_>) :Validation<_,_> =
            match fV, xV with
            | Ok f, Ok x -> Ok (f x)
            | Error errs1, Ok _ -> Error errs1
            | Ok _, Error errs2 -> Error errs2
            | Error errs1, Error errs2 -> Error (errs1 @ errs2)

        // combine a list of Validation, applicatively
        let sequence (aListOfValidations:Validation<_,_> list) =
            let (<*>) = apply
            let (<!>) = Result.map
            let cons head tail = head::tail
            let consR headR tailR = cons <!> headR <*> tailR
            let initialValue = Ok [] // empty list inside Result

            // loop through the list, prepending each element
            // to the initial value
            List.foldBack consR aListOfValidations initialValue

        //-----------------------------------
        // Converting between Validations and other types

        let ofResult xR :Validation<_,_> =
            xR |> Result.mapError List.singleton

        let toResult (xV:Validation<_,_>) :Result<_,_> =
            xV

    //==============================================
    // Async utilities
    //==============================================

    [<RequireQualifiedAccess>]  // RequireQualifiedAccess forces the `Async.xxx` prefix to be used
    module Async =

        /// Lift a function to Async
        let map f xA =
            async {
            let! x = xA
            return f x
            }

        /// Lift a value to Async
        let retn x =
            async.Return x

        /// Apply an Async function to an Async value
        let apply fA xA =
            async {
             // start the two asyncs in parallel
            let! fChild = Async.StartChild fA  // run in parallel
            let! x = xA
            // wait for the result of the first one
            let! f = fChild
            return f x
            }

        /// Apply a monadic function to an Async value
        let bind f xA = async.Bind(xA,f)


    //==============================================
    // AsyncResult
    //==============================================

    type AsyncResult<'Success,'Failure> =
        Async<Result<'Success,'Failure>>

    [<RequireQualifiedAccess>]  // RequireQualifiedAccess forces the `AsyncResult.xxx` prefix to be used
    module AsyncResult =

        /// Lift a function to AsyncResult
        let map f (x:AsyncResult<_,_>) : AsyncResult<_,_> =
            Async.map (Result.map f) x

        /// Lift a function to AsyncResult
        let mapError f (x:AsyncResult<_,_>) : AsyncResult<_,_> =
            Async.map (Result.mapError f) x

        /// Apply ignore to the internal value
        let ignore x =
            x |> map ignore

        /// Lift a value to AsyncResult
        let retn x : AsyncResult<_,_> =
            x |> Result.Ok |> Async.retn

        /// Handles asynchronous exceptions and maps them into Failure cases using the provided function
        let catch f (x:AsyncResult<_,_>) : AsyncResult<_,_> =
            x
            |> Async.Catch
            |> Async.map(function
                | Choice1Of2 (Ok v) -> Ok v
                | Choice1Of2 (Error err) -> Error err
                | Choice2Of2 ex -> Error (f ex))


        /// Apply an AsyncResult function to an AsyncResult value, monadically
        let applyM (fAsyncResult : AsyncResult<_, _>) (xAsyncResult : AsyncResult<_, _>) :AsyncResult<_,_> =
            fAsyncResult |> Async.bind (fun fResult ->
            xAsyncResult |> Async.map (fun xResult -> Result.apply fResult xResult))

        /// Apply an AsyncResult function to an AsyncResult value, applicatively
        let applyA (fAsyncResult : AsyncResult<_, _>) (xAsyncResult : AsyncResult<_, _>) :AsyncResult<_,_> =
            fAsyncResult |> Async.bind (fun fResult ->
            xAsyncResult |> Async.map (fun xResult -> Validation.apply fResult xResult))

        /// Apply a monadic function to an AsyncResult value
        let bind (f: 'a -> AsyncResult<'b,'c>) (xAsyncResult : AsyncResult<_, _>) :AsyncResult<_,_> = async {
            let! xResult = xAsyncResult
            match xResult with
            | Ok x -> return! f x
            | Error err -> return (Error err)
            }


        /// Convert a list of AsyncResult into a AsyncResult<list> using monadic style.
        /// Only the first error is returned. The error type need not be a list.
        let sequenceM resultList =
            let (<*>) = applyM
            let (<!>) = map
            let cons head tail = head::tail
            let consR headR tailR = cons <!> headR <*> tailR
            let initialValue = retn [] // empty list inside Result

            // loop through the list, prepending each element
            // to the initial value
            List.foldBack consR resultList  initialValue


        /// Convert a list of AsyncResult into a AsyncResult<list> using applicative style.
        /// All the errors are returned. The error type must be a list.
        let sequenceA resultList =
            let (<*>) = applyA
            let (<!>) = map
            let cons head tail = head::tail
            let consR headR tailR = cons <!> headR <*> tailR
            let initialValue = retn [] // empty list inside Result

            // loop through the list, prepending each element
            // to the initial value
            List.foldBack consR resultList  initialValue

        //-----------------------------------
        // Converting between AsyncResults and other types

        /// Lift a value into an Ok inside a AsyncResult
        let ofSuccess x : AsyncResult<_,_> =
            x |> Result.Ok |> Async.retn

        /// Lift a value into an Error inside a AsyncResult
        let ofError x : AsyncResult<_,_> =
            x |> Result.Error |> Async.retn

        /// Lift a Result into an AsyncResult
        let ofResult x : AsyncResult<_,_> =
            x |> Async.retn

        /// Lift a Async into an AsyncResult
        let ofAsync x : AsyncResult<_,_> =
            x |> Async.map Result.Ok

        //-----------------------------------
        // Utilities lifted from Async

        let sleep ms =
            Async.Sleep ms |> ofAsync


    // ==================================
    // AsyncResult computation expression
    // ==================================

    /// The `asyncResult` computation expression is available globally without qualification
    [<AutoOpen>]
    module AsyncResultComputationExpression =

        type AsyncResultBuilder() =
            member __.Return(x) = AsyncResult.retn x
            member __.Bind(x, f) = AsyncResult.bind f x

            member __.ReturnFrom(x) = x
            member this.Zero() = this.Return ()

            member __.Delay(f) = f
            member __.Run(f) = f()

            member this.While(guard, body) =
                if not (guard())
                then this.Zero()
                else this.Bind( body(), fun () ->
                    this.While(guard, body))

            member this.TryWith(body, handler) =
                try this.ReturnFrom(body())
                with e -> handler e

            member this.TryFinally(body, compensation) =
                try this.ReturnFrom(body())
                finally compensation()

            member this.Using(disposable:#System.IDisposable, body) =
                let body' = fun () -> body disposable
                this.TryFinally(body', fun () ->
                    match disposable with
                        | null -> ()
                        | disp -> disp.Dispose())

            member this.For(sequence:seq<_>, body) =
                this.Using(sequence.GetEnumerator(),fun enum ->
                    this.While(enum.MoveNext,
                        this.Delay(fun () -> body enum.Current)))

            member this.Combine (a,b) =
                this.Bind(a, fun () -> b())

        let asyncResult = AsyncResultBuilder()
