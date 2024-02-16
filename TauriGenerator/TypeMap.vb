Imports Microsoft.CodeAnalysis

Public Class TypeMap
    Public ReadOnly Property Type As ISymbol
    Public ReadOnly Property Members As New List(Of ISymbol)

    Public Sub New(type As ISymbol)
        Me.Type = type
    End Sub
End Class