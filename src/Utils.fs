namespace MF.ConsoleApplication

module internal OptionsOperators =
    /// Default value - if value is None, default value will be used
    let (<?=>) defaultValue opt = Option.defaultValue opt defaultValue

    /// Or else - if value is None, other option will be used
    let (<??>) other opt = Option.orElse opt other

    /// Mandatory - if value is None, error will be returned
    let (<?!>) opt onError =
        opt |> Result.ofOption onError

    /// Map action with side-effect and ignore the unit option result
    let (|>!) (opt: 'a option) (action: 'a -> unit) =
        opt
        |> Option.map action
        |> ignore

    /// Option.bind operator alias
    let (>>=) opt f =
        opt |> Option.bind f

    /// Option.map operator alias
    let (<!>) opt f =
        opt |> Option.map f

[<AutoOpen>]
module internal Utils =
    let debug message =
        if MF.ConsoleStyle.Console.isDebug() then
            MF.ConsoleStyle.Console.messagef "[DEBUG] %s" message

    let tee f a =
        f a
        a

    let inline isNotNull a =
        a
        |> isNull
        |> not

[<RequireQualifiedAccess>]
module internal Bool =
    let toOption bool =
        if bool then Some ()
        else None

    let fromOption = function
        | Some _ -> true
        | _ -> false

[<RequireQualifiedAccess>]
module internal String =
    open System

    let append suffix string =
        sprintf "%s%s" string suffix

    let toUpper (string: string) = string.ToUpper()

    let toInt (string: string) =
        match string |> Int32.TryParse with
        | true, int -> Some int
        | _ -> None

    let replace: string -> string * string -> string =
        fun string (placeholder, replacement) ->
            string.Replace(placeholder, replacement)

    let isNullOrEmpty = String.IsNullOrWhiteSpace

    let (|IsNullOrEmpty|_|) string =
        string |> isNullOrEmpty |> Bool.toOption

    let (|StartsWith|_|) (prefixes: string list) (string: string) =
        prefixes
        |> List.tryPick (fun prefix -> if string.StartsWith(prefix) then Some (string, prefix) else None)

    let (|Contains|_|) (subStrings: string list) (string: string) =
        subStrings
        |> List.tryPick (fun subString -> if string.Contains(subString) then Some (string, subString) else None)

    let (|EndsWith|_|) (sufixes: string list) (string: string) =
        sufixes
        |> List.tryPick (fun sufix -> if string.EndsWith(sufix) then Some (string, sufix) else None)

[<AutoOpen>]
module internal Regexp =
    open System.Text.RegularExpressions

    // http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let internal (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some (List.tail [ for g in m.Groups -> g.Value ])
        else None

[<RequireQualifiedAccess>]
module internal List =
    let getDuplicatesBy f items =
        items
        |> List.groupBy f
        |> List.choose (fun (key, set) ->
            if set.Length > 1 then Some key
            else None
        )
