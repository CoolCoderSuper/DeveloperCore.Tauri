Public Class EventFireInfo
    Public Sub New(id As String, params As EventFireParam())
        Me.Id = id
        Me.Params = params
    End Sub

    Public Property Id As String
    Public Property Params As EventFireParam()
End Class

Public Class EventFireParam
    Public Sub New(value As Object, isProxy As Boolean)
        Me.Value = value
        Me.IsProxy = isProxy
    End Sub

    Public Property Value As Object
    Public Property IsProxy As Boolean
End Class