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

[<RequireQualifiedAccess>]
module internal Error =
    open MF.ConsoleStyle

    let show output error =
        let printError =
            match output with
            | Some output -> sprintf "%s\n" >> output.Error
            | _ -> Console.errorf "\n%s\n"

        error
        |> ConsoleApplicationError.format
        |> printError
