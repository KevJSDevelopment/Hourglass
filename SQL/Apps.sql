USE [AppLimiter]
GO

/****** Object:  Table [dbo].[Apps]    Script Date: 11/25/2024 10:51:19 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Apps](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ComputerId] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Executable] [nvarchar](max) NULL,
	[Ignore] [bit] NULL,
	[WarningTime] [nvarchar](50) NULL,
	[KillTime] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Apps] ADD  DEFAULT ((0)) FOR [Ignore]
GO


