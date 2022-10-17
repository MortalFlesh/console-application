namespace MF.ConsoleApplication

[<RequireQualifiedAccess>]
module internal Help =
    open System
    open OptionsOperators

    let private createUsage decorationLevel options commandName command =
        sprintf "%s %s %s"
            (commandName |> CommandName.value)
            (decorationLevel |> OptionsDefinitions.usage options)
            (
                match command.Arguments with
                | [] -> ""
                | arguments ->
                    let formattedArguments = arguments |> List.map Argument.usage

                    (sprintf "[%s]" Arguments.Separator) :: formattedArguments
                    |> String.concat " "
            )

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
        |> List.fold String.replace help

    let showForCommand (output: Output) decorationLevel applicationOptions commandName (command: Command) =
        let options = command.Options @ applicationOptions

        output.SimpleOptions "Description:" [
            [ command.Description ]
        ]

        output.SimpleOptions "Usage:" [
            [ createUsage decorationLevel options commandName command ]
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
            output.SimpleOptions "Help" [ [ help |> replaceHelpPlaceholders commandName ] ]
        )

    let showSingleLine (output: Output) applicationOptions (commandName, command) =
        // list [--raw] [--format FORMAT] [--] [<namespace>]\n
        let options = command.Options @ applicationOptions

        createUsage Complete options commandName command
        |> sprintf "<c:dark-green>%s</c>\n"
        |> output.Message

[<RequireQualifiedAccess>]
module internal Error =
    open MF.ConsoleStyle
    open OptionsOperators

    let show (ConsoleApplication application) currentCommand error =
        match application with
        | Ok parts ->
            error
            |> ConsoleApplicationError.format
            |> List.iter parts.Output.Error

            currentCommand
            |>! Help.showSingleLine parts.Output parts.ApplicationOptions
        | Result.Error e ->
            e
            |> ConsoleApplicationError.format
            |> List.iter Output.defaults.Error
