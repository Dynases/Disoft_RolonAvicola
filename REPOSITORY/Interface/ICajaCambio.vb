Imports ENTITY

Public Interface ICajaCambio
    Function ListarCajaGeneral_Report(FechaDesde As DateTime, fechaHasta As DateTime) As List(Of VCajaGeneral)
    Function ListarCajaGeneral_ReportSucursal(FechaDesde As DateTime, fechaHasta As DateTime, IdSuc As Integer) As List(Of VCajaGeneral)
    Function Listar(IdCaja As Integer) As List(Of VCajaCambio)
    Function GuardarCajaCambio(listIdCaja As List(Of VCajaCambio), idCaja As Integer) As Boolean
End Interface
