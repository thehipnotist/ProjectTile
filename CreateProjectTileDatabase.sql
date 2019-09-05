USE [master];
GO

SET NOCOUNT ON

/* ==========================================   
		SET UP THE DATABASE 
   ========================================== */

-- Remove and recreate the database if it exists --

IF EXISTS (SELECT * FROM SYS.DATABASES WHERE NAME = 'ProjectTile')
BEGIN
	EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'ProjectTile';
	ALTER DATABASE [ProjectTile] SET SINGLE_USER WITH ROLLBACK IMMEDIATE	
	DROP DATABASE ProjectTile;
	IF @@ERROR <> 0
	BEGIN
		PRINT 'An error occurred deleting ProjectTile. Aborting.'
		RETURN
		SET NOEXEC ON
	END
	ELSE BEGIN
		PRINT 'Deleted database ProjectTile'
	END
END;
GO

CREATE DATABASE [ProjectTile]
CONTAINMENT = PARTIAL

PRINT 'Created database ProjectTile';
GO

USE [ProjectTile];
GO

-- Make sure the database exists and we are in it --

IF NOT EXISTS (SELECT * FROM SYS.DATABASES WHERE NAME = 'ProjectTile')
BEGIN
	PRINT 'An error occurred, could not create the ProjectTile database. Aborting'
	RETURN
	SET NOEXEC ON	
END;
	
IF (SELECT DB_NAME()) <> 'ProjectTile'
BEGIN
	PRINT 'An error occurred, could not work in the ProjectTile database. Aborting'
	RETURN
	SET NOEXEC ON	
END;

IF @@ERROR <> 0
BEGIN
	PRINT 'An error occurred creating or connecting to ProjectTile. Aborting.'
	RETURN
	SET NOEXEC ON	
END;

/* ==========================================   
		CREATE AUDITING AND DATABASE USERS
   ========================================== */

-- Procedure to create a database user --
GO
CREATE PROC usp_CreateDatabaseUser
	@UserID VARCHAR(50), @Passwd NVARCHAR(50)
AS
BEGIN
	DECLARE @SQL NVARCHAR(MAX)

	SET @SQL = 'CREATE USER ProT_' + @UserID + ' WITH PASSWORD = ''' + @Passwd + '''
		EXEC sp_addrolemember @rolename = ''db_datareader'', @membername = ''ProT_' + @UserID + '''
		EXEC sp_addrolemember @rolename = ''db_datawriter'', @membername = ''ProT_' + @UserID + ''''
	--PRINT (@SQL)
	EXEC (@SQL)

	PRINT 'Created user ProT_' + @UserID + ' in the database and granted membership'
END;

GO
PRINT 'Created procedure to create a standard database user'

-- Function to get the hashed password
GO
CREATE FUNCTION dbo.udf_GetHashedPassword (@Passwd NVARCHAR(50)) 
RETURNS BINARY(64)
AS
BEGIN
	RETURN (SELECT HASHBYTES('SHA2_512', @Passwd) AS HashedPassword)
END;

GO
PRINT 'Created password hash function'

GO
CREATE PROCEDURE [dbo].[stf_CheckHashedPassword] (@UserID VARCHAR(50), @Passwd NVARCHAR(50)) 
AS
BEGIN
	DECLARE @UserHashedPassword BINARY(64)
	DECLARE @TestHashedPassword BINARY(64)
	DECLARE @Match BIT

	SELECT @UserHashedPassword = s.PasswordHash
	FROM dbo.Staff s
	WHERE s.UserID = @UserID

	SELECT @TestHashedPassword = dbo.udf_GetHashedPassword(@Passwd)

	SELECT @Match = CASE WHEN @UserHashedPassword = @TestHashedPassword 
		THEN 1
		ELSE 0
	END

	SELECT @Match AS HashedPassword
END;

GO
PRINT 'Created password hash test procedure'

-- Create a master SQL-only user who can change logins in SQL --
CREATE USER ProT_PasswordAdmin WITH PASSWORD = 'Rumplestiltskin'
EXEC sp_addrolemember @rolename = 'db_securityadmin', @membername = 'ProT_PasswordAdmin'
EXEC sp_addrolemember @rolename = 'db_datareader', @membername = 'ProT_PasswordAdmin'
EXEC sp_addrolemember @rolename = 'db_datawriter', @membername = 'ProT_PasswordAdmin'
EXEC sp_addrolemember @rolename = 'db_owner', @membername = 'ProT_PasswordAdmin'

PRINT 'Created master user'

-- Audit table --

CREATE TABLE dbo.AuditEntries (
	ID								INT				IDENTITY(1,1)	PRIMARY KEY
	, ActionType					VARCHAR(20)
	, UserName						VARCHAR(50)
	, ChangeTime					DATETIME
	, TableName						VARCHAR(50)
	, PrimaryColumn					VARCHAR(50)
	, PrimaryValue					VARCHAR(50)
	, ChangeColumn					VARCHAR(50)
	, OldValue						NVARCHAR(400) 
	, NewValue						NVARCHAR(400)		
	)

PRINT 'Created audit entries table'

-- Procedure to populate audit table --

GO
CREATE PROC usp_AuditEntry
	@ActionType VARCHAR(20), @TableName VARCHAR(50), @PrimaryColumn VARCHAR(50), @PrimaryValue VARCHAR(50),
	@ChangeColumn VARCHAR(50), @OldValue NVARCHAR(400), @NewValue NVARCHAR(400)
AS
BEGIN
	
	DECLARE @SQL	NVARCHAR(MAX)
	SET @SQL = 'INSERT INTO dbo.AuditEntries (ActionType, UserName, ChangeTime, TableName, PrimaryColumn, PrimaryValue,' 
		+ 'ChangeColumn, OldValue, NewValue) 
		' + 'SELECT ''' + @ActionType + ''', ''' + SUSER_SNAME() + ''', SYSDATETIME(), ''' + @TableName + ''', ''' 
		+ @PrimaryColumn + ''', ''' + @PrimaryValue + ''', ''' + @ChangeColumn + ''', ''' + @OldValue + ''', ''' + @NewValue + ''''

	EXEC (@SQL)

END;

GO
PRINT 'Created audit entry procedure';

-- Create a generic trigger that writes an audit entry --

GO
CREATE PROC dbo.usp_CreateAuditTrigger 
	@TableName VARCHAR(50), @PrimaryColumn VARCHAR(50)					

AS
BEGIN

	SET NOCOUNT ON
	DECLARE @SQL				NVARCHAR(MAX)
	DECLARE @ThisColumn			VARCHAR(50)
	DECLARE @ColumnType			VARCHAR(50)
	DECLARE @PrimaryColType		VARCHAR(50)
	DECLARE @PrimaryMaxLength	INT

	SET @SQL = ''

	SELECT @PrimaryColType = UPPER(st.name), @PrimaryMaxLength = c.max_length
	FROM sys.columns c
		INNER JOIN sys.tables t ON c.object_id = t.object_id
		INNER JOIN sys.types st ON c.system_type_id = st.system_type_id
	WHERE t.name = @TableName
		AND c.name = @PrimaryColumn

	SET @SQL = 'CREATE TRIGGER tr_' + @TableName + 'Changed ON ' + @TableName + '
	AFTER INSERT, UPDATE, DELETE
	AS
	BEGIN
				
		DECLARE @ActionType VARCHAR(20)
		DECLARE @PrimaryValue ' + @PrimaryColType + (SELECT CASE 
			WHEN UPPER(@PrimaryColType) LIKE '%CHAR%' THEN '(' + CAST(@PrimaryMaxLength AS VARCHAR(10)) + ')' 
			ELSE '' END) + '
		DECLARE @OldValue NVARCHAR(400) 
		DECLARE @NewValue NVARCHAR(400)

		SELECT @ActionType = CASE WHEN NOT EXISTS (SELECT * FROM INSERTED) THEN ''Deleted''
			ELSE CASE WHEN NOT EXISTS (SELECT * FROM DELETED) THEN ''Inserted''
				ELSE ''Updated''
				END
			END					

		IF @ActionType = ''Deleted''
		BEGIN 
			DECLARE C_Rows CURSOR FOR
			SELECT d.' + @PrimaryColumn + '
			FROM DELETED d
		END
		ELSE BEGIN					
			DECLARE C_Rows CURSOR FOR
			SELECT i.' + @PrimaryColumn + '
			FROM INSERTED i
			LEFT JOIN DELETED d ON d.' + @PrimaryColumn + ' = i.' + @PrimaryColumn + '
		END

		OPEN C_Rows
		FETCH NEXT FROM C_Rows into @PrimaryValue 

		WHILE @@FETCH_STATUS = 0
		BEGIN
			'
		DECLARE C_Columns CURSOR FOR
		SELECT c.name, UPPER(st.name)
		FROM sys.columns c
			INNER JOIN sys.tables t ON c.object_id = t.object_id
			INNER JOIN sys.types st ON c.system_type_id = st.system_type_id
		WHERE t.name = @TableName
			AND c.is_identity = 0
			AND c.is_computed = 0
			AND c.name <> @PrimaryColumn
			AND c.name <> 'Passwd'
			AND UPPER(st.name) <> 'SYSNAME'

		OPEN C_Columns

		FETCH NEXT FROM C_Columns into @ThisColumn, @ColumnType

		WHILE @@FETCH_STATUS = 0
		BEGIN
			
			SET @SQL = @SQL + '
			-- Entry for ''' + @ThisColumn + ''' column
			
			SET @OldValue = ''''
			SET @NewValue = ''''

			IF @ActionType = ''Deleted''
			BEGIN 			
				SELECT @OldValue = ' + CASE WHEN @ThisColumn = 'PasswordHash' THEN '''[Hashed]''' 
					ELSE 'CAST(d.' + @ThisColumn + ' AS NVARCHAR(400))
					' END
					+ ', @NewValue = ''''
				FROM DELETED d
				WHERE d.' + @PrimaryColumn + ' =  @PrimaryValue 
			END
			ELSE IF @ActionType = ''Inserted'' OR UPDATE(' + @ThisColumn + ') 
			BEGIN
				' + CASE WHEN @ThisColumn = 'PasswordHash' THEN 'SELECT @OldValue = ''[OldHash]'', @NewValue = ''[NewHash]'''
				ELSE 'SELECT @OldValue = ISNULL(CAST(d.' + @ThisColumn + ' AS NVARCHAR(400)),''''),	
					@NewValue = CAST(i.' + @ThisColumn + ' AS NVARCHAR(400))
				FROM INSERTED i
					LEFT JOIN DELETED d ON d.' + @PrimaryColumn + ' = i.' + @PrimaryColumn + '
				WHERE i.' + @PrimaryColumn + ' =  @PrimaryValue' 
				END
			+ '
			END

			IF @OldValue <> @NewValue
			BEGIN			
				EXEC usp_AuditEntry
					@ActionType = @ActionType, @TableName = ''' + @TableName + ''', @PrimaryColumn = ''' + @PrimaryColumn + ''', 
						@PrimaryValue = @PrimaryValue, @ChangeColumn = ''' + @ThisColumn + ''', 
						@OldValue = @OldValue, @NewValue = @NewValue
			END
			'

			FETCH NEXT FROM C_Columns into @ThisColumn, @ColumnType	
		END

		SET @SQL = @SQL + 'FETCH NEXT FROM C_Rows into @PrimaryValue  
		END

		CLOSE C_Rows
		DEALLOCATE C_Rows										
	END	
	'
		--PRINT @SQL
		EXEC (@SQL)

	CLOSE C_Columns
	DEALLOCATE C_COLUMNS

