Imports DevComponents.DotNetBar

Public Class MensajesDeVentana
    Public Shared Sub MostrarMensajeError(control As Control, mensaje As String)
        ToastNotification.Show(control,
                               mensaje.ToUpper,
                               My.Resources.WARNING,
                               5000,
                               eToastGlowColor.Red,
                               eToastPosition.TopCenter)
    End Sub
    Public Shared Sub MostrarMensajeOk(control As Control, mensaje As String)
        ToastNotification.Show(control,
                               mensaje.ToUpper,
                               My.Resources.OK,
                               5000,
                               eToastGlowColor.Green,
                               eToastPosition.TopCenter)
    End Sub
End Class
