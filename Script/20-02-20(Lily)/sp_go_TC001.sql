USE [BDDistBHF_CF]
GO
/****** Object:  StoredProcedure [dbo].[sp_go_TC001]    Script Date: 20/02/2020 18:12:18 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_go_TC001](@tipo int, @numi int=-1, @cod nvarchar(10)='', @desc nvarchar(50)='', @desc2 nvarchar(15)='',
								    @cat int=-1, @img nvarchar(30)='', @stc bit=0, @est bit=0,
									@serie bit=0, @pcom int=-1, @fing date=null, @cemp int=-1, @uact nvarchar(10)='', @filtro int=1,
									@cbarra nvarchar(15)='', @smin int=-1, @gr1 int=-1, @gr2 int=-1, @gr3 int=-1, @gr4 int=-1,	
									@umed int=-1, @uventa int=-1,@umax int=-1, @conv int=-1, @cecon int=-1, @cedesc nvarchar(50)='',
									@pack int=-1, @numipro int=-1, @TC0013 dbo.TC0013Type Readonly)
AS
BEGIN
	DECLARE @newHora nvarchar(5)
	set @newHora=CONCAT(DATEPART(HOUR,GETDATE()),':',DATEPART(MINUTE,GETDATE()))

	DECLARE @newFecha date
	set @newFecha=GETDATE()
	declare @PrecioCosto int
	declare @chnumi int
	IF @tipo=-1 --ELIMINAR REGISTRO
	BEGIN
		BEGIN TRY 
			DELETE FROM TC001 WHERE canumi=@numi
			----Elimino el Costo del producto eliminado
			DELETE FROM TC003 WHERE chcprod=@numi
			DELETE FROM TC0013 WHERE cbtccanumi=@numi

			SELECT @numi AS newNumi
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), -1, @newFecha, @newHora, @uact)
		END CATCH
	END

	IF @tipo=1 --NUEVO REGISTRO
	BEGIN
		BEGIN TRY
			BEGIN TRAN INSERTAR
				set @numi=IIF((select COUNT(canumi) from TC001)= 0, 0, (select MAX(canumi) from TC001))+1

				if @img<>''
				begin
					set @img = CONCAT(CONVERT(nvarchar(10), @numi), '_', @img, '.jpg')
				end

				INSERT INTO TC001 VALUES(@numi, cast(@cod as nvarchar(10)), @desc, @desc2, @cat, @img, @stc, @est, @serie, @pcom,
				@fing, @cemp, @newFecha, @newHora, @uact, @cbarra, @smin, @gr1, @gr2, @gr3, @gr4, @umed, @uventa,
				@umax, @conv, @pack )

				------ INSERTO EN LA TC003 CON PRECIO COSTO O -----
				set @PrecioCosto =(select Min(k.cinumi) from TC007 as k where k.citcv =0)	
				set @chnumi=IIF((select COUNT(chnumi) from TC003)= 0, 0, (select MAX(chnumi) from TC003))+1			
				Insert into TC003 values(@chnumi,@numi, @PrecioCosto, 0, @newFecha, @newHora, @uact)

				if @pack=1
				Begin
					INSERT INTO TC0013(cbtccanumi,cbtccanumi1,cbcant)
					SELECT @numi, tc.cbtccanumi1, tc.cbcant
					FROM @TC0013 AS tc
					WHERE tc.cbtccanumi1<>0 and tc.estado=0
				End			

				-- DEVUELVO VALORES DE CONFIRMACION
				SELECT @numi AS newNumi
			COMMIT TRAN INSERTAR
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 1, @newFecha, @newHora, @uact)

			ROLLBACK TRAN INSERTAR
		END CATCH
	END
	
	IF @tipo=2--MODIFICACION
	BEGIN
		BEGIN TRY
			BEGIN TRAN MODIFICACION
				if @img<>''
				begin
					set @img = CONCAT(CONVERT(nvarchar(10), @numi), '_', @img, '.jpg')
				end

				UPDATE TC001 SET cacod=@cod, cadesc=@desc, cadesc2=@desc2, cacat=@cat, caimg=@img, castc=@stc, caest=@est,
								 caserie=@serie, capcom=@pcom, cafing=@fing, cacemp=@cemp, cafact=@newFecha, cahact=@newHora,
								 cauact=@uact, cacbarra=@cbarra, casmin=@smin, cagr1=@gr1, cagr2=@gr2, cagr3=@gr3,
								 cagr4=@gr4, caumed=@umed, cauventa= @uventa, caumax=@umax, caconv=@conv, capack=@pack
				Where canumi = @numi

				if @pack=1
				Begin		

					--MODIFICO EL DETALLE
					DELETE FROM TC0013 WHERE TC0013.cbnumi in (SELECT a.cbnumi 
													 FROM TC0013 a left join @TC0013 td on a.cbnumi=td.cbnumi and a.cbtccanumi=@numi
													 WHERE td.cbnumi is null) and TC0013.cbtccanumi=@numi;

					INSERT INTO TC0013(cbtccanumi,cbtccanumi1,cbcant)
					SELECT @numi, tc.cbtccanumi1, tc.cbcant
					FROM @TC0013 AS tc
					WHERE tc.cbtccanumi1>0 and tc.estado=0			

					UPDATE TC0013 SET TC0013.cbtccanumi1=td.cbtccanumi1 , TC0013.cbcant=td.cbcant
					FROM TC0013 INNER JOIN @TC0013 AS td ON TC0013.cbnumi=td.cbnumi and td.estado=2;
				End	


				--DEVUELVO VALORES DE CONFIRMACION
				select @numi as newNumi
			COMMIT TRAN MODIFICACION
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 2, @newFecha, @newHora, @uact)
			ROLLBACK TRAN MODIFICACION
		END CATCH
	END

	IF @tipo=3 --MOSTRAR TODOS LOS PRODUCTOS
	BEGIN
		BEGIN TRY
			if(@filtro=1)
			begin
				SELECT a.canumi as numi, a.cacod as cod, a.cadesc as [desc], a.cadesc2 as [desc2], a.cacat as cat, b.canombre as ncat,
				a.caimg as nimg, cast('' as image) as img, a.castc as stc, a.caest as est, a.caserie as serie, a.capcom as pcom, 
				a.cafing as fing, a.cacemp as cemp, c.scneg as ncemp, a.cafact as fact, a.cahact as hact, a.cauact as uact, a.cacbarra,
				a.casmin, a.cagr1, a.cagr2, a.cagr3, a.cagr4, a.caumed, a.cauventa, a.caumax, a.caconv, a.capack
				FROM TC001 a inner join TC005C b on a.cacat=b.canumi and a.caserie=0
					 inner join TS003 c on a.cacemp=c.scnumi 
				ORDER BY a.canumi ASC
			end
			else if(@filtro=2)
			begin
				SELECT a.canumi as numi, a.cacod as cod, a.cadesc as [desc], a.cadesc2 as [desc2], a.cacat as cat, b.cedesc as ncat,
					   a.caimg as nimg, cast('' as image) as img, a.castc as stc, a.caest as est, a.caserie as serie, a.capcom as pcom, 
					   a.cafing as fing, a.cacemp as cemp, c.scneg as ncemp, a.cafact as fact, a.cahact as hact, a.cauact as uact,
					   a.cacbarra, a.casmin, a.cagr1, a.cagr2, a.cagr3, a.cagr4, a.caumed, a.cauventa, a.caumax, a.caconv
				FROM TC001 a inner join TC0051 b on a.cacat=b.cenum and b.cecon=5 and a.caserie=1
					 inner join TS003 c on a.cacemp=c.scnumi 
				ORDER BY a.canumi ASC	
			end
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 3, @newFecha, @newHora, @uact)
		END CATCH
	END
	IF @tipo=4 --MOSTRAR LIBRERIA TC0051
	BEGIN
		BEGIN TRY		
			SELECT cenum ,cedesc
			from TC0051
			where cecon= @cecon  
			order by cenum  asc
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 3, @newFecha, @newHora, @uact)
		END CATCH
	END
	IF @tipo=5 --INSERTAR LIBRERIAS
	BEGIN
		BEGIN TRY
		DECLARE @numilib int
		set @numilib=IIF((select COUNT(cenum) from TC0051 where cecon=@cecon)=0,0,(select MAX(cenum) from TC0051 
		where cecon=@cecon))+1
		insert into TC0051 values (@cecon,@numilib ,@cedesc  ,@newFecha ,@newHora,@uact )
		--insert into SI001 values('TY0031',@numilib  ,1,@newFecha ,@newHora ,@yfuact )
		select @numi as newNumi
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 3, @newFecha, @newHora, @uact)
		END CATCH

	END
	IF @tipo=6 --MOSTRAR PRODUCTOS PACK
	BEGIN
		BEGIN TRY
			SELECT a.cbnumi, a.cbtccanumi, a.cbtccanumi1, b.cadesc, a.cbcant, 1 as estado
			FROM TC0013 a
			inner join TC001 b on a.cbtccanumi1=b.canumi
			where a.cbtccanumi=@numi
		END TRY
		BEGIN CATCH
			INSERT INTO TB001 (banum, baproc, balinea, bamensaje, batipo, bafact, bahact, bauact)
				   VALUES(ERROR_NUMBER(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), 3, @newFecha, @newHora, @uact)
		END CATCH

	END
END

