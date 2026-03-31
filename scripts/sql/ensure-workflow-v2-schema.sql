IF OBJECT_ID(N'[GoalAssignments]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('GoalAssignments', 'ActivationMethod') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments] ADD [ActivationMethod] nvarchar(4000) NULL;
    END

    IF COL_LENGTH('GoalAssignments', 'ActivationReviewedAt') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments] ADD [ActivationReviewedAt] datetime2 NULL;
    END

    IF COL_LENGTH('GoalAssignments', 'ActivationReviewedByUserId') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments] ADD [ActivationReviewedByUserId] int NULL;
    END

    IF COL_LENGTH('GoalAssignments', 'ActivationStatus') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments]
            ADD [ActivationStatus] nvarchar(32) NOT NULL
                CONSTRAINT [DF_GoalAssignments_ActivationStatus] DEFAULT(N'PendingEmployee');
    END

    IF COL_LENGTH('GoalAssignments', 'ActivationSubmittedAt') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments] ADD [ActivationSubmittedAt] datetime2 NULL;
    END

    IF COL_LENGTH('GoalAssignments', 'ActivationTlComment') IS NULL
    BEGIN
        ALTER TABLE [GoalAssignments] ADD [ActivationTlComment] nvarchar(2000) NULL;
    END
END

IF OBJECT_ID(N'[Evaluations]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Evaluations', 'WorkflowVersion') IS NULL
    BEGIN
        ALTER TABLE [Evaluations]
            ADD [WorkflowVersion] nvarchar(16) NOT NULL
                CONSTRAINT [DF_Evaluations_WorkflowVersion] DEFAULT(N'v1');
    END
END

