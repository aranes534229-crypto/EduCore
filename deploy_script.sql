IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Announcements] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [AuthorName] nvarchar(max) NOT NULL,
    [PostedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [DisplayName] nvarchar(max) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Expenses] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Date] datetime2 NOT NULL,
    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Faculty] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Contact] nvarchar(max) NOT NULL,
    [ApplicationUserId] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Faculty] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Fees] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Fees] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [GradeLevels] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_GradeLevels] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] nvarchar(max) NOT NULL,
    [RecipientId] nvarchar(max) NOT NULL,
    [StudentId] int NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [Seen] bit NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SchoolYears] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SchoolYears] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Subjects] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Sections] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [GradeLevelId] int NOT NULL,
    [SchoolYearId] int NOT NULL,
    [AdviserId] int NULL,
    CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sections_Faculty_AdviserId] FOREIGN KEY ([AdviserId]) REFERENCES [Faculty] ([Id]),
    CONSTRAINT [FK_Sections_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Sections_SchoolYears_SchoolYearId] FOREIGN KEY ([SchoolYearId]) REFERENCES [SchoolYears] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [SectionSubjects] (
    [Id] int NOT NULL IDENTITY,
    [SectionId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [FacultyId] int NOT NULL,
    CONSTRAINT [PK_SectionSubjects] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SectionSubjects_Faculty_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculty] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SectionSubjects_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SectionSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [BirthDate] datetime2 NULL,
    [Address] nvarchar(max) NOT NULL,
    [GuardianName] nvarchar(max) NOT NULL,
    [GuardianContact] nvarchar(max) NOT NULL,
    [GuardianEmail] nvarchar(max) NOT NULL,
    [ApplicationUserId] nvarchar(max) NULL,
    [SectionId] int NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [Attendance] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SectionId] int NOT NULL,
    [Date] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Attendance] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendance_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attendance_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Documents] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [Path] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Documents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Enrollments] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SchoolYearId] int NOT NULL,
    [GradeLevelId] int NOT NULL,
    [Status] int NOT NULL,
    [ApplicationDate] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Enrollments_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Enrollments_SchoolYears_SchoolYearId] FOREIGN KEY ([SchoolYearId]) REFERENCES [SchoolYears] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Enrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Grades] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SectionSubjectId] int NOT NULL,
    [Score] int NOT NULL,
    [Remarks] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Grades] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Grades_SectionSubjects_SectionSubjectId] FOREIGN KEY ([SectionSubjectId]) REFERENCES [SectionSubjects] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Grades_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Invoices] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SchoolYearId] int NOT NULL,
    [IssuedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_SchoolYears_SchoolYearId] FOREIGN KEY ([SchoolYearId]) REFERENCES [SchoolYears] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Invoices_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Inquiries] (
    [Id] int NOT NULL IDENTITY,
    [StudentName] nvarchar(max) NOT NULL,
    [GradeLevelId] int NOT NULL,
    [ContactEmail] nvarchar(max) NOT NULL,
    [ContactPhone] nvarchar(max) NOT NULL,
    [Question] nvarchar(max) NULL,
    [InternalNotes] nvarchar(max) NULL,
    [Source] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [AssignedToId] nvarchar(450) NULL,
    [CreatedByUserId] nvarchar(450) NULL,
    [Status] int NOT NULL,
    [DateCreated] datetime2 NOT NULL,
    [FirstResponseAt] datetime2 NULL,
    [ConvertedStudentId] int NULL,
    [ConvertedEnrollmentId] int NULL,
    CONSTRAINT [PK_Inquiries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Inquiries_AspNetUsers_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Inquiries_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inquiries_Enrollments_ConvertedEnrollmentId] FOREIGN KEY ([ConvertedEnrollmentId]) REFERENCES [Enrollments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inquiries_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inquiries_Students_ConvertedStudentId] FOREIGN KEY ([ConvertedStudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [InvoiceLines] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [InvoiceId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Date] datetime2 NOT NULL,
    [Method] nvarchar(max) NOT NULL,
    [Reference] nvarchar(max) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InquiryNotes] (
    [Id] int NOT NULL IDENTITY,
    [InquiryId] int NOT NULL,
    [StaffId] nvarchar(450) NULL,
    [Visibility] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_InquiryNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InquiryNotes_AspNetUsers_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InquiryNotes_Inquiries_InquiryId] FOREIGN KEY ([InquiryId]) REFERENCES [Inquiries] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[GradeLevels]'))
    SET IDENTITY_INSERT [GradeLevels] ON;
INSERT INTO [GradeLevels] ([Id], [Name], [SortOrder])
VALUES (1, N'Grade 1', 1),
(2, N'Grade 2', 2),
(3, N'Grade 3', 3),
(4, N'Grade 4', 4),
(5, N'Grade 5', 5),
(6, N'Grade 6', 6),
(7, N'Grade 7', 7),
(8, N'Grade 8', 8),
(9, N'Grade 9', 9),
(10, N'Grade 10', 10),
(11, N'Grade 11', 11),
(12, N'Grade 12', 12);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[GradeLevels]'))
    SET IDENTITY_INSERT [GradeLevels] OFF;
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_Attendance_SectionId] ON [Attendance] ([SectionId]);
GO

CREATE INDEX [IX_Attendance_StudentId] ON [Attendance] ([StudentId]);
GO

CREATE INDEX [IX_Documents_StudentId] ON [Documents] ([StudentId]);
GO

CREATE INDEX [IX_Enrollments_GradeLevelId] ON [Enrollments] ([GradeLevelId]);
GO

CREATE INDEX [IX_Enrollments_SchoolYearId] ON [Enrollments] ([SchoolYearId]);
GO

CREATE UNIQUE INDEX [IX_Enrollments_StudentId_SchoolYearId] ON [Enrollments] ([StudentId], [SchoolYearId]);
GO

CREATE INDEX [IX_Grades_SectionSubjectId] ON [Grades] ([SectionSubjectId]);
GO

CREATE INDEX [IX_Grades_StudentId] ON [Grades] ([StudentId]);
GO

CREATE INDEX [IX_Inquiries_AssignedToId] ON [Inquiries] ([AssignedToId]);
GO

CREATE INDEX [IX_Inquiries_ConvertedEnrollmentId] ON [Inquiries] ([ConvertedEnrollmentId]);
GO

CREATE INDEX [IX_Inquiries_ConvertedStudentId] ON [Inquiries] ([ConvertedStudentId]);
GO

CREATE INDEX [IX_Inquiries_CreatedByUserId] ON [Inquiries] ([CreatedByUserId]);
GO

CREATE INDEX [IX_Inquiries_GradeLevelId] ON [Inquiries] ([GradeLevelId]);
GO

CREATE INDEX [IX_InquiryNotes_InquiryId] ON [InquiryNotes] ([InquiryId]);
GO

CREATE INDEX [IX_InquiryNotes_StaffId] ON [InquiryNotes] ([StaffId]);
GO

CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [InvoiceLines] ([InvoiceId]);
GO

CREATE INDEX [IX_Invoices_SchoolYearId] ON [Invoices] ([SchoolYearId]);
GO

CREATE INDEX [IX_Invoices_StudentId] ON [Invoices] ([StudentId]);
GO

CREATE INDEX [IX_Payments_InvoiceId] ON [Payments] ([InvoiceId]);
GO

CREATE INDEX [IX_Sections_AdviserId] ON [Sections] ([AdviserId]);
GO

CREATE INDEX [IX_Sections_GradeLevelId] ON [Sections] ([GradeLevelId]);
GO

CREATE INDEX [IX_Sections_SchoolYearId] ON [Sections] ([SchoolYearId]);
GO

CREATE INDEX [IX_SectionSubjects_FacultyId] ON [SectionSubjects] ([FacultyId]);
GO

CREATE UNIQUE INDEX [IX_SectionSubjects_SectionId_SubjectId] ON [SectionSubjects] ([SectionId], [SubjectId]);
GO

CREATE INDEX [IX_SectionSubjects_SubjectId] ON [SectionSubjects] ([SubjectId]);
GO

CREATE INDEX [IX_Students_SectionId] ON [Students] ([SectionId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917074156_Init', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Documents] DROP CONSTRAINT [FK_Documents_Students_StudentId];
GO

ALTER TABLE [Students] ADD [StudentNumber] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Inquiries] ADD [Address] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Inquiries] ADD [BirthDate] datetime2 NULL;
GO

ALTER TABLE [Inquiries] ADD [GuardianName] nvarchar(max) NOT NULL DEFAULT N'';
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'StudentId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Documents] ALTER COLUMN [StudentId] int NULL;
GO

ALTER TABLE [Documents] ADD [InquiryId] int NULL;
GO

CREATE INDEX [IX_Documents_InquiryId] ON [Documents] ([InquiryId]);
GO

ALTER TABLE [Documents] ADD CONSTRAINT [FK_Documents_Inquiries_InquiryId] FOREIGN KEY ([InquiryId]) REFERENCES [Inquiries] ([Id]);
GO

ALTER TABLE [Documents] ADD CONSTRAINT [FK_Documents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260917090314_Module9_Admissions', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'Question');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Inquiries] DROP COLUMN [Question];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918015213_RemoveInquiryQuestion', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

UPDATE Inquiries SET Status = 1 WHERE Status = 4;
GO

UPDATE Inquiries SET Status = 2 WHERE Status = 3;
GO

UPDATE Inquiries SET Status = 0 WHERE Status IN (1, 2);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918020827_SimplifyInquiryStatus', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [SectionSubjects] DROP CONSTRAINT [FK_SectionSubjects_Faculty_FacultyId];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SectionSubjects]') AND [c].[name] = N'FacultyId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SectionSubjects] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [SectionSubjects] ALTER COLUMN [FacultyId] int NULL;
GO

ALTER TABLE [SectionSubjects] ADD CONSTRAINT [FK_SectionSubjects_Faculty_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculty] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260918125209_SectionSubjectTeacherOptional', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Sections] ADD [SubmittedToFinanceAt] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260919031438_RegistrarSubmitToFinance', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [GradeLevels] ADD [Amount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

UPDATE [GradeLevels] SET [Amount] = 11500.0
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 13000.0
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 14500.0
WHERE [Id] = 3;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 16000.0
WHERE [Id] = 4;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 17500.0
WHERE [Id] = 5;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 19000.0
WHERE [Id] = 6;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 20500.0
WHERE [Id] = 7;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 22000.0
WHERE [Id] = 8;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 23500.0
WHERE [Id] = 9;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 25000.0
WHERE [Id] = 10;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 26500.0
WHERE [Id] = 11;
SELECT @@ROWCOUNT;

GO

UPDATE [GradeLevels] SET [Amount] = 28000.0
WHERE [Id] = 12;
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260919145516_AddGradeLevelTuitionAmount', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Invoices] ADD [Number] nvarchar(16) NULL;
GO

UPDATE [Invoices]
SET [Number] = CONCAT('INV-', FORMAT([Id], 'D4'))
WHERE [Number] IS NULL
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260921013155_AddInvoiceNumber', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Inquiries] ADD [InquiryNumber] nvarchar(32) NOT NULL DEFAULT N'';
GO

WITH Ordered AS (
    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
    FROM [Inquiries]
)
UPDATE i
SET [InquiryNumber] = CONCAT('INQ-', 1000 + RowNum)
FROM [Inquiries] i
INNER JOIN Ordered o ON o.[Id] = i.[Id]
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260922035051_AddInquiryNumber', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

UPDATE [Students]
SET [StudentNumber] = (1000 + CAST(RIGHT([StudentNumber], 4) AS INT))
WHERE [StudentNumber] LIKE '____-____'
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260922041032_ConvertStudentNumberToSequential', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Fees] ADD [GradeLevelId] int NULL;
GO

CREATE INDEX [IX_Fees_GradeLevelId] ON [Fees] ([GradeLevelId]);
GO

ALTER TABLE [Fees] ADD CONSTRAINT [FK_Fees_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260922044822_AddFeeGradeLevelLink', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fees]') AND [c].[name] = N'Amount');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Fees] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Fees] DROP COLUMN [Amount];
GO

