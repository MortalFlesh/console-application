namespace MF.ConsoleApplication

// Common types
// ***************************

type Name = private Name of string

[<RequireQualifiedAccess>]
type NameError =
    | Empty
    | StartsWith of name: string * string
    | Contains of name: string * string
    | EndsWith of name: string * string

[<RequireQualifiedAccess>]
module internal Name =
    let create invalidPrefixes invalidSubStrings invalidSufixes = function
        | String.IsNullOrEmpty -> Error NameError.Empty
        | String.StartsWith invalidPrefixes invalidPrefix -> Error (NameError.StartsWith invalidPrefix)
        | String.Contains invalidSubStrings invalidSubstring -> Error (NameError.Contains invalidSubstring)
        | String.EndsWith invalidSufixes invalidSufix -> Error (NameError.EndsWith invalidSufix)
        | name -> Ok (Name name)

[<RequireQualifiedAccess>]
module internal NameError =
    let format prefix = function
        | NameError.Empty -> sprintf "%s name is empty." prefix
        | NameError.StartsWith (name, invalid) -> sprintf "%s name \"%s\" must not starts with \"%s\"." prefix name invalid
        | NameError.Contains (name, invalid) -> sprintf "%s \"%s\" name must not contains \"%s\"." prefix name invalid
        | NameError.EndsWith (name, invalid) -> sprintf "%s name \"%s\" must not ends with \"%s\"." prefix name invalid

type ListWithItems<'a> = private ListWithItems of 'a list

type NotEmptyList<'a> =
    | WithItems of ListWithItems<'a>
    | PartiallyCreated of 'a list

[<RequireQualifiedAccess>]
type NotEmptyListError =
    | NoValues

[<RequireQualifiedAccess>]
module internal NotEmptyListError =
    let format = function
        | NotEmptyListError.NoValues -> "List must have at least one value."

[<RequireQualifiedAccess>]
module NotEmptyList =
    let create = function
        | [] -> Error NotEmptyListError.NoValues
        | list -> Ok (WithItems (ListWithItems list))

    /// Create NotEmptyList with values, or fail with Exception. This function should not be used unless it you are sure, there are values.
    let ofListWithValues values =
        values
        |> create
        |> Result.orFail

    let values = function
        | WithItems (ListWithItems items) -> items
        | PartiallyCreated _ -> failwithf "NotEmptyList values are accessible only after is created with items! (Use \"NotEmptyList.complete\" function to finish creation)"

    /// Create NotEmptyList with values, which may be empty now, but to get values, you have to complete the creation first.
    /// Use "NotEmptyList.complete" function to do it.
    let internal createPartial = function
        | [] -> PartiallyCreated []
        | items -> WithItems (ListWithItems items)

    /// Complete a creation of NotEmptyList and validate current values.
    let internal complete = function
        | PartiallyCreated values -> values |> create
        | WithItems items -> Ok (WithItems items)

    let internal append values = function
        | PartiallyCreated [] -> createPartial values
        | PartiallyCreated items
        | WithItems (ListWithItems items) -> items @ values |> ofListWithValues

// Terminal I/O
// ***********************

type InputValue = string

[<RequireQualifiedAccess>]
type ExitCode =
    | Success
    | Error

[<RequireQualifiedAccess>]
module ExitCode =
    let code = function
        | ExitCode.Success -> 0
        | ExitCode.Error -> 1

    let internal fromResult printError = function
        | Ok code -> code
        | Error (error, commandInfo) ->
            error |> printError commandInfo
            ExitCode.Error

// Input / Output
// ***********************

open MF.ConsoleStyle

type ProgressBar = ShellProgressBar.ProgressBar option

type Output = {
    SetVerbosity: Verbosity.Level -> unit
    IsQuiet: unit -> bool
    IsNormal: unit -> bool
    IsVerbose: unit -> bool
    IsVeryVerbose: unit -> bool
    IsDebug: unit -> bool

    Message: string -> unit
    NewLine: unit -> unit
    MainTitle: string -> unit
    Title: string -> unit
    Section: string -> unit
    SubTitle: string -> unit

    Error: string -> unit
    Success: string -> unit

    Messages: string -> string list -> unit
    Options: string -> (string * string) list -> unit
    SimpleOptions: string -> (string * string) list -> unit
    GroupedOptions: string -> string -> (string * string) list -> unit
    List: string list -> unit

    Table: string list -> (string list) list -> unit

    ProgressStart: string -> int -> ProgressBar
    ProgressAdvance: ProgressBar -> unit
    ProgressFinish: ProgressBar -> unit
}

