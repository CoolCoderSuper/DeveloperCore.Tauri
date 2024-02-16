Public Class ProxyEventInfo
    Public Sub New(id As String, name As String, guid As Guid, handler As [Delegate])
        Me.Id = id
        Me.Name = name
        Me.Guid = guid
        Me.Handler = handler
    End Sub

    Public Property Id As String
    Public Property Name As String
    Public Property Guid As Guid
    Public Property Handler As [Delegate]
End Class