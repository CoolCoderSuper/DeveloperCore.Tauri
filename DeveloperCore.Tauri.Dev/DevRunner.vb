Public Class DevRunner
    Implements IRunner
    Private ReadOnly _url As String
    Private ReadOnly _port As Integer
    Private ReadOnly _webView As IWebView

    Public Sub New(url As String, port As Integer, webView As IWebView)
        _url = url
        _port = port
        _webView = webView
        Host = New Host("localhost", _port)
    End Sub

    Public ReadOnly Property Host As Host Implements IRunner.Host

    Public Sub Run() Implements IRunner.Run
        Host.Start()
        _webView.Run(_url)
        Host.Stop()
    End Sub
End Class