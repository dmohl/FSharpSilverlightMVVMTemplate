namespace FSSLAppTemplateWizard

open System
open System.IO
open System.Collections.Generic
open System.Collections
open EnvDTE
open Microsoft.VisualStudio.TemplateWizard
open VSLangProj
open FsSlAppDialog

[<AutoOpen>]
module TemplateWizardMod =
    let AddProjectReference (target:Option<Project>) (projectToReference:Option<Project>) =
        if ((Option.isSome target) && (Option.isSome projectToReference)) then
            let vsControllerProject = target.Value.Object :?> VSProject
            let existingProjectReference = 
                vsControllerProject.References.Find(projectToReference.Value.Name) 
            if (existingProjectReference <> null) then existingProjectReference.Remove() 
            vsControllerProject.References.AddProject(projectToReference.Value) |> ignore

    let BuildProjectMap (projectEnumerator:IEnumerator) =
        let rec buildProjects (projectMap:Map<string,Project>) = 
            match projectEnumerator.MoveNext() with
            | true -> let project = projectEnumerator.Current :?> Project
                      projectMap 
                      |> Map.add project.Name project
                      |> buildProjects 
            | _ -> projectMap
        buildProjects Map.empty

type TemplateWizard() =
    let projectRefs = [("View", "Core")]
    [<DefaultValue>] val mutable Dte : DTE
    interface IWizard with
        member x.RunStarted (automationObject:Object, replacementsDictionary:Dictionary<string,string>, 
                             runKind:WizardRunKind, customParams:Object[]) =
            x.Dte <- automationObject :?> DTE
            let x86Path = 
                match Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) with
                | progFiles when String.IsNullOrEmpty progFiles -> 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                | _ -> 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            let sl4Exists = Directory.Exists(Path.Combine(x86Path, @"Microsoft F#\Silverlight\Libraries\Client\v4.0"))
            let sl5Exists = Directory.Exists(Path.Combine(x86Path, @"Microsoft F#\Silverlight\Libraries\Client\v5.0"))
            match sl4Exists, sl5Exists with
            | true, true ->
                let dialog = new TemplateWizardDialog()
                match dialog.ShowDialog().Value with
                | true -> 
                    replacementsDictionary.["$targetframeworkversion$"] <- dialog.SelectedSilverlightVersion
                | _ ->
                    raise (new WizardCancelledException())
            | false, false ->
                raise(new ApplicationException("Please install version 4 and/or 5 of the F# Silverlight Developer Tools - see http://blogs.msdn.com/b/fsharpteam/archive/2011/04/22/update-to-the-f-2-0-free-tools-release-corresponding-to-visual-studio-2010-sp1-april-2011-ctp.aspx."))
            | true, false -> replacementsDictionary.["$targetframeworkversion$"] <- "4.0"
            | false, true -> replacementsDictionary.["$targetframeworkversion$"] <- "5.0"
        member x.ProjectFinishedGenerating (project:Project) =
            try
                let projects = BuildProjectMap (x.Dte.Solution.Projects.GetEnumerator())
                projectRefs 
                |> Seq.iter (fun (target,source) -> 
                             do AddProjectReference (projects.TryFind target) (projects.TryFind source))
            with 
            | ex -> ex.Message |> ignore
        member x.ProjectItemFinishedGenerating projectItem = "Do Nothing" |> ignore
        member x.ShouldAddProjectItem filePath = true
        member x.BeforeOpeningFile projectItem = "Do Nothing" |> ignore
        member x.RunFinished() = "Do Nothing" |> ignore
