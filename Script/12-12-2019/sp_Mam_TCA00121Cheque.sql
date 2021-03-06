USE [BDDistBHF_CF]
GO
/****** Object:  StoredProcedure [dbo].[sp_Mam_TCA00121Cheque]    Script Date: 12/12/2019 06:27:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO






--drop procedure sp_Mam_TV00121Cheque
ALTER PROCEDURE [dbo].[sp_Mam_TCA00121Cheque] (@tipo int,@tenumi int=-1,@tefdoc date=null,
@tety4vend int=-1,@teobs nvarchar(100)='',@tdnumi int=-1,@teuact nvarchar(10)='',@TC00121 TC00121TypeCheque ReadOnly,
@credito int=-1)

AS
BEGIN
	DECLARE @codProv int			
	DECLARE @nomcliprov nvarchar(200)
	DECLARE @caemision int
	DECLARE @montot decimal (18,2)
	DECLARE @newHora nvarchar(5)
	set @newHora=CONCAT(DATEPART(HOUR,GETDATE()),':',DATEPART(MINUTE,GETDATE()))	
	DECLARE @newFecha date
	set @newFecha=GETDATE()
	declare @contabilizo int

	IF @tipo=-1 --ELIMINAR REGISTRO
	BEGIN
		BEGIN TRY

		DELETE FROM TCA00121  WHERE tdtc13numi =@tenumi 
		delete from TCA0013 where tenumi=@tenumi 

		-----Inserto con Estado 3 "Eliminado/Anulado" en BDDiconCF.dbo.TPA001 que servirá para hacer el asiento contable-----	
		set @contabilizo=(select count(*) from BDDiconCF.dbo.TPA001 as a where a.aanumipadre=@tenumi and aatipo=3 and aanumiasiento>0)
		if 	@contabilizo>=1	
		Begin
			INSERT INTO BDDiconCF.dbo.TPA001
			SELECT aanumipadre, aafecha,3, aacodcliprov, aanomcliprov,
			aaemision,3,aamontototal, aamoneda,6.96, aanscf, -2
			FROM BDDiconCF.dbo.TPA001 
			WHERE aanumi in (SELECT max(aanumi) FROM BDDiconCF.dbo.TPA001 WHERE aanumipadre=@tenumi and aatipo=3)
		End
		else
		Begin
			INSERT INTO BDDiconCF.dbo.TPA001
			SELECT aanumipadre, aafecha,3, aacodcliprov, aanomcliprov,
			aaemision,3,aamontototal, aamoneda,6.96, aanscf, -1
			FROM BDDiconCF.dbo.TPA001 
			WHERE aanumi in (SELECT max(aanumi) FROM BDDiconCF.dbo.TPA001 WHERE aanumipadre=@tenumi and aatipo=3)

			----Actualizo el aanumiasiento a -1 de los demás registros que corresponden al aanumipadre que se está eliminando----	
			UPDATE BDDiconCF.dbo.TPA001 SET aanumiasiento=-1
			where (aanumipadre=@tenumi and aatipo=3) and (aaestado=1 or aaestado=2)
		End	
		
		select @tdnumi as newNumi  --Consultar que hace newNumi
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),-1,@newFecha,@newHora,@teuact)
		END CATCH
	END

	IF @tipo=1 --NUEVO REGISTRO
	BEGIN
		BEGIN TRY 

		      set @tenumi=IIF((select COUNT(tenumi) from TCA0013)=0,0,(select MAX(tenumi) from TCA0013))+1
			  insert into TCA0013 values(@tenumi ,@tefdoc,@tety4vend ,@teobs ,@newFecha ,@newHora ,@teuact )
		----INSERTO EL DETALLE
				INSERT INTO TCA00121 (tdtc12numi,tdtc13numi  ,tdnrodoc ,tdfechaPago ,tdmonto ,tdnrorecibo,tdty3banco ,
				tdnrocheque ,tdfact ,tdhact ,tduact)

			SELECT td.tdtc12numi ,@tenumi ,td.tdnrodoc ,@newFecha ,td.tdmonto ,td.tdnrorecibo ,td.tdty3banco,
			td.tdnrocheque, @newFecha  ,@newHora  ,@teuact 
			FROM @TC00121 AS td where td.estado =0

			---Inserto con Estado 1 "Vigente" en BDDiconCF.dbo.TPA001 que servirá para hacer el asiento contable-----
			set @codProv= (select a.tcty4prov  from TCA0012 as a,@TC00121 AS td where a.tcnumi=td.tdtc12numi)			
			set @nomcliprov = (select tc.cmdesc  from TC010 as tc where tc.cmnumi=@codProv)
			set @montot= (SELECT td.tdmonto FROM @TC00121 AS td)
				
			INSERT INTO BDDiconCF.dbo.TPA001 values (@tenumi,@tefdoc,3,@codProv,@nomcliprov,-1,1,@montot,1,6.96, 0, 0)			

			select @tdnumi as newNumi

		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END

		IF @tipo=2 --MODIFICAR REGISTRO
	BEGIN
		BEGIN TRY 

			  update TCA0013 set tefdoc =@tefdoc ,
			  tety4vend =@tety4vend ,teobs=@teobs,
			  tefact =@newFecha ,tehact =@newHora ,teuact =@teuact  
			  where tenumi =@tenumi 
		----INSERTO EL DETALLE
				INSERT INTO TCA00121 (tdtc12numi ,tdtc13numi ,tdnrodoc ,tdfechaPago ,tdmonto ,tdnrorecibo,tdty3banco ,
				tdnrocheque ,tdfact ,tdhact ,tduact)
			SELECT td.tdtc12numi,@tenumi  ,td.tdnrodoc ,@newFecha ,td.tdmonto ,td.tdnrorecibo ,td.tdty3banco,
			td.tdnrocheque, @newFecha  ,@newHora  ,@teuact 
			FROM @TC00121 AS td where td.estado =0

		    UPDATE TCA00121 
			SET tdmonto=td.tdmonto ,tdnrorecibo =td.tdnrorecibo ,
			tdty3banco =td.tdty3banco ,tdnrocheque =td.tdnrocheque  
			FROM TCA00121  INNER JOIN @TC00121 AS td
			ON TCA00121.tdnumi     = td.tdnumi   and td.estado=2;

				--ELIMINO LOS REGISTROS
			DELETE FROM TCA00121 WHERE tdnumi   in (SELECT td.tdnumi   FROM @TC00121 AS td WHERE td.estado=-1)


			-------Inserto con Estado 2 "Modificado" en BDDiconCF.dbo.TPA001 que servirá para hacer el asiento contable-----
			--set @codProv= (select a.tcty4prov  from TC0012 as a,@TC00121 AS td where a.tcnumi=td.tdtc12numi)			
			--set @nomcliprov = (select ty.yddesc  from TY004 as ty where ty.ydnumi=@codProv)			
			--set @montot= (SELECT td.tdmonto FROM @TC00121 AS td)
				
			--INSERT INTO BDDiconCF.dbo.TPA001 values (@tenumi,@tefdoc,3,@codProv,@nomcliprov,-1,2,@montot,1,6.96, 0, 0)
			

			select @tdnumi as newNumi
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END

		IF @tipo=3
	BEGIN
		BEGIN TRY 
		select a.tenumi,detalle.tdnrodoc,a.tefdoc ,a.tety4vend,vendedor.cbdesc as vendedor,
		a.teobs ,a.tefact ,a.tehact ,a.teuact  ,Sum(detalle .tdmonto) as total ,
	isnull((select top 1 transaccion .aanumiasiento   from BDDiconCF .dbo.TPA001 as transaccion where transaccion.aatipo =3 and transaccion .aanumipadre =a.tenumi and transaccion .aaestado in (1,2)),0) as asiento
		from TCA0013 as a
		left join TC002 as vendedor on vendedor.cbnumi =a.tety4vend 
		inner join TCA00121 as detalle on detalle .tdtc13numi =a.tenumi 
		group by a.tenumi,detalle.tdnrodoc,a.tefdoc ,a.tety4vend,vendedor.cbdesc,
		a.teobs ,a.tefact ,a.tehact ,a.teuact  
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END
		IF @tipo=4
	BEGIN
		BEGIN TRY 
	select  detalle .tdnumi as numidetalle,proveedor.cmdesc as proveedor,compra.caanumi NroDoc,
	a.tcnumi as numiCredito,cobranza .tenumi as numiCobranza
	,a.tctc1numi ,a.tcty4prov  ,detalle.tdfechaPago,
	(tctotcre -(select Isnull(Sum(detalle.tdmonto ),0) from TCA00121 as detalle where detalle .tdtc12numi =a.tcnumi ))
	 as pendiente,detalle .tdmonto  as PagoAc,detalle .tdnrorecibo  as NumeroRecibo,
    concat (banco .canombre,' ',IIF(banco.canumi=1,'',banco.canrocuenta ))   as DescBanco,detalle .tdty3banco as banco, detalle.tdnrocheque,Cast('' as image)as img ,1 as estado 
	from TCA0012 as a inner join TC010 as proveedor
	on proveedor.cmnumi =a.tcty4prov 
	inner join TCA001 as compra on compra.caanumi =a.tctc1numi 	
	inner join TCA00121 as detalle on detalle.tdtc12numi =a.tcnumi 
	inner join TCA0013 as cobranza on cobranza .tenumi =detalle .tdtc13numi 
	left join BDDiconCF.dbo.BA001   as banco on banco.canumi =detalle .tdty3banco  
	where cobranza.tenumi =@tenumi 
	order by a.tcnumi asc

		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END
	IF @tipo=5
	BEGIN
		BEGIN TRY 
	select a.tdnumi ,a.tdtc12numi ,a.tdnrodoc ,a.tdfechaPago ,a.tdmonto ,a.tdnrorecibo ,a.tdfact ,a.tdhact
	,a.tduact
	from TCA00121 as a
	where a.tdtc12numi =@credito
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END

	IF @tipo=6  ---------LISTAR DEUDAS PENDIENTES
	BEGIN
		BEGIN TRY 
	select a.tcnumi,compra.caanumi  NroDoc
	,a.tctc1numi ,a.tcty4prov, proveedor.cmdesc as proveedor,a.tcfdoc ,tctotcre as totalfactura,
	(tctotcre -(select Isnull(Sum(detalle.tdmonto ),0) from TCA00121 as detalle where detalle .tdtc12numi =a.tcnumi ))as pendiente
	,Cast(0 as decimal(18,2)) as PagoAc,'' as NumeroRecibo
	from TCA0012 as a inner join TC010 as proveedor
	on proveedor.cmnumi =a.tcty4prov 
	inner join TCA001 as compra on compra.caanumi =a.tctc1numi 	
	where (tctotcre -(select Isnull(Sum(detalle.tdmonto ),0) from TCA00121 as detalle where detalle .tdtc12numi =a.tcnumi ))>0
	order by a.tcnumi asc
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END

	
	IF @tipo=7  ---------LISTAR DEUDAS PENDIENTES
	BEGIN
		BEGIN TRY 
	select a.tdnumi ,a.tdtc12numi ,a.tdtc13numi ,a.tdnrodoc ,a.tdfechaPago ,a.tdmonto ,a.tdnrorecibo ,a.tdty3banco ,
	a.tdnrocheque ,a.tdfact ,a.tdhact ,a.tduact,Cast('' as image) as img,1 as estado
	from TCA00121 as a where a.tdnumi =@tdnumi 

		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END

		IF @tipo=8  ---------LISTAR DEUDAS PENDIENTES
	BEGIN
		BEGIN TRY 
	
SELECT cobranza.tenumi AS numiCobranza, a.tcty4prov  numiproveedor, proveedor.cmdesc AS proveedor, 
FORMAT (cobranza.tefdoc, 'dd-MM-yyyy') AS fechaPago, Concat(compra.caanumi , '-', Year(a.tcfdoc)) 
                  NroDoccompra, detalle.tdmonto AS importe, detalle.tdnrorecibo AS nroRecibo, banco.canombre AS banco, detalle.tdnrocheque AS NroCheque, IIF
                      ((SELECT Sum(auxdetallepago.tdmonto)
                        FROM      TCA00121 AS auxdetallepago
                        WHERE   auxdetallepago.tdtc12numi = a.tcnumi) = a.tctotcre, IIF
                      ((SELECT Max(ayuda.tdnumi)
                        FROM      TCA00121 ayuda
                        WHERE   ayuda.tdtc12numi = a.tcnumi) = detalle.tdnumi, 'CANCELACION TOTAL', 'CANCELACION PARCIAL'), 'CANCELACION PARCIAL') AS observacion, cobranza.teobs AS glosa
FROM     TCA0012 AS a INNER JOIN
                  TC010 AS proveedor ON proveedor.cmnumi = a.tcty4prov  INNER JOIN
                  TCA001 AS compra ON compra.caanumi  = a.tctc1numi INNER JOIN                  
                  TCA00121 AS detalle ON detalle.tdtc12numi = a.tcnumi INNER JOIN
                  TCA0013 AS cobranza ON cobranza.tenumi = detalle.tdtc13numi
				  left join BDDiconCF.dbo.BA001   as banco on banco.canumi =detalle .tdty3banco  AND cobranza.tenumi = @tenumi 				 
	order by a.tcnumi asc

		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
				   VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),1,@newFecha,@newHora,@teuact)
		END CATCH
	END
	IF @tipo=9 --Verificar si el Pago de las Compras a Crédito ya fue contabilizada
	BEGIN	
		BEGIN TRY	
			select *
			from BDDiconCF.dbo.TPA001 as a 
			where a.aanumipadre=@tenumi and aatipo=3 and aanumiasiento>0	
		END TRY
		BEGIN CATCH	
			INSERT INTO TB001(banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
			VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@teuact)
		END CATCH
	END
	
	IF @tipo=10 --MOSTRAR BANCOS
	BEGIN
		BEGIN TRY	
			select banco .canumi as yccod3,concat (banco .canombre,' ',IIF(banco.canumi=1,'',banco.canrocuenta )) as ycdes3 
			from BDDiconCF.dbo.BA001 as banco where banco.caestado=1
			order by canumi asc
		END TRY
		BEGIN CATCH
			INSERT INTO TB001(banum,baproc,balinea,bamensaje,batipo,bafact,bahact,bauact)
			VALUES(ERROR_NUMBER(),ERROR_PROCEDURE(),ERROR_LINE(),ERROR_MESSAGE(),3,@newFecha,@newHora,@teuact)
		END CATCH	
	END

End












