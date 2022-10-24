namespace MF.ConsoleApplication

open System
open MF.ConsoleStyle

type internal Progress (io: IO, name: string) =
    let (input, output) = io
    let mutable progressBar: ProgressBar option = None

    member _.IsAvailable() =
        match input with
        | Input.Option.Has OptionNames.NoProgress _ -> false
        | _ -> true

    member this.Start(total: int) =
        progressBar <-
            if this.IsAvailable() then
                if output.IsDebug()
                then
                    output.Message $"<c:dark-yellow>[Debug] Progress for \"{name}\" for (</c><c:magenta>{total}</c><c:dark-yellow>) is disabled</c>"
                    None
                else
                    output.ProgressStartDefault(name, total)
                    |> Some
            else None

    member _.Advance() =
        progressBar |> Option.iter output.ProgressAdvance
        if output.IsDebug() then output.Message $"  ├──> <c:gray>[Debug] Progress advanced</c>"

    member _.Finish() =
        if output.IsDebug() then output.Message $"  └──> <c:dark-yellow>[Debug] Progress finished</c>\n"
        progressBar |> Option.iter output.ProgressFinish
        progressBar <- None

    member _.SpawnChild(message, keep) =
        match progressBar with
        | Some progress -> progress.SpawnChild(message, keep)
        | _ -> None

    interface IProgress with
        member this.Start(total) = this.Start(total)
        member this.Advance() = this.Advance()
        member this.Finish() = this.Finish()
        member this.SpawnChild(message, keep) = this.SpawnChild(message, keep)
        member this.IsAvailable() = this.IsAvailable()

    interface IDisposable with
        member this.Dispose() = this.Finish()
