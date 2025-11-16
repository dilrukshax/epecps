-- Manual migration script to add Employee Goals tables
-- Run this if you have an existing database with ScoreTemplates but need to add Employee Goals

-- 1. Add TargetScore column to ScoreItems if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ScoreItems]') AND name = 'TargetScore')
BEGIN
    ALTER TABLE [ScoreItems] ADD [TargetScore] decimal(10,2) NOT NULL DEFAULT 100;
END
GO

-- 2. Create SuggestedActivities table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SuggestedActivities]') AND type in (N'U'))
BEGIN
    CREATE TABLE [SuggestedActivities] (
        [Id] uniqueidentifier NOT NULL,
        [ScoreItemId] uniqueidentifier NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_SuggestedActivities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SuggestedActivities_ScoreItems_ScoreItemId] FOREIGN KEY ([ScoreItemId]) 
            REFERENCES [ScoreItems] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_SuggestedActivities_ScoreItemId] ON [SuggestedActivities] ([ScoreItemId]);
    CREATE INDEX [IX_SuggestedActivities_ScoreItemId_DisplayOrder] ON [SuggestedActivities] ([ScoreItemId], [DisplayOrder]);
END
GO

-- 3. Create PersonalGoals table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PersonalGoals]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PersonalGoals] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [GoalItemId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [TargetScore] decimal(10,2) NOT NULL DEFAULT 100,
        [StartDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CurrentScore] decimal(10,2) NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_PersonalGoals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PersonalGoals_Users_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PersonalGoals_ScoreItems_GoalItemId] FOREIGN KEY ([GoalItemId]) 
            REFERENCES [ScoreItems] ([Id]) ON DELETE NO ACTION
    );
    
    CREATE INDEX [IX_PersonalGoals_UserId] ON [PersonalGoals] ([UserId]);
    CREATE INDEX [IX_PersonalGoals_GoalItemId] ON [PersonalGoals] ([GoalItemId]);
    CREATE INDEX [IX_PersonalGoals_Status] ON [PersonalGoals] ([Status]);
    CREATE INDEX [IX_PersonalGoals_UserId_Status] ON [PersonalGoals] ([UserId], [Status]);
    CREATE INDEX [IX_PersonalGoals_UserId_DueDate] ON [PersonalGoals] ([UserId], [DueDate]);
END
GO

-- 4. Create PersonalGoalActivities table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PersonalGoalActivities]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PersonalGoalActivities] (
        [Id] uniqueidentifier NOT NULL,
        [PersonalGoalId] uniqueidentifier NOT NULL,
        [SuggestedActivityId] uniqueidentifier NULL,
        [Description] nvarchar(1000) NOT NULL,
        [IsFromTemplate] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Status] int NOT NULL DEFAULT 0,
        [DueDate] datetime2 NULL,
        [EvidenceUrl] nvarchar(2000) NULL,
        [EvidenceNotes] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_PersonalGoalActivities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PersonalGoalActivities_PersonalGoals_PersonalGoalId] FOREIGN KEY ([PersonalGoalId]) 
            REFERENCES [PersonalGoals] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PersonalGoalActivities_SuggestedActivities_SuggestedActivityId] FOREIGN KEY ([SuggestedActivityId]) 
            REFERENCES [SuggestedActivities] ([Id]) ON DELETE SET NULL
    );
    
    CREATE INDEX [IX_PersonalGoalActivities_PersonalGoalId] ON [PersonalGoalActivities] ([PersonalGoalId]);
    CREATE INDEX [IX_PersonalGoalActivities_SuggestedActivityId] ON [PersonalGoalActivities] ([SuggestedActivityId]);
    CREATE INDEX [IX_PersonalGoalActivities_Status] ON [PersonalGoalActivities] ([Status]);
END
GO

PRINT 'Employee Goals tables created successfully!';
