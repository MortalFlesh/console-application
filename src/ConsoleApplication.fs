namespace MF.ConsoleApplication

[<AutoOpen>]
module MFConsoleApplication =
    open MF.ConsoleStyle
    open OptionsOperators
    open ResultOperators

    let consoleApplication =
        let buildApplication = ConsoleApplicationBuilder.buildApplication Help.showForCommand Error.show
        ConsoleApplicationBuilder (buildApplication)

    let private showApplicationInfo parts =
        let output = parts.Output

        let renderTitle () =
            parts.Title
            |>! (ApplicationTitle.value >> output.MainTitle)

        let name = parts.Name |> ApplicationName.value

        match parts.ApplicationInfo with
        | ApplicationInfo.Hidden -> ()
        | ApplicationInfo.MainTitle -> renderTitle()
        | ApplicationInfo.OnlyNameAndVersion ->
            match parts.Version with
            | Some (ApplicationVersion version) -> sprintf "%s <c:green><%s></c>" name version
            | _ -> name
            |> output.Message
        | ApplicationInfo.NameAndVersion ->
            match parts.Version with
            | Some (ApplicationVersion version) -> sprintf "%s <%s>" name version
            | _ -> name
            |> output.Title
        | ApplicationInfo.Interactive ->
            match parts.Version with
            | Some (ApplicationVersion version) -> sprintf "%s <%s>" name version
            | _ -> name
            |> sprintf "%s - interactive mode"
            |> output.Title
        | ApplicationInfo.All ->
            renderTitle()
            match parts.Version with
            | Some (ApplicationVersion version) -> sprintf "%s <%s>" name version
            | _ -> name
            |> output.Title

    [<RequireQualifiedAccess>]
    module private Args =
        let private containsOption (option: Option) args =
            match args |> Array.tryFind (Option.isMatching option) with
            | Some _ -> true
            | _ -> false

        let (|Empty|_|) args =
            if args |> Array.isEmpty then Some Empty
            else None

        let (|ContainsOnlyOptions|_|) (args: InputValue []) =
            if args |> Array.forall (fun arg -> arg.StartsWith "-" && arg <> Arguments.Separator) then Some ContainsOnlyOptions
            else None

        let (|ContainsOption|_|) option args =
            if args |> containsOption option then Some ContainsOption
            else None

        let getCommandName (args: InputValue []) =
            args
            |> Array.pick (function
                | CommandName.IsCommandName commandName -> Some commandName
                | _ -> None
            )

        let (|HasOption|_|) option args =
            args
            |> Array.tryFind (Option.isMatching option)

        let parse commands: InputValue [] -> Result<Input * UnfilledArgumentDefinitions, ArgsError> =
            fun args ->
                match args |> List.ofArray with
                | [] -> Ok (Input.empty, [])
                | command :: rawArgs ->
                    result {
                        let! commandName =
                            command
                            |> CommandName.createInRuntime <!!> ArgsError.CommandNameError

                        let! (optionDefinitions, argumentDefinitions) =
                            commands
                            |> Commands.definitions commandName
                            |> Result.ofOption (ArgsError.CommandNotFound commandName)

                        let optionDefinitions = Commands.applicationOptions @ optionDefinitions
                        let definitions = (optionDefinitions, argumentDefinitions)

                        let! parsedInput =
                            rawArgs
                            |> Input.parse argumentDefinitions definitions ParsedInput.empty <!!> ArgsError.InputError

                        let input = {
                            Arguments = parsedInput.Arguments.Add(ArgumentNames.Command, ArgumentValue.Required command)
                            Options = optionDefinitions |> Options.prepareOptionsDefaults parsedInput.Options
                            ArgumentDefinitions = argumentDefinitions
                            OptionDefinitions = optionDefinitions
                        }

                        return input, parsedInput.UnfilledArgumentDefinitions
                    }

    let runResult args (ConsoleApplication application) =
        match application with
        | Ok parts ->
            result {
                let output = parts.Output

                match args with
                | Args.ContainsOption OptionsDefinitions.quiet -> output.SetVerbosity Verbosity.Quiet
                | Args.HasOption OptionsDefinitions.verbose verbosity ->
                    match verbosity with
                    // todo <later> - add -vvvv special debug mode, which will allow debuging of console-application directly - allow `debug` function
                    | "-vvv" -> Verbosity.Debug
                    | "-vv" -> Verbosity.VeryVerbose
                    | _ -> Verbosity.Verbose
                    |> output.SetVerbosity
                | _ -> ()

                let args =
                    match args with
                    | Args.Empty
                    | Args.ContainsOnlyOptions ->
                        [|
                            yield parts.DefaultCommand |> CommandName.value
                            yield! args
                        |]
                    | _ -> args

                match args with
                | Args.ContainsOption OptionsDefinitions.help ->
                    parts |> showApplicationInfo

                    let commandName = args |> Args.getCommandName

                    return!
                        match parts.Commands |> Map.tryFind commandName with
                        | None -> Error (ArgsError.CommandNotFound commandName) <!!> ConsoleApplicationError.ArgsError
                        | Some command ->
                            command
                            |> Help.showForCommand output parts.OptionDecorationLevel parts.ApplicationOptions commandName

                            Ok ExitCode.Success
                | Args.ContainsOption OptionsDefinitions.version ->
                    { parts with ApplicationInfo = ApplicationInfo.OnlyNameAndVersion }
                    |> showApplicationInfo

                    return ExitCode.Success
                | args ->
                    parts |> showApplicationInfo

                    let! (input, unfilledArguments) =
                        args
                        |> Args.parse parts.Commands <!!> ConsoleApplicationError.ArgsError

                    let commandName = input |> Input.getCommandName
                    let command = parts.Commands.[commandName]

                    //debug <| sprintf "Input:\n%A" input
                    // todo <later> - show input as table(s) ->
                    // - same as `dumpInput` function in Example
                    //    | Option | Value |
                    //    | Argument | Value |
                    // this could be done after `debug` is set for application directly, with -vvvv

                    try
                        let (input, _) =
                            (input, output)
                            |> (command.Initialize <?=> id)
                            |> Interact.map parts.Ask (command.Interact <?=> Interact.id)

                        let! input =
                            input
                            |> Input.prepareUnfilledArguments unfilledArguments <!!> (ArgsError.InputError >> ConsoleApplicationError.ArgsError)

                        return
                            (input, output)
                            |> command.Execute
                    with
                    | e ->
                        return! Error (ConsoleApplicationError.ConsoleApplicationError e.Message)
            }
        | Error error -> Error error

    let run args application =
        application
        |> runResult args
        |> ExitCode.fromResult (Error.show (application |> ConsoleApplication.output))
        |> ExitCode.code

    let runInteractively args application =
        let args = args |> List.ofArray
        let mutable shouldRun = true
        let mutable exitCode = 0

        let interactiveApplication =
            application
            |> ConsoleApplication.map (fun parts -> { parts with ApplicationInfo = ApplicationInfo.Hidden })

        while shouldRun do
            application
            |> ConsoleApplication.iter (fun parts ->
                { parts with ApplicationInfo = ApplicationInfo.Interactive }
                |> showApplicationInfo

                parts.Commands.Add(CommandName (Name CommandNames.Exit), Commands.exitInteractiveModeCommand)
                |> Commands.showAvailable parts.Output
            )

            match Console.ask "Command:" with
            | CommandNames.Exit -> shouldRun <- false
            | command ->
                match run ((command :: args) |> List.toArray) interactiveApplication with
                | 0 -> ()
                | code ->
                    shouldRun <- false
                    exitCode <- code
        exitCode