CREATE TABLE [FeeLines] (
    [Id] int NOT NULL IDENTITY,
    [FeeId] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FeeLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FeeLines_Fees_FeeId] FOREIGN KEY ([FeeId]) REFERENCES [Fees] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FeeLines_FeeId] ON [FeeLines] ([FeeId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925120334_AddFeeLines', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Students]') AND [c].[name] = N'Address');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Students] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Students] DROP COLUMN [Address];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Students]') AND [c].[name] = N'ApplicationUserId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Students] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Students] DROP COLUMN [ApplicationUserId];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Students]') AND [c].[name] = N'FirstName');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Students] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Students] DROP COLUMN [FirstName];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Students]') AND [c].[name] = N'LastName');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Students] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Students] DROP COLUMN [LastName];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Faculty]') AND [c].[name] = N'ApplicationUserId');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Faculty] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Faculty] DROP COLUMN [ApplicationUserId];
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Faculty]') AND [c].[name] = N'Contact');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Faculty] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Faculty] DROP COLUMN [Contact];
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Faculty]') AND [c].[name] = N'Email');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Faculty] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Faculty] DROP COLUMN [Email];
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Faculty]') AND [c].[name] = N'FirstName');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Faculty] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Faculty] DROP COLUMN [FirstName];
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'DisplayName');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [AspNetUsers] DROP COLUMN [DisplayName];
GO

