namespace MF.ConsoleApplication

[<RequireQualifiedAccess>]
module internal Help =
    open System
    open OptionsOperators

    let private replaceHelpPlaceholders commandName help =
        // AppDomain.CurrentDomain.FriendlyName  // example
        // AppDomain.CurrentDomain.BaseDirectory // /Users/<user>/fsharp/console-application/example/bin/Debug/netcoreapp2.2/
        // Environment.CurrentDirectory          // /Users/<user>/fsharp/console-application/example
        // Environment.CommandLine               // /Users/<user>/fsharp/console-application/example/bin/Debug/netcoreapp2.2/example.dll list --help

        let baseDir = AppDomain.CurrentDomain.BaseDirectory
        let executableFileName = AppDomain.CurrentDomain.FriendlyName
        let currentDir = Environment.CurrentDirectory

        let relativePath = baseDir.Replace(currentDir, "").TrimStart '/'
        let commandName = commandName |> CommandName.value

        [
            "{{command.name}}", commandName
            "{{command.full_name}}", sprintf "%s%s.dll %s" relativePath executableFileName commandName
        ]
        |> List.fold (fun (help: string) (placeholder, value) ->
            help.Replace(placeholder, value)
        ) help

    let showForCommand output decorationLevel applicationOptions commandName (command: Command) =
        let options = command.Options @ applicationOptions

        output.SimpleOptions "Description:" [
            command.Description, ""
        ]

        output.SimpleOptions "Usage:" [
            sprintf "%s %s %s"
                (commandName |> CommandName.value)
                (options |> OptionsDefinitions.usage decorationLevel)
                (
                    match command.Arguments with
                    | [] -> ""
                    | arguments ->
                        let formattedArguments = arguments |> List.map Argument.usage

                        (sprintf "[%s]" Arguments.Separator) :: formattedArguments
                        |> String.concat " "
                ), ""
        ]

        match command.Arguments with
        | [] -> ()
        | arguments ->
            arguments
            |> List.map Argument.format
            |> output.SimpleOptions "Arguments:"

        match options with
        | [] -> ()
        | options ->
            options
            |> OptionsDefinitions.format
            |> output.SimpleOptions "Options:"

        command.Help
        |>! (fun help ->
            output.SimpleOptions "Help" [help |> replaceHelpPlaceholders commandName, ""]
        )

    let showSingleLine output applicationOptions (commandName, command) =
        // list [--raw] [--format FORMAT] [--] [<namespace>]\n
        let options = command.Options @ applicationOptions

        sprintf "<c:dark-green>%s %s %s</c>\n"
            (commandName |> CommandName.value)
            (options |> OptionsDefinitions.usage Complete)
            (
                match command.Arguments with
                | [] -> ""
                | arguments ->
                    let formattedArguments = arguments |> List.map Argument.usage

                    (sprintf "[%s]" Arguments.Separator) :: formattedArguments
                    |> String.concat " "
            )
        |> output.Message

[<RequireQualifiedAccess>]
module internal Error =
    open MF.ConsoleStyle
    open OptionsOperators

    let show (ConsoleApplication application) commandInfo error =
        match application with
        | Ok parts ->
            let output = parts.Output
            let printError = sprintf "%s\n" >> output.Error

            error
            |> ConsoleApplicationError.format
            |> printError

            commandInfo
            |>! Help.showSingleLine output parts.ApplicationOptions
        | Error e ->
            e
            |> ConsoleApplicationError.format
            |> Console.errorf "\n%s\n"
