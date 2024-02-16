Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Web.WebView2.WinForms

Public Class WinFormsWebView
    Implements IWebView

    Public Property Form As Form
    Public Property WebView As WebView2

    Public Sub Run(url As String) Implements IWebView.Run
        If Form Is Nothing Then
            Form = New Form With {
                .WindowState = FormWindowState.Maximized,
                .Size = New Size(800, 600)
                }
        End If
        If WebView Is Nothing Then
            WebView = New WebView2 With {
                .Dock = DockStyle.Fill
                }
            Form.Controls.Add(WebView)
        End If
        WebView.EnsureCoreWebView2Async().ContinueWith(Sub()
            Form.Invoke(Sub() WebView.CoreWebView2.Navigate(url))
        End Sub)
        Application.Run(Form)
    End Sub
End Class