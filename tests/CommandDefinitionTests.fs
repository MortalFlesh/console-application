module MF.ConsoleApplication.Tests.CommandDefinitions

open Expecto
open MF.ConsoleApplication

type ArgumentsDefinition = {
    Arguments: Result<RawArgumentDefinition, ArgumentDefinitionError> list
    Argv: string []
    Expected: Result<ExitCode, ConsoleApplicationError>
    Description: string
}

type OptionsDefinition = {
    Options: Result<RawOptionDefinition, OptionDefinitionError> list
    Argv: string []
    Expected: Result<ExitCode, ConsoleApplicationError>
    Description: string
}

type CommandNameDefinition = {
    CommandName: string
    Argv: string []
    Expected: Result<ExitCode, ConsoleApplicationError>
    Description: string
}

let provideArgumentDefinitions = seq {
    yield {
        Description = "Empty arguments"
        Arguments = []
        Argv = [| |]
        Expected = Ok ExitCode.Success
    }
    yield {
        Description = "Argument with empty name"
        Arguments = [
            Argument.required "" "Argument with empty name"
        ]
        Argv = [| |]
        Expected =
            NameError.Empty
            |> ArgumentNameError.NameError
            |> ArgumentDefinitionError.ArgumentNameError
            |> List.singleton
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Name with space in it"
        Arguments = [
            Argument.required "foo bar" "Argument with wrong name"
        ]
        Argv = [| |]
        Expected =
            NameError.Contains ("foo bar", " ")
            |> ArgumentNameError.NameError
            |> ArgumentDefinitionError.ArgumentNameError
            |> List.singleton
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "More than one array argument defined"
        Arguments = [
            Argument.optionalArray "optionalArray" "First array argument" None
            Argument.optionalArray "anotherOptionalArray" "Second array argument" None
        ]
        Argv = [| |]
        Expected =
            [ArgumentDefinitionError.ArgumentAfterArrayArgument]
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Array argument is not last"
        Arguments = [
            Argument.optionalArray "optionalArray" "First array argument" None
            Argument.required "argument" "Mandatory argument"
        ]
        Argv = [| |]
        Expected =
            [
                ArgumentDefinitionError.ArgumentAfterArrayArgument
                ArgumentDefinitionError.RequiredArgumentAfterOptional
            ]
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Optional argument before required"
        Arguments = [
            Argument.optional "optional" "First array argument" None
            Argument.required "argument" "Mandatory argument"
        ]
        Argv = [| |]
        Expected =
            [ArgumentDefinitionError.RequiredArgumentAfterOptional]
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Optional argument before required - with array"
        Arguments = [
            Argument.required "mandatory" "Mandatory argument"
            Argument.optional "optional" "Optional argument" None
            Argument.requiredArray "argument" "Mandatory argument"
        ]
        Argv = [| |]
        Expected =
            [ArgumentDefinitionError.RequiredArgumentAfterOptional]
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Not unique argument name"
        Arguments = [
            Argument.required "mandatory" "Mandatory argument"
            Argument.required "mandatory" "Mandatory argument duplicated"
            Argument.optional "optional" "Optional argument" None
        ]
        Argv = [| |]
        Expected =
            [ArgumentDefinitionError.ArgumentAlreadyExists "mandatory"]
            |> CommandDefinitionError.ArgumentDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
}

let provideOptionDefinitions = seq {
    yield {
        Description = "Empty options"
        Options = []
        Argv = [| |]
        Expected = Ok ExitCode.Success
    }
    yield {
        Description = "Option with empty name"
        Options = [
            Option.required "" None "With empty name" "default"
        ]
        Argv = [| |]
        Expected =
            NameError.Empty
            |> OptionNameError.NameError
            |> OptionDefinitionError.OptionNameError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option with starting with -"
        Options = [
            Option.optional "-opt" None "Starting with -" None
        ]
        Argv = [| |]
        Expected =
            NameError.StartsWith ("-opt", "-")
            |> OptionNameError.NameError
            |> OptionDefinitionError.OptionNameError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option with invalid char"
        Options = [
            Option.optional "o p t i o n" None "Contains a space" None
        ]
        Argv = [| |]
        Expected =
            NameError.Contains ("o p t i o n", " ")
            |> OptionNameError.NameError
            |> OptionDefinitionError.OptionNameError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option with ends with ="
        Options = [
            Option.optional "opt=" None "Ends with =" None
        ]
        Argv = [| |]
        Expected =
            NameError.Contains ("opt=", "=")
            |> OptionNameError.NameError
            |> OptionDefinitionError.OptionNameError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option with empty shortcut"
        Options = [
            Option.optional "opt" (Some "") "Empty shortcut" None
        ]
        Argv = [| |]
        Expected =
            OptionShortcutError.Empty
            |> OptionDefinitionError.OptionShortcutError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option shortcut with -"
        Options = [
            Option.optional "opt" (Some "-o") "Shortcut with -" None
        ]
        Argv = [| |]
        Expected =
            OptionShortcutError.Contains ("-o", "-")
            |> OptionDefinitionError.OptionShortcutError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option shortcut with multiple options"
        Options = [
            Option.optional "opt" (Some "o|oo") "Shortcut with more than one option" None
        ]
        Argv = [| |]
        Expected =
            OptionShortcutError.MoreThanSingleLetter "o|oo"
            |> OptionDefinitionError.OptionShortcutError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "Option shortcut with reserved value"
        Options = [
            Option.optional "opt" (Some "V") "Shortcut with reserved shortcut" None
        ]
        Argv = [| |]
        Expected =
            OptionShortcutError.Reserved (ReservedShortcut ("V", "version"))
            |> OptionDefinitionError.OptionShortcutError
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "More than one option with the same name"
        Options = [
            Option.optional "opt" (Some "o") "First" None
            Option.optional "opt" None "Second" None
        ]
        Argv = [| |]
        Expected =
            OptionDefinitionError.OptionAlreadyExists "opt"
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
    yield {
        Description = "More than one option without shortcut"
        Options = [
            Option.optional "opt1" None "First" None
            Option.optional "opt2" None "Second" None
        ]
        Argv = [| |]
        Expected = Ok ExitCode.Success
    }
    yield {
        Description = "More than one option with the same shortcut"
        Options = [
            Option.optional "opt1" (Some "o") "First" None
            Option.optional "opt2" (Some "o") "Second" None
        ]
        Argv = [| |]
        Expected =
            OptionDefinitionError.OptionShortcutAlreadyExists "o"
            |> List.singleton
            |> CommandDefinitionError.OptionDefinitionError
            |> ConsoleApplicationError.CommandDefinitionError
            |> Error
    }
}

let provideCommandName = seq {
    yield {
        Description = "Simple name"
        CommandName = "simple"
        Argv = [| "simple" |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Name contains ."
        CommandName = "with.dot"
        Argv = [| "with.dot" |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Name contains . used by shortcut"
        CommandName = "with.dot"
        Argv = [| "with." |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Ambigous name contains . used by unique shortcut"
        CommandName = "ambigous:command.foo"
        Argv = [| "a:command.f" |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Ambigous name contains . used by shortcut"
        CommandName = "ambigous:command.foo"
        Argv = [| "a:command." |]
        Expected =
            AmbigousCommandFound.create "a:command." [ "ambigous:command.foo"; "ambigous:command.name" ]
            |> ConsoleApplicationError.ArgsError
            |> Error
    }

    yield {
        Description = "Name contains namespaces"
        CommandName = "group:sub:command"
        Argv = [| "group:sub:command" |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Name contains namespaces used by shortcut"
        CommandName = "group:sub:command"
        Argv = [| "g:s:c" |]
        Expected = Ok ExitCode.Success
    }

    yield {
        Description = "Name contains namespaces and should not be matched when not all namespaces are given"
        CommandName = "group:sub:command"
        Argv = [| "g:c" |]
        Expected =
            CommandNotFound.create "g:c"
            |> ConsoleApplicationError.ArgsError
            |> Error
    }

    yield {
        Description = "Invalid name - starting with -"
        CommandName = "-invalid"
        Argv = [| |]
        Expected =
            NameError.StartsWith ("-invalid", "-")
            |> CommandNameError.NameError
            |> ConsoleApplicationError.CommandNameError
            |> Error
    }

    yield {
        Description = "Invalid name - ends with :"
        CommandName = "invalid:"
        Argv = [| |]
        Expected =
            NameError.EndsWith ("invalid:", ":")
            |> CommandNameError.NameError
            |> ConsoleApplicationError.CommandNameError
            |> Error
    }

    yield {
        Description = "Invalid name - contains space"
        CommandName = "in valid"
        Argv = [| |]
        Expected =
            NameError.Contains ("in valid", " ")
            |> CommandNameError.NameError
            |> ConsoleApplicationError.CommandNameError
            |> Error
    }

    yield {
        Description = "Invalid name - contains ::"
        CommandName = "in::valid"
        Argv = [| |]
        Expected =
            NameError.Contains ("in::valid", "::")
            |> CommandNameError.NameError
            |> ConsoleApplicationError.CommandNameError
            |> Error
    }

    yield {
        Description = "Invalid name - contains invalid character ^"
        CommandName = "contains^"
        Argv = [| |]
        Expected =
            CommandNameError.Invalid "contains^"
            |> ConsoleApplicationError.CommandNameError
            |> Error
    }
}

let runConsoleApplicationWithCommandName commandNameDefinition commandNameRuntime arguments options argv =
    let argv = [|
        yield! commandNameRuntime |> Option.toList
        yield! argv
    |]

    consoleApplication {
        command commandNameDefinition {
            Description = "Test command."
            Help = None
            Arguments = arguments
            Options = options
            Initialize = None
            Interact = None
            Execute = Execute (fun _ -> ExitCode.Success)
        }

        command "ambigous:command.name" {
            Description = "Test command."
            Help = None
            Arguments = arguments
            Options = options
            Initialize = None
            Interact = None
            Execute = Execute (fun _ -> failwith "Should not be called.")
        }
    }
    |> runResult argv

let runConsoleApplication =
    runConsoleApplicationWithCommandName "test" (Some "test")

[<Tests>]
let defineCommandTests =
    testList "ConsoleApplication - define command" [
        yield!
            provideArgumentDefinitions
            |> Seq.map (fun { Arguments = arguments; Argv = argv; Expected = expected; Description = description } ->
                testCase $"arguments - {description}" <| fun _ ->
                    let description = sprintf "args: %s\n%s" (argv |> String.concat " ") description

                    let result = runConsoleApplication arguments [] argv
                    let description = sprintf "%s\nResult:\n%A\n" description result

                    Expect.equal result expected description
            )

        yield!
            provideOptionDefinitions
            |> Seq.map (fun { Options = options; Argv = argv; Expected = expected; Description = description } ->
                testCase $"options - {description}" <| fun _ ->
                    let description = sprintf "args: %s\n%s" (argv |> String.concat " ") description

                    let result = runConsoleApplication [] options argv
                    let description = sprintf "%s\nResult:\n%A\n" description result

                    Expect.equal result expected description
            )

        yield!
            provideCommandName
            |> Seq.map (fun { CommandName = name; Argv = argv; Expected = expected; Description = description } ->
                testCase $"commandNames - {description}" <| fun _ ->
                    let description = sprintf "args: %s\n%s" (argv |> String.concat " ") description

                    let result = runConsoleApplicationWithCommandName name None [] [] argv
                    let description = sprintf "%s\nResult:\n%A\n" description result

                    Expect.equal result expected description
            )

        testCase "Test should just compile with all keywords" <| fun _ ->
            let customTag: MF.ConsoleStyle.CustomTag = { Tag = MF.ConsoleStyle.TagName "tag"; Markup = MF.ConsoleStyle.MarkupString "<c:yellow>" }

            use buffer = new MF.ConsoleStyle.Output.BufferOutput(MF.ConsoleStyle.Verbosity.Normal)
            let console = MF.ConsoleStyle.ConsoleStyle(buffer)

            let app =
                consoleApplication {
                    title "MF.ConsoleApplication"
                    name "Example"
                    version "1.0.0"
                    info ApplicationInfo.NameAndVersion

                    useOutput console

                    withStyle MF.ConsoleStyle.Style.defaults
                    withCustomTags [
                        customTag
                    ]
                    withCustomTags [
                        MF.ConsoleStyle.CustomTag.createAndParseMarkup (MF.ConsoleStyle.TagName "name") "<c:black|bg:cyan>"
                    ]

                    defaultCommand "run"

                    command "run" {
                        Description = "Test command."
                        Help = None
                        Arguments = []
                        Options = []
                        Initialize = None
                        Interact = None
                        Execute = Execute <| fun (input, output) ->
                            output.Title("Command <name>%s</name> %s", "run", "is running")
                            ExitCode.Success
                    }
                }
                |> runResult [| "--no-ansi" |]

            Expect.equal app (Ok ExitCode.Success) "Run command with most of the options"

            let output =
                buffer.Fetch().Split "\n"
                |> List.ofArray

            let expectedOutput =
                [
                    "Example <1.0.0>"
                    "==============="
                    ""
                    "Command run is running"
                    "======================"
                    ""
                    ""
                ]
            Expect.equal output expectedOutput "Command should run with buffer output."
    ]
