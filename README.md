Console Application
===================

[![NuGet Version and Downloads count](https://buildstats.info/nuget/MF.ConsoleApplication)](https://www.nuget.org/packages/MF.ConsoleApplication)
[![Build Status](https://dev.azure.com/MortalFlesh/console-application/_apis/build/status/MortalFlesh.console-application)](https://dev.azure.com/MortalFlesh/console-application/_build/latest?definitionId=1)
[![Build Status](https://api.travis-ci.com/MortalFlesh/console-application.svg?branch=master)](https://travis-ci.com/MortalFlesh/console-application)

> The Console application eases the creation of beautiful and testable command line interfaces in F#.

The Console application allows you to create command-line commands. Your console commands can be used for any recurring task, such as cronjobs, imports, or other batch jobs.

This library is inspired by [Symfony/Style](https://symfony.com/doc/current/console/style.html) and [Symfony/Console](https://symfony.com/doc/current/components/console.html)

## Table of Contents
- [Installation](#installation)
- [Creating a Console Application](#creating-a-console-application)
    - [Builder](#builder)
- [Life-cycle](#life-cycle)
    - [Interaction](#interaction)
- [Console Input](#console-input-arguments--options)
    - [Command Name](#command-name)
        - [Shortcut syntax](#shortcut-syntax)
    - [Arguments](#arguments)
    - [Options](#options)
- [Console Output](#console-output)
- [Default commands](#default-commands)
    - [Help](#help)
    - [List](#list)

## Installation
```sh
dotnet add package MF.ConsoleApplication
```

## Creating a Console Application
```fs
open MF.ConsoleApplication

[<EntryPoint>]
let main argv =
    consoleApplication {
        name "Example"
        version "1.0.0"
        info ApplicationInfo.NameAndVersion

        command "my:first-command" {
            Description = "This is my first command."
            Help = Some "It even has some explicit help. 🎉"
            Arguments = [
                Argument.required "firstName" "First name of the user."
                Argument.optional "lastName" "Last name" None
            ]
            Options = [
                Option.noValue "formal" None "Whether to use a formal greetings."
                Option.optional "yell" (Some "y") "Whether to greet by yelling." None
            ]
            Initialize = None
            Interact = None
            Execute = fun (input, output) ->
                let names =
                    let firstName = input |> Input.getArgumentValue "firstName"

                    match input with
                    | Input.ArgumentOptionalValue "lastName" lastName -> sprintf "%s %s" firstName lastName
                    | _ -> firstName

                let greet name =
                    match input with
                    | Input.IsSetOption "formal" _ -> sprintf "Good morning, %s" name
                    | _ -> sprintf "Hello, %s" name

                let (shouldYell, loudly) =
                    match input with
                    | Input.HasOption "yell" value ->
                        match value |> OptionValue.stringValue with
                        | Some "loud" -> (true, true)
                        | _ -> (true, false)
                    | _ -> (false, false)

                names
                |> greet
                |> fun greetings ->
                    let greetings = if shouldYell then greetings.ToUpper() else greetings

                    if loudly then greetings + "!!!" else greetings + "."
                |> output.Message

                ExitCode.Success
        }
    }
    |> run argv
```

### Outputs:
Command: `dotnet example.dll my:first-command Mortal`

    Example <1.0.0>
    ===============

    Hello, Mortal!

Command: `dotnet example.dll my:first-command Mortal Flesh`

    Example <1.0.0>
    ===============

    Hello, Mortal Flesh!

Command: `dotnet example.dll my:first-command Mortal Flesh --formal`

    Example <1.0.0>
    ===============

    Good morning, Mortal Flesh.

Command: `dotnet example.dll my:first-command Mortal Flesh --yell`

    Example <1.0.0>
    ===============

    HELLO, MORTAL FLESH.

Command: `dotnet example.dll my:first-command Mortal Flesh --yell loud --formal`

    Example <1.0.0>
    ===============

    GOOD MORNING, MORTAL FLESH!!!

Command: `dotnet example.dll my:first-command --help`

    Example <1.0.0>
    ===============

    Description:
        This is my first command.

    Usage:
        my:first-command [options] [--] <firstName> [<lastName>]

    Arguments:
        firstName  First name of the user.
        lastName   Last name

    Options:
            --formal          Whether to use a formal greetings.
        -y, --yell[=YELL]     Whether to greet by yelling.
        -h, --help            Display this help message
        -q, --quiet           Do not output any message
        -V, --version         Display this application version
        -n, --no-interaction  Do not ask any interactive question
        -v|vv|vvv, --verbose  Increase the verbosity of messages

    Help
        It even has some explicit help. 🎉

### Builder

| Function | Arguments | Description |
| --- | --- | --- |
| name | `name: string` | It will set a name of the application. (_This is part of ApplicationInfo._) |
| version | `version: string` | It will set a version of the application. (_This is part of ApplicationInfo._) |
| title | `title: string` | It will set a main title of the application. (_This is part of ApplicationInfo._) |
| info | `ApplicationInfo` | It will define, how application info will be shown in commands output. (Default is `Hidden`) |
| showOptions | `OptionDecorationLevel` | It will define, how options will be shown in the command help output. (Default is `Minimal`) |
| command | `commandName: string`, `CommandDefinition` | It will register a command to the application. |
| defaultCommand | `commandName: string` | It will set a name of default command. Default command is run when no command name is pass to the arguments. (_Default is `list`._) |
| useOutput | `Output` | It will override `Output` in `IO`, which gets every command life-cycle function. (_Default is implemented by [ConsoleStyle](https://github.com/MortalFlesh/console-style)_) |
| useAsk | `question: string -> answer: string` | It will override an Ask function, which is used in `Interact` life-cycle stage. (_Default is implemented by [ConsoleStyle](https://github.com/MortalFlesh/console-style#ask))_ |

NOTES:
- All functions has the first argument for the `state: Definition`, but this is a current state of the application and it is passed implicitly in the background by computation expression.
- All functions are optional to call. Those which _sets_ a value will override the previous definition.

## Life-cycle
Commands have three life-cycle functions that are invoked when running the command:

- `Initialize` (_optional_)
    - This function is executed before the `interact` and the `execute` functions. Its main purpose is to initialize variables used in the rest of the command functions.
- `Interact` (_optional_)
    - This function is executed after `initialize` and before `execute`. Its purpose is to check if some of the options/arguments are missing and interactively ask the user for those values. This is the last place where you can ask for missing options/arguments. After this function, missing options/arguments will result in an error.
    - This _stage_ may be skipped by setting `--no-interaction` option.
- `Execute` (required)
    - This method is executed after `initialize` and `interact`. It contains the logic you want the command to execute.

All life-cycle functions gets an `IO` (`Input * Output`).

Both `Initialize` and `Interact` may change the `IO` on the way. `Execute` will get final result of `IO`.

_TIP: There is a Type for every Life-cycle function._

### Interaction
As was mentioned before, its purpose is to check if some of the options/arguments are missing and interactively ask the user for those values.

Interact function gets an `InteractiveInput`, which is `{ Input: Input; Ask: Ask }` record, where `Ask` is `string -> string` function.

#### Examples:

Add missing argument:
```fs
Interact = Some (fun ({ Input = input; Ask = ask }, output) ->
    let input =
        match input with
        | Input.HasArgument "mandatoryArg" _ -> input   // already has a value
        | _ ->  // value is missing, as user for a value
            ask "Please, give a value for mandatory argument:"
            |> Input.setArgumentValue input "mandatoryArg"

    (input, output)
)
```

Add missing option:
```fs
Interact = Some (fun (input, output) ->
    let input =
        match input.Input with
        | Input.HasOption "message" value ->
            output.Message <| sprintf "Message value is already given from arguments, it is: %s" (value |> OptionValue.value)
            input.Input
        | _ ->
            input.Ask "Message:"
            |> Input.setOptionValue input.Input "message"

    (input, output)
)
```

## Console Input (Arguments & Options)
The most interesting part of the commands are the arguments and options that you can make available. These arguments and options allow you to pass dynamic information from the terminal to the command.

### Command name
Command name is a special type of _required_ argument, which has a reserved name (`command`) and will always be there (_if it is not passed by user, it will be a default command_).

#### Shortcut Syntax
You do not have to type out the full command names. You can just type the shortest unambiguous name to run a command. So if there are non-clashing commands, then you can run help like this:

```sh
dotnet example.dll h
```

If you have commands using `:` to namespace commands then you only need to type the shortest unambiguous text for each part. If you have created the `my:first-command` as shown above then you can run it with:

```sh
dotnet example.dll m:f Mortal Flesh
```

If you enter a short command that's ambiguous (_i.e. there are more than one command that match_), then no command will be run and some suggestions of the possible commands to choose from will be output.

### Arguments
Arguments are the strings - separated by spaces - that come after the command name itself. They are ordered, and can be optional or required.
It is also possible to let an argument take a list of values (_only the last argument ca be a list_).

Note: There is a more [detailed documentation here](ARGUMENTS.md).

There are four argument variants you can use:

- Required
    - The argument is mandatory. The command doesn't run if the argument isn't provided
- Optional
    - The argument is optional and therefore can be omitted.
- RequiredArray
    - The argument can contain one or more values. For that reason, it must be used at the end of the argument list.
- Array
    - The argument can contain any number of values. For that reason, it must be used at the end of the argument list.

There are many ways how to access Arguments:

- Through pattern matching

    | Active Pattern                | Description |
    | ---                           | ---         |
    | _Input_.**IsArgumentDefined** | Matched when given string is defined as argument name. |
    | _Input_.**HasArgument**       | Matched when given string has any value in current Input (_default or from args_). |
    | _Input_.**IsSetArgument**     | Matched when input _has_ argument AND that value is _not empty_. |

    - Active patterns for accessing a value

    | Active Pattern                    | Description | Value |
    | ---                               | ---         | ---   |
    | _Input_.**ArgumentValue**         | Matched when input _has_ argument. (_Fail with exception when value is not set or it is a list._) | `string` |
    | _Input_.**ArgumentOptionalValue** | Matched when input _has_ argument AND it has a single value. | `string` |
    | _Input_.**ArgumentListValue**     | Matched when input _has_ argument. | `string list` |

- Just get a value from `Input`

    | Function                           | Description |
    | ---                                | ---         |
    | _Input_.**tryGetArgument**           | Returns an `ArgumentValue option`, when Input _has_ argument. |
    | _Input_.**getArgument**              | Returns an `ArgumentValue`, when Input _has_ argument OR fail with exception. |
    | _Input_.**getArgumentValue**         | Returns a `string` value from ArgumentValue, when Input _has_ argument OR fail with exception. |
    | _Input_.**getArgumentValueAsString** | Returns a `string option` value from ArgumentValue, when Input _has_ argument. |
    | _Input_.**getArgumentValueAsInt**    | Returns an `int option` value from ArgumentValue, when Input _has_ argument. (_It fails with an exception when string value is not int._) |
    | _Input_.**getArgumentValueAsList**   | Returns an `string list` value from ArgumentValue, when Input _has_ argument. (_It returns a list even for single values._) |
    | _Input_.**tryGetArgumentValueAsInt** | Returns an `int option` value from ArgumentValue, when Input _has_ argument. (_It returns None when string value is not int._) |
    | _Input_.**isArgumentValueSet**       | Checks whether argument has a value AND that value is _not empty_. |

    Note: All functions above will fail with an exception when given "argument" is not defined.

### Options
Unlike arguments, options are not ordered (_meaning you can specify them in any order_) and are specified with two dashes (_e.g. --yell_). Options are always optional, and can be setup to accept a value (_e.g. --dir=src_) or simply as a boolean flag without a value (_e.g. --yell_).

You can also declare a one-letter shortcut that you can call with a single dash (_e.g. -y_).

Note that to comply with the [docopt standard](http://docopt.org/), long options can specify their values after a white space or an `=` sign (_e.g. --iterations 5 or --iterations=5_), but short options can only use white spaces or no separation at all (_e.g. -i 5 or -i5_).

Note: There is a more [detailed documentation here](OPTIONS.md).

There are five option variants you can use:

- ValueNone
    - Do not accept input for this option (_e.g. --yell_).
- ValueRequired
    - This value is required (_e.g. --iterations=5 or -i5_), the option itself is still optional;
- ValueOptional
    - This option may or may not have a value (_e.g. --yell or --yell=loud_).
- ValueIsArray
    - This option accepts multiple values (_e.g. --dir=/foo --dir=/bar_)
- ValueRequiredArray
    - This option accepts multiple not empty values (_e.g. --dir=/foo --dir=/bar_)

#### Application Options

Are built-in options, which every command has. And they are parsed before other given arguments. They may even bypass other values.

- Help (`--help`, `-h`)
    - If only `--help` option is passed, it will show overall help for console application.
    - You can get help information for any command, if you pass a command name and `--help` option (_this would ignore any other options_).
- Version (`--version`, `-V`)
    - This will show current application name and version. (_Default name is `Console Application`_)
- NoInteraction (`--no-interaction`, `-n`)
    - You can suppress any interactive questions from the command you are running with this option.
- Quiet (`--quiet`, `-q`)
    - You can suppress output with this option.
- Verbose (`--verbose`, `-v|vv|vvv`)
    - You can get more verbose message (_if this is supported for a command_).
    - Number of given `v` determines a level of verbosity.

There are many ways how to access Options:

- Through pattern matching

    | Active Pattern              | Description |
    | ---                         | ---         |
    | _Input_.**IsOptionDefined** | Matched when given string is defined as option name. |
    | _Input_.**HasOption**       | Matched when given string has any value in current Input (_default or from args_). |
    | _Input_.**IsSetOption**     | Matched when input _has_ option AND that value is _not empty_. |

    - Active patterns for accessing a value

    | Active Pattern                  | Description | Value |
    | ---                             | ---         | ---   |
    | _Input_.**OptionValue**         | Matched when input _has_ option. (_Fail with exception when value is not set or it is a list._) | `string` |
    | _Input_.**OptionOptionalValue** | Matched when input _has_ option AND it has a single value. | `string` |
    | _Input_.**OptionListValue**     | Matched when input _has_ option. | `string list` |

- Just get a value from `Input`

    | Function                           | Description |
    | ---                                | ---         |
    | _Input_.**tryGetOption**           | Returns an `OptionValue option`, when Input _has_ option. |
    | _Input_.**getOption**              | Returns an `OptionValue`, when Input _has_ option OR fail with exception. |
    | _Input_.**getOptionValue**         | Returns a `string` value from OptionValue, when Input _has_ option OR fail with exception. |
    | _Input_.**getOptionValueAsString** | Returns a `string option` value from OptionValue, when Input _has_ option. |
    | _Input_.**getOptionValueAsInt**    | Returns an `int option` value from OptionValue, when Input _has_ option. (_It fails with an exception when string value is not int._) |
    | _Input_.**getOptionValueAsList**   | Returns an `string list` value from OptionValue, when Input _has_ option. (_It returns a list even for single values._) |
    | _Input_.**tryGetOptionValueAsInt** | Returns an `int option` value from OptionValue, when Input _has_ option. (_It returns None when string value is not int._) |
    | _Input_.**isOptionValueSet**       | Checks whether option has a value AND that value is _not empty_. |

    Note: All functions above will fail with an exception when given "option" is not defined.

## Console Output
Output is handled by [ConsoleStyle](https://github.com/MortalFlesh/console-style).

You can even alter an Output by using `useOutput` function to set your own implementation.

---
## Default commands

There are two default commands:

- List
    - Shows list of available commands.
    - It is a default command, when you do not specify your own (_by `defaultCommand` console application function_), it means, when user do not specify a command in arguments, the `List` will be used.
- Help
    - Shows help for commands.
    - It can be triggered by `--help` (`-h`) option as well.

There are some different ways to display the same output:
- `help`, `help --help` or `help help` will execute a `Help` command for itself
- `list` or _no-command_ will execute `list` command (_when no other default command is set_)
- `list --help`, `help list` or `--help` will execute `Help` command for `List` command

### Help
Displays help for a command
```sh
dotnet path/to/console.dll help
```

    {application info}

    Description:
        Displays help for a command

    Usage:
        help [options] [--] [<command_name>]

    Arguments:
        command_name  The command name [default: "help"]

    Options:
        -h, --help            Display this help message
        -q, --quiet           Do not output any message
        -V, --version         Display this application version
        -n, --no-interaction  Do not ask any interactive question
        -v|vv|vvv, --verbose  Increase the verbosity of messages

    Help
        The help command displays help for a given command:

            dotnet path/to/console.dll help list

        To display list of available commands, please use list command.

### List
Show list of available commands.
```sh
dotnet path/to/console.dll list
```

    {application info}

    Description:
        Lists commands

    Usage:
        list [options] [--] [<namespace>]

    Arguments:
        namespace  The namespace name

    Options:
        -h, --help            Display this help message
        -q, --quiet           Do not output any message
        -V, --version         Display this application version
        -n, --no-interaction  Do not ask any interactive question
        -v|vv|vvv, --verbose  Increase the verbosity of messages

    Help
        The list command lists all commands:

            dotnet path/to/console.dll list

        You can also display the commands for a specific namespace:

            dotnet path/to/console.dll list test

### Create help message
There are some placeholders which may help you to create a better help message.

- `{{command.name}}` - The name of the current command.
- `{{command.full_name}}` - The name of the current command including a relative path.

This is a help, used in `list` command:
```fs
Help =
    [
        "The <c:dark-green>{{command.name}}</c> command lists all commands:"
        "        <c:dark-green>dotnet {{command.full_name}}</c>"
        "    You can also display the commands for a specific namespace:"
        "        <c:dark-green>dotnet {{command.full_name}} test</c>"
    ]
    |> String.concat "\n\n"
    |> Some
```

## Tips

Add `bin/console` file with following content to allow a simple entry point for your application

```sh
#!/usr/bin/env bash

APP="my-console"
NET="net6.0"

CONSOLE="bin/Debug/$NET/$APP.dll"
if [ ! -f "$CONSOLE" ]; then
    CONSOLE="bin/Release/$NET/$APP.dll"
fi

dotnet "$CONSOLE" "$@"
```

Then just go by `bin/console list` or other commands.
