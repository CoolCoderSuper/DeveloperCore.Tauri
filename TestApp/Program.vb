Imports System.IO
Imports DeveloperCore.Tauri
Imports DeveloperCore.Tauri.WinForms

Public Module Program
    Public Sub Main(args As String())
        Dim app As IRunner
        Dim webView As New WinFormsWebView
        #If DEBUG Then
            app = New Dev.DevRunner("http://localhost:5173", 5178, webView)
        #Else
            app = New Local.LocalRunner(5178, webView)
        #End If
        With app.Host
            With .Commands
                .Add("add", Function(a As Integer, b As Integer) a + b)
                .Add("subtract", Function(a As Integer, b As Integer) a - b)
                .Add("multiply", Function(a As Integer, b As Integer) a * b)
                .Add("divide", Function(a As Integer, b As Integer) a / b)
                .Add("concat", Function(a As String, b As String) a & b)
                .Add("sum", Function(a As Integer()) a.Sum())
                .Add("average", Function(a As List(Of Integer)) a.Average())
                .Add("test", Function(a As TestInput) a)
                .Add("test2", Function() app.Host.Instances.First)
                .Add("test3", New TestDelegate(AddressOf Test))
                .Add("cleanup", Async Function() As Task
                    Await app.Host.Cleanup()
                End Function)
            End With
            .TypeAliases.Add("TestInput", GetType(TestInput))
            .Instances.Add(New TestInput With {.Id = "1", .Name = "Joe", .Child = New TestInput With {.Id = "3", .Name = "Child"}})
            .Instances.Add(New TestInput With {.Id = "2", .Name = "Bob"})
        End With
        app.Run()
    End Sub
    
    Delegate Function TestDelegate(a As TestInput) As Task(Of TestInput)
    
    Private Async Function Test(a As TestInput) As Task(Of TestInput)
        Await Task.Delay(5000)
        Await File.WriteAllTextAsync("test.txt", a.Name)
        Return a
    End Function
End Module