EXEC sp_rename N'[Faculty].[LastName]', N'EmployeeNumber', N'COLUMN';
GO

ALTER TABLE [Students] ADD [PersonId] int NULL;
GO

ALTER TABLE [Faculty] ADD [HireDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Faculty] ADD [PersonId] int NULL;
GO

ALTER TABLE [AspNetUsers] ADD [PersonId] int NULL;
GO

CREATE TABLE [Persons] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Persons] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_Students_PersonId] ON [Students] ([PersonId]) WHERE [PersonId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Faculty_PersonId] ON [Faculty] ([PersonId]) WHERE [PersonId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_AspNetUsers_PersonId] ON [AspNetUsers] ([PersonId]) WHERE [PersonId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Persons_Email] ON [Persons] ([Email]) WHERE [Email] IS NOT NULL AND [Email] <> '';
GO

ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Faculty] ADD CONSTRAINT [FK_Faculty_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Students] ADD CONSTRAINT [FK_Students_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260926140216_AddPersonTable', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'StudentId');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Messages] ALTER COLUMN [StudentId] int NULL;
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserTokens]') AND [c].[name] = N'Name');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [AspNetUserTokens] ALTER COLUMN [Name] nvarchar(450) NOT NULL;
GO

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserTokens]') AND [c].[name] = N'LoginProvider');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserTokens] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [AspNetUserTokens] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL;
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserLogins]') AND [c].[name] = N'ProviderKey');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [AspNetUserLogins] ALTER COLUMN [ProviderKey] nvarchar(450) NOT NULL;
GO

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUserLogins]') AND [c].[name] = N'LoginProvider');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [AspNetUserLogins] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261001045650_MessagesStudentIdNullable', N'8.0.17');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Students] ADD [FirstName] nvarchar(100) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Students] ADD [LastName] nvarchar(100) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261001104715_AddStudentNameColumns', N'8.0.17');
GO

COMMIT;
GO