[<RequireQualifiedAccess>]
module internal Output =
    let console = {
        SetVerbosity = Console.setVerbosity
        IsQuiet = Console.isQuiet
        IsNormal = Console.isNormal
        IsVerbose = Console.isVerbose
        IsVeryVerbose = Console.isVeryVerbose
        IsDebug = Console.isDebug

        Message = Console.message
        NewLine = Console.newLine
        MainTitle = Console.mainTitle
        Title = Console.title
        Section = Console.section
        SubTitle = Console.subTitle

        Error = Console.error
        Success = Console.success

        Messages = Console.messages
        Options = Console.options
        SimpleOptions = Console.simpleOptions
        GroupedOptions = Console.groupedOptions
        List = Console.list

        Table = fun headers items ->
            Console.table headers (items |> Seq.map seq)

        ProgressStart = Console.progressStart
        ProgressAdvance = Console.progressAdvance
        ProgressFinish = Console.progressFinish
    }

// Command
// ***************************

[<RequireQualifiedAccess>]
module internal ArgumentNames =
    [<Literal>]
    let Command = "command"

[<RequireQualifiedAccess>]
module OptionNames =
    [<Literal>]
    let Help = "help"
    [<Literal>]
    let Quiet = "quiet"
    [<Literal>]
    let Version = "version"
    [<Literal>]
    let Verbose = "verbose"
    [<Literal>]
    let NoInteraction = "no-interaction"

    let internal reserved = [ Help; Quiet; Version; Verbose; NoInteraction ]

type ReservedShortcut = ReservedShortcut of shortcut: string * optionName: string

[<RequireQualifiedAccess>]
module OptionShortcuts =
    [<Literal>]
    let Help = "h"
    [<Literal>]
    let Quiet = "q"
    [<Literal>]
    let Version = "V"
    [<Literal>]
    let Verbose = "v|vv|vvv"
    [<Literal>]
    let NoInteraction = "n"

    let private reserved =
        [
            Help, OptionNames.Help
            Quiet, OptionNames.Quiet
            Version, OptionNames.Version
            Verbose, OptionNames.Verbose
            NoInteraction, OptionNames.NoInteraction
        ]
        |> Map.ofList

    let internal (|IsReserved|_|) shortcut: ReservedShortcut option =
        reserved
        |> Map.tryFind shortcut
        |> Option.map (fun option -> ReservedShortcut (shortcut, option))

//
// Command name
//

[<RequireQualifiedAccess>]
type CommandNameError =
    | Reserved of string
    | NameError of NameError
    | Invalid of string

[<RequireQualifiedAccess>]
module internal CommandNameError =
    let format = function
        | CommandNameError.Reserved reservedName -> sprintf "Command name \"%s\" is reserved. Please use something else." reservedName
        | CommandNameError.NameError error -> NameError.format "Command" error
        | CommandNameError.Invalid invalidName -> sprintf "Command name \"%s\" is invalid." invalidName

type CommandName = private CommandName of Name

[<RequireQualifiedAccess>]
module CommandNames =
    [<Literal>]
    let List = "list"
    [<Literal>]
    let Help = "help"
    [<Literal>]
    let Exit = "exit"

    let internal all = [ List; Help; Exit ]

