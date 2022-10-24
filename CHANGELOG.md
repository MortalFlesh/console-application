# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased

## 4.1.0 - 2022-10-24
- Use `ConsoleStyle` `4.2`
- Fix `no-progress` option

## 4.0.0 - 2022-10-19
- Use `green` instead of a `dark-green`
- Add `consoleApplication` keywords
    - `updateOutput`
    - `withStyle`
    - `withCustomTags`
- [**BC**] Add `no-progress` option for all commands (`no-progress` is also reserved key word now)
- [**BC**] Add `about` command (`about` is also reserved key word now)
- [**BC**] Require a default value for a `Option.required`
- [**BC**] Move `Option` functions in `Input` module into `Input.Option` module and rename them to be shorter
- [**BC**] Move `Argument` functions in `Input` module into `Input.Argument` module and rename them to be shorter
- Add `Help.lines` function to format lines for a command help
- [**BC**] Replace `ConsoleApplicationError.ConsoleApplicationError` with `ConsoleApplicationError.ConsoleApplicationException`
- Show full exception stack trace with verbose output
- [**BC**] Add Execute cases, which must be explicitly declared for an execute function
    - `Execute.Execute`
    - `Execute.ExecuteResult`
    - `Execute.ExecuteAsync`
    - `Execute.ExecuteAsyncResult`
- Add `runAsyncResult` function

## 3.1.0 - 2022-10-17
- Show multiple errors instead of a first one, if there are more than one

## 3.0.0 - 2022-10-17
- [**BC**] Use net6.0
- Update dependencies
    - [**BC**] Use `ConsoleStyle` `3.0`
        - Use `MF.ConsoleStyle.ConsoleStyle` as the `Output` for the ConsoleApplication
        - `Verbosity` is not set globally anymore
- Add abstraction over `ProgressBar` which handles `debug` verbosity better

## 2.0.0 - 2020-01-13
- Update dependencies
    - [**BC**] Use `ConsoleStyle ^2.0`
- [**BC**] Require .net core `^3.1`
- Add `AssemblyInfo`
- [**BC**] `Output` functions uses `string list` instead of `string * string` in
    - `Options`
    - `SimpleOptions`
    - `GroupedOptions`

## 1.1.1 - 2019-08-22
Fix matching command by short name, with multiple namespaces.

## 1.1.0 - 2019-08-11
- Fix format of the default value for required `Options` in the help.
- Match command by partial name, if it is unique.

## 1.0.0 - 2019-08-01
- Initial implementation
