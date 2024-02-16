Imports Microsoft.CodeAnalysis

Public Module Program
    Public Sub Main(args As String())
        Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults()
        Dim slnPath = If(args.Length = 1, args(0), "C:\CodingCool\Code\Projects\DeveloperCore.Tauri\DeveloperCore.Tauri.sln")
        Console.WriteLine("Loading solution...")
        Dim tLoad As Task(Of Solution) = SolutionLoader.LoadSolution(slnPath)
        tLoad.Wait()
        Dim sln As Solution = tLoad.Result
        'Console.WriteLine("Calculating map...")
        'Dim maps As List(Of ProjectMap) = ProjectMap.GetMap(sln)
    End Sub
End Module