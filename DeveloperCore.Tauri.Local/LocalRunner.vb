Imports WatsonWebserver

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
        Dim server As New Webserver(New Core.WebserverSettings("localhost", _port + 1), Async Function(x) Await x.Response.Send(""))
        server.Routes.PreAuthentication.Content.Add("/", True)
        server.Start()
        _webView.Run($"localhost:{_port + 1}/index.html")
        Host.Stop()
        server.Stop()
    End Sub
End Class
