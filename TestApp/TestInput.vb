Imports DeveloperCore.Tauri

Public Class TestInput
    Inherits ProxyObject

    Public Property Name As String
    Public Property Child As TestInput

    Public Function UpdateName(name As String) As TestInput
        Me.Name = name
        Return Me
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function

    Public Event TestEvent(sender As Object, e As EventArgs)
    Public Event TestEvent2(sender As Object, e As EventArgs)

    Public Sub RaiseTestEvent()
        RaiseEvent TestEvent(Me, New EventArgs)
    End Sub
    
    Public Sub RaiseTestEvent2()
        RaiseEvent TestEvent2(Me, New EventArgs)
    End Sub
End Class