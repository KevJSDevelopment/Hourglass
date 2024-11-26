USE [AppLimiter]
GO

/****** Object:  Table [dbo].[GoalStep]    Script Date: 11/25/2024 10:50:19 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GoalStep](
	[StepId] [int] NULL,
	[GoalMessageId] [int] NULL,
	[StepText] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[GoalStep]  WITH CHECK ADD  CONSTRAINT [FK_GoalStep_MotivationalMessage] FOREIGN KEY([GoalMessageId])
REFERENCES [dbo].[MotivationalMessage] ([Id])
GO

ALTER TABLE [dbo].[GoalStep] CHECK CONSTRAINT [FK_GoalStep_MotivationalMessage]
GO


