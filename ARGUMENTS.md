Arguments
=========

Arguments are the strings - separated by spaces - that come after the command name itself. They are ordered, and can be optional or required.
It is also possible to let an argument take a list of values (_only the last argument ca be a list_).

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
    ```fs
    let (argumentValue: ArgumentValue option) = // raw value could be null or None or empty list
        match input with
        | Input.Argument.Has "argumentName" argumentValue -> Some argumentValue
        | _ -> None

    let (argumentValue: ArgumentValue option) = // raw value is neither null nor None nor empty list
        match input with
        | Input.Argument.IsSet "argumentName" argumentValue -> Some argumentValue
        | _ -> None

    let (argumentValue: string option) =
        match input with
        | Input.Argument.Value "argumentName" value -> Some value
        | _ -> None

    let (argumentValue: string list) =
        match input with
        | Input.Argument.ListValue "argumentName" value -> value
        | _ -> []
    ```
    - All Input active patterns for matching Arguments

    | Active Pattern                | Description |
    | ---                           | ---         |
    | _Input_._Argument_.**IsDefined** | Matched when given string is defined as argument name. |
    | _Input_._Argument_.**Has**       | Matched when given string has any value in current Input (_default or from args_). |
    | _Input_._Argument_.**IsSet**     | Matched when input _has_ argument AND that value is _not empty_. |

    - Active patterns for accessing a value

    | Active Pattern                    | Description | Value |
    | ---                               | ---         | ---   |
    | _Input_._Argument_.**Value**         | Matched when input _has_ argument. (_Fail with exception when value is not set or it is a list._) | `string` |
    | _Input_._Argument_.**OptionalValue** | Matched when input _has_ argument AND it has a single value. | `string` |
    | _Input_._Argument_.**ListValue**     | Matched when input _has_ argument. | `string list` |

- Just get a value from `Input`
    ```fs
    let argumentValue: ArgumentValue option = input |> Input.Argument.get "argumentName"
    let argumentValue: string = input |> Input.Argument.value "argumentName"    // or exception
    let argumentValue: string option = input |> Input.Argument.asString "argumentName"
    let argumentValue: int option = input |> Input.Argument.asInt "argumentName"   // or exception
    let argumentValue: int option = input |> Input.tryAsInt "argumentName"
    let argumentValue: string list = input |> Input.Argument.asList "argumentName"
    ```
    - All Input functions for accessing Arguments

    | Function                           | Description |
    | ---                                | ---         |
    | _Input_._Argument_.**tryGet**      | Returns an `ArgumentValue option`, when Input _has_ argument. |
    | _Input_._Argument_.**get**         | Returns an `ArgumentValue`, when Input _has_ argument OR fail with exception. |
    | _Input_._Argument_.**value**       | Returns a `string` value from ArgumentValue, when Input _has_ argument OR fail with exception. |
    | _Input_._Argument_.**asString**    | Returns a `string option` value from ArgumentValue, when Input _has_ argument. |
    | _Input_._Argument_.**asInt**       | Returns an `int option` value from ArgumentValue, when Input _has_ argument. (_It fails with an exception when string value is not int._) |
    | _Input_._Argument_.**asList**      | Returns an `string list` value from ArgumentValue, when Input _has_ argument. (_It returns a list even for single values._) |
    | _Input_._Argument_.**tryGetAsInt** | Returns an `int option` value from ArgumentValue, when Input _has_ argument. (_It returns None when string value is not int._) |
    | _Input_._Argument_.**isValueSet**  | Checks whether argument has a value AND that value is _not empty_. |

    Note: All functions above will fail with an exception when given "argument" is not defined.

- Directly `Input.Arguments` - it is `Map<string, ArgumentValue>`
    ```fs
    let argumentValue: ArgumentValue = input.Arguments.["argumentName"] // or exception
    ```

#### What value will ArgumentValue has?

| ValueDefinition | Has Default | In args | ArgumentValue  |
| ---             | ---         | ---     | ---            |
| Required        | No          | Yes     | from Args      |
|                 | No          | No      | **Error**      |
| Optional        | Yes         | Yes     | Some from Args |
|                 | Yes         | No      | Some default   |
|                 | No          | Yes     | Some from Args |
|                 | No          | No      | None           |
| Array           | Yes         | Yes     | [from Args]    |
|                 | Yes         | No      | [default]      |
|                 | No          | Yes     | [from Args]    |
|                 | No          | No      | []             |
| RequiredArray   | No          | Yes     | [from Args]    |
|                 | No          | No      | **Error**      |
