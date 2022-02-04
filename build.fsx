#load ".fake/build.fsx/intellisense.fsx"
open System

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Tools.Git

// ========================================================================================================
// === F# / Public Library fake build ============================================================= 2.0.0 =
// --------------------------------------------------------------------------------------------------------
// Options:
//  - no-clean   - disables clean of dirs in the first step (required on CI)
//  - no-lint    - lint will be executed, but the result is not validated
// --------------------------------------------------------------------------------------------------------
// Table of contents:
//      1. Information about project, configuration
//      2. Utilities, DotnetCore functions
//      3. FAKE targets
//      4. FAKE targets hierarchy
// ========================================================================================================

// --------------------------------------------------------------------------------------------------------
// 1. Information about the project to be used at NuGet and in AssemblyInfo files and other FAKE configuration
// --------------------------------------------------------------------------------------------------------

let project = "MF/ConsoleApplication"
let summary = "The Console application eases the creation of beautiful and testable command line interfaces in F#."

let changeLog = "CHANGELOG.md"
let gitCommit = Information.getCurrentSHA1(".")
let gitBranch = Information.getBranchName(".")

// --------------------------------------------------------------------------------------------------------
// 2. Utilities, DotnetCore functions, etc.
// --------------------------------------------------------------------------------------------------------

[<AutoOpen>]
module private Utils =
    let tee f a =
        f a
        a

    let skipOn option action p =
        if p.Context.Arguments |> Seq.contains option
        then Trace.tracefn "Skipped ..."
        else action p

    let createProcess exe arg dir =
        CreateProcess.fromRawCommandLine exe arg
        |> CreateProcess.withWorkingDirectory dir
        |> CreateProcess.ensureExitCode

    let run proc arg dir =
        proc arg dir
        |> Proc.run
        |> ignore

    let orFail = function
        | Error e -> raise e
        | Ok ok -> ok

    let stringToOption = function
        | null | "" -> None
        | string -> Some string

[<RequireQualifiedAccess>]
module Dotnet =
    let dotnet = createProcess "dotnet"

    let run command dir = try run dotnet command dir |> Ok with e -> Error e
    let runInRoot command = run command "."
    let runOrFail command dir = run command dir |> orFail

[<RequireQualifiedAccess>]
module ProjectSources =
    let library =
        !! "./*.fsproj"
        ++ "src/*.fsproj"
        ++ "src/**/*.fsproj"

    let tests =
        !! "tests/*.fsproj"

    let all =
        library
        ++ "tests/*.fsproj"

// --------------------------------------------------------------------------------------------------------
// 3. Targets for FAKE
// --------------------------------------------------------------------------------------------------------

Target.create "Clean" <| skipOn "no-clean" (fun _ ->
    !! "./**/bin/Release"
    ++ "./**/bin/Debug"
    ++ "./**/obj"
    ++ "./**/.ionide"
    |> Shell.cleanDirs
)

Target.create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        let now = DateTime.Now
        let release = ReleaseNotes.parse (System.IO.File.ReadAllLines changeLog |> Seq.filter ((<>) "## Unreleased"))

        let gitValue initialValue =
            initialValue
            |> stringToOption
            |> Option.defaultValue "unknown"

        [
            AssemblyInfo.Title projectName
            AssemblyInfo.Product project
            AssemblyInfo.Description summary
            AssemblyInfo.Version release.AssemblyVersion
            AssemblyInfo.FileVersion release.AssemblyVersion
            AssemblyInfo.InternalsVisibleTo "tests"
            AssemblyInfo.Metadata("gitbranch", gitBranch |> gitValue)
            AssemblyInfo.Metadata("gitcommit", gitCommit |> gitValue)
            AssemblyInfo.Metadata("createdAt", now.ToString("yyyy-MM-dd HH:mm:ss"))
        ]

    let getProjectDetails (projectPath: string) =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        (
            projectPath,
            projectName,
            System.IO.Path.GetDirectoryName(projectPath),
            (getAssemblyInfoAttributes projectName)
        )

    ProjectSources.all
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (_, _, folderName, attributes) ->
        AssemblyInfoFile.createFSharp (folderName </> "AssemblyInfo.fs") attributes
    )
)

Target.create "Build" (fun _ ->
    ProjectSources.library
    |> Seq.iter (DotNet.build id)
)

Target.create "BuildTests" (fun _ ->
    ProjectSources.tests
    |> Seq.iter (DotNet.build id)
)

Target.create "Lint" <| skipOn "no-lint" (fun _ ->
    ProjectSources.all
    ++ "./Build.fsproj"
    |> Seq.iter (fun fsproj ->
        match Dotnet.runInRoot (sprintf "fsharplint lint %s" fsproj) with
        | Ok () -> Trace.tracefn "Lint %s is Ok" fsproj
        | Error e -> raise e
    )
)

Target.create "Tests" (fun _ ->
    if ProjectSources.tests |> Seq.isEmpty
    then Trace.tracefn "There are no tests yet."
    else Dotnet.runOrFail "run" "tests"
)

Target.create "Release" (fun _ ->
    match UserInput.getUserInput "Are you sure - is it tagged yet? [y|n]: " with
    | "y"
    | "yes" ->
        match UserInput.getUserPassword "Nuget ApiKey: " with
        | "" -> failwithf "You have to provide an api key for nuget."
        | apiKey ->
            !! "*.fsproj"
            |> Seq.iter (DotNet.pack id)

            Directory.ensure "release"

            !! "bin/**/*.nupkg"
            |> Seq.map (tee (DotNet.nugetPush (fun defaults ->
                { defaults with
                    PushParams = {
                        defaults.PushParams with
                            ApiKey = Some apiKey
                            Source = Some "https://api.nuget.org/v3/index.json"
                    }
                }
            )))
            |> Seq.iter (Shell.moveFile "release")
    | _ -> ()
)

// --------------------------------------------------------------------------------------------------------
// 4. FAKE targets hierarchy
// --------------------------------------------------------------------------------------------------------

"Clean"
    ==> "AssemblyInfo"
    ==> "Build" <=> "BuildTests"
    ==> "Lint"
    ==> "Tests"
    ==> "Release"

Target.runOrDefaultWithArguments "Build"
