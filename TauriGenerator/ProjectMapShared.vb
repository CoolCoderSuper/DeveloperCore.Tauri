Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Partial Public Class ProjectMap
    Public Shared Function GetMap(sln As Solution) As List(Of ProjectMap)
        Dim maps As New List(Of ProjectMap)
        For Each proj As Project In sln.Projects
            Dim map As New ProjectMap With {.Name = proj.Name}
            For Each doc As Document In proj.Documents
                Dim model As SemanticModel = doc.GetSemanticModelAsync().Result
                Dim root As SyntaxNode = doc.GetSyntaxRootAsync().Result
                For Each classDecl As ClassStatementSyntax In root.DescendantNodes.OfType(Of ClassStatementSyntax)
                    Dim symbol As ISymbol = model.GetDeclaredSymbol(classDecl)
                    Dim typeMap As New TypeMap(symbol)
                    LoadTypeMap(typeMap, model, classDecl.Parent)
                    map.Types.Add(typeMap)
                Next
                For Each structDecl As StructureStatementSyntax In root.DescendantNodes.OfType(Of StructureStatementSyntax)
                    Dim symbol As ISymbol = model.GetDeclaredSymbol(structDecl)
                    Dim typeMap As New TypeMap(symbol)
                    LoadTypeMap(typeMap, model, structDecl.Parent)
                    map.Types.Add(typeMap)
                Next
                For Each enumDecl As EnumStatementSyntax In root.DescendantNodes.OfType(Of EnumStatementSyntax)
                    Dim symbol As ISymbol = model.GetDeclaredSymbol(enumDecl)
                    Dim typeMap As New TypeMap(symbol)
                    LoadTypeMap(typeMap, model, enumDecl.Parent)
                    map.Types.Add(typeMap)
                Next
                For Each interfaceDecl As InterfaceStatementSyntax In root.DescendantNodes.OfType(Of InterfaceStatementSyntax)
                    Dim symbol As ISymbol = model.GetDeclaredSymbol(interfaceDecl)
                    Dim typeMap As New TypeMap(symbol)
                    LoadTypeMap(typeMap, model, interfaceDecl.Parent)
                    map.Types.Add(typeMap)
                Next
                For Each moduleDecl As ModuleStatementSyntax In root.DescendantNodes.OfType(Of ModuleStatementSyntax)
                    Dim symbol As ISymbol = model.GetDeclaredSymbol(moduleDecl)
                    Dim typeMap As New TypeMap(symbol)
                    LoadTypeMap(typeMap, model, moduleDecl.Parent)
                    map.Types.Add(typeMap)
                Next
            Next
            map.Types.RemoveAll(Function(m) m.Type.DeclaredAccessibility = Accessibility.Private OrElse m.Type.DeclaredAccessibility = Accessibility.Friend)
            maps.Add(map)
        Next
        Return maps
    End Function

    Private Shared Sub LoadTypeMap(map As TypeMap, model As SemanticModel, block As SyntaxNode)
        map.Members.AddRange(block.DescendantNodes.OfType(Of MethodStatementSyntax).Select(Function(m) model.GetDeclaredSymbol(m)))
        map.Members.AddRange(block.DescendantNodes.OfType(Of PropertyStatementSyntax).Select(Function(m) model.GetDeclaredSymbol(m)))
        map.Members.AddRange(block.DescendantNodes.OfType(Of EventStatementSyntax).Select(Function(m) model.GetDeclaredSymbol(m)))
        map.Members.RemoveAll(Function(m) m.DeclaredAccessibility = Accessibility.Private OrElse m.DeclaredAccessibility = Accessibility.Friend)
    End Sub
End Class