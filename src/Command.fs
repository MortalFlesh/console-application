namespace MF.ConsoleApplication

open MF.ErrorHandling

type InteractiveInput = {
    Input: Input
    Ask: string -> string
}

[<RequireQualifiedAccess>]
module internal InteractiveInput =
    let input ask input = {
        Input = input
        Ask = ask
    }

type IO = Input * Output
type InteractiveIO = InteractiveInput * Output

[<RequireQualifiedAccess>]
module internal IO =
    let toInteractive ask: IO -> InteractiveIO =
        fun (input, output) ->
            (InteractiveInput.input ask input, output)

// Command
// ***********************

type Initialize = IO -> IO
type Interact = InteractiveInput * Output -> IO

[<RequireQualifiedAccess>]
module internal Interact =
    let id: Interact =
        fun ({ Input = input }, output) -> input, output

    let map ask: Interact -> IO -> IO =
        fun interact (input, output) ->
            match input with
            | Input.IsSetOption OptionNames.NoInteraction _ -> (input, output)
            | _ -> (input, output) |> IO.toInteractive ask |> interact

type Execute = IO -> ExitCode

type CommandDefinition = {
    Description: string
    Help: string option
    Arguments: Result<RawArgumentDefinition, ArgumentDefinitionError> list
    Options: Result<RawOptionDefinition, OptionDefinitionError> list
    Initialize: Initialize option
    Interact: Interact option
    Execute: Execute
}

type Command = private {
    Description: string
    Help: string option
    Arguments: ArgumentsDefinitions
    Options: OptionsDefinitions
    Initialize: Initialize option
    Interact: Interact option
    Execute: Execute
}

[<RequireQualifiedAccess>]
module internal CommandDefinition =
    open MF.ErrorHandling.Result.Operators

    let validate (definition: CommandDefinition): Result<Command, CommandDefinitionError> =
        result {
            let! arguments =
                definition.Arguments
                |> Result.sequence
                >>= ArgumentsDefinitions.validate <@> CommandDefinitionError.ArgumentDefinitionError

            let! options =
                definition.Options
                |> Result.sequence
                >>= OptionsDefinitions.validate <@> CommandDefinitionError.OptionDefinitionError

            return {
                Description = definition.Description
                Help = definition.Help
                Arguments = arguments
                Options = options
                Initialize = definition.Initialize
                Interact = definition.Interact
                Execute = definition.Execute
            }
        }

[<RequireQualifiedAccess>]
module internal Command =
    let description ({ Description = description }: Command) = description
    let definitions ({ Options = options; Arguments = arguments }: Command) = options, arguments

type internal Commands = Map<CommandName, Command>

type internal FindResult =
    | ExactlyOne of CommandName * Command
    | MoreThanOne of CommandName * CommandName list
    | NoCommand of CommandName