[<RequireQualifiedAccess>]
module internal CommandName =
    open ResultOperators

    [<Literal>]
    let NamespaceSeparator = ":"

    let private createName name =
        name
        |> Name.create [ NamespaceSeparator; "-" ] [ " "; NamespaceSeparator + NamespaceSeparator ] [ NamespaceSeparator ]
        <!> CommandName
        <!!> CommandNameError.NameError

    let create = function
        | ArgumentNames.Command -> Error (CommandNameError.Reserved ArgumentNames.Command)
        | name when CommandNames.all |> List.contains name -> Error (CommandNameError.Reserved name)
        | name -> name |> createName

    let createInRuntime = function
        | ArgumentNames.Command -> Error (CommandNameError.Invalid ArgumentNames.Command)
        | name -> name |> createName

    let value (CommandName (Name name)) = name
    let namespaceValue (CommandName (Name name)) = name.Split NamespaceSeparator |> Seq.tryHead

    /// Match string as CommandName.
    /// NOTE: It does NOT check, whether such command exists.
    let (|IsCommandName|_|) = createInRuntime >> Result.toOption

//
// Application name, version, ...
//

[<RequireQualifiedAccess>]
type ApplicationNameError =
    | NameError of NameError

[<RequireQualifiedAccess>]
module internal ApplicationNameError =
    let format = function
        | ApplicationNameError.NameError error -> NameError.format "Application" error

type ApplicationName = private ApplicationName of Name

[<RequireQualifiedAccess>]
module internal ApplicationName =
    open ResultOperators

    let create name =
        name
        |> Name.create [] [] []
        <!> ApplicationName
        <!!> ApplicationNameError.NameError

    let value (ApplicationName (Name name)) = name

type ApplicationVersion = internal ApplicationVersion of string

[<RequireQualifiedAccess>]
module internal ApplicationVersion =
    let value (ApplicationVersion version) = version

type ApplicationTitle = internal ApplicationTitle of string

[<RequireQualifiedAccess>]
module internal ApplicationTitle =
    let value (ApplicationTitle title) = title

// Errors
// ***************************

// Definitions

[<RequireQualifiedAccess>]
type ArgumentNameError =
    | NameError of NameError
    | Reserved of string

[<RequireQualifiedAccess>]
module internal ArgumentNameError =
    let format = function
        | ArgumentNameError.NameError error -> NameError.format "Argument" error
        | ArgumentNameError.Reserved reservedName -> sprintf "Argument name \"%s\" is reserved. Please use something else." reservedName

[<RequireQualifiedAccess>]
type ArgumentDefinitionError =
    | ArgumentNameError of ArgumentNameError
    | ArgumentAlreadyExists of string
    | ArgumentAfterArrayArgument
    | RequiredArgumentAfterOptional

[<RequireQualifiedAccess>]
module internal ArgumentDefinitionError =
    let format = function
        | ArgumentDefinitionError.ArgumentNameError error -> ArgumentNameError.format error
        | ArgumentDefinitionError.ArgumentAlreadyExists notUnique -> sprintf "An argument with name \"%s\" already exists." notUnique
        | ArgumentDefinitionError.ArgumentAfterArrayArgument -> "Cannot add an argument after an array argument."
        | ArgumentDefinitionError.RequiredArgumentAfterOptional -> "Cannot add a required argument after an optional one."

[<RequireQualifiedAccess>]
type OptionNameError =
    | NameError of NameError
    | Reserved of string

[<RequireQualifiedAccess>]
module internal OptionNameError =
    let format = function
        | OptionNameError.NameError error -> NameError.format "Option" error
        | OptionNameError.Reserved reservedName -> sprintf "Option name \"%s\" is reserved. Please use something else." reservedName

[<RequireQualifiedAccess>]
type OptionShortcutError =
    | Empty
    | Contains of shortcut: string * string
    | MoreThanSingleLetter of string
    | Reserved of ReservedShortcut

[<RequireQualifiedAccess>]
module internal OptionShortcutError =
    let format = function
        | OptionShortcutError.Empty -> "An option shortcut cannot be empty."
        | OptionShortcutError.Contains (shortcut, invalid) -> sprintf "An option shortcut \"%s\" cannot contain \"%s\"." shortcut invalid
        | OptionShortcutError.MoreThanSingleLetter shortcut -> sprintf "An option shortcut \"%s\" cannon be more than single letter." shortcut
        | OptionShortcutError.Reserved (ReservedShortcut (shortut, option)) -> sprintf "An option shortcut \"%s\" is reserved for option \"%s\". Please use something else." shortut option

