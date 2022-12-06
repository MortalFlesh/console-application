// ========================================================================================================
// === F# / Public Library fake build ============================================================= 3.1.0 =
// --------------------------------------------------------------------------------------------------------
// Options:
//  - no-clean   - disables clean of dirs in the first step (required on CI)
//  - no-lint    - lint will be executed, but the result is not validated
// --------------------------------------------------------------------------------------------------------
// Table of contents:
//      1. Information about project, configuration
//      2. FAKE targets
//      3. FAKE targets hierarchy
// ========================================================================================================

open System
open System.IO

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Tools.Git
open Utils

// --------------------------------------------------------------------------------------------------------
// 1. Information about the project to be used at NuGet and in AssemblyInfo files and other FAKE configuration
// --------------------------------------------------------------------------------------------------------

let project = "MF/ConsoleApplication"
let summary = "The Console application eases the creation of beautiful and testable command line interfaces in F#."

let changeLog = "CHANGELOG.md"
let gitCommit = Information.getCurrentSHA1(".")
let gitBranch = Information.getBranchName(".")

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
        ++ "build/*.fsproj"

// --------------------------------------------------------------------------------------------------------
// 2. Targets for FAKE
// --------------------------------------------------------------------------------------------------------

let initTargets () =
    Target.initEnvironment ()

    Target.create "Clean" <| skipOn "no-clean" (fun _ ->
        !! "./**/bin/Release"
        ++ "./**/bin/Debug"
        ++ "./**/obj"
        ++ "./**/.ionide"
        -- "./build/**"
        |> Shell.cleanDirs
    )

    Target.create "AssemblyInfo" (fun _ ->
        let getAssemblyInfoAttributes projectName =
            let now = DateTime.Now
            let release = ReleaseNotes.parse (File.ReadAllLines changeLog |> Seq.filter ((<>) "## Unreleased"))

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
            let projectName = Path.GetFileNameWithoutExtension(projectPath)
            (
                projectPath,
                projectName,
                Path.GetDirectoryName(projectPath),
                (getAssemblyInfoAttributes projectName)
            )

        ProjectSources.all
        |> Seq.map getProjectDetails
        |> Seq.iter (fun (_, _, folderName, attributes) ->
            AssemblyInfoFile.createFSharp (folderName </> "AssemblyInfo.fs") attributes
        )
    )

    Target.create "Build" (fun _ ->
        ProjectSources.all
        |> Seq.iter (DotNet.build id)
    )

    Target.create "Lint" <| skipOn "no-lint" (fun _ ->
        let lint project =
            project
            |> sprintf "fsharplint lint %s"
            |> Dotnet.runInRoot
            |> tee (function Ok _ -> Trace.tracefn "Lint %s is OK" project | _ -> ())

        let errors =
            ProjectSources.all
            |> Seq.map lint
            |> Seq.choose (function Error e -> Some e.Message | _ -> None)
            |> Seq.toList

        match errors with
        | [] -> Trace.tracefn "Lint is OK!"
        | errors -> errors |> String.concat "\n" |> failwithf "Lint ends with errors:\n%s"
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
    // 3. FAKE targets hierarchy
    // --------------------------------------------------------------------------------------------------------

    "Clean"
        ==> "AssemblyInfo"
        ==> "Build"
        ==> "Lint"
        ==> "Tests"
        ==> "Release"

[<EntryPoint>]
let main args =
    args
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    initTargets ()
    |> ignore

    match args with
    | [| "-t"; target |]
    | [| target |] -> Target.runOrDefaultWithArguments target
    | _ -> Target.runOrDefaultWithArguments "Build"

    0 // return an integer exit code
