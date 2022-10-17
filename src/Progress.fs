namespace MF.ConsoleApplication

module Progress =
    open System
    open MF.ConsoleStyle

    let noProgressOption = Option.noValue "no-progress" None "Whether to disable all progress bars."

    type Progress (io: IO, name: string) =
        let (input, output) = io
        let mutable progressBar: ProgressBar option = None

        member private __.IsEnabled() =
            let enableProgressBars =
                match input with
                | Input.IsSetOption "no-progress" _ -> false
                | _ -> true

            enableProgressBars && (not <| output.IsDebug())

        member this.Start(total: int) =
            progressBar <-
                if this.IsEnabled()
                    then Some <| output.ProgressStart name total
                    else
                        output.Message $"<c:dark-yellow>[Debug] Progress for \"{name}\" for (</c><c:magenta>{total}</c><c:dark-yellow>) is disabled</c>"
                        None

        member __.Advance() =
            progressBar |> Option.iter output.ProgressAdvance
            if output.IsDebug() then output.Message $"  ├──> <c:gray>[Debug] Progress advanced</c>"

        member __.Finish() =
            if output.IsDebug() then output.Message $"  └──> <c:dark-yellow>[Debug] Progress finished</c>"
            progressBar |> Option.iter output.ProgressFinish
            progressBar <- None

        interface IDisposable with
            member this.Dispose() =
                this.Finish()
