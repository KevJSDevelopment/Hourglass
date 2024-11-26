USE [AppLimiter]
GO

/****** Object:  Table [dbo].[MotivationalMessage]    Script Date: 11/25/2024 10:51:23 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MotivationalMessage](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeId] [int] NOT NULL,
	[TypeDescription] [nvarchar](20) NOT NULL,
	[ComputerId] [varchar](50) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[FilePath] [nvarchar](max) NULL,
	[FileName] [nvarchar](max) NULL,
 CONSTRAINT [PK_MotivationalMessage] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[MotivationalMessage]  WITH CHECK ADD  CONSTRAINT [FK_MotivationalMessage_UserComputers] FOREIGN KEY([ComputerId])
REFERENCES [dbo].[UserComputers] ([ComputerId])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[MotivationalMessage] CHECK CONSTRAINT [FK_MotivationalMessage_UserComputers]
GO


