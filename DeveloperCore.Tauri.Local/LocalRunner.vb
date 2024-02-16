Imports WatsonWebserver
Imports WatsonWebserver.Core

Public Class LocalRunner
    Implements IRunner
    Private ReadOnly _port As Integer
    Private ReadOnly _webView As IWebView

    Public Sub New(port As Integer, webView As IWebView)
        _port = port
        _webView = webView
        Host = New Host("localhost", _port)
    End Sub

    Public ReadOnly Property Host As Host Implements IRunner.Host

    Public Sub Run() Implements IRunner.Run
        Host.Start()
        Dim server As New Webserver(New WebserverSettings("localhost", _port + 1), Async Function(ctx)
                                                                                       If ctx.Request.Method = HttpMethod.GET Then
                                                                                           Dim route As String = ctx.Request.Url.RawWithQuery.Substring(1)
                                                                                           If IO.File.Exists(route) Then
                                                                                               Dim ext As String = IO.Path.GetExtension(route).ToLower()
                                                                                               Dim content As Byte() = IO.File.ReadAllBytes(route)
                                                                                               ctx.Response.StatusCode = 200
                                                                                               ctx.Response.ContentType = MimeTypes.GetFromExtension(ext)
                                                                                               Await ctx.Response.Send(content)
                                                                                           Else
                                                                                               Dim indexPath As String = "index.html"
                                                                                               Dim content As String = IO.File.ReadAllText(indexPath)
                                                                                               ctx.Response.StatusCode = 200
                                                                                               ctx.Response.ContentType = "text/html"
                                                                                               Await ctx.Response.Send(content)
                                                                                           End If
                                                                                       End If
                                                                                   End Function)
        server.Start()
        _webView.Run($"localhost:{_port + 1}/")
        Host.Stop()
        server.Stop()
    End Sub
End Class
