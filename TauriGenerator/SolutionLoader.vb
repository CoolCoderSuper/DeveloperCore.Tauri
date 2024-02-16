Imports Microsoft.CodeAnalysis.MSBuild

Public Class SolutionLoader
    Public Shared Property Workspace As MSBuildWorkspace

    ''' <summary>
    ''' Loads a solution into the default workspace
    ''' </summary>
    ''' <param name="strFile">The solution to load.</param>
    ''' <returns></returns>
    Public Shared Async Function LoadSolution(strFile As String) As Task(Of Microsoft.CodeAnalysis.Solution)
        Workspace = MSBuildWorkspace.Create
        Return Await Workspace.OpenSolutionAsync(strFile)
    End Function
End Class