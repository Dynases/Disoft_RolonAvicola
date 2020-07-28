﻿Imports DevComponents.DotNetBar
Imports DevComponents.DotNetBar.Controls
Imports ENTITY
Imports Janus.Windows.GridEX
Imports LOGIC
Imports UTILITIES
Imports Facturacion
Imports Logica.AccesoLogica
Imports System.Drawing.Printing
Imports System.IO
Imports CrystalDecisions.Shared

Public Class frmBillingDispatch
    Dim _inter As Integer = 0
    Public _nameButton As String
    Public _tab As SuperTabItem
    Public _modulo As SideNavItem

    Private _cargaCompleta = False

#Region "Eventos"
    Private Sub frmBillingDispatch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Init()
    End Sub

    Private Sub cbChoferes_ValueChanged(sender As Object, e As EventArgs) Handles cbChoferes.ValueChanged
        Try
            If (_cargaCompleta) Then
                CargarPedidos()
            End If
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub

    Private Sub btFacturar_Click(sender As Object, e As EventArgs) Handles btFacturar.Click
        Try
            Dim idChofer = Me.cbChoferes.Value
            If (Convert.ToInt32(idChofer) = ENCombo.ID_SELECCIONAR) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If
            Dim list2 As List(Of VPedido_BillingDispatch) = CType(dgjPedido.DataSource, List(Of VPedido_BillingDispatch))
            Dim list1 As List(Of VPedido_BillingDispatch) = New List(Of VPedido_BillingDispatch)

            For i As Integer = 0 To list2.Count - 1 Step 1
                'If (list2(i).NroFactura.Equals("") Or list2(i).NroFactura.Equals("0")) Then
                If (list2(i).NroFactura = Nothing) Then
                    list1.Add(list2(i))
                Else
                    If (list2(i).NroFactura.Equals("") Or list2(i).NroFactura.Equals("0")) Then
                        list1.Add(list2(i))
                    End If
                End If
            Next

            If (list1.Count = 0) Then
                ToastNotification.Show(Me, "No Existe ningun dato para generar Notas de Venta!!".ToUpper,
                                    My.Resources.OK,
                                    5 * 1000,
                                    eToastGlowColor.Red,
                                    eToastPosition.TopCenter)
                Return
            End If

            For i As Integer = 0 To list1.Count - 1 Step 1
                If L_YaSeGraboTV001(list1(i).Id) = False Then
                    GrabarTV001(Str(list1(i).Id))
                End If

                Dim dtDetalle As DataTable = L_prObtenerDetallePedidoFactura(Str(list1(i).Id))

                'P_fnGenerarFactura(dtDetalle.Rows(0).Item("oanumi"), dtDetalle.Rows(0).Item("subtotal"), dtDetalle.Rows(0).Item("descuento"), dtDetalle.Rows(0).Item("total"), dtDetalle.Rows(0).Item("nit"), dtDetalle.Rows(0).Item("cliente"), dtDetalle.Rows(0).Item("codcli"))
                P_prImprimirNotaVenta(dtDetalle.Rows(0).Item("oanumi"), True, True, idChofer)

            Next

            Dim img As Bitmap = New Bitmap(My.Resources.checked, 50, 50)

            CargarPedidos()
            ToastNotification.Show(Me, "Notas de Venta Generadas Correctamente".ToUpper,
                                      img, 2000,
                                      eToastGlowColor.Green,
                                      eToastPosition.TopCenter
                                      )



        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub
    Private Function P_fnValidarFactura() As Boolean
        Return True
    End Function

    Private Function P_fnGenerarFactura(numi As String, subtotal As Double, descuento As Double, total As Double, nit As String, Nombre As String, Codcli As String) As Boolean
        Dim res As Boolean = False
        res = P_fnGrabarFacturarTFV001(numi, subtotal, descuento, total, nit, Nombre, Codcli) ' Grabar en la TFV001
        If (res) Then
            If (P_fnValidarFactura()) Then
                'Validar para facturar
                P_prImprimirFacturar(numi, True, True, nit) '_Codigo de a tabla TV001
            Else
                'Volver todo al estada anterior
                ToastNotification.Show(Me, "No es posible facturar, vuelva a ingresar a la mesa he intente nuevamente!!!".ToUpper,
                                       My.Resources.OK,
                                       5 * 1000,
                                       eToastGlowColor.Red,
                                       eToastPosition.MiddleCenter)
            End If

            If (Not nit.Equals("0")) Then
                L_Grabar_Nit(nit, Nombre, "")
            Else
                L_Grabar_Nit(nit, "S/N", "")
            End If
        End If
        Dim dtfv001 As DataTable = L_fnObtenerTabla("fvanitcli, fvadescli1, fvadescli2, fvaautoriz, fvanfac, fvaccont, fvafec,fvaest", "TFV001", "fvanumi=" + numi + " or fvanumi=" + "-" + numi)
        If dtfv001.Rows.Count = 2 Then
            L_ActualizaNegativosTFV001(numi, "0")
        End If

        Return res
    End Function


    Private Sub P_prImprimirFacturar(numi As String, impFactura As Boolean, grabarPDF As Boolean, nit As String)
        Dim _Fecha, _FechaAl As Date
        Dim _Ds, _Ds1, _Ds2, _Ds3 As New DataSet
        Dim _Autorizacion, _Nit, _Fechainv, _Total, _Key, _Cod_Control, _Hora,
            _Literal, _TotalDecimal, _TotalDecimal2 As String
        Dim I, _NumFac, _numidosif, _TotalCC As Integer
        Dim ice, _Desc, _TotalLi As Decimal
        Dim _VistaPrevia As Integer = 0
        Dim QrFactura1 As String

        _Desc = CDbl(0)
        If Not IsNothing(P_Global.Visualizador) Then
            P_Global.Visualizador.Close()
        End If

        _Fecha = Now.Date.ToString("dd/MM/yyyy")
        _Hora = Now.Hour.ToString + ":" + Now.Minute.ToString
        _Ds1 = L_Dosificacion("1", "1", _Fecha)

        _Ds = L_Reporte_Factura(numi, numi)
        _Autorizacion = _Ds1.Tables(0).Rows(0).Item("yeautoriz").ToString
        _NumFac = CInt(_Ds1.Tables(0).Rows(0).Item("yenunf")) + 1
        _Nit = _Ds.Tables(0).Rows(0).Item("fvanitcli").ToString
        _Fechainv = Microsoft.VisualBasic.Right(_Fecha.ToShortDateString, 4) +
                    Microsoft.VisualBasic.Right(Microsoft.VisualBasic.Left(_Fecha.ToShortDateString, 5), 2) +
                    Microsoft.VisualBasic.Left(_Fecha.ToShortDateString, 2)
        _Total = _Ds.Tables(0).Rows(0).Item("fvatotal").ToString
        ice = _Ds.Tables(0).Rows(0).Item("fvaimpsi")
        _numidosif = _Ds1.Tables(0).Rows(0).Item("yenumi").ToString
        _Key = _Ds1.Tables(0).Rows(0).Item("yekey")
        _FechaAl = _Ds1.Tables(0).Rows(0).Item("yefal")

        Dim maxNFac As Integer = L_fnObtenerMaxIdTabla("TFV001", "fvanfac", "fvaautoriz = " + _Autorizacion)
        _NumFac = maxNFac + 1

        _TotalCC = Math.Round(CDbl(_Total), MidpointRounding.AwayFromZero)
        _Cod_Control = ControlCode.generateControlCode(_Autorizacion, _NumFac, _Nit, _Fechainv, CStr(_TotalCC), _Key)

        'Literal 
        _TotalLi = _Ds.Tables(0).Rows(0).Item("fvastot") - _Ds.Tables(0).Rows(0).Item("fvadesc")
        _TotalDecimal = _TotalLi - Math.Truncate(_TotalLi)
        _TotalDecimal2 = CDbl(_TotalDecimal) * 100

        'Dim li As String = Facturacion.ConvertirLiteral.A_fnConvertirLiteral(CDbl(_Total) - CDbl(_TotalDecimal)) + " con " + IIf(_TotalDecimal2.Equals("0"), "00", _TotalDecimal2) + "/100 Bolivianos"
        _Literal = Facturacion.ConvertirLiteral.A_fnConvertirLiteral(CDbl(_TotalLi) - CDbl(_TotalDecimal)) + " con " + IIf(_TotalDecimal2.Equals("0"), "00", _TotalDecimal2) + "/100 Bolivianos"
        _Ds2 = L_Reporte_Factura_Cia("1")
        QrFactura.Text = _Ds2.Tables(0).Rows(0).Item("scnit").ToString + "|" + Str(_NumFac).Trim + "|" + _Autorizacion + "|" + _Fecha + "|" + _Total + "|" + _TotalLi.ToString + "|" + _Cod_Control + "|" + nit + "|" + ice.ToString + "|0|0|" + Str(_Desc).Trim

        L_Modificar_Factura("fvanumi = " + CStr(numi),
                            "",
                            CStr(_NumFac),
                            CStr(_Autorizacion),
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            _Cod_Control,
                            _FechaAl.ToString("yyyy/MM/dd"),
                            "",
                            "",
                            CStr(numi))


        updateTO001C(numi, Str(_NumFac))
        _Ds = L_Reporte_Factura(numi, numi)

        _Ds3 = L_ObtenerRutaImpresora("1") ' Datos de Impresion de Facturación

        For I = 0 To _Ds.Tables(0).Rows.Count - 1
            _Ds.Tables(0).Rows(I).Item("fvaimgqr") = P_fnImageToByteArray(QrFactura.Image)
        Next
        P_Global.Visualizador = New Visualizador
        Dim objrep As New R_FacturaPreImpresa1
        Dim dia, mes, ano As Integer
        Dim Fecliteral, mesl As String
        Fecliteral = _Ds.Tables(0).Rows(0).Item("fvafec").ToString
        dia = Microsoft.VisualBasic.Left(Fecliteral, 2)
        mes = Microsoft.VisualBasic.Mid(Fecliteral, 4, 2)
        ano = Microsoft.VisualBasic.Mid(Fecliteral, 7, 4)
        If mes = 1 Then
            mesl = "Enero"
        End If
        If mes = 2 Then
            mesl = "Febrero"
        End If
        If mes = 3 Then
            mesl = "Marzo"
        End If
        If mes = 4 Then
            mesl = "Abril"
        End If
        If mes = 5 Then
            mesl = "Mayo"
        End If
        If mes = 6 Then
            mesl = "Junio"
        End If
        If mes = 7 Then
            mesl = "Julio"
        End If
        If mes = 8 Then
            mesl = "Agosto"
        End If
        If mes = 9 Then
            mesl = "Septiembre"
        End If
        If mes = 10 Then
            mesl = "Octubre"
        End If
        If mes = 11 Then
            mesl = "Noviembre"
        End If
        If mes = 12 Then
            mesl = "Diciembre"
        End If

        Fecliteral = "Cochabamba, " + dia.ToString + " de " + mesl + " del " + ano.ToString
        objrep.SetDataSource(_Ds.Tables(0))
        objrep.SetParameterValue("Literal", _Literal)
        objrep.SetParameterValue("Fechali", Fecliteral)
        objrep.SetParameterValue("Ley", _Ds1.Tables(0).Rows(0).Item("yenota").ToString())
        'objrep.PrintOptions.PrinterName = "L4150 Series(Red) (Copiar 1)"

        Dim pd As New PrintDocument()
        pd.PrinterSettings.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
        If (Not pd.PrinterSettings.IsValid) Then
            ToastNotification.Show(Me, "La Impresora ".ToUpper + _Ds3.Tables(0).Rows(0).Item("cbrut").ToString + Chr(13) + "No Existe".ToUpper,
                                   My.Resources.WARNING, 5 * 1000,
                                   eToastGlowColor.Blue, eToastPosition.BottomRight)
        Else
            objrep.PrintOptions.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
            objrep.PrintToPrinter(1, False, 1, 1)
        End If
        'objrep.PrintOptions.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
        'objrep.PrintToPrinter(1, False, 1, 1)




        ''For I = 0 To _Ds.Tables(0).Rows.Count - 1
        ''    _Ds.Tables(0).Rows(I).Item("fvaimgqr") = P_fnImageToByteArray(QrFactura.Image)
        ''Next
        'If (impFactura) Then
        '    _Ds3 = L_ObtenerRutaImpresora("1") ' Datos de Impresion de Facturación
        '    If (_Ds3.Tables(0).Rows(0).Item("cbvp")) Then 'Vista Previa de la Ventana de Vizualización 1 = True 0 = False
        '        P_Global.Visualizador = New Visualizador 'Comentar
        '    End If


        '    Dim objrep As Object = Nothing
        '    objrep = New R_FacturaPreImpresa

        '    objrep.SetDataSource(_Ds.Tables(0))
        '    objrep.SetParameterValue("Hora", _Hora)
        '    objrep.SetParameterValue("Literal", _Literal)

        '    P_Global.Visualizador.CRV1.ReportSource = objrep
        '    P_Global.Visualizador.Show()
        '    P_Global.Visualizador.BringToFront()


        '    'If (_Ds3.Tables(0).Rows(0).Item("cbvp")) Then 'Vista Previa de la Ventana de Vizualización 1 = True 0 = False
        '    '    P_Global.Visualizador.CRV1.ReportSource = objrep 'Comentar
        '    '    P_Global.Visualizador.ShowDialog() 'Comentar
        '    '    P_Global.Visualizador.BringToFront() 'Comentar
        '    'End If

        '    'Dim pd As New PrintDocument()
        '    'pd.PrinterSettings.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
        '    'If (Not pd.PrinterSettings.IsValid) Then
        '    '    ToastNotification.Show(Me, "La Impresora ".ToUpper + _Ds3.Tables(0).Rows(0).Item("cbrut").ToString + Chr(13) + "No Existe".ToUpper,
        '    '                           My.Resources.WARNING, 5 * 1000,
        '    '                           eToastGlowColor.Blue, eToastPosition.BottomRight)
        '    'Else
        '    '    objrep.PrintOptions.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString '"EPSON TM-T20II Receipt5 (1)"
        '    '    objrep.PrintToPrinter(1, False, 1, 1)
        '    'End If

        '    'If (grabarPDF) Then
        '    '    'Copia de Factura en PDF
        '    '    If (Not Directory.Exists(gs_CarpetaRaiz + "\Facturas")) Then
        '    '        Directory.CreateDirectory(gs_CarpetaRaiz + "\Facturas")
        '    '    End If
        '    '    objrep.ExportToDisk(ExportFormatType.PortableDocFormat, gs_CarpetaRaiz + "\Facturas\" + CStr(_NumFac) + "_" + CStr(_Autorizacion) + ".pdf")

        '    'End If
        'End If
        L_Actualiza_Dosificacion(_numidosif, _NumFac, numi)
    End Sub
    Private Sub P_prImprimirNotaVenta(idPedido As String, impFactura As Boolean, grabarPDF As Boolean, idChofer As String)
        Dim _Fecha, _FechaAl As Date
        Dim _Ds, _Ds2, _Ds3 As New DataSet
        Dim _Hora, _Literal, _TotalDecimal, _TotalDecimal2 As String
        Dim _NumFac, _numidosif As Integer
        Dim _Desc, _TotalLi As Decimal
        Dim _VistaPrevia As Integer = 0

        _Desc = CDbl(0)

        Dim listResult = New LPedido().ListarDespachoXNotaVentaDeChofer(idChofer, idPedido)
        If (listResult.Count = 0) Then
            Throw New Exception("No hay registros para generar el reporte.")
        End If
        If Not IsNothing(P_Global.Visualizador) Then
            P_Global.Visualizador.Close()
        End If

        _Fecha = Now.Date.ToString("dd/MM/yyyy")
        _Hora = Now.Hour.ToString + ":" + Now.Minute.ToString

        '_Ds = L_Reporte_Factura(numi, numi)


        'Literal 
        _TotalLi = listResult.Item(0).Total
        _TotalDecimal = _TotalLi - Math.Truncate(_TotalLi)
        _TotalDecimal2 = CDbl(_TotalDecimal) * 100

        _Literal = Facturacion.ConvertirLiteral.A_fnConvertirLiteral(CDbl(_TotalLi) - CDbl(_TotalDecimal)) + " con " + IIf(_TotalDecimal2.Equals("0"), "00", _TotalDecimal2) + "/100 Bolivianos"


        _Ds2 = L_Reporte_Factura_Cia("1")
        _Ds3 = L_ObtenerRutaImpresora("1") ' Datos de Impresion de Facturación

        P_Global.Visualizador = New Visualizador
        Dim objrep As New NotaVenta
        Dim dia, mes, ano As Integer
        Dim Fecliteral, mesl As String
        'Fecliteral = _Ds.Tables(0).Rows(0).Item("fvafec").ToString
        Fecliteral = listResult.Item(0).oafdoc
        dia = Microsoft.VisualBasic.Left(Fecliteral, 2)
        mes = Microsoft.VisualBasic.Mid(Fecliteral, 4, 2)
        ano = Microsoft.VisualBasic.Mid(Fecliteral, 7, 4)
        If mes = 1 Then
            mesl = "Enero"
        End If
        If mes = 2 Then
            mesl = "Febrero"
        End If
        If mes = 3 Then
            mesl = "Marzo"
        End If
        If mes = 4 Then
            mesl = "Abril"
        End If
        If mes = 5 Then
            mesl = "Mayo"
        End If
        If mes = 6 Then
            mesl = "Junio"
        End If
        If mes = 7 Then
            mesl = "Julio"
        End If
        If mes = 8 Then
            mesl = "Agosto"
        End If
        If mes = 9 Then
            mesl = "Septiembre"
        End If
        If mes = 10 Then
            mesl = "Octubre"
        End If
        If mes = 11 Then
            mesl = "Noviembre"
        End If
        If mes = 12 Then
            mesl = "Diciembre"
        End If

        Fecliteral = "La Paz, " + dia.ToString + " de " + mesl + " del " + ano.ToString
        objrep.SetDataSource(listResult)
        objrep.SetParameterValue("Literal", _Literal)
        objrep.SetParameterValue("Fechali", Fecliteral)
        objrep.SetParameterValue("nombreUsuario", P_Global.gs_user)
        objrep.SetParameterValue("Empresa", _Ds2.Tables(0).Rows(0).Item("scneg").ToString)

        If (_Ds3.Tables(0).Rows(0).Item("cbvp")) Then 'Vista Previa de la Ventana de Vizualización 1 = True 0 = False
            P_Global.Visualizador.CRV1.ReportSource = objrep 'Comentar
            P_Global.Visualizador.ShowDialog() 'Comentar
            P_Global.Visualizador.BringToFront() 'Comentar
        End If

        Dim pd As New PrintDocument()
        pd.PrinterSettings.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
        If (Not pd.PrinterSettings.IsValid) Then
            ToastNotification.Show(Me, "La Impresora ".ToUpper + _Ds3.Tables(0).Rows(0).Item("cbrut").ToString + Chr(13) + "No Existe".ToUpper,
                                   My.Resources.WARNING, 5 * 1000,
                                   eToastGlowColor.Blue, eToastPosition.BottomRight)
        Else
            objrep.PrintOptions.PrinterName = _Ds3.Tables(0).Rows(0).Item("cbrut").ToString
            objrep.PrintToPrinter(2, False, 1, 1)
        End If

        If (grabarPDF) Then
            'Copia de Factura en PDF
            If (Not Directory.Exists(gs_CarpetaRaiz + "\Facturas")) Then
                Directory.CreateDirectory(gs_CarpetaRaiz + "\Facturas")
            End If
            objrep.ExportToDisk(ExportFormatType.PortableDocFormat, gs_CarpetaRaiz + "\Facturas\" + CStr(idPedido) + ".pdf")

        End If

    End Sub
    Public Function P_fnImageToByteArray(ByVal imageIn As Image) As Byte()
        Dim ms As New System.IO.MemoryStream()
        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg)
        Return ms.ToArray()
    End Function
    Private Function P_fnGrabarFacturarTFV001(numi As String, subtotal As Double, descuento As Double, total As Double, nit As String, nameCliente As String, Codcli As String) As Boolean
        Dim a As Double = subtotal
        Dim b As Double = CDbl(0) 'Ya esta calculado el 55% del ICE
        Dim c As Double = CDbl("0")
        Dim d As Double = CDbl("0")
        Dim e As Double = a - b - c - d
        Dim f As Double = descuento
        Dim g As Double = e - f
        Dim h As Double = g * (13 / 100)

        Dim res As Boolean = False
        'Grabado de Cabesera Factura
        L_Grabar_Factura(numi,
                        Now.Date.ToString("yyyy/MM/dd"), "0", "0",
                        "1",
                        nit,
                        Codcli,
                       nameCliente,
                        "",
                        CStr(Format(a, "####0.00")),
                        CStr(Format(b, "####0.00")),
                        CStr(Format(c, "####0.00")),
                        CStr(Format(d, "####0.00")),
                        CStr(Format(e, "####0.00")),
                        CStr(Format(f, "####0.00")),
                        CStr(Format(g, "####0.00")),
                        CStr(Format(h, "####0.00")),
                        "",
                        Now.Date.ToString("yyyy/MM/dd"),
                        "''",
                        "0",
                        numi)


        Dim dtDetalle As DataTable = L_prObtenerDetallePedido(numi)
        For i As Integer = 0 To dtDetalle.Rows.Count - 1 Step 1

            L_Grabar_Factura_Detalle(numi.ToString,
                                        dtDetalle.Rows(i).Item("obcprod").ToString,
                                         dtDetalle.Rows(i).Item("producto").ToString,
                                        dtDetalle.Rows(i).Item("obpcant").ToString,
                                        dtDetalle.Rows(i).Item("obpbase").ToString,
                                        numi)

        Next
        Return True
    End Function

    Private Sub btReporteDespachoCliente_Click(sender As Object, e As EventArgs) Handles btReporteDespachoCliente.Click
        Try
            Dim idChofer = Me.cbChoferes.Value
            If (Not IsNumeric(idChofer)) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If
            If (Convert.ToInt32(idChofer) = ENCombo.ID_SELECCIONAR) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If

            Dim listResult = New LPedido().ListarDespachoXClienteDeChofer(idChofer, ENEstadoPedido.DICTADO)
            Dim lista = (From a In listResult
                         Where a.oafdoc = Tb_Fecha.Value).ToList
            If (lista.Count = 0) Then
                Throw New Exception("No hay registros para generar el reporte.")
            End If

            If Not IsNothing(P_Global.Visualizador) Then
                P_Global.Visualizador.Close()
            End If

            P_Global.Visualizador = New Visualizador
            Dim objrep As New DespachoXCliente

            objrep.SetDataSource(lista)
            objrep.SetParameterValue("nroDespacho", String.Empty)
            objrep.SetParameterValue("nombreDistribuidor", cbChoferes.Text)
            objrep.SetParameterValue("FechaDocumento", Tb_Fecha.Value)
            objrep.SetParameterValue("nombreUsuario", P_Global.gs_user)

            P_Global.Visualizador.CRV1.ReportSource = objrep
            P_Global.Visualizador.Show()
            P_Global.Visualizador.BringToFront()
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub

    Private Sub btReporteDespachoLinea_Click(sender As Object, e As EventArgs) Handles btReporteDespachoLinea.Click
        Try
            Dim idChofer = Me.cbChoferes.Value
            If (Not IsNumeric(idChofer)) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If
            If (Convert.ToInt32(idChofer) = ENCombo.ID_SELECCIONAR) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If

            Dim listResult = New LPedido().ListarDespachoXProductoDeChofer(idChofer, ENEstadoPedido.DICTADO)
            Dim lista = (From a In listResult
                         Where a.oafdoc = Tb_Fecha.Value).ToList
            If (lista.Count = 0) Then
                Throw New Exception("No hay registros para generar el reporte.")
            End If

            If Not IsNothing(P_Global.Visualizador) Then
                P_Global.Visualizador.Close()
            End If

            P_Global.Visualizador = New Visualizador
            Dim objrep As New DespachoXProducto

            objrep.SetDataSource(lista)
            objrep.SetParameterValue("nroDespacho", String.Empty)
            objrep.SetParameterValue("nombreDistribuidor", cbChoferes.Text)
            objrep.SetParameterValue("FechaDocumento", Tb_Fecha.Value)
            objrep.SetParameterValue("nombreUsuario", P_Global.gs_user)

            P_Global.Visualizador.CRV1.ReportSource = objrep
            P_Global.Visualizador.Show()
            P_Global.Visualizador.BringToFront()
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub

    Private Sub dgjPedido_SelectionChanged(sender As Object, e As EventArgs) Handles dgjPedido.SelectionChanged
        Try
            Dim idPedido = 0
            If (dgjPedido.GetRows().Count > 0) Then
                idPedido = Convert.ToInt32(dgjPedido.CurrentRow.Cells("Id").Value)
            End If

            CargarProductos(idPedido)
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub
    Private Sub Tb_Fecha_ValueChanged(sender As Object, e As EventArgs) Handles Tb_Fecha.ValueChanged
        Try
            If (_cargaCompleta) Then
                CargarPedidos()
            End If
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub
#End Region

#Region "Privado, metodos y funciones"
    Private Sub Init()
        Try
            ConfigForm()
            CargarChoferes()
            Tb_Fecha.Value = DateTime.Today
            _cargaCompleta = True
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub

    Private Sub ConfigForm()
        Try
            Me.Text = "FACTURACIÓN/DESPACHO"
            ' Me.WindowState = FormWindowState.Maximized
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Sub

    Private Sub CargarChoferes()
        Try
            Dim listResult As List(Of VCombo) = New LPersonal().ListarRepatidorCombo()

            With cbChoferes.DropDownList
                .Columns.Clear()

                .Columns.Add("Id").Width = 30
                .Columns("Id").Caption = "Id"
                .Columns("Id").Visible = True

                .Columns.Add("Descripcion").Width = 180
                .Columns("Descripcion").Caption = "Nombre repartidor"
                .Columns("Descripcion").Visible = True

                .ValueMember = "Id"
                .DisplayMember = "Descripcion"
                .DataSource = listResult

                .AlternatingColors = True
                .AllowColumnDrag = False
                .AutomaticSort = False
                .Refresh()
            End With
            cbChoferes.VisualStyle = VisualStyle.Office2007

            cbChoferes.SelectedIndex = 0
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Sub

    Private Sub CargarPedidos()
        Try
            Dim idChofer = Me.cbChoferes.Value
            If (Not IsNumeric(idChofer)) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If
            If (Convert.ToInt32(idChofer) = ENCombo.ID_SELECCIONAR) Then
                Throw New Exception("Debe seleccionar un chofer.")
            End If

            Dim listResult = New LPedido().ListarPedidoAsignadoAChofer(idChofer, ENEstadoPedido.DICTADO)
            Dim lista = (From a In listResult
                         Where a.Fecha = Tb_Fecha.Value).ToList
            dgjPedido.BoundMode = Janus.Data.BoundMode.Bound
            dgjPedido.DataSource = lista
            dgjPedido.RetrieveStructure()

            With dgjPedido.RootTable.Columns("Fecha")
                .Caption = "Fecha Pedido"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .Position = 0
            End With

            With dgjPedido.RootTable.Columns("NombreCliente")
                .Caption = "Cliente"
                .Width = 400
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Near
                .Visible = True
                .Position = 1
            End With

            With dgjPedido.RootTable.Columns("Id")
                .Caption = "Pedido"
                .Width = 60
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .Position = 2
            End With

            With dgjPedido.RootTable.Columns("NombreVendedor")
                .Caption = "Vendedor"
                .Width = 250
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Near
                .Visible = True
                .Position = 3
            End With

            With dgjPedido.RootTable.Columns("idZona")
                .Caption = "Zona"
                .Width = 120
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Center
                .Visible = True
                .Position = 4
            End With
            With dgjPedido.RootTable.Columns("EstaFacturado")
                .Caption = "Facturado"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Center
                .Visible = False
                .Position = 5
            End With

            With dgjPedido.RootTable.Columns("NroFactura")
                .Caption = "Nro. Factura"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = False
                .Position = 6
            End With
            With dgjPedido.RootTable.Columns("nombreZona")
                .Caption = "Nombre Zona"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = False
                .Position = 7
            End With
            dgjPedido.RootTable.Columns.Add(New GridEXColumn("Check"))
            With dgjPedido.RootTable.Columns("Check")
                .Caption = "Volver a Dist."
                .Width = 100
                .ShowRowSelector = True
                .UseHeaderSelector = True
                .FilterEditType = FilterEditType.NoEdit
                .Position = 8
            End With
            With dgjPedido
                .GroupByBoxVisible = False
                .DefaultFilterRowComparison = FilterConditionOperator.Contains
                .FilterMode = FilterMode.Automatic
                .FilterRowUpdateMode = FilterRowUpdateMode.WhenValueChanges
                .VisualStyle = VisualStyle.Office2007
                .SelectionMode = SelectionMode.MultipleSelection
                .AlternatingColors = True
                .AllowEdit = InheritableBoolean.False
                .AllowColumnDrag = False
                .AutomaticSort = False
                '.ColumnHeaders = InheritableBoolean.False
            End With
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Sub

    Private Sub CargarProductos(idPedido As Integer)
        Try
            Dim listResult = New LProducto().ListarProductoXPedido(idPedido)

            dgjProducto.BoundMode = Janus.Data.BoundMode.Bound
            dgjProducto.DataSource = listResult
            dgjProducto.RetrieveStructure()

            With dgjProducto.RootTable.Columns("Id")
                .Visible = False
            End With

            With dgjProducto.RootTable.Columns("NombreProducto")
                .Caption = "Producto"
                .Width = 250
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Near
                .Visible = True
            End With

            With dgjProducto.RootTable.Columns("Cantidad")
                .Caption = "Cantidad"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .FormatString = "0"
            End With

            With dgjProducto.RootTable.Columns("Precio")
                .Caption = "Precio"
                .Width = 80
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .FormatString = "0.00"
            End With
            With dgjProducto.RootTable.Columns("SubTotal")
                .Caption = "SubTotal"
                .Width = 120
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .FormatString = "0.00"
                .AggregateFunction = AggregateFunction.Sum
            End With
            With dgjProducto.RootTable.Columns("Descuento")
                .Caption = "Descuento"
                .Width = 120
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .FormatString = "0.00"
                .AggregateFunction = AggregateFunction.Sum
            End With
            With dgjProducto.RootTable.Columns("Total")
                .Caption = "Total"
                .Width = 120
                .CellStyle.TextAlignment = Janus.Windows.GridEX.TextAlignment.Far
                .Visible = True
                .FormatString = "0.00"
                .AggregateFunction = AggregateFunction.Sum
            End With

            With dgjProducto
                .GroupByBoxVisible = False
                .DefaultFilterRowComparison = FilterConditionOperator.Contains
                '.FilterMode = FilterMode.Automatic
                .FilterRowUpdateMode = FilterRowUpdateMode.WhenValueChanges
                .VisualStyle = VisualStyle.Office2007
                .SelectionMode = SelectionMode.MultipleSelection
                .AlternatingColors = True
                .AllowEdit = InheritableBoolean.False
                .AllowColumnDrag = False
                .AutomaticSort = False
                '.ColumnHeaders = InheritableBoolean.False

                .TotalRow = InheritableBoolean.True
                .TotalRowFormatStyle.BackColor = Color.Gold
                .TotalRowPosition = TotalRowPosition.BottomFixed
            End With
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Sub

    Private Sub MostrarMensajeError(mensaje As String)
        ToastNotification.Show(Me,
                               mensaje.ToUpper,
                               My.Resources.WARNING,
                               ENMensaje.MEDIANO,
                               eToastGlowColor.Red,
                               eToastPosition.TopCenter)
    End Sub
    Private Sub MostrarMensajeOk(mensaje As String)
        ToastNotification.Show(Me,
                               mensaje.ToUpper,
                               My.Resources.OK,
                               ENMensaje.MEDIANO,
                               eToastGlowColor.Green,
                               eToastPosition.TopCenter)
    End Sub
#End Region

#Region "Publico, metodos y funciones"
    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub btVolverDist_Click(sender As Object, e As EventArgs) Handles btVolverDist.Click
        Try
            VolverPedidoDistribucion()
        Catch ex As Exception
            MostrarMensajeError(ex.Message)
        End Try
    End Sub
    Private Sub VolverPedidoDistribucion()
        Try
            Dim checks = Me.dgjPedido.GetCheckedRows()
            Dim listIdPedido = checks.Select(Function(a) Convert.ToInt32(a.Cells("Id").Value)).ToList()
            Dim listFacPedido = checks.Select(Function(a) Convert.ToInt32(a.Cells("NroFactura").Value)).ToList()

            If (listIdPedido.Count = 0) Then
                Throw New Exception("Debe seleccionar por lo menos un pedido.")
            End If
            For Each nfact As Integer In listFacPedido
                Dim nro As Integer = nfact
                If nro > 0 Then
                    MostrarMensajeError("Debe de seleccionar solo los pedidos que no hayan sido facturados")
                    Exit Sub
                End If
            Next
            Dim idChofer = Me.cbChoferes.Value
            Dim result = New LPedido().VolverPedidoDistribucion(listIdPedido, idChofer)
            If (result) Then
                CargarPedidos()
                MostrarMensajeOk("Pedidos volvieron a Distribución correctamente")
            End If
        Catch ex As Exception
            Throw New Exception(ex.Message)
        End Try
    End Sub

#End Region
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        _inter = _inter + 1
        If _inter = 1 Then
            Me.WindowState = FormWindowState.Normal

        Else
            Me.Opacity = 100
            Timer1.Enabled = False
        End If
        'Me.Opacity = 100
        'Timer1.Enabled = False
    End Sub
End Class