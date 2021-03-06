USE [commuter]
GO
ALTER TABLE [dbo].[devices] DROP CONSTRAINT [DF_devices_UpdateTime]
GO
ALTER TABLE [dbo].[devices] DROP CONSTRAINT [DF_devices_Enabled]
GO
ALTER TABLE [dbo].[commutes] DROP CONSTRAINT [DF_commutes_Updatetime]
GO
/****** Object:  StoredProcedure [dbo].[AddCommute]    Script Date: 08/10/2010 17:21:46 ******/
DROP PROCEDURE [dbo].[AddCommute]
GO
/****** Object:  StoredProcedure [dbo].[AddDevice]    Script Date: 08/10/2010 17:21:46 ******/
DROP PROCEDURE [dbo].[AddDevice]
GO
/****** Object:  StoredProcedure [dbo].[AddRoute]    Script Date: 08/10/2010 17:21:46 ******/
DROP PROCEDURE [dbo].[AddRoute]
GO
/****** Object:  Table [dbo].[commutes]    Script Date: 08/10/2010 17:21:45 ******/
DROP TABLE [dbo].[commutes]
GO
/****** Object:  Table [dbo].[devices]    Script Date: 08/10/2010 17:21:45 ******/
DROP TABLE [dbo].[devices]
GO
/****** Object:  Table [dbo].[routes]    Script Date: 08/10/2010 17:21:45 ******/
DROP TABLE [dbo].[routes]
GO
/****** Object:  Table [dbo].[routes]    Script Date: 08/10/2010 17:21:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[routes](
	[pkRoute] [uniqueidentifier] NOT NULL,
	[idCommute] [uniqueidentifier] NOT NULL,
	[Definition] [xml] NOT NULL,
	[UpdateTime] [date] NOT NULL,
 CONSTRAINT [PK_routes] PRIMARY KEY CLUSTERED 
(
	[pkRoute] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[devices]    Script Date: 08/10/2010 17:21:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[devices](
	[pkDevice] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[UpdateTime] [datetime] NOT NULL,
	[Enabled] [bit] NOT NULL,
	[ChannelURI] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_devices] PRIMARY KEY CLUSTERED 
(
	[pkDevice] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[commutes]    Script Date: 08/10/2010 17:21:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[commutes](
	[pkCommute] [uniqueidentifier] NOT NULL,
	[idDevice] [uniqueidentifier] NOT NULL,
	[DepartTime] [datetime] NOT NULL,
	[ReturnTime] [datetime] NOT NULL,
	[Definition] [xml] NOT NULL,
	[UpdateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_commutes] PRIMARY KEY CLUSTERED 
(
	[pkCommute] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[AddRoute]    Script Date: 08/10/2010 17:21:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AddRoute]
	-- Add the parameters for the stored procedure here
	@idRoute	uniqueidentifier,
	@idCommute  uniqueidentifier,
	@DepartTime datetime,
	@ReturnTime datetime,
	@Definition XML
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF EXISTS(SELECT pkRoute FROM routes WHERE @idRoute = pkRoute AND @idCommute = idCommute)
	BEGIN
		UPDATE routes
			SET
				Definition = @Definition,
				UpdateTime = getdate()
		WHERE @idRoute = pkRoute AND @idCommute = idCommute
	END
	ELSE
	BEGIN
		INSERT INTO routes(pkRoute, idCommute, Definition, UpdateTime)
		VALUES (@idRoute, @idCommute, @Definition, getDate())

	END
END
GO
/****** Object:  StoredProcedure [dbo].[AddDevice]    Script Date: 08/10/2010 17:21:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AddDevice]
	-- Add the parameters for the stored procedure here
	@idDevice	uniqueidentifier,
	@Name		nvarchar(150),
	@ChannelURI	nvarchar(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF EXISTS(SELECT pkDevice FROM devices WHERE @idDevice = pkDevice)
	BEGIN
		UPDATE devices
			SET
				Name = @Name,
				ChannelURI = @ChannelURI,
				UpdateTime = getdate(),
				Enabled = 1
		WHERE pkDevice = @idDevice
	END
	ELSE
	BEGIN
		INSERT INTO devices(pkDevice, Name, UpdateTime, Enabled, ChannelURI)
		VALUES (@idDevice, @Name, getdate(), 1, @ChannelURI)

	END
END
GO
/****** Object:  StoredProcedure [dbo].[AddCommute]    Script Date: 08/10/2010 17:21:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AddCommute]
	-- Add the parameters for the stored procedure here
	@idCommute  uniqueidentifier,
	@idDevice	uniqueidentifier,
	@DepartTime datetime,
	@ReturnTime datetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF EXISTS(SELECT pkCommute FROM commutes WHERE @idDevice = idDevice AND @idCommute = pkCommute)
	BEGIN
		UPDATE commutes
			SET
				DepartTime = @DepartTime,
				ReturnTime = @ReturnTime,
				UpdateTime = getdate()
		WHERE idDevice = @idDevice AND pkCommute = @idCommute
	END
	ELSE
	BEGIN
		INSERT INTO commutes(pkCommute, idDevice, DepartTime, ReturnTime, UpdateTime)
		VALUES (@idCommute, @idDevice, @DepartTime, @ReturnTime, getDate())

	END
END
GO
/****** Object:  Default [DF_devices_UpdateTime]    Script Date: 08/10/2010 17:21:45 ******/
ALTER TABLE [dbo].[devices] ADD  CONSTRAINT [DF_devices_UpdateTime]  DEFAULT (getdate()) FOR [UpdateTime]
GO
/****** Object:  Default [DF_devices_Enabled]    Script Date: 08/10/2010 17:21:45 ******/
ALTER TABLE [dbo].[devices] ADD  CONSTRAINT [DF_devices_Enabled]  DEFAULT ((1)) FOR [Enabled]
GO
/****** Object:  Default [DF_commutes_Updatetime]    Script Date: 08/10/2010 17:21:45 ******/
ALTER TABLE [dbo].[commutes] ADD  CONSTRAINT [DF_commutes_Updatetime]  DEFAULT (getdate()) FOR [UpdateTime]
GO
