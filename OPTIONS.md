Options
=======

Unlike arguments, options are not ordered (_meaning you can specify them in any order_) and are specified with two dashes (_e.g. --yell_). Options are always optional, and can be setup to accept a value (_e.g. --dir=src_) or simply as a boolean flag without a value (_e.g. --yell_).

You can also declare a one-letter shortcut that you can call with a single dash (_e.g. -y_).

Note that to comply with the [docopt standard](http://docopt.org/), long options can specify their values after a white space or an `=` sign (_e.g. --iterations 5 or --iterations=5_), but short options can only use white spaces or no separation at all (_e.g. -i 5 or -i5_).

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

## Application Options
> Or Global options

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
    ```fs
    let (optionValue: OptionValue option) = // raw value could be null or None or empty list
        match input with
        | Input.Option.Has "optionName" optionValue -> Some optionValue
        | _ -> None

    let (optionValue: OptionValue option) = // raw value is neither null nor None nor empty list
        match input with
        | Input.Option.IsSet "optionName" optionValue -> Some optionValue
        | _ -> None
    ```
    - All Input active patterns for matching Options

    | Active Pattern              | Description |
    | ---                         | ---         |
    | _Input_._Option_.**IsDefined** | Matched when given string is defined as option name. |
    | _Input_._Option_.**Has**       | Matched when given string has any value in current Input (_default or from args_). |
    | _Input_._Option_.**IsSet**     | Matched when input _has_ option AND that value is _not empty_. |

    - Active patterns for accessing a value

    | Active Pattern                  | Description | Value |
    | ---                             | ---         | ---   |
    | _Input_._Option_.**Value**         | Matched when input _has_ option. (_Fail with exception when value is not set or it is a list._) | `string` |
    | _Input_._Option_.**OptionalValue** | Matched when input _has_ option AND it has a single value. | `string` |
    | _Input_._Option_.**ListValue**     | Matched when input _has_ option. | `string list` |

- Just get a value from `Input`
    ```fs
    let optionValue: OptionValue option = input |> Input.Option.get "optionName"
    let optionValue: string = input |> Input.Option.value "optionName"    // or exception
    let optionValue: string option = input |> Input.Option.asString "optionName"
    let optionValue: int option = input |> Input.Option.asInt "optionName"   // or exception
    let optionValue: int option = input |> Input.Option.tryValueAsInt "optionName"
    let optionValue: string list = input |> Input.Option.asList "optionName"
    ```
    - All Input functions for accessing Options

    | Function                         | Description |
    | ---                              | ---         |
    | _Input_._Option_.**tryGet**      | Returns an `OptionValue option`, when Input _has_ option. |
    | _Input_._Option_.**get**         | Returns an `OptionValue`, when Input _has_ option OR fail with exception. |
    | _Input_._Option_.**value**       | Returns a `string` value from OptionValue, when Input _has_ option OR fail with exception. |
    | _Input_._Option_.**asString**    | Returns a `string option` value from OptionValue, when Input _has_ option. |
    | _Input_._Option_.**asInt**       | Returns an `int option` value from OptionValue, when Input _has_ option. (_It fails with an exception when string value is not int._) |
    | _Input_._Option_.**asList**      | Returns an `string list` value from OptionValue, when Input _has_ option. (_It returns a list even for single values._) |
    | _Input_._Option_.**tryAsInt**    | Returns an `int option` value from OptionValue, when Input _has_ option. (_It returns None when string value is not int._) |
    | _Input_._Option_.**isValueSet**  | Checks whether option has a value AND that value is _not empty_. |

    Note: All functions above will fail with an exception when given "option" is not defined.

- Directly `Input.Options` - it is `Map<string, OptionValue>`
    ```fs
    let optionValue: OptionValue = input.Options.["optionName"] // or exception
    ```

## What value will OptionValue has?

| ValueDefinition    | Has Default | In args | OptionValue    |
| ---                | ---         | ---     | ---            |
| ValueNone          | -           | Yes     | _true_         |
|                    | -           | No      | _false_        |
| ValueRequired      | Yes         | Yes     | from Args      |
|                    | Yes         | No      | default        |
| ValueOptional      | Yes         | Yes     | Some from Args |
|                    | Yes         | No      | Some default   |
|                    | No          | Yes     | Some from Args |
|                    | No          | No      | None           |
| ValueIsArray       | Yes         | Yes     | [from Args]    |
|                    | Yes         | No      | [default]      |
|                    | No          | Yes     | [from Args]    |
|                    | No          | No      | []             |
| ValueRequiredArray | Yes         | Yes     | [from Args]    |
|                    | Yes         | No      | [default]      |
|                    | No          | Yes     | [from Args]    |

## Handle OptionValue
You can use `OptionValue` functions for all `OptionValue` cases, but some of them may fail, see next table to clarify possibilities

```fs
// you may match value for "raw value"
// this might be unnecessary since you should know how option is defined (value corresponds)
match optionValue with
| OptionValue.ValueNone -> // ...
| OptionValue.ValueRequired rawValue -> // ...
| OptionValue.ValueOptional rawValueOption -> // ...
| OptionValue.ValueIsArray rawValues -> // ...
| OptionValue.ValueRequiredArray rawValues -> // ...
```

Or access `OptionValue` with function by `OptionValue definition`
```fs
// for ValueNone use:
let (rawValue: bool) = optionValue |> OptionValue.isSet

// for ValueRequired use:
let (rawValue: string) = optionValue |> OptionValue.value

// for ValueOptional use:
let (rawValue: string option) = optionValue |> OptionValue.stringValue

// for ValueIsArray use:
let (rawValue: string list) = optionValue |> OptionValue.listValue

// for ValueRequiredArray use:
let (rawValue: string list) = optionValue |> OptionValue.listValue
```

| Function      | Value Definition   | Value | Result       |
| ---           | ---                | ---   | ---          |
| value         | ValueNone          | Yes   | _Exception_  |
|               |                    | No    | _Exception_  |
|               | ValueRequired      | Yes   | string       |
|               | ValueOptional      | Yes   | string       |
|               |                    | No    | _Exception_  |
|               | ValueIsArray       | Yes   | _Exception_  |
|               |                    | No    | _Exception_  |
|               | ValueRequiredArray | Yes   | _Exception_  |
|               |                    | No    | _Exception_  |
| stringValue   | ValueNone          | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequired      | Yes   | Some string  |
|               | ValueOptional      | Yes   | Some string  |
|               |                    | No    | None         |
|               | ValueIsArray       | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequiredArray | Yes   | None         |
|               |                    | No    | None         |
| intValue      | ValueNone          | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequired      | Yes   | Some int or _Exception_ |
|               | ValueOptional      | Yes   | Some int or _Exception_ |
|               |                    | No    | None         |
|               | ValueIsArray       | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequiredArray | Yes   | None         |
|               |                    | No    | None         |
| tryIntValue   | ValueNone          | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequired      | Yes   | int option   |
|               | ValueOptional      | Yes   | int option   |
|               |                    | No    | None         |
|               | ValueIsArray       | Yes   | None         |
|               |                    | No    | None         |
|               | ValueRequiredArray | Yes   | None         |
|               |                    | No    | None         |
| listValue     | ValueNone          | Yes   | []           |
|               |                    | No    | []           |
|               | ValueRequired      | Yes   | [string]     |
|               | ValueOptional      | Yes   | [string]     |
|               |                    | No    | []           |
|               | ValueIsArray       | Yes   | string list  |
|               |                    | No    | []           |
|               | ValueRequiredArray | Yes   | string list  |
|               |                    | No    | []           |
| isSet         | ValueNone          | Yes   | true         |
|               |                    | No    | false        |
|               | ValueRequired      | Yes   | true (_unless value is null_) |
|               | ValueOptional      | Yes   | true (_unless value is null_) |
|               |                    | No    | false        |
|               | ValueIsArray       | Yes   | true         |
|               |                    | No    | false        |
|               | ValueRequiredArray | Yes   | true         |
|               |                    | No    | false        |

## Handle Shortcut edge-cases
How to handle edge-cases when the commands define options with required values, without values, etc.

Let's have a command, which define three options:
```fs
Options = [
    Option.noValue "foo" (Some "f") ""
    Option.required "bar" (Some "b") "" None
    Option.optional "cat" (Some "c") "" None
]
```

Since the `foo` option doesn't accept a value, it will be either `false` (_when it is not passed to the command_) or `true` (_when `--foo` was passed by the user_). The value of the `bar` option (_and its `b` shortcut respectively_) is **required**. It can be separated from the option name either by _spaces_ or _=_ characters. The `cat` option (_and its `c` shortcut_) behaves similar except that it doesn't require a value. Have a look at the following table to get an overview of the possible ways to pass options:

| Input               | foo     | bar        | cat        |
| ---                 | ---     | ---        | ---        |
| `--bar=Hello`       | `false` | `"Hello"`  | `null`     |
| `--bar Hello`       | `false` | `"Hello"`  | `null`     |
| `-b=Hello`          | `false` | `"=Hello"` | `null`     |
| `-b Hello`          | `false` | `"Hello"`  | `null`     |
| `-bHello`           | `false` | `"Hello"`  | `null`     |
| `-fcWorld -b Hello` | `true`  | `"Hello"`  | `"World"`  |
| `-cfWorld -b Hello` | `false` | `"Hello"`  | `"fWorld"` |
| `-cbWorld`          | `false` | `null`     | `"bWorld"` |

Things get a little bit more tricky when the command also accepts an optional argument:

```fs
Arguments = [
    Argument.optional "arg" "Optional argument" None
]
```

You might have to use the special `--` separator to separate options from arguments. Have a look at the fifth example in the following table where it is used to tell the command that World is the value for `arg` and not the value of the optional `cat` option:

| Input                        | bar             | cat       | arg       |
| ---                          | ---             | ---       | ---       |
| `--bar Hello`                | `"Hello"`       | `null`    | `null`    |
| `--bar Hello World`          | `"Hello"`       | `null`    | "World"   |
| `--bar "Hello World"`        | `"Hello World"` | `null`    | `null`    |
| `--bar Hello --cat World`    | `"Hello"`       | `"World"` | `null`    |
| `--bar Hello --cat -- World` | `"Hello"`       | `null`    | `"World"` |
| `-b Hello -c World`          | `"Hello"`       | `"World"` | `null`    |