END
GO

IF @@ERROR <> 0
BEGIN
	PRINT 'An error occurred creating dbo.usp_CreateAuditTrigger. Aborting.'
	RETURN
	SET NOEXEC ON
END
ELSE BEGIN
	PRINT 'Created audit trigger procedure';		
END;

/* ==========================================   
		CREATE GENERIC PROCEDURE TEMPLATES 
   ========================================== */

-- Create a generic procedure that creates a SELECT procedure --

USE [ProjectTile];
GO
CREATE PROC dbo.usp_CreateGetProcedure 
	@TableName VARCHAR(50),	@IDColumn VARCHAR(50), @Prefix VARCHAR(5) = "usp"						

AS
BEGIN

	SET NOCOUNT ON
	DECLARE @SQL				VARCHAR(MAX)

	SET @SQL = 'CREATE PROC dbo.' + @Prefix + '_Get' + @TableName + 'By' + @IDColumn + '
		@' + @IDColumn + ' VARCHAR(50)
		AS BEGIN
			SET NOCOUNT ON
			SELECT * 
			FROM ' + @TableName + '
			WHERE ' + @IDColumn + ' = @' + @IDColumn + '
		END'

	--PRINT @SQL
	EXEC (@SQL)

END;
GO

IF @@ERROR <> 0
BEGIN
	PRINT 'An error occurred creating dbo.usp_CreateGetProcedure. Aborting.'
	RETURN
	SET NOEXEC ON		
END
ELSE BEGIN
	PRINT 'Created generic ''get'' procedure';		
END;

-- Create a generic procedure that creates an UPDATE procedure --
GO
CREATE PROC dbo.usp_CreateUpdateProcedure 
	@TableName VARCHAR(50), @IDColumn VARCHAR(50), @UpdateColumn VARCHAR(50), @Prefix VARCHAR(5) = "usp"					

AS
BEGIN

	SET NOCOUNT ON
	DECLARE @SQL				VARCHAR(MAX)

	SET @SQL = 'CREATE PROC dbo.' + @Prefix + '_Update' + @UpdateColumn + 'In' + @TableName + '
		@' + @IDColumn + ' VARCHAR(50)
		, @' + @UpdateColumn + ' VARCHAR(50)
		AS BEGIN
			SET NOCOUNT ON
			UPDATE ' + @TableName + '
			SET ' + @UpdateColumn + '= @' + @UpdateColumn + '
			WHERE ' + @IDColumn + ' = @' + @IDColumn + '
		END'

	--PRINT @SQL
	EXEC (@SQL)

END;
GO

IF @@ERROR <> 0
BEGIN
	PRINT 'An error occurred creating dbo.usp_CreateUpdateProcedure. Aborting.'
	RETURN
	SET NOEXEC ON		
END
ELSE BEGIN
	PRINT 'Created generic ''update'' procedure';		
END;

-- Create a generic procedure that creates an INSERT procedure --
USE [ProjectTile];
GO
CREATE PROC dbo.usp_CreateInsertProcedure 
	@TableName VARCHAR(50), @Prefix VARCHAR(5) = "usp"						

AS
BEGIN

	SET NOCOUNT ON
	DECLARE @SQL				VARCHAR(MAX)
	DECLARE @ColumnList			VARCHAR(MAX)
	DECLARE @ThisColumn			VARCHAR(MAX)
	DECLARE @ColumnType			VARCHAR(50)
	DECLARE @MaxLength			INT
	DECLARE @VarList			VARCHAR(MAX)
	DECLARE @FullVarList		VARCHAR(MAX)

	SET @ColumnList = ''
	SET @VarList = ''
	SET @FullVarList = ''
	
	DECLARE C_Columns CURSOR FOR
	SELECT c.name, UPPER(st.name), c.max_length
	FROM sys.columns c
		INNER JOIN sys.tables t ON c.object_id = t.object_id
		INNER JOIN sys.types st ON c.system_type_id = st.system_type_id
	WHERE t.name = @TableName
		AND c.is_identity = 0
		AND c.is_computed = 0
		AND UPPER(st.name) <> 'SYSNAME'

	OPEN C_Columns

	FETCH NEXT FROM C_Columns into @ThisColumn, @ColumnType, @MaxLength
	--SET @ColumnList = @ThisColumn

	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @ColumnList <> '' SET @ColumnList = @ColumnList + ', '
		IF @VarList <> '' SET @VarList = @VarList + ', ' 
		IF @FullVarList <> '' SET @FullVarList = @FullVarList + ', '  
		
		SET @ColumnList = @ColumnList + @ThisColumn		
		SET @VarList = @VarList + '@' + @ThisColumn
		
		SET @FullVarList = @FullVarList + '@' + @ThisColumn + ' ' + @ColumnType
			+ CASE WHEN UPPER(@ColumnType) LIKE '%CHAR%' THEN '(' + CAST(@MaxLength AS VARCHAR(10)) + ')' ELSE '' END
		
		FETCH NEXT FROM C_Columns into @ThisColumn, @ColumnType, @MaxLength		
	END

	--SELECT @ColumnList

	CLOSE C_Columns
	DEALLOCATE C_COLUMNS
	
	SET @SQL = 'CREATE PROC dbo.' + @Prefix + '_InsertInto' + @TableName + ' ( 
		' + @FullVarList + '
		)	 
		AS BEGIN
			SET NOCOUNT ON
			INSERT INTO ' + @TableName + ' (
				' + @ColumnList + '
				)
			VALUES (
				' + @VarList + '
				)
		END'

	--PRINT @SQL
	EXEC (@SQL)

END;
GO

IF @@ERROR <> 0
BEGIN
	PRINT 'An error occurred creating dbo.usp_CreateInsertProcedure. Aborting.'
	RETURN
	SET NOEXEC ON		
END
ELSE BEGIN
	PRINT 'Created generic ''insert'' procedure';		
END;

/* ==========================================   
		CREATE AND POPULATE TABLES 
   ========================================== */

