USE BDDistBHF_CF 
GO
/****** Object:  StoredProcedure [dbo].[sp_Mam_Asiento]    Script Date: 07/12/2019 09:44:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


--drop procedure sp_Mam_TS006
alter PROCEDURE [dbo].[sp_Mam_Asiento] (@tipo int,@seuact nvarchar(10)='',@categoria int=-1,@canumi int=-1,
@cuenta nvarchar(20)='',@descripcion nvarchar(200)='',@empresa int=-1,@sector int=-1,@vcnumi int=-1,@servicio int=-1,@fechaI date=null,
@fechaF date=null,@sucursal int=-1,@Estado int=-1, @tventa int=-1,@modulo int=-1,@factura int=-1,@Id int=-1,@proveedor int=-1)
AS
BEGIN
	DECLARE @newHora nvarchar(5)
	set @newHora=CONCAT(DATEPART(HOUR,GETDATE()),':',DATEPART(MINUTE,GETDATE()))

	DECLARE @newFecha date
	set @newFecha=GETDATE()
IF @tipo=10 --MOSTRAR CUENTAS
	BEGIN
		BEGIN TRY	
  
   select isnull(cuenta .canumi ,-1) as canumi,isnull(cuenta .cacta,0) as nro,isnull(cuenta .cadesc,'') as cadesc ,b.Porcentaje  as chporcen,b.Debe as  chdebe ,b.Haber  as chhaber,cast(null as decimal (18,2)) as tc
   ,cast(null as decimal (18,2)) as debe,cast(null as decimal (18,2)) as haber,cast(null as decimal (18,2)) as debesus
   ,cast(null as decimal (18,2)) as habersus,cast(null as int) as variable,cast(null as int) as linea
  from  BDDiconCF .DBO. Plantilla  as a 
  inner join BDDiconCF .DBO. DetallePlantilla  as b on a.Id  =b.PlantillaId   
  left join BDDiconCF .DBO. TC001 as cuenta on cuenta.canumi =b.CuentaId 
  where a.Id =@Id  and b.Debe >0

  union

    select isnull(cuenta .canumi ,-1) as canumi,isnull(cuenta .cacta,0) as nro,isnull(cuenta .cadesc,'') as cadesc ,100  as chporcen,0as  chdebe ,1 as chhaber,cast(null as decimal (18,2)) as tc
   ,cast(null as decimal (18,2)) as debe,cast(null as decimal (18,2)) as haber,cast(null as decimal (18,2)) as debesus
   ,cast(null as decimal (18,2)) as habersus,cast(null as int) as variable,cast(null as int) as linea
  from  TC010  as proveedor 
  inner join BDDiconCF .DBO. TC001 as cuenta on cuenta.canumi =proveedor.cmncuenta 
  where proveedor.cmnumi  =@proveedor

		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@seuact)
		END CATCH

END

	IF @tipo=37 --Obtener Detalle Plantilla por ID
	BEGIN
		BEGIN TRY	
	
		select *
		from BDDiconCF .DBO.DetallePlantilla where PlantillaId =@Id and CuentaId =@cuenta 
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
					VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@seuact)
		END CATCH		

	END


	IF @tipo=26  -------Obtener Cuenta Diferencia  de Cambio
	BEGIN
		BEGIN TRY	
    select hijo.canumi ,hijo.cacta,hijo.cadesc 
	from BDDiconCF .DBO.TC001 as hijo where hijo.canumi =@cuenta 
 union 
 select padre.canumi ,padre.cacta ,padre .cadesc 
 from BDDiconCF .DBO.TC001 as padre inner join BDDiconCF .DBO.TC001 as hijo 
 on hijo.capadre =padre.canumi 
 and hijo.canumi =@cuenta
 order by canumi asc
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@seuact)
		END CATCH

END
		IF @tipo=35 --preguntar si es un servicio
	BEGIN
		BEGIN TRY	
			
			select Plantilla .Id ,Plantilla .Descripcion ,Plantilla .Tipo ,Plantilla .Factura
			from BDDiconCF .DBO.Plantilla where id=@Id 
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
					VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@seuact)
		END CATCH		

	END

End