[<RequireQualifiedAccess>]
module internal Commands =
    open MF.ErrorHandling.Result.Operators

    let private namesByPattern pattern (commands: Commands): CommandName list =
        commands
        |> Map.toList
        |> List.map fst
        |> List.choose (CommandName.isMatchingPattern pattern)

    let find name (commands: Commands) =
        match commands |> Map.tryFind name with
        | Some command -> ExactlyOne (name, command)
        | _ ->
            let escapeForRegex (name: string) =
                name.Replace(".", @"\.")

            let regexToMatchAllToTheNamespaceSeparator =
                sprintf "[^%s]*?" CommandName.NamespaceSeparator

            let partialNamePattern =
                name
                |> CommandName.splitByNamespaces
                |> List.map (escapeForRegex >> String.append regexToMatchAllToTheNamespaceSeparator)
                |> String.concat CommandName.NamespaceSeparator
                |> sprintf "^%s$"

            match commands |> namesByPattern partialNamePattern with
            | [] -> NoCommand name
            | [ commandName ] -> ExactlyOne (commandName, commands.[commandName])
            | commandNames -> MoreThanOne (name, commandNames)

    let applicationOptions: OptionsDefinitions =
        [
            OptionsDefinitions.help
            OptionsDefinitions.quiet
            OptionsDefinitions.version
            OptionsDefinitions.noInteraction
            OptionsDefinitions.verbose
        ]

    let format (commands: Commands) =
        commands
        |> Map.toList
        |> List.map (fun (name, command) -> [
            sprintf "<c:dark-green>%s</c>" (name |> CommandName.value)
            command |> Command.description
        ])

    let showAvailable (output: Output) commands =
        commands
        |> format
        |> output.GroupedOptions CommandName.NamespaceSeparator "Available commands:"

    let add name command (commands: Commands) =
        commands.Add (CommandName (Name name), command)

    let helpCommand showHelpForCommand showError (commands: Commands): Command =
        CommandDefinition.validate {
            Description = "Displays help for a command"
            Help =
                [
                    "The <c:dark-green>{{command.name}}</c> command displays help for a given command:"
                    "        <c:dark-green>dotnet {{command.full_name}} list</c>"
                    "    To display list of available commands, please use <c:dark-green>list</c> command."
                ]
                |> String.concat "\n\n"
                |> Some
            Arguments = [
                Argument.optional "command_name" "The command name" (Some CommandNames.Help)
            ]
            Options = []
            Initialize = None
            Interact = None
            Execute = fun (input, output) ->
                result {
                    let! rawCommandName =
                        input
                        |> Input.getArgumentValue "command_name"
                        |> CommandName.createInRuntime <@> ArgsError.CommandNameError

                    return!
                        match commands |> find rawCommandName with
                        | ExactlyOne (commandName, command) -> Ok (commandName, command)
                        | MoreThanOne (givenName, names) -> Error (ArgsError.AmbigousCommandFound (givenName, names))
                        | NoCommand unknownName -> Error (ArgsError.CommandNotFound unknownName)
                }
                <@> ConsoleApplicationError.ArgsError
                |> function
                    | Ok (commandName, command) ->
                        showHelpForCommand commandName command
                        ExitCode.Success
                    | Error error ->
                        showError error
                        ExitCode.Error
        }
        |> Result.orFail

    let helpCommandDummy =
        let ignore2 _ = ignore
        helpCommand ignore2 ignore Map.empty

    let private byNamespace namespaceValue (commands: Commands) =
        commands
        |> Map.filter (fun name _ ->
            name |> CommandName.namespaceValue = Some namespaceValue
        )

    let listCommand applicationOptions (commands: Commands): Command =
        CommandDefinition.validate {
            Description = "Lists commands"
            Help =
                [
                    "The <c:dark-green>{{command.name}}</c> command lists all commands:"
                    "        <c:dark-green>dotnet {{command.full_name}}</c>"
                    "    You can also display the commands for a specific namespace:"
                    "        <c:dark-green>dotnet {{command.full_name}} test</c>"
                ]
                |> String.concat "\n\n"
                |> Some
            Arguments = [
                Argument.optional "namespace" "The namespace name" None
            ]
            Options = []
            Initialize = None
            Interact = None
            Execute = fun (input, output) ->
                let showUsage () =
                    output.SimpleOptions "Usage:" [
                        [ "command [options] [--] [arguments]"; "" ]
                    ]

                let showOptions () =
                    applicationOptions
                    |> OptionsDefinitions.format
                    |> output.SimpleOptions "Options:"

                match input with
                | Input.ArgumentOptionalValue "namespace" namespaceToList ->
                    match commands |> byNamespace namespaceToList with
                    | commandsByNamespace when commandsByNamespace |> Map.isEmpty ->
                        output.Error <| sprintf "There are no commands defined in the \"%s\" namespace.\n" namespaceToList
                        ExitCode.Error
                    | commandsByNamespace ->
                        showUsage()
                        showOptions()

                        commandsByNamespace
                        |> format
                        |> output.SimpleOptions "Available commands:"

                        ExitCode.Success
                | _ ->
                    showUsage()
                    showOptions()

                    commands
                    |> showAvailable output

                    ExitCode.Success
        }
        |> Result.orFail

    let listCommandDummy =
        listCommand [] Map.empty

    let exitInteractiveModeCommand =
        {
            Description = "Exit interactive mode"
            Help = None
            Arguments = []
            Options = []
            Initialize = None
            Interact = None
            Execute = fun _ -> failwith "Exit command should not be executed."
        }