IF OBJECT_ID(N'[DepartmentHodMappings]', N'U') IS NULL
BEGIN
    CREATE TABLE [DepartmentHodMappings] (
        [DeptId] int NOT NULL,
        [HodUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_DepartmentHodMappings] PRIMARY KEY ([DeptId], [HodUserId]),
        CONSTRAINT [FK_DepartmentHodMappings_Departments_DeptId]
            FOREIGN KEY ([DeptId]) REFERENCES [Departments] ([DeptId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DepartmentHodMappings_Users_HodUserId]
            FOREIGN KEY ([HodUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END

IF OBJECT_ID(N'[UserManagerMappings]', N'U') IS NULL
BEGIN
    CREATE TABLE [UserManagerMappings] (
        [EmployeeUserId] int NOT NULL,
        [ManagerUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserManagerMappings] PRIMARY KEY ([EmployeeUserId], [ManagerUserId]),
        CONSTRAINT [FK_UserManagerMappings_Users_EmployeeUserId]
            FOREIGN KEY ([EmployeeUserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserManagerMappings_Users_ManagerUserId]
            FOREIGN KEY ([ManagerUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END

IF OBJECT_ID(N'[WorkflowReviewWeights]', N'U') IS NULL
BEGIN
    CREATE TABLE [WorkflowReviewWeights] (
        [WorkflowReviewWeightId] int IDENTITY(1,1) NOT NULL,
        [ReviewerKey] nvarchar(32) NOT NULL,
        [WeightPercent] decimal(6,2) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_WorkflowReviewWeights_IsActive] DEFAULT(1),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_WorkflowReviewWeights] PRIMARY KEY ([WorkflowReviewWeightId])
    );
END

IF OBJECT_ID(N'[PipCases]', N'U') IS NULL
BEGIN
    CREATE TABLE [PipCases] (
        [PipCaseId] int IDENTITY(1,1) NOT NULL,
        [EvaluationId] int NOT NULL,
        [EmployeeUserId] int NOT NULL,
        [AssignedHrUserId] int NOT NULL,
        [Status] nvarchar(32) NOT NULL CONSTRAINT [DF_PipCases_Status] DEFAULT(N'Open'),
        [Reason] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DueDate] datetime2 NULL,
        [ClosedAt] datetime2 NULL,
        CONSTRAINT [PK_PipCases] PRIMARY KEY ([PipCaseId]),
        CONSTRAINT [FK_PipCases_Evaluations_EvaluationId]
            FOREIGN KEY ([EvaluationId]) REFERENCES [Evaluations] ([EvaluationId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PipCases_Users_AssignedHrUserId]
            FOREIGN KEY ([AssignedHrUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PipCases_Users_EmployeeUserId]
            FOREIGN KEY ([EmployeeUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );
END

IF OBJECT_ID(N'[PipActionItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [PipActionItems] (
        [PipActionItemId] int IDENTITY(1,1) NOT NULL,
        [PipCaseId] int NOT NULL,
        [Title] nvarchar(400) NOT NULL,
        [Description] nvarchar(3000) NULL,
        [TrainingMaterialId] int NULL,
        [ExternalTrainingLink] nvarchar(2000) NULL,
        [DueDate] datetime2 NULL,
        [Status] nvarchar(32) NOT NULL CONSTRAINT [DF_PipActionItems_Status] DEFAULT(N'Pending'),
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_PipActionItems] PRIMARY KEY ([PipActionItemId]),
        CONSTRAINT [FK_PipActionItems_PipCases_PipCaseId]
            FOREIGN KEY ([PipCaseId]) REFERENCES [PipCases] ([PipCaseId]) ON DELETE CASCADE,
        CONSTRAINT [FK_PipActionItems_TrainingMaterials_TrainingMaterialId]
            FOREIGN KEY ([TrainingMaterialId]) REFERENCES [TrainingMaterials] ([TrainingMaterialId]) ON DELETE SET NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_Evaluations_WorkflowVersion' AND object_id = OBJECT_ID(N'[Evaluations]'))
BEGIN
    CREATE INDEX [IX_Evaluations_WorkflowVersion] ON [Evaluations] ([WorkflowVersion]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_GoalAssignments_ActivationReviewedByUserId' AND object_id = OBJECT_ID(N'[GoalAssignments]'))
BEGIN
    CREATE INDEX [IX_GoalAssignments_ActivationReviewedByUserId] ON [GoalAssignments] ([ActivationReviewedByUserId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_GoalAssignments_GoalSetId_ActivationStatus' AND object_id = OBJECT_ID(N'[GoalAssignments]'))
BEGIN
    CREATE INDEX [IX_GoalAssignments_GoalSetId_ActivationStatus] ON [GoalAssignments] ([GoalSetId], [ActivationStatus]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_DepartmentHodMappings_HodUserId' AND object_id = OBJECT_ID(N'[DepartmentHodMappings]'))
BEGIN
    CREATE INDEX [IX_DepartmentHodMappings_HodUserId] ON [DepartmentHodMappings] ([HodUserId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_UserManagerMappings_ManagerUserId' AND object_id = OBJECT_ID(N'[UserManagerMappings]'))
BEGIN
    CREATE INDEX [IX_UserManagerMappings_ManagerUserId] ON [UserManagerMappings] ([ManagerUserId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_WorkflowReviewWeights_ReviewerKey' AND object_id = OBJECT_ID(N'[WorkflowReviewWeights]'))
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowReviewWeights_ReviewerKey] ON [WorkflowReviewWeights] ([ReviewerKey]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipCases_AssignedHrUserId' AND object_id = OBJECT_ID(N'[PipCases]'))
BEGIN
    CREATE INDEX [IX_PipCases_AssignedHrUserId] ON [PipCases] ([AssignedHrUserId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipCases_EmployeeUserId' AND object_id = OBJECT_ID(N'[PipCases]'))
BEGIN
    CREATE INDEX [IX_PipCases_EmployeeUserId] ON [PipCases] ([EmployeeUserId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipCases_EvaluationId' AND object_id = OBJECT_ID(N'[PipCases]'))
BEGIN
    CREATE INDEX [IX_PipCases_EvaluationId] ON [PipCases] ([EvaluationId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipCases_Status' AND object_id = OBJECT_ID(N'[PipCases]'))
BEGIN
    CREATE INDEX [IX_PipCases_Status] ON [PipCases] ([Status]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipActionItems_DueDate' AND object_id = OBJECT_ID(N'[PipActionItems]'))
BEGIN
    CREATE INDEX [IX_PipActionItems_DueDate] ON [PipActionItems] ([DueDate]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipActionItems_PipCaseId' AND object_id = OBJECT_ID(N'[PipActionItems]'))
BEGIN
    CREATE INDEX [IX_PipActionItems_PipCaseId] ON [PipActionItems] ([PipCaseId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipActionItems_Status' AND object_id = OBJECT_ID(N'[PipActionItems]'))
BEGIN
    CREATE INDEX [IX_PipActionItems_Status] ON [PipActionItems] ([Status]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_PipActionItems_TrainingMaterialId' AND object_id = OBJECT_ID(N'[PipActionItems]'))
BEGIN
    CREATE INDEX [IX_PipActionItems_TrainingMaterialId] ON [PipActionItems] ([TrainingMaterialId]);
END

IF OBJECT_ID(N'[GoalAssignments]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[Users]', N'U') IS NOT NULL
   AND COL_LENGTH('GoalAssignments', 'ActivationReviewedByUserId') IS NOT NULL
   AND OBJECT_ID(N'[FK_GoalAssignments_Users_ActivationReviewedByUserId]', N'F') IS NULL
BEGIN
    ALTER TABLE [GoalAssignments]
        ADD CONSTRAINT [FK_GoalAssignments_Users_ActivationReviewedByUserId]
        FOREIGN KEY ([ActivationReviewedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL;
END
