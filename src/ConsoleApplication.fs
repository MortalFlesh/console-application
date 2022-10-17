namespace MF.ConsoleApplication

[<AutoOpen>]
module MFConsoleApplication =
    open MF.ConsoleStyle
    open OptionsOperators
    open MF.ErrorHandling
    open MF.ErrorHandling.Result.Operators

    let consoleApplication =
        let showError application = Error.show application None
        let buildApplication = ConsoleApplicationBuilder.buildApplication Help.showForCommand showError

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

    type private CurrentCommand = (CommandName * Command) option

    /// Map error by appending a current command.
    let private (<!!*>) result (currentCommand: CurrentCommand) =
        result <@> fun __ -> (__, currentCommand)

    /// Map error with appended current command.
    let private (<!!!>) result f =
        result <@> fun (error, (__: CurrentCommand)) -> (error |> f, __)

    type private Args = InputValue []

    [<RequireQualifiedAccess>]
    module private Args =
        let private containsOption option (args: Args) =
            args |> Array.tryFind (Option.isMatching option) |> Bool.fromOption

        let (|Empty|_|) (args: Args) =
            args |> Array.isEmpty |> Bool.toOption

        let (|ContainsOnlyOptions|_|) (args: Args) =
            args |> Array.forall Option.isOptionOrShortcut |> Bool.toOption

        let (|ContainsOption|_|) option (args: Args) =
            args |> containsOption option |> Bool.toOption

        let getCommandName (args: Args) =
            args
            |> Array.pick (function
                | CommandName.IsCommandName commandName -> Some commandName
                | _ -> None
            )

        let (|HasOption|_|) option (args: Args) =
            args |> Array.tryFind (Option.isMatching option)

        let parse output applicationOptions (commands: Commands): Args -> Result<Input * UnfilledArgumentDefinitions, ArgsError * CurrentCommand> =
            fun args ->
                match args |> List.ofArray with
                | [] -> Ok (Input.empty, [])
                | rawArg :: rawArgs ->
                    result {
                        let currentCommand: CurrentCommand = None

                        let! rawCommandName =
                            rawArg
                            |> CommandName.createInRuntime <@> ArgsError.CommandNameError <!!*> currentCommand

                        let! (commandName, command) =
                            match commands |> Commands.find rawCommandName with
                            | ExactlyOne (commandName, command) -> Ok (commandName, command)
                            | MoreThanOne (givenName, names) -> Result.Error (ArgsError.AmbigousCommandFound (givenName, names))
                            | NoCommand unknownName -> Result.Error (ArgsError.CommandNotFound unknownName)
                            <!!*> currentCommand

                        let currentCommand: CurrentCommand = Some (commandName, command)
                        let optionDefinitions = command.Options @ applicationOptions
                        let definitions = (optionDefinitions, command.Arguments)

                        let! parsedInput =
                            rawArgs
                            |> Input.parse output command.Arguments definitions ParsedInput.empty <@> ArgsError.InputError <!!*> currentCommand

                        let input = {
                            Arguments = parsedInput.Arguments.Add(ArgumentNames.Command, ArgumentValue.Required (commandName |> CommandName.value))
                            Options = optionDefinitions |> Options.prepareOptionsDefaults parsedInput.Options
                            ArgumentDefinitions = command.Arguments
                            OptionDefinitions = optionDefinitions
                        }

                        return input, parsedInput.UnfilledArgumentDefinitions
                    }

    let private runApplication (args: Args) (ConsoleApplication application): Result<ExitCode, ConsoleApplicationError * CurrentCommand> =
        let currentCommand: CurrentCommand = None

        match application with
        | Ok parts ->
            result {
                let output = parts.Output

                match args with
                | Args.ContainsOption OptionsDefinitions.quiet -> output.Verbosity <- Verbosity.Quiet
                | Args.HasOption OptionsDefinitions.verbose verbosity ->
                    output.Verbosity <-
                        match verbosity with
                        // todo <later> - add -vvvv special debug mode, which will allow debuging of console-application directly - allow `debug` function
                        | "-vvv" -> Verbosity.Debug
                        | "-vv" -> Verbosity.VeryVerbose
                        | _ -> Verbosity.Verbose
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

                    let rawCommandName = args |> Args.getCommandName

                    return!
                        match parts.Commands |> Commands.find rawCommandName with
                        | ExactlyOne (commandName, command) ->
                            command
                            |> Help.showForCommand output parts.OptionDecorationLevel parts.ApplicationOptions commandName

                            Ok ExitCode.Success
                        | MoreThanOne (givenName, names) -> Result.Error (ArgsError.AmbigousCommandFound (givenName, names))
                        | NoCommand unknownName -> Result.Error (ArgsError.CommandNotFound unknownName)
                        <@> ConsoleApplicationError.ArgsError
                        <!!*> currentCommand
                | Args.ContainsOption OptionsDefinitions.version ->
                    { parts with ApplicationInfo = ApplicationInfo.OnlyNameAndVersion }
                    |> showApplicationInfo

                    return ExitCode.Success
                | args ->
                    parts |> showApplicationInfo

                    let! (input, unfilledArguments) =
                        args
                        |> Args.parse output parts.ApplicationOptions parts.Commands <!!!> ConsoleApplicationError.ArgsError

                    let commandName = input |> Input.getCommandName
                    let command = parts.Commands.[commandName]
                    let currentCommand: CurrentCommand = Some (commandName, command)

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
                            |> Input.prepareUnfilledArguments unfilledArguments <@> (ArgsError.InputError >> ConsoleApplicationError.ArgsError) <!!*> currentCommand

                        return
                            (input, output)
                            |> command.Execute
                    with
                    | e ->
                        return! Result.Error (ConsoleApplicationError.ConsoleApplicationError e.Message) <!!*> currentCommand
            }
        | Result.Error error -> Result.Error (error, currentCommand)

    let runResult args application =
        application
        |> runApplication args <@> fst

    let run args application =
        application
        |> runApplication args
        |> ExitCode.fromResult (Error.show application)
        |> ExitCode.code

    let runInteractively args application =
        let args = args |> List.ofArray
        let mutable shouldRun = true
        let mutable exitCode = 0

        let output =
            application
            |> ConsoleApplication.output
            |> Option.defaultValue Output.defaults

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

            match output.Ask "Command:" with
            | CommandNames.Exit -> shouldRun <- false
            | command ->
                match run ((command :: args) |> List.toArray) interactiveApplication with
                | 0 -> ()
                | code ->
                    shouldRun <- false
                    exitCode <- code
        exitCode