BEGIN TRY
		
	USE [ProjectTile];
	BEGIN TRANSACTION

		SET NOCOUNT ON

		-----------------
		-- Staff Roles --
		-----------------
				
		CREATE TABLE dbo.StaffRoles (
			RoleCode					VARCHAR(5)		PRIMARY KEY
			, RoleDescription			VARCHAR(100)	UNIQUE		NOT NULL
			)
		
		PRINT 'Created staff roles table'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'StaffRoles', @PrimaryColumn = 'RoleCode'

		PRINT 'Created audit trigger for staff roles'

		INSERT INTO dbo.StaffRoles (
							RoleCode,	RoleDescription)
			SELECT			'AD',		'Administrator'
			UNION SELECT	'SM',		'Senior Manager'
			UNION SELECT	'PM',		'Project Manager'
			UNION SELECT	'AM',		'Account Manager'
			UNION SELECT	'SC',		'Senior Consultant'
			UNION SELECT	'AC',		'Application Consultant'
			UNION SELECT	'TM',		'Technical Manager'
			UNION SELECT	'TC',		'Technical Consultant'

		PRINT 'Populated staff roles table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'StaffRoles'
			, @IDColumn = 'RoleCode'
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'StaffRoles'
			, @IDColumn = 'RoleCode'
			, @UpdateColumn = 'RoleDescription'
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'StaffRoles'
			, @Prefix = 'stf'

		PRINT 'Created standard procedures for staff roles table'

		-----------------------
		-- Table Permissions --
		-----------------------
				
		CREATE TABLE dbo.TablePermissions (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY			
			, TableName					VARCHAR(50)
			, RoleCode					VARCHAR(5)
				CONSTRAINT fk_PermissionRoleCode FOREIGN KEY REFERENCES dbo.StaffRoles (RoleCode)			
			, ViewTable					BIT
			, UpdateRows				BIT
			, InsertRows				BIT
			, ChangeStatus				BIT		-- Applies only where tables have an "Active", "Live", "Stage" or equivalent column
			)
		
		PRINT 'Created main table permissions table'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'TablePermissions', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for table permissions'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'TablePermissions',	'AD',		1,			1,			1,			0	
			UNION SELECT	'TablePermissions',	'SM',		1,			1,			1,			0
			UNION SELECT	'TablePermissions',	'PM',		0,			0,			0,			0
			UNION SELECT	'TablePermissions',	'AM',		0,			0,			0,			0
			UNION SELECT	'TablePermissions',	'SC',		0,			0,			0,			0
			UNION SELECT	'TablePermissions',	'AC',		0,			0,			0,			0
			UNION SELECT	'TablePermissions',	'TM',		0,			0,			0,			0
			UNION SELECT	'TablePermissions',	'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for staff roles'	-- This will be further populated as tables are added

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'TablePermissions'
			, @IDColumn = 'ID'
			, @Prefix = 'sec'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'TablePermissions'
			, @Prefix = 'sec'

		PRINT 'Created standard procedures for table permissions table'

		-----------------
		-- Entities --
		-----------------
				
		CREATE TABLE dbo.Entities (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, EntityName				VARCHAR(30)		UNIQUE			NOT NULL
			, EntityDescription			NVARCHAR(100)	UNIQUE			NOT NULL
			)
		
		PRINT 'Created entities table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'Entities',			'AD',		1,			1,			1,			0
			UNION SELECT	'Entities',			'SM',		1,			0,			0,			0
			UNION SELECT	'Entities',			'PM',		1,			0,			0,			0
			UNION SELECT	'Entities',			'AM',		0,			0,			0,			0
			UNION SELECT	'Entities',			'SC',		0,			0,			0,			0
			UNION SELECT	'Entities',			'AC',		0,			0,			0,			0
			UNION SELECT	'Entities',			'TM',		0,			0,			0,			0
			UNION SELECT	'Entities',			'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for entities'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'Entities', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for entities'

		INSERT INTO dbo.Entities (
							EntityName,	EntityDescription)
			SELECT			'SampleCo',	'Demonstration Entity'

		PRINT 'Populated entities table'


		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Entities'
			, @IDColumn = 'ID'
			, @Prefix = 'ent'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Entities'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'EntityName'
			, @Prefix = 'ent'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Entities'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'EntityDescription'
			, @Prefix = 'ent'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'Entities'
			, @Prefix = 'ent'

		PRINT 'Created standard procedures for entities table'

		-----------
		-- Staff --
		-----------

		CREATE TABLE dbo.Staff (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, EmployeeID AS RIGHT('E000000' + CAST(ID AS VARCHAR(100)), 7) PERSISTED	 
			, FirstName					NVARCHAR(100)	NOT NULL
			, Surname					NVARCHAR(100)	NOT NULL
			, RoleCode					VARCHAR(5)
				CONSTRAINT fk_StaffRoleCode FOREIGN KEY REFERENCES dbo.StaffRoles (RoleCode)
			, StartDate					DATE			NOT NULL
			, LeaveDate					DATE
			, UserID					VARCHAR(50)		
			, Passwd					NVARCHAR(50)
			, PasswordHash				BINARY(64)
			, Active					BIT				NOT NULL
			, DefaultEntity				INT
				CONSTRAINT fk_StaffEntity FOREIGN KEY REFERENCES dbo.Entities (ID)
			) 

		PRINT 'Created staff table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'Staff',			'AD',		1,			1,			1,			1
			UNION SELECT	'Staff',			'SM',		1,			1,			1,			1
			UNION SELECT	'Staff',			'PM',		1,			0,			0,			0
			UNION SELECT	'Staff',			'AM',		1,			0,			0,			0
			UNION SELECT	'Staff',			'SC',		1,			0,			0,			0
			UNION SELECT	'Staff',			'AC',		0,			0,			0,			0
			UNION SELECT	'Staff',			'TM',		1,			0,			0,			0
			UNION SELECT	'Staff',			'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for staff'

		-- Ensure that users who do have a username have a unique one
		
		CREATE UNIQUE NONCLUSTERED INDEX ix_StaffUserID_NotNull
		ON dbo.Staff(UserID)
		WHERE UserID IS NOT NULL

		PRINT 'Created staff index for unique usernames'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'Staff', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for staff'

		-- Hash passwords on entry, and/or create database logins for auditing (dynamically so it can go in the same batch)
		
		EXEC ('CREATE TRIGGER tr_StaffLogins ON dbo.Staff
		AFTER INSERT, UPDATE
		AS
		BEGIN
			DECLARE @UserID	VARCHAR(50)
			DECLARE @Passwd NVARCHAR(50) 
			DECLARE @Active BIT	
		
			IF UPDATE(UserID)
			BEGIN
				DECLARE C_Users CURSOR FOR				
					SELECT s.UserID, s.Passwd, s.Active
					FROM dbo.Staff s
					WHERE s.ID IN (SELECT i.ID FROM INSERTED i)

				OPEN C_Users
				FETCH NEXT FROM C_Users INTO @UserID, @Passwd, @Active

				WHILE @@FETCH_STATUS = 0
				BEGIN
					IF @UserID <> '''' AND @Passwd IS NOT NULL AND @Active = 1
					BEGIN
						IF NOT EXISTS (SELECT * FROM sys.sysusers WHERE NAME = @UserID)
						BEGIN
							EXEC usp_CreateDatabaseUser @UserID = @UserID, @Passwd = @Passwd
						END
					END

					FETCH NEXT FROM C_Users INTO @UserID, @Passwd, @Active
				END

				CLOSE C_Users
				DEALLOCATE C_Users
			END
						
			IF UPDATE(Passwd)
			BEGIN
				IF NOT UPDATE(UserID)
				BEGIN
					SELECT @UserID = s.UserID, @Passwd = s.Passwd
					FROM dbo.Staff s
					WHERE s.ID IN (SELECT i.ID FROM INSERTED i)	
														
					EXEC(''ALTER USER ProT_'' + @UserID + '' WITH PASSWORD = '''''' + @Passwd + '''''''')
				END

				UPDATE s
				SET PasswordHash = dbo.udf_GetHashedPassword(Passwd),
					Passwd = ''''						
				FROM dbo.Staff s
				WHERE s.ID IN (SELECT i.ID FROM INSERTED i)

			END	
		END')		

		PRINT 'Created trigger to hash passwords and create SQL logins'
		
		-- Poplate the table

		INSERT INTO dbo.Staff (
							FirstName,	Surname,		RoleCode,	StartDate,		LeaveDate,		UserID,		Passwd,	Active, DefaultEntity)
			SELECT			'System',	'Admin',		'AD',		'2000-01-01',	NULL,			'pjadmin',	'pja',	1,		1
			UNION SELECT	'Julie',	'Drench',		'SM',		'2010-01-01',	'2019-08-03',	NULL,		'',		0,		1
			UNION SELECT	'Michel',	'Jambon',		'SM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Maddie',	'Smidt',		'SM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Benjamin',	'Lumberjack',	'PM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Kayleigh',	'Dawes',		'PM',		'2018-01-01',	'2019-10-15',	NULL,		'',		0,		1
			UNION SELECT	'Kenny',	'Hendry',		'PM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Gemma',	'Johnson',		'PM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Amit',		'Malawi',		'AM',		'2015-09-30',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Sandie',	'Newtown',		'AM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Tim',		'Middleton',	'AM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Olive',	'Coleman',		'AM',		'2010-05-02',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Jack',		'Greengage',	'SC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Nellie',	'Harrison',		'SC',		'2012-07-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Ken',		'Bramall',		'SC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Jessie',	'Higgs',		'AC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Simone',	'Egg',			'AC',		'2010-01-01',	'2018-05-02',	NULL,		'',		0,		1
			UNION SELECT	'Kelly',	'Goldiman',		'AC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Terry',	'Robins',		'AC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Meena',	'Hyal',			'TM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Len',		'Wisher',		'TM',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Emmie',	'Swanson',		'TM',		'2015-10-07',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Nev',		'Patil',		'TC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Billy',	'Paper',		'TC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'James',	'Bellman',		'TC',		'2010-01-01',	NULL,			NULL,		'',		0,		1
			UNION SELECT	'Jamelia',	'Jemal',		'TC',		'2010-01-01',	NULL,			NULL,		'',		0,		1

		PRINT 'Populated staff table'

		-- Set up logins
		
		UPDATE s
		SET UserID = 'S_' + FirstName + LEFT(Surname, 1)
			, Passwd = LEFT(Firstname, 1) + LEFT(Surname, 1)
			, Active = 1
		FROM dbo.Staff s
		WHERE s.StartDate <= GETDATE()
			AND (ISNULL(s.LeaveDate, GETDATE() + 1) >= GETDATE())
			AND s.UserID IS NULL

		PRINT 'Set up staff logins'

		-- Create the usual procedures that are appropriate
		
		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Staff'
			, @IDColumn = 'ID'
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Staff'
			, @IDColumn = 'RoleCode'			
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Staff'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'RoleCode'
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Staff'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'LeaveDate'
			, @Prefix = 'stf'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'Staff'
			, @Prefix = 'stf'

		PRINT 'Created standard procedures for staff roles table'

		-- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_StaffWithRoles AS	
			SELECT s.*, sr.RoleDescription
			FROM dbo.Staff s 
				INNER JOIN dbo.StaffRoles sr ON s.RoleCode = sr.RoleCode')

		PRINT 'Created view of staff with their roles'

		--------------------
		-- Staff Entities --
		--------------------

		CREATE TABLE dbo.StaffEntities (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, StaffID					INT
				CONSTRAINT fk_EntityStaffID FOREIGN KEY REFERENCES dbo.Staff (ID)
			, EntityID					INT
				CONSTRAINT fk_StaffEntityID FOREIGN KEY REFERENCES dbo.Entities (ID)	
			)

		PRINT 'Created staff entities table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'StaffEntities',	'AD',		1,			1,			1,			0
			UNION SELECT	'StaffEntities',	'SM',		1,			1,			1,			1
			UNION SELECT	'StaffEntities',	'PM',		1,			0,			0,			0
			UNION SELECT	'StaffEntities',	'AM',		1,			0,			0,			0
			UNION SELECT	'StaffEntities',	'SC',		0,			0,			0,			0
			UNION SELECT	'StaffEntities',	'AC',		0,			0,			0,			0
			UNION SELECT	'StaffEntities',	'TM',		0,			0,			0,			0
			UNION SELECT	'StaffEntities',	'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for staff entities'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'StaffEntities', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for staff entities'

		-- Populate with existing staff

		INSERT INTO dbo.StaffEntities (
							StaffID,	EntityID)
			SELECT			ID,			DefaultEntity
			FROM dbo.Staff	

		PRINT 'Populated staff entities table'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'StaffEntities'
			, @Prefix = 'stf'

		PRINT 'Created standard procedures for staff entities table'

		---- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_StaffEntities AS	
			SELECT s.FirstName + '' '' + s.Surname AS StaffName, e.EntityName, e.EntityDescription, 
				CASE WHEN s.DefaultEntity = e.ID THEN 1 ELSE 0 END AS DefaultEntity 
			FROM dbo.StaffEntities se
				INNER JOIN dbo.Staff s ON se.StaffID = s.ID
				INNER JOIN dbo.Entities e ON se.EntityID = e.ID')

		PRINT 'Created a view of clients with their products'

		-------------
		-- Clients --
		-------------
				
		CREATE TABLE dbo.Clients (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, EntityID					INT				NOT NULL
				CONSTRAINT fk_ClientEntityID FOREIGN KEY REFERENCES dbo.Entities (ID)
			, ClientCode				VARCHAR(15)		NOT NULL
			, ClientName				NVARCHAR(200)	NOT NULL
			, AccountManagerID			INT
				CONSTRAINT fk_ClientAccountManagerID FOREIGN KEY REFERENCES dbo.Staff (ID)
			, Active					BIT				NOT NULL
			)
		
		PRINT 'Created clients table'	

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'Clients',			'AD',		1,			1,			1,			1
			UNION SELECT	'Clients',			'SM',		1,			1,			1,			1
			UNION SELECT	'Clients',			'PM',		1,			0,			0,			0
			UNION SELECT	'Clients',			'AM',		1,			1,			1,			1
			UNION SELECT	'Clients',			'SC',		1,			0,			0,			0
			UNION SELECT	'Clients',			'AC',		1,			0,			0,			0
			UNION SELECT	'Clients',			'TM',		1,			0,			0,			0
			UNION SELECT	'Clients',			'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for clients'
			
		-- Ensure each client code is unique to each entity
		
		CREATE UNIQUE NONCLUSTERED INDEX ix_Client_Unique_Per_Entity
		ON dbo.Clients(ClientCode, EntityID)		

		PRINT 'Created index on clients to ensure clients are unique to each entity'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'Clients', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for clients'
				
		INSERT INTO dbo.Clients (
							EntityID,	ClientCode,	ClientName,		AccountManagerID,
								Active)
			SELECT			1,			'C_S_LG001',	'Littlegoods',	(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Sandie Newtown'),
								1
			UNION SELECT	1,			'C_S_WD001',	'Woodworths',	(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Amit Malawi'),
								1
			UNION SELECT	1,			'C_S_LE001',	'Levisons',		(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Amit Malawi'),
								1
			UNION SELECT	1,			'C_S_YP001',	'Your Place',	(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Olive Coleman'),
								1
			UNION SELECT	1,			'C_S_SD001',	'Saleday',		(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Tim Middleton'),
								1
			UNION SELECT	1,			'C_S_BH001',	'BHN',			(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Olive Coleman'),
								1
			UNION SELECT	1,			'C_S_CA001',	'Captons',		(SELECT ID FROM dbo.Staff where FirstName + ' ' + Surname = 'Sandie Newtown'),
								1		

		PRINT 'Populated clients table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Clients'
			, @IDColumn = 'ID'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Clients'
			, @IDColumn = 'ClientCode'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Clients'
			, @IDColumn = 'AccountManagerID'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Clients'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ClientName'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Clients'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'AccountManagerID'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'Clients'
			, @Prefix = 'cln'

		PRINT 'Created standard procedures for clients table'

		-- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_ClientsWithAccountManagers AS	
			SELECT e.EntityName, c.ID, c.ClientCode, c.ClientName, c.Active,
				s.FirstName + '' '' + s.Surname AS ''AccountManagerName''
			FROM dbo.Clients c
				INNER JOIN dbo.Staff s ON c.AccountManagerID = s.ID
				INNER JOIN dbo.Entities e ON c.EntityID = e.ID')

		PRINT 'Created view of clients with their Account Managers'

		--------------
		-- Products --
		--------------

		CREATE TABLE Products (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ProductName				VARCHAR(100)	UNIQUE
			, LatestVersion				DECIMAL(10, 2)
			, ProductDescription		NVARCHAR(300)			
			)

		PRINT 'Created products table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'Products',			'AD',		1,			1,			1,			0
			UNION SELECT	'Products',			'SM',		1,			1,			1,			0
			UNION SELECT	'Products',			'PM',		1,			0,			0,			0
			UNION SELECT	'Products',			'AM',		1,			0,			0,			0
			UNION SELECT	'Products',			'SC',		1,			0,			0,			0
			UNION SELECT	'Products',			'AC',		1,			0,			0,			0
			UNION SELECT	'Products',			'TM',		1,			1,			1,			0
			UNION SELECT	'Products',			'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for products'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'Products', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for products'

		INSERT INTO dbo.Products (
							ProductName,	LatestVersion,	ProductDescription)
			SELECT			'Accountible',	5.34,			'Accounting software'
			UNION SELECT	'FlogIT',		2.07,			'Online sales software'
			UNION SELECT	'Inventistry',	3.5,			'Stock management software'
			UNION SELECT	'PeoplePower',	1.5,			'Human Resources software'
			UNION SELECT	'BankIT',		2.00,			'Banking software'

		PRINT 'Populated products table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Products'
			, @IDColumn = 'ID'
			, @Prefix = 'prd'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Products'
			, @IDColumn = 'ProductName'
			, @Prefix = 'prd'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Products'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'LatestVersion'
			, @Prefix = 'prd'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'Products'
			, @Prefix = 'prd'

		PRINT 'Created standard procedures for products table'

		---------------------
		-- Client Products --
		---------------------

		CREATE TABLE dbo.ClientProducts (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ClientID					INT
				CONSTRAINT fk_ProductClientID FOREIGN KEY REFERENCES dbo.Clients (ID)
			, ProductID					INT
				CONSTRAINT fk_ClientProductID FOREIGN KEY REFERENCES dbo.Products (ID)
			, ProductVersion			DECIMAL(9, 1)
			, Live						BIT
			)

		PRINT 'Created client products table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ClientProducts',	'AD',		1,			1,			1,			1
			UNION SELECT	'ClientProducts',	'SM',		1,			1,			1,			1
			UNION SELECT	'ClientProducts',	'PM',		1,			1,			1,			1
			UNION SELECT	'ClientProducts',	'AM',		1,			1,			1,			1
			UNION SELECT	'ClientProducts',	'SC',		1,			0,			0,			0
			UNION SELECT	'ClientProducts',	'AC',		1,			0,			0,			0
			UNION SELECT	'ClientProducts',	'TM',		1,			0,			0,			0
			UNION SELECT	'ClientProducts',	'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for client products'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ClientProducts', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for client products'

		INSERT INTO dbo.ClientProducts (
							ClientID,	
				ProductID,															ProductVersion)
			SELECT			(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LG001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'FlogIT'),			2.0
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LG001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'BankIT'),			1.0
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_WD001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	4.1
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_WD001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'Inventistry'),	3.4
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LE001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'PeoplePower'),	1.2
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_YP001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	3.5
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_YP001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'FlogIT'),			1.7
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_BH001'),	
				(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	3.7
			UNION SELECT	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_BH001'),
				(SELECT ID FROM dbo.Products WHERE ProductName = 'BankIT'),			2.0

		PRINT 'Populated client products table'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ClientProducts'			
			, @Prefix = 'cln'

		PRINT 'Created standard procedures for client products table'

		---- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_ClientsWithProducts AS	
			SELECT e.EntityName, cp.ClientID, c.ClientCode, c.ClientName, cp.ProductID, p.ProductName, cp.ProductVersion, p.LatestVersion, p.ProductDescription
			FROM dbo.Clients c
				INNER JOIN dbo.ClientProducts cp ON cp.ClientID = c.ID
				INNER JOIN dbo.Products p ON cp.ProductID = p.ID
				INNER JOIN dbo.Entities e ON c.EntityID = e.ID')

		PRINT 'Created a view of clients with their products'
		
		-------------------				
		-- Project Types --
		-------------------

		CREATE TABLE ProjectTypes (
			TypeCode					VARCHAR(5)		PRIMARY KEY
			, TypeName					VARCHAR(100)	UNIQUE
			, TypeDescription			NVARCHAR(200)
			)	
		
		PRINT 'Created project types table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ProjectTypes',		'AD',		1,			1,			1,			0
			UNION SELECT	'ProjectTypes',		'SM',		1,			1,			1,			0
			UNION SELECT	'ProjectTypes',		'PM',		1,			0,			0,			0
			UNION SELECT	'ProjectTypes',		'AM',		1,			0,			0,			0
			UNION SELECT	'ProjectTypes',		'SC',		0,			0,			0,			0
			UNION SELECT	'ProjectTypes',		'AC',		0,			0,			0,			0
			UNION SELECT	'ProjectTypes',		'TM',		0,			0,			0,			0
			UNION SELECT	'ProjectTypes',		'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for project types'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ProjectTypes', @PrimaryColumn = 'TypeCode'
		
		PRINT 'Created audit trigger for project types'	

		INSERT INTO dbo.ProjectTypes(
							TypeCode,	TypeName,				TypeDescription)
			SELECT			'NS',		'New site',				'New system installation for a brand new client'
			UNION SELECT	'AS',		'Add system',			'Additional new system for an existing client with other TechNickel systems'
			UNION SELECT	'RB',		'Rebuild',				'Migrate to new restructured installation on existing version'
			UNION SELECT	'UG',		'Upgrade',				'Upgrade existing client to new version (may include new functionality)'
			UNION SELECT	'UR',		'Upgrade and rebuild',	'Migrate to new restructured installation on new version'
			UNION SELECT	'RS',		'Restructure',			'Significant restructuring of current system'
			UNION SELECT	'AF',		'Add functionality',	'Implement new functionality in existing version'
			UNION SELECT	'TO',		'Take-on',				'Adapt existing system for new client'
			UNION SELECT	'IP',		'Internal project',		'Project without client'			

		PRINT 'Populated project types table'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectTypes'
			, @IDColumn = 'TypeCode'
			, @UpdateColumn = 'TypeName'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectTypes'
			, @IDColumn = 'TypeName'
			, @UpdateColumn = 'TypeDescription'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ProjectTypes'	
			, @Prefix = 'pja'	

		PRINT 'Created standard procedures for project types table'

		----------------------
		-- Project Stages --
		----------------------
				
		CREATE TABLE dbo.ProjectStages (
			StageCode					INT				PRIMARY KEY
			, StageName					VARCHAR(20)		NOT NULL
			, StageDescription			NVARCHAR(100)	NOT NULL
			, ProjectStatus				VARCHAR(20)		NOT NULL
			)
		
		PRINT 'Created project stages table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ProjectStages',	'AD',		1,			1,			1,			0
			UNION SELECT	'ProjectStages',	'SM',		1,			0,			0,			0
			UNION SELECT	'ProjectStages',	'PM',		1,			0,			0,			0
			UNION SELECT	'ProjectStages',	'AM',		1,			0,			0,			0
			UNION SELECT	'ProjectStages',	'SC',		0,			0,			0,			0
			UNION SELECT	'ProjectStages',	'AC',		0,			0,			0,			0
			UNION SELECT	'ProjectStages',	'TM',		0,			0,			0,			0
			UNION SELECT	'ProjectStages',	'TC',		0,			0,			0,			0

		PRINT 'Populated table permissions table for project stages'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ProjectStages', @PrimaryColumn = 'StageCode'

		PRINT 'Created audit trigger for project statuses'

		INSERT INTO dbo.ProjectStages (
							StageCode,	StageName,				StageDescription,								ProjectStatus)
			SELECT			0,			'Pre-project',			'Any work prior to formal start date',			'Not Started'
			UNION SELECT	1,			'Initiation',			'Kick-off and planning',						'In Progress'
			UNION SELECT	2,			'Technical Prep',		'Creation of new server environment',			'In Progress'
			UNION SELECT	3,			'Installation',			'Installation of new software',					'In Progress'
			UNION SELECT	4,			'Data Migration',		'Initial migration of data',					'In Progress'
			UNION SELECT	5,			'Configuration',		'Application build, setup or reconfiguration',	'In Progress'
			UNION SELECT	6,			'System Test',			'Testing by TechNickel team before handover',	'In Progress'
			UNION SELECT	7,			'Admin Training',		'Training of administrators and superusers',	'In Progress'
			UNION SELECT	8,			'User Test',			'User Acceptance Testing by client staff',		'In Progress'
			UNION SELECT	9,			'End User Training',	'Roll-out to all users before Go-Live',			'In Progress'
			UNION SELECT	10,			'Live Preparation',		'Re-migration and/or preparation for Go-Live',	'In Progress'
			UNION SELECT	11,			'Go-Live',				'Live Cutover',									'Live'
			UNION SELECT	12,			'Live Running',			'Initial assistance with Live use',				'Live'
			UNION SELECT	13,			'Post-Live Work',		'Any follow-up work for after the Go-Live',		'Live'
			UNION SELECT	14,			'Closure',				'Formal project completion and closure',		'Live'
			UNION SELECT	15,			'Completed',			'Business-as-usual after formal closure',		'Closed'
			UNION SELECT	99,			'Cancelled',			'Project closed before completion',				'Closed'

		PRINT 'Populated project stages table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ProjectStages'
			, @IDColumn = 'StageCode'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectStages'
			, @IDColumn = 'StageCode'
			, @UpdateColumn = 'StageDescription'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ProjectStages'
			, @Prefix = 'pja'

		PRINT 'Created standard procedures for project stages table'

		-----------------
		-- Projects --
		-----------------
						
		CREATE TABLE dbo.Projects (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, EntityID					INT				NOT NULL
				CONSTRAINT fk_ProjectEntityID FOREIGN KEY REFERENCES dbo.Entities (ID)
			, ProjectCode	AS 'PJ_' + RIGHT('000000' + CAST(ID AS VARCHAR(6)), 6) PERSISTED
			, TypeCode					VARCHAR(5)		NOT NULL
				CONSTRAINT fk_ProjectTypeCode FOREIGN KEY REFERENCES dbo.ProjectTypes(TypeCode)			
			, ProjectName				VARCHAR(100)	NOT NULL
			, ClientID					INT
				CONSTRAINT fk_ProjectClientID FOREIGN KEY REFERENCES dbo.Clients(ID)
			, StartDate					DATE
			, StageCode					INT				NOT NULL
				CONSTRAINT fk_ProjectStageCode FOREIGN KEY REFERENCES dbo.ProjectStages(StageCode)
			, ProjectSummary			NVARCHAR(400)	NOT NULL				
			)
		
		PRINT 'Created projects table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'Projects',			'AD',		1,			1,			1,			1
			UNION SELECT	'Projects',			'SM',		1,			1,			1,			1
			UNION SELECT	'Projects',			'PM',		1,			1,			0,			1
			UNION SELECT	'Projects',			'AM',		1,			0,			1,			0
			UNION SELECT	'Projects',			'SC',		1,			0,			0,			0
			UNION SELECT	'Projects',			'AC',		1,			0,			0,			0
			UNION SELECT	'Projects',			'TM',		1,			0,			0,			0
			UNION SELECT	'Projects',			'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for projects'

		-- Create a function-based constraint to make sure the entity matches the client's entity

		EXEC ('CREATE FUNCTION dbo.udf_CheckClientEntity (@ClientID AS INT, @EntityID AS INT, @TypeCode AS VARCHAR(5))
		RETURNS INT
		AS BEGIN
			RETURN (SELECT CASE WHEN @ClientID IS NULL
				THEN CASE @TypeCode
						WHEN ''IP'' THEN 1
						ELSE 0
					END
				ELSE CASE @EntityID
						WHEN (SELECT EntityID FROM dbo.Clients WHERE ID = @ClientID) THEN 1
						ELSE 0
					END
				END)
		END')

		ALTER TABLE dbo.Projects
		ADD CONSTRAINT ck_ClientEntityID 
		CHECK (dbo.udf_CheckClientEntity(ClientID, EntityID, TypeCode) = 1)

		-- Create the usual triggers and procedures etc.

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'Projects', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for projects'

		INSERT INTO dbo.Projects (
							EntityID,	TypeCode,	ProjectName,								ClientID,													
								StageCode,	StartDate,		ProjectSummary)
			SELECT			1,			'AS',		'Accountible 5.3 Implementation',			(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_BH001'),	
								3,			'2019-06-01',	'Install Accountible for new client who already has BankIT'
			UNION SELECT	1,			'TO',		'BankIT 1.0 Take-On',						(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_BH001'),
								5,			'2019-05-12',	'Adapt BankIT to use TechNickel integration'
			UNION SELECT	1,			'UG',		'FlogIT 2.0 and Accountible 5.3 Upgrade',	(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_YP001'),
								8,			'2019-03-22',	'Upgrade FlogIT from version 1.7 and Accountible from 3.5 to latest versions'							
			UNION SELECT	1,			'UG',		'PeoplePower 1.5 Upgrade',					(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LE001'),
								6,			'2019-04-15',	'Upgrade PeoplePower from version 1.2 to latest version, adding Payroll module'
			UNION SELECT	1,			'UR',		'Accountible 5.3 Upgrade and Rebuild',		(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_WD001'),
								2,			'2019-06-19',	'Build a new instance of Accountible with a new structure on the latest version'
			UNION SELECT	1,			'AF',		'Add Warehousing to Inventistry 3.4',		(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_WD001'),
								7,			'2019-03-11',	'Implement the Warehousing module for multi-warehouse stock control'
			UNION SELECT	1,			'RB',		'FlogIT 2.0 Rebuild',						(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LG001'),
								1,			'2019-06-01',	'Build a new instance of FlogIT with a new structure (no upgrade required)'
			UNION SELECT	1,			'UG',		'FlogIT 2.0 Upgrade',						(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_LG001'),
								15,			'2019-01-09',	'Upgrade FlogIT from version 1.1'
			UNION SELECT	1,			'NS',		'PeoplePower 1.2 Implementation',			(SELECT ID FROM dbo.Clients WHERE ClientCode = 'C_S_CA001'),	
								0,			'2019-09-21',	'Install PeoplePower for new client'
			UNION SELECT	1,			'IP',		'Prepare BankIT 2.1',						NULL,
								5,			'2019-07-30',	'Ready version 2.1 of BankIT for future distribution'

		PRINT 'Populated projects table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'Projects'
			, @IDColumn = 'ID'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Projects'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ProjectName'
			, @Prefix = 'prj'

				EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'Projects'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ProjectSummary'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'Projects'
			, @Prefix = 'prj'

		PRINT 'Created standard procedures for projects table'

		---- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_OpenProjects AS	
			SELECT e.EntityName, pj.ProjectCode, 
				ISNULL(pj.ClientID, 0) AS ''ClientID'', ISNULL(c.ClientCode, '''') AS ''ClientCode'', ISNULL(c.ClientName, '''') AS ''ClientName'', 
				pt.TypeName, pj.ProjectName, pj.StartDate,
				pj.StageCode, ps.StageName, ps.ProjectStatus, pj.ProjectSummary
			FROM Projects pj
				INNER JOIN dbo.ProjectTypes pt ON pj.TypeCode = pt.TypeCode
				INNER JOIN dbo.ProjectStages ps ON pj.StageCode = ps.StageCode
				INNER JOIN dbo.Entities e ON pj.EntityID = e.ID
				LEFT JOIN dbo.Clients c ON pj.ClientID = c.ID		
			WHERE ps.ProjectStatus not in (''Not started'', ''Completed'')')

		PRINT 'Created a view of open projects with key details'

		-----------------
		-- Project Products --
		-----------------
				
		CREATE TABLE dbo.ProjectProducts (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ProjectID					INT	
				CONSTRAINT fk_ProductProjectID FOREIGN KEY REFERENCES dbo.Projects(ID)			
			, ProductID					INT
				CONSTRAINT fk_ProjectProductID FOREIGN KEY REFERENCES dbo.Products(ID)
			, OldVersion				DECIMAL(9, 1)
			, NewVersion				DECIMAL(9, 1)	NOT NULL						
			)
		
		PRINT 'Created project products table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ProjectProducts',	'AD',		1,			1,			1,			0
			UNION SELECT	'ProjectProducts',	'SM',		1,			1,			1,			0
			UNION SELECT	'ProjectProducts',	'PM',		1,			1,			1,			0
			UNION SELECT	'ProjectProducts',	'AM',		1,			0,			1,			0
			UNION SELECT	'ProjectProducts',	'SC',		1,			0,			0,			0
			UNION SELECT	'ProjectProducts',	'AC',		1,			0,			0,			0
			UNION SELECT	'ProjectProducts',	'TM',		1,			0,			0,			0
			UNION SELECT	'ProjectProducts',	'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for project products'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ProjectProducts', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for project products'

		INSERT INTO dbo.ProjectProducts (
							ProjectID,
								ProductID,															OldVersion, NewVersion)
			SELECT			(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	3.7,		5.3
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'BankIT'),			2.0,		2.0
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'FlogIT'),			1.7,		2.0
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	3.5,		5.3
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'PeoplePower'),	1.2,		1.5
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'Accountible'),	4.1,		5.3
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'Inventistry'),	3.4,		3.4
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'FlogIT'),			2.0,		2.0
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'FlogIT'),			1.1,		2.0
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'PeoplePower'),	NULL,		1.2
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Prepare BankIT 2.1'),
								(SELECT ID FROM dbo.Products WHERE ProductName = 'BankIT'),			2.0,		2.1																																																																																												

		PRINT 'Populated project products table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'OldVersion'
			, @IDColumn = 'ID'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'NewVersion'
			, @IDColumn = 'ID'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectProducts'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'OldVersion'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectProducts'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'NewVersion'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ProjectProducts'
			, @Prefix = 'prj'

		PRINT 'Created standard procedures for project products table'

		---- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_OpenProjectProducts AS	
			SELECT pj.ProjectCode, ISNULL(c.ClientCode, '''') AS ''ClientCode'', ISNULL(c.ClientName, '''') AS ''ClientName'', 
					pt.TypeName, pj.ProjectName, ps.StageName, ps.ProjectStatus, 
					p.ProductName, pp.OldVersion, pp.NewVersion, ISNULL(cp.ProductVersion, 0) AS ClientVersion
			FROM Projects pj
				INNER JOIN dbo.ProjectTypes pt ON pj.TypeCode = pt.TypeCode
				INNER JOIN dbo.ProjectStages ps ON pj.StageCode = ps.StageCode
				INNER JOIN dbo.ProjectProducts pp ON pp.ProjectID = pj.ID
				INNER JOIN dbo.Products p ON pp.ProductID = p.ID
				LEFT JOIN dbo.Clients c ON pj.ClientID = c.ID
				LEFT JOIN dbo.ClientProducts cp ON cp.ClientID = c.ID and cp.ProductID = p.ID
				
			WHERE ps.ProjectStatus not in (''Not started'', ''Completed'')')

		PRINT 'Created a view of open project products with key details'		

		------------------------
		-- Project Team Roles --
		------------------------
				
		CREATE TABLE dbo.ProjectRoles (
			RoleCode					VARCHAR(5)		PRIMARY KEY
			, RoleDescription			VARCHAR(100)	UNIQUE		NOT NULL
			)
		
		PRINT 'Created project roles table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ProjectRoles',		'AD',		1,			1,			1,			0
			UNION SELECT	'ProjectRoles',		'SM',		1,			1,			1,			0
			UNION SELECT	'ProjectRoles',		'PM',		1,			0,			0,			0
			UNION SELECT	'ProjectRoles',		'AM',		1,			0,			0,			0
			UNION SELECT	'ProjectRoles',		'SC',		1,			0,			0,			0
			UNION SELECT	'ProjectRoles',		'AC',		1,			0,			0,			0
			UNION SELECT	'ProjectRoles',		'TM',		1,			0,			0,			0
			UNION SELECT	'ProjectRoles',		'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for project roles'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ProjectRoles', @PrimaryColumn = 'RoleCode'

		PRINT 'Created audit trigger for project roles'

		INSERT INTO dbo.ProjectRoles (
							RoleCode,	RoleDescription)
			SELECT			'PS',		'Project Sponsor'
			UNION SELECT	'PM',		'Project Manager'
			UNION SELECT	'SC',		'Senior Consultant'
			UNION SELECT	'AC',		'Application Consultant'
			UNION SELECT	'IC',		'Integration Consultant'			
			UNION SELECT	'TL',		'Technical Lead'
			UNION SELECT	'TC',		'Technical Consultant'
			UNION SELECT	'OT',		'Other'

		PRINT 'Populated project roles table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ProjectRoles'
			, @IDColumn = 'RoleCode'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectRoles'
			, @IDColumn = 'RoleCode'
			, @UpdateColumn = 'RoleDescription'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ProjectRoles'
			, @Prefix = 'pja'

		PRINT 'Created standard procedures for project roles table'

		------------------
		-- Project Teams --
		------------------
				
		CREATE TABLE dbo.ProjectTeams (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ProjectID					INT				NOT NULL
				CONSTRAINT fk_TeamProjectID FOREIGN KEY REFERENCES dbo.Projects (ID)
			, StaffID					INT				NOT NULL
				CONSTRAINT fk_TeamStaffID FOREIGN KEY REFERENCES dbo.Staff (ID)
			, ProjectRoleCode			VARCHAR(5)
				CONSTRAINT fk_TeamRoleCode FOREIGN KEY REFERENCES dbo.ProjectRoles (RoleCode)
			, FromDate					DATE
			, ToDate					DATE				
			)

		PRINT 'Created project teams table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ProjectTeams',		'AD',		1,			1,			1,			0
			UNION SELECT	'ProjectTeams',		'SM',		1,			1,			1,			0
			UNION SELECT	'ProjectTeams',		'PM',		1,			1,			1,			0
			UNION SELECT	'ProjectTeams',		'AM',		1,			1,			1,			0
			UNION SELECT	'ProjectTeams',		'SC',		1,			0,			1,			0
			UNION SELECT	'ProjectTeams',		'AC',		1,			0,			0,			0
			UNION SELECT	'ProjectTeams',		'TM',		1,			0,			0,			0
			UNION SELECT	'ProjectTeams',		'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for project teams'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ProjectTeams', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for project teams'
			
		INSERT INTO dbo.ProjectTeams (
							ProjectID,	
								StaffID,																			ProjectRoleCode)
			SELECT			(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Olive Coleman'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Benjamin Lumberjack'),	'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Emmie Swanson'),		'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jack Greengage'),		'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jessie Higgs'),		'AC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Olive Coleman'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Gemma Johnson'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Billy Paper'),		'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Ken Bramall'),			'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Simone Egg'),			'IC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Olive Coleman'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Kayleigh Dawes'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Len Wisher'),			'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Nellie Harrison'),		'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Kelly Goldiman'),		'AC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Terry Robins'),		'IC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Amit Malawi'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Kenny Hendry'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Simone Egg'),			'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jamelia Jemal'),		'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Amit Malawi'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Kayleigh Dawes'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Meena Hyal'),			'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jack Greengage'),		'SC'																											
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Amit Malawi'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Benjamin Lumberjack'),	'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'James Bellman'),		'TL'	
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Terry Robins'),		'SC'	
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Sandie Newtown'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Gemma Johnson'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Nev Patil'),			'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Ken Bramall'),			'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Sandie Newtown'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Gemma Johnson'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Nev Patil'),			'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Ken Bramall'),			'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Sandie Newtown'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Kayleigh Dawes'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Emmie Swanson'),		'TL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Nellie Harrison'),		'SC'																								
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Prepare BankIT 2.1'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Julie Drench'),		'PS'									
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Prepare BankIT 2.1'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Gemma Johnson'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Prepare BankIT 2.1'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jack Greengage'),		'SC'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Prepare BankIT 2.1'),
								(SELECT ID FROM dbo.Staff WHERE FirstName + ' ' + Surname = 'Jamelia Jemal'),		'IC'										

		PRINT 'Populated project teams table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ProjectTeams'
			, @IDColumn = 'ID'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ProjectRoleCode'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'StaffID'
			, @Prefix = 'prj'
			
		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ProjectTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ToDate'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ProjectTeams'
			, @Prefix = 'prj'

		PRINT 'Created standard procedures for project teams table'

		-- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_ProjectTeams AS	
			SELECT e.EntityName, pj.ProjectName, ISNULL(c.ClientName, '''') AS ''ClientName'', 
				s.FirstName + '' '' + s.Surname AS ''StaffName'', 
				pt.ProjectRoleCode AS ''ProjectRoleCode'', pr.RoleDescription AS ''ProjectRoleDescription'', 
				sr.RoleDescription AS ''JobTitle''
			FROM dbo.ProjectTeams pt
				INNER JOIN dbo.Staff s ON pt.StaffID = s.ID
				INNER JOIN dbo.Projects pj ON pt.ProjectID = pj.ID
				INNER JOIN dbo.Entities e ON pj.EntityID = e.ID
				INNER JOIN dbo.ProjectRoles pr ON pt.ProjectRoleCode = pr.RoleCode
				INNER JOIN dbo.StaffRoles sr ON s.RoleCode = sr.RoleCode
				LEFT JOIN dbo.Clients c ON pj.ClientID = c.ID')

		PRINT 'Created view of project teams'

		------------------
		-- Client Staff --
		------------------
				
		CREATE TABLE dbo.ClientStaff (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ClientID					INT				NOT NULL
				CONSTRAINT fk_StaffClientID FOREIGN KEY REFERENCES dbo.Clients (ID)
			, FirstName					NVARCHAR(100)	NOT NULL
			, Surname					NVARCHAR(100)	NOT NULL
			, JobTitle					NVARCHAR(100)
			, PhoneNumber				VARCHAR(50)
			, Email						NVARCHAR(100)
			, Active					BIT				NOT NULL
			)
	
		PRINT 'Created client staff table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ClientStaff',		'AD',		1,			1,			1,			0
			UNION SELECT	'ClientStaff',		'SM',		1,			1,			1,			0
			UNION SELECT	'ClientStaff',		'PM',		1,			1,			0,			0
			UNION SELECT	'ClientStaff',		'AM',		1,			1,			1,			0
			UNION SELECT	'ClientStaff',		'SC',		1,			0,			0,			0
			UNION SELECT	'ClientStaff',		'AC',		1,			0,			0,			0
			UNION SELECT	'ClientStaff',		'TM',		1,			0,			0,			0
			UNION SELECT	'ClientStaff',		'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for client staff'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ClientStaff', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for client staff'
				
		INSERT INTO dbo.ClientStaff (
							ClientID,														FirstName,		Surname,		JobTitle,		
								PhoneNumber,	Email,								Active)
			SELECT			(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LG001'),	'Michel',		'Parisienne',	'Project Manager',
								'01234 567890',	'michelparisienne@littlegoods.com',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LG001'),	'Erin',			'Idol',			'Sales Director',
								'02345 678901',	'erinidol@littlegoods.com',			1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LG001'),	'Jerry',		'Williams',		'Sales Manager',
								'03456 789012',	'jerrywilliams@littlegoods.com',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LG001'),	'Johann',		'Kliese',		'IT Director',
								'04567 890123',	'johnannkliese@littlegoods.com',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LG001'),	'Gareth',		'Charman',		'IT Manager',
								'02345 678901',	'garethcharman@littlegoods.com',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_WD001'),	'Selena',		'Gilliam',		'Project Manager',
								'09876 543210', 's.gilliam@woodworths.co.uk',		1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_WD001'),	'Stephanie',	'Grant',		'Warehouse Manager',
								'08765 432109', 's.grant@woodworths.co.uk',			1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_WD001'),	'Vittoria',		'Wood',			'Purchasing Manager',
								'07654 321098', 'v.wood@woodworths.co.uk',			1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_WD001'),	'Betty Jules',	'Keane',		'Production Manager',
								'06543 210987', 'b.j.keane@woodworths.co.uk',		1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_WD001'),	'Marina',		'Nataliova',	'IT Support Manager',
								'05432 109876', 'm.nataliova@woodworths.co.uk',		1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LE001'),	'Dorian',		'Esteban',		'HR Director',
								'01112 131415', 'doriane@levisons.org',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LE001'),	'Mick',			'Kerslake',		'HR Manager',
								'02122 232425', 'mickk@levisons.org',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LE001'),	'Kyle',			'Minardi',		'Payroll Manager',
								'03132 333435', 'kylem@levisons.org',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_LE001'),	'Rich',			'Anstey',		'Service Delivery Manager',
								'01112 131415', 'richa@levisons.org',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_YP001'),	'Johnny',		'Lemon',		'Programme Manager',
								'09998 979695',	'johnny.lemon@yourplace.org.uk',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_YP001'),	'Jordi',		'Hamilton',		'Project Manager',
								'08988 878685',	'jordi.hamilton@yourplace.org.uk',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_YP001'),	'Pablo',		'McCarthy',		'Chief Information Officer',
								'07978 777675',	'pablo.mccarthy@yourplace.org.uk',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_YP001'),	'Roger',		'Starkey',		'Sales Director',
								'06968 676665',	'roger.starkey@yourplace.org.uk',	1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_SD001'),	'Michaela',		'Osaka',		'Project Manager',
								'01010 101010',	'michaela.o@saleday.net',			1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_SD001'),	'Jilly',		'Kenny',		'Finance Manager',
								'02020 202020',	'jilly.k@saleday.net',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_BH001'),	'Gerry',		'Stringer',		'Chief Executive Officer',
								'09090 909090',	'gstringer@bhn.co.uk',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_BH001'),	'Helen',		'Debonaires',	'Chief Financial Officer',
								'08080 808080',	'hdebonaires@bhn.co.uk',			1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_BH001'),	'Brian',		'Seachest',		'Head of Finance',
								'07070 707070',	'bseachest@bhn.co.uk',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_BH001'),	'Olivia',		'Wilfred',		'Head of IT',
								'06060 606060',	'owilfred@bhn.co.uk',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_BH001'),	'Jerry',		'Fillion',		'Treasury Manager',
								'05050 505050',	'jfillion@bhn.co.uk',				1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_CA001'),	'Rosa',			'Geiler',		'Project Manager',
								'01122 334455',	'r_geiler@cna.com',					1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_CA001'),	'Mona',			'Geiler',		'HR Director',
								'02233 445566',	'm_geiler@cna.com',					1
			UNION SELECT	(SELECT ID FROM dbo.Clients where ClientCode = 'C_S_CA001'),	'Charles',		'Brighton',		'Infrastructure Manager',
								'03344 556677',	'c_brighton@cna.com',				1

		PRINT 'Populated client staff table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ClientStaff'
			, @IDColumn = 'ID'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientStaff'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'Surname'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientStaff'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'JobTitle'
			, @Prefix = 'cln'
			
		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientStaff'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'Active'
			, @Prefix = 'cln'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ClientStaff'
			, @Prefix = 'cln'

		PRINT 'Created standard procedures for client staff table'

		-- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_ClientStaff AS	
			SELECT e.EntityName, c.ClientCode, c.ClientName, 
				cs.FirstName + '' '' + cs.Surname AS ''StaffName'', cs.JobTitle, cs.PhoneNumber, cs.Email
			FROM dbo.ClientStaff cs
				INNER JOIN dbo.Clients c ON cs.ClientID = c.ID
				INNER JOIN dbo.Entities e ON c.EntityID = e.ID')

		PRINT 'Created view of client staff'

		-----------------------
		-- Client Team Roles --
		-----------------------
				
		CREATE TABLE dbo.ClientTeamRoles (
			RoleCode					VARCHAR(5)		PRIMARY KEY
			, RoleDescription			VARCHAR(100)	UNIQUE		NOT NULL
			)
		
		PRINT 'Created client team roles table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ClientTeamRoles',	'AD',		1,			1,			1,			0
			UNION SELECT	'ClientTeamRoles',	'SM',		1,			1,			1,			0
			UNION SELECT	'ClientTeamRoles',	'PM',		1,			0,			0,			0
			UNION SELECT	'ClientTeamRoles',	'AM',		1,			0,			0,			0
			UNION SELECT	'ClientTeamRoles',	'SC',		1,			0,			0,			0
			UNION SELECT	'ClientTeamRoles',	'AC',		1,			0,			0,			0
			UNION SELECT	'ClientTeamRoles',	'TM',		1,			0,			0,			0
			UNION SELECT	'ClientTeamRoles',	'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for client team roles'

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ClientTeamRoles', @PrimaryColumn = 'RoleCode'

		PRINT 'Created audit trigger for client team roles'

		INSERT INTO dbo.ClientTeamRoles (
							RoleCode,	RoleDescription)
			SELECT			'PS',		'Project Sponsor'
			UNION SELECT	'PM',		'Project Manager'
			UNION SELECT	'PA',		'Project Administrator'
			UNION SELECT	'AM',		'Application Team Member'
			UNION SELECT	'IL',		'IT Lead'
			UNION SELECT	'IM',		'IT Team Member'
			UNION SELECT	'OT',		'Other'

		PRINT 'Populated client team roles table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ClientTeamRoles'
			, @IDColumn = 'RoleCode'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientTeamRoles'
			, @IDColumn = 'RoleCode'
			, @UpdateColumn = 'RoleDescription'
			, @Prefix = 'pja'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ClientTeamRoles'
			, @Prefix = 'pja'

		PRINT 'Created standard procedures for client team roles table'

		------------------
		-- Client Teams --
		------------------
				
		CREATE TABLE dbo.ClientTeams (
			ID							INT				IDENTITY(1,1)	PRIMARY KEY
			, ProjectID					INT				NOT NULL
				CONSTRAINT fk_ClientTeamProjectID FOREIGN KEY REFERENCES dbo.Projects (ID)
			, ClientStaffID				INT				NOT NULL
				CONSTRAINT fk_ClientTeamStaffID FOREIGN KEY REFERENCES dbo.ClientStaff (ID)
			, ClientTeamRoleCode		VARCHAR(5)
				CONSTRAINT fk_ClientTeamRoleCode FOREIGN KEY REFERENCES dbo.ClientTeamRoles (RoleCode)
			, FromDate					DATE
			, ToDate					DATE				
			)
	
		PRINT 'Created client teams table'

		INSERT INTO dbo.TablePermissions (
							TableName,			RoleCode,	ViewTable,	UpdateRows,	InsertRows, ChangeStatus)
			SELECT			'ClientTeams',		'AD',		1,			1,			1,			0
			UNION SELECT	'ClientTeams',		'SM',		1,			1,			1,			0
			UNION SELECT	'ClientTeams',		'PM',		1,			1,			1,			0
			UNION SELECT	'ClientTeams',		'AM',		1,			1,			1,			0
			UNION SELECT	'ClientTeams',		'SC',		1,			0,			0,			0
			UNION SELECT	'ClientTeams',		'AC',		1,			0,			0,			0
			UNION SELECT	'ClientTeams',		'TM',		1,			0,			0,			0
			UNION SELECT	'ClientTeams',		'TC',		1,			0,			0,			0

		PRINT 'Populated table permissions table for client teams'
		
		-- Create a function-based constraint to make sure the client staff member matches the client

		EXEC ('CREATE FUNCTION dbo.udf_CheckClientStaffMember (@ProjectID AS INT, @ClientStaffID AS INT)
		RETURNS INT
		AS BEGIN
			RETURN (SELECT CASE (SELECT ClientID FROM dbo.Projects WHERE ID = @ProjectID)
					WHEN (SELECT ClientID FROM dbo.ClientStaff WHERE ID = @ClientStaffID) THEN 1
					ELSE 0
				END)
		END')

		ALTER TABLE dbo.ClientTeams
		ADD CONSTRAINT ck_ClientID 
		CHECK (dbo.udf_CheckClientStaffMember (ProjectID, ClientStaffID) = 1)

		-- Create the usual triggers and procedures etc.

		EXEC dbo.usp_CreateAuditTrigger @TableName = 'ClientTeams', @PrimaryColumn = 'ID'

		PRINT 'Created audit trigger for client teams'
				
		INSERT INTO dbo.ClientTeams (
							ProjectID,	
								ClientStaffID,																				ClientTeamRoleCode)
			SELECT			(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Gerry Stringer'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Helen Debonaires'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Olivia Wilfred'),		'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Jerry Fillion'),			'AM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Brian Seachest'),		'AM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Jerry Fillion'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Helen Debonaires'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Olivia Wilfred'),		'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'BankIT 1.0 Take-On'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Brian Seachest'),		'AM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Johnny Lemon'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Jordi Hamilton'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Pablo McCarthy'),		'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 and Accountible 5.3 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Roger Starkey'),			'AM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Dorian Esteban'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Mick Kerslake'),			'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Kyle Minardi'),			'AM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.5 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Rich Anstey'),			'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Vittoria Wood'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Selena Gilliam'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Accountible 5.3 Upgrade and Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Marina Nataliova'),		'IL'											
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Stephanie Grant'),		'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Betty Jules Keane'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'Add Warehousing to Inventistry 3.4'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Marina Nataliova'),		'IL'	
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Erin Idol'),				'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Michel Parisienne'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Johann Kliese'),			'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Rebuild'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Gareth Charman'),		'IM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Erin Idol'),				'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Michel Parisienne'),		'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Johann Kliese'),			'IL'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'FlogIT 2.0 Upgrade'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Gareth Charman'),		'IM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Mona Geiler'),			'PS'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Rosa Geiler'),			'PM'
			UNION SELECT	(SELECT ID FROM dbo.Projects WHERE ProjectName = 'PeoplePower 1.2 Implementation'),
								(SELECT ID FROM dbo.ClientStaff WHERE FirstName + ' ' + Surname = 'Charles Brighton'),		'IL'																	

		PRINT 'Populated client teams table'

		EXEC [dbo].[usp_CreateGetProcedure] 
			@TableName = 'ClientTeams'
			, @IDColumn = 'ID'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ClientTeamRoleCode'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ClientStaffID'
			, @Prefix = 'prj'
			
		EXEC [dbo].[usp_CreateUpdateProcedure] 
			@TableName = 'ClientTeams'
			, @IDColumn = 'ID'
			, @UpdateColumn = 'ToDate'
			, @Prefix = 'prj'

		EXEC [dbo].[usp_CreateInsertProcedure] 
			@TableName = 'ClientTeams'
			, @Prefix = 'prj'

		PRINT 'Created standard procedures for client teams table'

		-- Create a view dynamically so it can go in the same batch
		EXEC ('CREATE VIEW dbo.vi_ClientTeams AS	
			SELECT e.EntityName, c.ClientName, pj.ProjectName, 
				cs.FirstName + '' '' + cs.Surname AS ''StaffName'', cs.JobTitle, 
				ctr.RoleDescription
			FROM dbo.ClientTeams ct
				INNER JOIN dbo.ClientStaff cs ON ct.ClientStaffID = cs.ID
				INNER JOIN dbo.Clients c ON cs.ClientID = c.ID
				INNER JOIN dbo.Entities e ON c.EntityID = e.ID
				INNER JOIN dbo.ClientTeamRoles ctr ON ct.ClientTeamRoleCode = ctr.RoleCode
				INNER JOIN dbo.Projects pj ON ct.ProjectID = pj.ID')

		PRINT 'Created view of client teams'


		/* ------------------------------------------   
				Check results and finish off
		   ------------------------------------------ */

	-- Views
	
	SELECT *
	FROM dbo.vi_StaffWithRoles

	SELECT *
	FROM dbo.vi_ClientsWithAccountManagers

	SELECT *
	FROM dbo.vi_ClientsWithProducts

	SELECT *
	FROM dbo.vi_OpenProjects

	SELECT *
	FROM dbo.vi_OpenProjectProducts

	SELECT *
	FROM dbo.vi_ProjectTeams

	SELECT *
	FROM dbo.vi_ClientStaff

	SELECT *
	FROM dbo.vi_ClientTeams

	-- Audit records

	SELECT *
	FROM dbo.AuditEntries

	-- Confirm completion and commit

	PRINT 'Selected output to show results'

	COMMIT TRANSACTION

	PRINT 'Transaction committed'


END TRY

BEGIN CATCH
			
	/* ------------------------------------------   
		Finish off and check results
	------------------------------------------ */
	
	DECLARE @ErrorMessage NVARCHAR(4000)  
	DECLARE @ErrorSeverity INT
	DECLARE @ErrorState INT
	DECLARE @ErrorLine INT 

	SELECT   
		@ErrorMessage = ERROR_MESSAGE(),  
		@ErrorSeverity = ERROR_SEVERITY(),  
		@ErrorState = ERROR_STATE()  
 	
	PRINT 'Error in line ' + CAST(@ErrorLine AS VARCHAR(10)) + ' of severity ' + CAST(@ErrorSeverity AS VARCHAR(10)) 
		+ ' in state ' + CAST(@ErrorState AS VARCHAR(10)) + ': ' + @ErrorMessage 

	RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
	ROLLBACK TRANSACTION

	PRINT 'Transaction rolled back'

END CATCH;

SET NOEXEC OFF