[<RequireQualifiedAccess>]
type OptionDefinitionError =
    | OptionNameError of OptionNameError
    | OptionShortcutError of OptionShortcutError
    | RequiredArrayDefaultValueError of NotEmptyListError
    | OptionAlreadyExists of string
    | OptionShortcutAlreadyExists of string

[<RequireQualifiedAccess>]
module internal OptionDefinitionError =
    let format = function
        | OptionDefinitionError.OptionNameError error -> OptionNameError.format error
        | OptionDefinitionError.OptionShortcutError error -> OptionShortcutError.format error
        | OptionDefinitionError.RequiredArrayDefaultValueError error -> NotEmptyListError.format error
        | OptionDefinitionError.OptionAlreadyExists notUnique -> sprintf "An option named \"%s\" already exists." notUnique
        | OptionDefinitionError.OptionShortcutAlreadyExists notUnique -> sprintf "An option with shortcut \"%s\" already exists." notUnique

[<RequireQualifiedAccess>]
type CommandDefinitionError =
    | ArgumentDefinitionError of ArgumentDefinitionError
    | OptionDefinitionError of OptionDefinitionError

[<RequireQualifiedAccess>]
module internal CommandDefinitionError =
    let format = function
        | CommandDefinitionError.ArgumentDefinitionError error -> ArgumentDefinitionError.format error
        | CommandDefinitionError.OptionDefinitionError error -> OptionDefinitionError.format error

// Runtime

[<RequireQualifiedAccess>]
type OptionsError =
    | RequiredValueNotSet of string
    | UndefinedOption of string

[<RequireQualifiedAccess>]
module internal OptionsError =
    let format = function
        | OptionsError.RequiredValueNotSet option -> sprintf "The \"--%s\" option requires a value." option
        | OptionsError.UndefinedOption undefinedOption -> sprintf "The \"%s\" option does not exist." undefinedOption

[<RequireQualifiedAccess>]
type ArgumentsError =
    | NotEnoughArguments of string
    | TooManyArguments of string list

[<RequireQualifiedAccess>]
module internal ArgumentsError =
    let format = function
        | ArgumentsError.NotEnoughArguments missingArgument -> sprintf "Not enough arguments (missing: \"%s\")." missingArgument
        | ArgumentsError.TooManyArguments definedArguments ->
            match definedArguments with
            | [] -> "Too many arguments, no arguments expected."
            | definedArguments ->
                definedArguments
                |> List.map (sprintf "\"%s\"")
                |> String.concat " "
                |> sprintf "Too many arguments, expected arguments %s."

[<RequireQualifiedAccess>]
type InputError =
    | OptionsError of OptionsError
    | ArgumentsError of ArgumentsError

[<RequireQualifiedAccess>]
module internal InputError =
    let format = function
        | InputError.OptionsError error -> OptionsError.format error
        | InputError.ArgumentsError error -> ArgumentsError.format error

[<RequireQualifiedAccess>]
type ArgsError =
    | CommandNameError of CommandNameError
    | CommandNotFound of CommandName
    | InputError of InputError

[<RequireQualifiedAccess>]
module internal ArgsError =
    let format = function
        | ArgsError.CommandNameError error -> CommandNameError.format error
        | ArgsError.CommandNotFound name -> name |> CommandName.value |> sprintf "Command by name \"%s\" is not defined. Run \"list\" to show available commands."
        | ArgsError.InputError error -> InputError.format error

[<RequireQualifiedAccess>]
type ConsoleApplicationError =
    | ArgsError of ArgsError
    | CommandNameError of CommandNameError
    | ApplicationNameError of ApplicationNameError
    | CommandDefinitionError of CommandDefinitionError
    | ConsoleApplicationError of string

[<RequireQualifiedAccess>]
module internal ConsoleApplicationError =
    let format = function
        | ConsoleApplicationError.ArgsError error -> ArgsError.format error
        | ConsoleApplicationError.CommandNameError error -> CommandNameError.format error
        | ConsoleApplicationError.ApplicationNameError error -> ApplicationNameError.format error
        | ConsoleApplicationError.CommandDefinitionError error -> CommandDefinitionError.format error
        | ConsoleApplicationError.ConsoleApplicationError error -> error
