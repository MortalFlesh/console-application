# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased

## 2.0.0 - 2020-01-13
- Update dependencies
    - [**BC**] Use `ConsoleStyle ^2.0`
- [**BC**] Require .net core `^3.1`
- Add `AssemblyInfo`
- [**BC**] `Output` functions uses `string list` instead of `string * string` in
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
