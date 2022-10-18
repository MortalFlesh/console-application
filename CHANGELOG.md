# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased
- Use `green` instead of a `dark-green`
- Add `consoleApplication` keywords
    - `updateOutput`
    - `withStyle`
    - `withCustomTags`
- [**BC**] Require a default value for a `Option.required`

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
