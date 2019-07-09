namespace MF.ConsoleApplication

open ResultOperators

[<RequireQualifiedAccess>]
type ApplicationInfo =
    | Hidden
    | MainTitle
    | NameAndVersion
    | OnlyNameAndVersion
    | Interactive
    | All

type internal DefinitionParts = {
    Name: ApplicationName
    Version: ApplicationVersion option
    Title: ApplicationTitle option
    ApplicationInfo: ApplicationInfo
    ApplicationOptions: OptionsDefinitions
    Output: Output
    Ask: string -> string
    Commands: Commands
    DefaultCommand: CommandName
    OptionDecorationLevel: OptionDecorationLevel
}

[<RequireQualifiedAccess>]
module internal DefinitionParts =
    let defaults = {
        Name = ApplicationName (Name "Console Application")
        Version = None
        Title = None
        ApplicationInfo = ApplicationInfo.Hidden
        ApplicationOptions = Commands.applicationOptions
        Output = Output.console
        Ask = MF.ConsoleStyle.Console.ask
        Commands = Map.empty
        DefaultCommand = CommandName (Name CommandNames.List)
        OptionDecorationLevel = Minimal
    }

    let output { Output = output } = output

type Definition = private Definition of Result<DefinitionParts, ConsoleApplicationError>
type ConsoleApplication = internal ConsoleApplication of Result<DefinitionParts, ConsoleApplicationError>

[<RequireQualifiedAccess>]
module internal ConsoleApplication =
    let iter f (ConsoleApplication application) =
        application
        |> Result.iter f

    let map f (ConsoleApplication application) =
        ConsoleApplication (application <!> f)

    let output (ConsoleApplication appliction) =
        appliction
        |> Result.toOption
        |> Option.map DefinitionParts.output

type ConsoleApplicationBuilder<'r> internal (buildApplication: Definition -> 'r) =
    let (>>=) (Definition definition) f =
        Definition (definition >>= f)

    let (<!>) state f =
        state >>= (f >> Ok)

    member __.Yield (_): Definition =
        DefinitionParts.defaults
        |> Ok
        |> Definition

    member __.Run (state) =
        buildApplication state

    [<CustomOperation("name")>]
    member __.Name(state, name): Definition =
        state >>= fun parts ->
            result {
                let! name =
                    name
                    |> ApplicationName.create <!!> ConsoleApplicationError.ApplicationNameError

                return { parts with Name = name }
            }

    [<CustomOperation("version")>]
    member __.Version(state, version): Definition =
        state <!> fun parts -> { parts with Version = Some (ApplicationVersion version) }

    [<CustomOperation("title")>]
    member __.Title(state, title): Definition =
        state <!> fun parts -> { parts with Title = Some (ApplicationTitle title) }

    [<CustomOperation("info")>]
    member __.Info(state, applicationInfo): Definition =
        state <!> fun parts -> { parts with ApplicationInfo = applicationInfo }

    [<CustomOperation("defaultCommand")>]
    member __.DefaultCommand(state, defaultCommand): Definition =
        state >>= fun parts ->
            result {
                let! commandName =
                    defaultCommand
                    |> CommandName.create <!!> ConsoleApplicationError.CommandNameError

                return { parts with DefaultCommand = commandName }
            }

    /// <summary>
    /// When options are shown, this `decorationLevel` is used to determine, how much information should be shown.
    /// <para>Minimal is just: [options]</para>
    /// <para>Complete is: [a|optionA OPTIONA] [b|optionB OPTIONB] ...</para>
    /// </summary>
    [<CustomOperation("showOptions")>]
    member __.ShowOptions(state, decorationLevel): Definition =
        state <!> fun parts -> { parts with OptionDecorationLevel = decorationLevel }

    [<CustomOperation("command")>]
    member __.Command(state, name, command): Definition =
        state >>= fun parts ->
            result {
                let! commandName =
                    name
                    |> CommandName.create <!!> ConsoleApplicationError.CommandNameError

                let! command =
                    command
                    |> CommandDefinition.validate <!!> ConsoleApplicationError.CommandDefinitionError

                return { parts with Commands = parts.Commands.Add(commandName, command) }
            }

    [<CustomOperation("useOutput")>]
    member __.UseOutput(state, output): Definition =
        state <!> fun parts -> { parts with Output = output }

    [<CustomOperation("useAsk")>]
    member __.UseAsk(state, ask): Definition =
        state <!> fun parts -> { parts with Ask = ask }

[<RequireQualifiedAccess>]
module internal ConsoleApplicationBuilder =
    let buildApplication showHelpForCommand showError (Definition definition) =
        definition <!> (fun parts ->
            let showHelpForCommand =
                showHelpForCommand parts.Output parts.OptionDecorationLevel parts.ApplicationOptions

            let showError =
                showError (Some parts.Output)

            let commands =
                [
                    // dummy commands are here just as placeholder to be shown in the list, etc.
                    yield (CommandName (Name CommandNames.List), Commands.listCommandDummy)
                    yield (CommandName (Name CommandNames.Help), Commands.helpCommandDummy)

                    yield! parts.Commands |> Map.toList
                ]
                |> Map.ofList

            { parts with
                Commands = commands
                    |> Commands.add CommandNames.List (Commands.listCommand parts.ApplicationOptions commands)
                    |> Commands.add CommandNames.Help (Commands.helpCommand showHelpForCommand showError commands)
            }
        )
        |> ConsoleApplication
