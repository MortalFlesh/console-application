module MF.ConsoleApplication.Tests.Commands

open MF.ConsoleApplication

let commandOne executeCallback: CommandDefinition =
    {
        Description = "Test command 1."
        Help = None
        Arguments = [
            Argument.required "mandatoryArg" "Mandatory argument"
            Argument.optional "optionalArg" "Optional argument" (Some "default")
        ]
        Options = [
            Option.required "opt1" (Some "o") "Option one" "opt1-default-value"
            Option.optional "opt2" (Some "O") "Option two" None
            Option.optional "message" None "Message option" None
        ]
        Initialize = None
        Interact = Some (fun ({ Input = input }, output) ->
            let input =
                match input with
                | Input.IsSetOption "message" _ -> input
                | _ -> "from-interaction" |> Input.setOptionValue input "message"

            (input, output)
        )
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandTwo executeCallback: CommandDefinition =
    {
        Description = "Test command 2."
        Help = None
        Arguments = [
            Argument.required "mandatoryArg" "Mandatory argument"
            Argument.optionalArray "argumentList" "Argument list" None
        ]
        Options = [
            Option.optional "opt1" (Some "o") "Option one" None
            Option.optionalArray "item" (Some "i") "Option one" None
        ]
        Initialize = None
        Interact = None
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandThree executeCallback: CommandDefinition =
    {
        Description = "Test command 3."
        Help = None
        Arguments = [
            Argument.optional "optionalArg" "Optional argument" None
            Argument.optionalArray "argumentList" "Argument list" None
        ]
        Options = [
            Option.required "opt1" (Some "o") "Option one" "opt1-default-value"
            Option.requiredArray "item" (Some "i") "Items" (Some ["foo"])
        ]
        Initialize = None
        Interact = None
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandFour executeCallback: CommandDefinition =
    {
        Description = "Test command 4."
        Help = None
        Arguments = [
            Argument.required "mandatoryArg" "Mandatory argument"
            Argument.requiredArray "mandatoryArgumentList" "Mandatory argument list"
        ]
        Options = [
            Option.optional "opt1" (Some "o") "Option one" (Some "opt1-default-value")
            Option.optionalArray "item" (Some "i") "Items" (Some ["foo"; "bar"])
        ]
        Initialize = None
        Interact = None
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandFive executeCallback: CommandDefinition =
    {
        Description = "Test command 5."
        Help = None
        Arguments = [
            Argument.required "mandatoryArg" "Mandatory argument"
        ]
        Options = []
        Initialize = None
        Interact = Some (fun ({ Input = input; Ask = ask }, output) ->
            let input =
                match input with
                | Input.HasArgument "mandatoryArg" _ -> input
                | _ -> ask "Add mandatory argument" |> Input.setArgumentValue input "mandatoryArg"

            (input, output)
        )
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandSix executeCallback: CommandDefinition =
    {
        Description = "Test command 6 - for testing https://symfony.com/doc/current/components/console/console_arguments.html."
        Help = None
        Arguments = [
            Argument.optional "arg" "Optional argument" None
        ]
        Options = [
            Option.noValue "foo" (Some "f") ""
            Option.required "bar" (Some "b") "" ""
            Option.optional "cat" (Some "c") "" None
        ]
        Initialize = None
        Interact = None
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }

let commandSeven executeCallback: CommandDefinition =
    {
        Description = "Test command 7"
        Help = None
        Arguments = [
            Argument.optional "arg" "Optional argument" None
        ]
        Options = [
            Option.noValue "force" (Some "f") ""
            Option.required "api-url" None "" "https://api"
            Option.optional "output" (Some "o") "If specified, output goes to the file, if just presented stdout is used." None
        ]
        Initialize = None
        Interact = None
        Execute = fun (input, output) ->
            executeCallback input
            ExitCode.Success
    }
