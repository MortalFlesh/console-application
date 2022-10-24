module MF.ConsoleApplication.Tests.DefaultCommands

open System
open Expecto
open MF.ConsoleApplication

type TestCase = {
    Description: string
    Command: string
    ExpectedOutput: string list
}

let provideDefaultCommands = seq {
    let header = [
        "Default command test <1.0.0>"
        "============================"
        ""
    ]

    {
        Description = "Help command"
        Command = "help"
        ExpectedOutput =
            [
                yield! header

                "Description:"
                "    Displays help for a command"
                ""
                "Usage:"
                "    help [options] [--] [<command_name>]"
                ""
                "Arguments:"
                "    command_name  The command name [default: \"help\"]"
                ""
                "Options:"
                "    -h, --help            Display this help message           "
                "    -q, --quiet           Do not output any message           "
                "    -V, --version         Display this application version    "
                "    -n, --no-interaction  Do not ask any interactive question "
                "        --no-progress     Whether to disable all progress bars"
                "    -v|vv|vvv, --verbose  Increase the verbosity of messages  "
                ""
                "Help:"
                "    The help command displays help for a given command:"
                ""
                "        dotnet bin/Debug/net6.0/tests.dll help list"
                ""
                "    To display list of available commands, please use list command."
                ""
                ""
            ]
    }

    {
        Description = "List command"
        Command = "list"
        ExpectedOutput =
            [
                yield! header

                "Usage:"
                "    command [options] [--] [arguments]  "
                ""
                "Options:"
                "    -h, --help            Display this help message           "
                "    -q, --quiet           Do not output any message           "
                "    -V, --version         Display this application version    "
                "    -n, --no-interaction  Do not ask any interactive question "
                "        --no-progress     Whether to disable all progress bars"
                "    -v|vv|vvv, --verbose  Increase the verbosity of messages  "
                ""
                "Available commands:"
                "    about  Displays information about the current project"
                "    help   Displays help for a command                   "
                "    list   Lists commands                                "
                ""
                ""
            ]
    }

    {
        Description = "About command - help"
        Command = "about -h"
        ExpectedOutput =
            [
                yield! header

                "Description:"
                "    Displays information about the current project"
                ""
                "Usage:"
                "    about [options] "
                ""
                "Options:"
                "    -h, --help            Display this help message           "
                "    -q, --quiet           Do not output any message           "
                "    -V, --version         Display this application version    "
                "    -n, --no-interaction  Do not ask any interactive question "
                "        --no-progress     Whether to disable all progress bars"
                "    -v|vv|vvv, --verbose  Increase the verbosity of messages  "
                ""
                "Help:"
                "    The about command displays information about the current project:"
                ""
                "        dotnet bin/Debug/net6.0/tests.dll about about"
                ""
                "    There are multiple sections shown in the output:"
                "      - current project details/meta information"
                "      - environment"
                "      - console application library"
                ""
                ""
            ]
    }

    {
        Description = "About command"
        Command = "about"
        ExpectedOutput =
            let version = AssemblyVersionInformation.AssemblyVersion
            let spaces value = String.replicate (101 - (string value).Length) " "
            let createdAt = AssemblyVersionInformation.AssemblyMetadata_createdAt.[0 .. "yyyy-mm-dd".Length - 1]
            let gitCommit = AssemblyVersionInformation.AssemblyMetadata_gitcommit
            let dotnetVersion = string Environment.Version

            [
                yield! header

                "                                                                                                          "
                "  Default command test             .NET Core                 Git Branch            MF/ConsoleApplication  "
                $"          1.0.0                     {dotnetVersion}                    <branch>              {version} ({createdAt})    "
                "                                                                                                          "
                ""
                "----------------------- ------------------------------------------------------------------------------------------------------"
                " Application           "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                " Name                    Default command test                                                                                 "
                " Version                 1.0.0                                                                                                "
                " Description             About command                                                                                        "
                " Environment             Test                                                                                                 "
                " command                 about                                                                                                "
                " args                                                                                                                         "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                " Environment           "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                $" .NET Core               {dotnetVersion}{spaces dotnetVersion}"
                $" Command Line            {Environment.CommandLine}{spaces Environment.CommandLine}"
                $" Current Directory       {Environment.CurrentDirectory}{spaces Environment.CurrentDirectory}"
                $" Machine Name            {Environment.MachineName}{spaces Environment.MachineName}"
                $" OS Version              {Environment.OSVersion}{spaces Environment.OSVersion}"
                $" Processor Count         {Environment.ProcessorCount}{spaces Environment.ProcessorCount}"
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                " Git                   "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                " Branch                  <branch>                                                                                             "
                " Commit                  <commit>                                                                                             "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                " MF/ConsoleApplication "
                " ---------------------   ---------------------------------------------------------------------------------------------------- "
                $" Version                 {version}{spaces version}"
                $" Commit                  {gitCommit}{spaces gitCommit}"
                $" Released                {createdAt}{spaces createdAt}"
                "----------------------- ------------------------------------------------------------------------------------------------------"
                ""
                ""
            ]
    }
}

[<Tests>]
let defaultCommandsTests =
    testList "ConsoleApplication - default commands" [
        yield!
            provideDefaultCommands
            |> Seq.map (fun { Description = desc; Command = command; ExpectedOutput = expected } ->
                testCase $"command {desc}" <| fun _ ->
                    use buffer = new MF.ConsoleStyle.Output.BufferOutput(MF.ConsoleStyle.Verbosity.Normal)
                    let console = MF.ConsoleStyle.ConsoleStyle(buffer)

                    let cmd, args =
                        match command.Split(" ") |> List.ofArray with
                        | [] -> "", ""
                        | cmd :: args -> cmd, (args |> String.concat " ")

                    let app =
                        consoleApplication {
                            title "MF.ConsoleApplication.Test"
                            name "Default command test"
                            description desc
                            version "1.0.0"
                            info ApplicationInfo.NameAndVersion

                            git (
                                None,
                                Some "<branch>",
                                Some "<commit>"
                            )

                            meta ("Environment", "Test")
                            meta [
                                "command", cmd
                                "args", args
                            ]

                            useOutput console
                        }
                        |> runResult (command.Split " ")

                    Expect.equal app (Ok ExitCode.Success) $"Command {command} should be always successful"

                    let output =
                        (buffer.Fetch() |> console.RemoveMarkup).Split "\n"
                        |> List.ofArray

                    Expect.equal output expected "Command should run with buffer output."
            )
    ]
