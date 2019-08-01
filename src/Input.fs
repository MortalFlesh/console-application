namespace MF.ConsoleApplication

type internal ParsedInput = {
    Arguments: Arguments
    Options: Options
    UnfilledArgumentDefinitions: UnfilledArgumentDefinitions
}

[<RequireQualifiedAccess>]
module internal ParsedInput =
    let internal empty = {
        Arguments = Map.empty
        Options = Map.empty
        UnfilledArgumentDefinitions = []
    }

type Input = {
    Arguments: Arguments
    Options: Options
    ArgumentDefinitions: ArgumentsDefinitions
    OptionDefinitions: OptionsDefinitions
}

[<RequireQualifiedAccess>]
module Input =
    open ResultOperators

    let internal empty = {
        Arguments = Map.empty
        Options = Map.empty
        ArgumentDefinitions = []
        OptionDefinitions = []
    }

    let rec internal parse allArgumentDefinitions definitionsToParse (input: ParsedInput) (args: InputValue list) =
        let (optionDefinitions: OptionsDefinitions, argumentDefinitions: ArgumentsDefinitions) = definitionsToParse

        match args with
        | [] -> Ok { input with UnfilledArgumentDefinitions = argumentDefinitions }
        | Arguments.Separator :: rawArguments ->
            result {
                let! (arguments, unfilledArgumentDefinitions) =
                    rawArguments
                    |> Arguments.parse input.Arguments argumentDefinitions <!!> InputError.ArgumentsError

                return { input with Arguments = arguments; UnfilledArgumentDefinitions = unfilledArgumentDefinitions }
            }
        | rawArg :: rawArgs ->
            result {
                let! (parsedInput, definitionsToParse, rawArgs) =
                    match rawArg with
                    | OptionsDefinitions.IsDefinedOption optionDefinitions option ->
                        debug <| sprintf "Parse option \"%s\" value for %A" rawArg option.Name

                        result {
                            let! options, rawArgs =
                                if rawArg |> Option.containsValue
                                    then (rawArg |> OptionValue.parse) :: rawArgs
                                    else rawArgs
                                |> Options.parseValue option input.Options <!!> InputError.OptionsError

                            return { input with Options = options }, definitionsToParse, rawArgs
                        }

                    | OptionsDefinitions.IsShortcut ->
                        debug <| sprintf "Parse shortcut(s) \"%s\"" rawArg

                        result {
                            let! options, rawArgs =
                                rawArg
                                |> Letters.fromShortcut
                                |> Options.parseShortcut optionDefinitions input.Options rawArgs <!!> InputError.OptionsError

                            return { input with Options = options }, definitionsToParse, rawArgs
                        }

                    | Option.IsOptionOrShortcut ->
                        Error <| InputError.OptionsError (OptionsError.UndefinedOption rawArg)

                    | rawArgument ->
                        debug <| sprintf "Parse argument \"%s\"" rawArgument

                        result {
                            let! (arguments, argumentDefinitions) =
                                rawArgument
                                |> Arguments.parseArgument input.Arguments allArgumentDefinitions argumentDefinitions <!!> InputError.ArgumentsError

                            return { input with Arguments = arguments }, (optionDefinitions, argumentDefinitions), rawArgs
                        }

                return!
                    rawArgs
                    |> parse allArgumentDefinitions definitionsToParse parsedInput
            }

    let internal prepareUnfilledArguments (unfilledArgumentDefinitions: UnfilledArgumentDefinitions) (input: Input) =
        match unfilledArgumentDefinitions with
        | [] -> Ok input
        | unfilledDefinitions ->
            result {
                let! arguments =
                    input.Arguments
                    |> Arguments.prepareMissingArguments unfilledDefinitions <!!> InputError.ArgumentsError

                return { input with Arguments = arguments }
            }

    //
    // Matching & Getters
    //

    // Options

    /// Check whether option is defined.
    let (|IsOptionDefined|_|) option input =
        match input.OptionDefinitions with
        | OptionsDefinitions.HasDefinedOption option _ -> Some ()
        | _ -> None

    /// Check whether Input.Options contains an option, fail on exception when option is not defined.
    let (|HasOption|_|) option input =
        match input with
        | IsOptionDefined option ->
            match input.Options with
            | Options.HasOption option value -> Some value
            | _ -> None
        | _ -> failwithf "The \"--%s\" option does not exists." option

    /// Try to find an OptionValue in Input.Options, fail on exception when option is not defined.
    let tryGetOption option = function
        | HasOption option value -> Some value
        | _ -> None

    /// Get an OptionValue from Input.Options by option name, fail on exception when option is not defined or presented.
    let getOption option = function
        | HasOption option value -> value
        | _ -> failwithf "The \"--%s\" option does not have a value." option

    /// Get option value or fail with exception when option is not defined or presented.
    /// This function should be used with Options with Required values.
    let getOptionValue option input =
        input |> getOption option |> OptionValue.value option

    /// Get option value as string or fail with exception when option is not defined or presented.
    let getOptionValueAsString option input =
        input |> getOption option |> OptionValue.stringValue

    /// Get option value as int or fail with exception when option is not defined or presented.
    let getOptionValueAsInt option input =
        input |> getOption option |> OptionValue.intValue

    /// Get option value as list or fail with exception when option is not defined or presented.
    let getOptionValueAsList option input =
        input |> getOption option |> OptionValue.listValue

    /// Try to parse option value as int or fail with exception when option is not defined or presented.
    let tryGetOptionValueAsInt option input =
        input |> getOption option |> OptionValue.tryIntValue

    /// Check whether Input.Options contains option name and the value is set or fail with exception when option is not defined.
    let (|IsSetOption|_|) option = function
        | HasOption option value when value |> OptionValue.isSet -> Some value
        | _ -> None

    /// Checks whether option value is set or fail with exception when option is not defined.
    let isOptionValueSet option = function
        | IsSetOption option _ -> true
        | _ -> false

    /// Check whether Input.Options contains option name or fail with exception when option is not defined.
    /// Access the value directly in matching.
    let (|OptionValue|_|) option = function
        | HasOption option value -> Some (value |> OptionValue.value option)
        | _ -> None

    /// Check whether Input.Options contains option name or fail with exception when option is not defined.
    /// Access the value directly in matching.
    let (|OptionOptionalValue|_|) option = function
        | HasOption option value -> value |> OptionValue.stringValue
        | _ -> None

    /// Check whether Input.Options contains option name or fail with exception when option is not defined.
    /// Access the value as list directly in matching.
    let (|OptionListValue|_|) option = function
        | HasOption option value -> Some (value |> OptionValue.listValue)
        | _ -> None

    // Arguments

    /// Check whether argument is defined.
    let (|IsArgumentDefined|_|) argument input =
        match argument with
        | ArgumentNames.Command -> Some ()
        | _ ->
            match input.ArgumentDefinitions with
            | ArgumentsDefinitions.HasDefinedArgument argument _ -> Some ()
            | _ -> None

    /// Check whether Input.Arguments contains argument name or fail with exception when argument is not defined.
    let (|HasArgument|_|) argument input =
        match input with
        | IsArgumentDefined argument ->
            match input.Arguments with
            | Arguments.HasArgument argument value -> Some value
            | _ -> None
        | _ -> failwithf "The \"%s\" argument does not exists." argument

    /// Check whether Input.Arguments contains argument name or fail with exception when argument is not defined.
    /// Access the value directly in matching.
    let (|ArgumentValue|_|) argument = function
        | HasArgument argument value -> Some (value |> ArgumentValue.value argument)
        | _ -> None

    /// Check whether Input.Arguments contains argument name or fail with exception when argument is not defined.
    /// Access the value directly in matching.
    let (|ArgumentOptionalValue|_|) argument = function
        | HasArgument argument value -> value |> ArgumentValue.stringValue
        | _ -> None

    /// Check whether Input.Arguments contains argument name or fail with exception when argument is not defined.
    /// Access the value as list directly in matching.
    let (|ArgumentListValue|_|) argument = function
        | HasArgument argument value -> Some (value |> ArgumentValue.listValue)
        | _ -> None

    /// Try to find an ArgumentValue in Input.Arguments or fail with exception when argument is not defined.
    let tryGetArgument argument = function
        | HasArgument argument value -> Some value
        | _ -> None

    /// Get an ArgumentValue from Input.Arguments by argument name, fail on exception when option is not defined or presented.
    let getArgument argument = function
        | HasArgument argument value -> value
        | _ -> failwithf "The \"%s\" argument does not have a value." argument

    /// Get argument value or fail with exception, fail on exception when option is not defined or presented.
    /// This function should be used with Required Arguments.
    let getArgumentValue argument input =
        input |> getArgument argument |> ArgumentValue.value argument

    /// Get argument value as string, fail on exception when option is not defined or presented.
    let getArgumentValueAsString argument input =
        input |> getArgument argument |> ArgumentValue.stringValue

    /// Get argument value as int, fail on exception when option is not defined or presented.
    let getArgumentValueAsInt argument input =
        input |> getArgument argument |> ArgumentValue.intValue

    /// Try to parse argument value as int, fail on exception when option is not defined or presented.
    let tryGetArgumentValueAsInt argument input =
        input |> getArgument argument |> ArgumentValue.tryIntValue

    /// Get argument value as list, fail on exception when option is not defined or presented.
    let getArgumentValueAsList argument input =
        input |> getArgument argument |> ArgumentValue.listValue

    /// Check whether Input.Arguments contains argument name and the value is set.
    let (|IsSetArgument|_|) argument = function
        | HasArgument argument value when value |> ArgumentValue.isSet -> Some value
        | _ -> None

    /// Checks whether argument value is set or fail with exception when argument is not defined.
    let isArgumentValueSet argument = function
        | IsSetArgument argument _ -> true
        | _ -> false

    let internal getCommandName input =
        input
        |> getArgumentValue ArgumentNames.Command
        |> Name
        |> CommandName

    //
    // Modify an Input
    //

    // Arguments

    /// Set a value for an argument or fail with exception when argument is not defined.
    /// You can set only a single string value (even for array argument).
    /// If you want to set more than one value for Array argument, use `Input.setArgumentListValue` instead.
    let setArgumentValue (input: Input) (argument: string) value =
        match input.ArgumentDefinitions with
        | ArgumentsDefinitions.HasDefinedArgument argument definition ->
            let value =
                match definition.Value with
                | ArgumentValueDefinition.Required ->
                    match value with
                    | String.IsNullOrEmpty -> failwithf "The \"%s\" argument does not accept an empty value." argument
                    | value -> ArgumentValue.Required value
                | ArgumentValueDefinition.Optional _ ->
                    match value with
                    | null -> ArgumentValue.Optional None
                    | value -> ArgumentValue.Optional (Some value)
                | ArgumentValueDefinition.Array _ -> ArgumentValue.Array [value]
                | ArgumentValueDefinition.RequiredArray ->
                    match value with
                    | String.IsNullOrEmpty -> failwithf "The \"%s\" argument does not accept an empty value." argument
                    | value -> ArgumentValue.RequiredArray (NotEmptyList.ofListWithValues [value])

            { input with Arguments = input.Arguments.Add(argument, value) }
        | _ -> failwithf "The \"%s\" argument does not exists." argument

    /// Set a list value for an array argument or fail with exception when argument is not defined.
    /// You can set only a list value, if you want to set a single value for argument, use `Input.setArgumentValue` instead.
    let setArgumentListValue (input: Input) (argument: string) values =
        match input.ArgumentDefinitions with
        | ArgumentsDefinitions.HasDefinedArgument argument definition ->
            let value =
                match definition.Value with
                | ArgumentValueDefinition.Array _ -> ArgumentValue.Array values
                | ArgumentValueDefinition.RequiredArray ->
                    match values |> NotEmptyList.create with
                    | Ok notEmptyList -> ArgumentValue.RequiredArray notEmptyList
                    | Error NotEmptyListError.NoValues -> failwithf "The \"%s\" argument does not accept an empty list." argument
                | ArgumentValueDefinition.Required
                | ArgumentValueDefinition.Optional _ -> failwithf "The \"%s\" argument does not accept a list value. Use `Input.setArgumentValue` instead." argument

            { input with Arguments = input.Arguments.Add(argument, value) }
        | _ -> failwithf "The \"%s\" argument does not exists." argument

    // Options

    /// Set a value for an option or fail with exception when option is not defined.
    /// You can set only a single string value (even for option with array value).
    /// If you want to set more then one value for array option, use `Input.setOptionListValue` instead.
    let setOptionValue (input: Input) (option: string) value =
        match input.OptionDefinitions with
        | OptionsDefinitions.HasDefinedOption option definition ->
            let value =
                match definition.Value with
                | OptionValueDefinition.ValueNone -> failwithf "The \"%s\" option does not accept any value." option
                | OptionValueDefinition.ValueRequired _ ->
                    match value with
                    | String.IsNullOrEmpty -> failwithf "The \"%s\" option does not accept an empty value." option
                    | value -> OptionValue.ValueRequired value
                | OptionValueDefinition.ValueOptional _ ->
                    match value with
                    | null -> OptionValue.ValueOptional None
                    | value -> OptionValue.ValueOptional (Some value)
                | OptionValueDefinition.ValueIsArray _ -> OptionValue.ValueIsArray [value]
                | OptionValueDefinition.ValueRequiredArray _ ->
                    match value with
                    | String.IsNullOrEmpty -> failwithf "The \"%s\" option does not accept an empty value." option
                    | value -> OptionValue.ValueRequiredArray [value]

            { input with Options = input.Options.Add(option, value) }
        | _ -> failwithf "The \"--%s\" option does not exists." option

    /// Set a list value for an option with array value or fail with exception when option is not defined.
    /// You can set only a list value, if you want to set a single value for option, use `Input.setOptionValue` instead.
    let setOptionListValue (input: Input) (option: string) values =
        match input.OptionDefinitions with
        | OptionsDefinitions.HasDefinedOption option definition ->
            let value =
                match definition.Value with
                | OptionValueDefinition.ValueNone -> failwithf "The \"%s\" option does not accept any value." option
                | OptionValueDefinition.ValueIsArray _ -> OptionValue.ValueIsArray values
                | OptionValueDefinition.ValueRequiredArray _ ->
                    match values with
                    | [] -> failwithf "The \"%s\" option does not accept an empty list." option
                    | values -> OptionValue.ValueRequiredArray values
                | OptionValueDefinition.ValueRequired _
                | OptionValueDefinition.ValueOptional _ -> failwithf "The \"%s\" option does not accept a list value. Use `Input.setOptionValue` instead." option

            { input with Options = input.Options.Add(option, value) }
        | _ -> failwithf "The \"--%s\" option does not exists." option
