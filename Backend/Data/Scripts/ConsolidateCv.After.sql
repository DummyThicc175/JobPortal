SET NOCOUNT ON;

CREATE TABLE dbo.__CvUpgrade_Map (OldId int NOT NULL PRIMARY KEY, NewId int NOT NULL);
MERGE dbo.CVs AS target
USING (SELECT c.*, u.Id AS OwnerId,
    CASE WHEN EXISTS(SELECT 1 FROM dbo.CVTemplates t WHERE t.Id = c.TemplateId) THEN c.TemplateId ELSE NULL END AS ResolvedTemplateId
    FROM dbo.__CvUpgrade_UserCVs c JOIN dbo.AspNetUsers u ON UPPER(c.Email) = UPPER(u.Email)) AS source
ON 1 = 0
WHEN NOT MATCHED THEN INSERT
    (CandidateId, Title, UploadDate, TemplateId, FullName, JobTitle, Email, Phone, Address, Website, Summary,
     AvatarPath, Activities, Certifications, Interests, IsSnapshot)
VALUES
    (source.OwnerId, source.JobTitle, SYSUTCDATETIME(),
     source.ResolvedTemplateId,
     source.FullName, source.JobTitle, source.Email, source.Phone, source.Address, source.Website, source.Summary,
     source.AvatarPath, source.Activities, source.Certifications, source.Interests, 0)
OUTPUT source.Id, inserted.Id INTO dbo.__CvUpgrade_Map;

INSERT dbo.Experiences (CVId, SortOrder, Company, Role, Duration, Description)
SELECT m.NewId, s.Id, s.Company, s.Role, s.Duration, s.Description FROM dbo.__CvUpgrade_Experiences s JOIN dbo.__CvUpgrade_Map m ON m.OldId = s.EditCVViewModelId;
INSERT dbo.Educations (CVId, SortOrder, School, Major, Duration)
SELECT m.NewId, s.Id, s.School, s.Major, s.Duration FROM dbo.__CvUpgrade_Educations s JOIN dbo.__CvUpgrade_Map m ON m.OldId = s.EditCVViewModelId;
INSERT dbo.ITSKills (CVId, SortOrder, SkillName, Description)
SELECT m.NewId, s.Id, s.SkillName, s.Description FROM dbo.__CvUpgrade_ITSKills s JOIN dbo.__CvUpgrade_Map m ON m.OldId = s.EditCVViewModelId;
INSERT dbo.Languages (CVId, SortOrder, LanguageName, Level)
SELECT m.NewId, s.Id, s.LanguageName, s.Level FROM dbo.__CvUpgrade_Languages s JOIN dbo.__CvUpgrade_Map m ON m.OldId = s.EditCVViewModelId;
INSERT dbo.[References] (CVId, SortOrder, Name, Position, Company, Phone)
SELECT m.NewId, s.Id, s.Name, s.Position, s.Company, s.Phone FROM dbo.__CvUpgrade_References s JOIN dbo.__CvUpgrade_Map m ON m.OldId = s.EditCVViewModelId;

INSERT dbo.Experiences (CVId, SortOrder, Company, Role, Duration, Description)
SELECT c.Id, CONVERT(int,j.[key]),
    COALESCE(JSON_VALUE(j.value,'$.Company'), JSON_VALUE(j.value,'$.company')),
    COALESCE(JSON_VALUE(j.value,'$.Role'), JSON_VALUE(j.value,'$.role')),
    COALESCE(JSON_VALUE(j.value,'$.Duration'), JSON_VALUE(j.value,'$.duration'), JSON_VALUE(j.value,'$.Period')),
    COALESCE(JSON_VALUE(j.value,'$.Description'), JSON_VALUE(j.value,'$.description'))
FROM dbo.__CvUpgrade_CVs c CROSS APPLY OPENJSON(NULLIF(c.ExperiencesJson,'')) j;
INSERT dbo.Educations (CVId, SortOrder, School, Major, Duration)
SELECT c.Id, CONVERT(int,j.[key]),
    COALESCE(JSON_VALUE(j.value,'$.School'), JSON_VALUE(j.value,'$.school')),
    COALESCE(JSON_VALUE(j.value,'$.Major'), JSON_VALUE(j.value,'$.major'), JSON_VALUE(j.value,'$.Degree')),
    COALESCE(JSON_VALUE(j.value,'$.Duration'), JSON_VALUE(j.value,'$.duration'), JSON_VALUE(j.value,'$.Period'))
FROM dbo.__CvUpgrade_CVs c CROSS APPLY OPENJSON(NULLIF(c.EducationsJson,'')) j;
INSERT dbo.ITSKills (CVId, SortOrder, SkillName, Description)
SELECT c.Id, CONVERT(int,j.[key]),
    CASE WHEN j.type = 1 THEN j.value ELSE COALESCE(JSON_VALUE(j.value,'$.SkillName'), JSON_VALUE(j.value,'$.skillName')) END,
    CASE WHEN j.type = 5 THEN COALESCE(JSON_VALUE(j.value,'$.Description'), JSON_VALUE(j.value,'$.description')) END
FROM dbo.__CvUpgrade_CVs c CROSS APPLY OPENJSON(NULLIF(c.SkillsJson,'')) j;

SET IDENTITY_INSERT dbo.Candidates ON;
INSERT dbo.Candidates (Id, UserId, FullName, Phone, Gender, Birthday, AvatarUrl, Address)
SELECT c.Id, u.Id, c.FullName, c.Phone, c.Gender, c.Birthday, c.AvatarUrl, c.Address
FROM dbo.__CvUpgrade_Candidates c JOIN dbo.AspNetUsers u ON UPPER(c.Email) = UPPER(u.Email);
SET IDENTITY_INSERT dbo.Candidates OFF;

-- Preserve the uploaded PDF in the canonical CV table.
INSERT dbo.CVs (CandidateId, Title, UploadDate, FileName, FullName, Email, Phone, Address, AvatarPath, IsSnapshot)
SELECT u.Id, N'CV tải lên', SYSUTCDATETIME(), f.FileName, c.FullName, u.Email, c.Phone, c.Address, c.AvatarUrl, 0
FROM dbo.__CvUpgrade_Candidates c JOIN dbo.AspNetUsers u ON UPPER(c.Email) = UPPER(u.Email)
CROSS APPLY (SELECT RIGHT(REPLACE(c.CVUrl, '\', '/'), CHARINDEX('/',REVERSE('/' + REPLACE(c.CVUrl, '\', '/'))) - 1) AS FileName) f
WHERE NULLIF(c.CVUrl,'') IS NOT NULL AND f.FileName <> 'default-cv.pdf'
AND NOT EXISTS (SELECT 1 FROM dbo.CVs existing WHERE existing.CandidateId = u.Id AND existing.FileName = f.FileName);

INSERT dbo.Candidates (UserId)
SELECT DISTINCT cv.CandidateId FROM dbo.CVs cv
WHERE NOT EXISTS (SELECT 1 FROM dbo.Candidates profile WHERE profile.UserId = cv.CandidateId);

UPDATE profile SET DefaultCVId = chosen.Id
FROM dbo.Candidates profile
OUTER APPLY (SELECT TOP (1) cv.Id FROM dbo.CVs cv WHERE cv.CandidateId = profile.UserId AND cv.IsSnapshot = 0
    ORDER BY CASE WHEN cv.FileName IS NOT NULL THEN 0 ELSE 1 END, cv.UploadDate DESC, cv.Id DESC) chosen;

-- Recover personal fields that were left in AspNetUsers by old migrations.
IF COL_LENGTH('dbo.AspNetUsers','FullName') IS NOT NULL
EXEC(N'
    INSERT dbo.Candidates (UserId, FullName, Birthday, Gender)
    SELECT u.Id, NULLIF(u.FullName,''''), u.BirthDate, u.Gender FROM dbo.AspNetUsers u
    WHERE (NULLIF(u.FullName,'''') IS NOT NULL OR u.BirthDate IS NOT NULL OR NULLIF(u.Gender,'''') IS NOT NULL)
    AND NOT EXISTS (SELECT 1 FROM dbo.Candidates c WHERE c.UserId = u.Id);
    UPDATE c SET FullName = COALESCE(NULLIF(c.FullName,''''), NULLIF(u.FullName,'''')),
        Birthday = COALESCE(c.Birthday,u.BirthDate), Gender = COALESCE(NULLIF(c.Gender,''''), u.Gender)
    FROM dbo.Candidates c JOIN dbo.AspNetUsers u ON u.Id = c.UserId;
');

DECLARE @dropDefaults nvarchar(max) = N'';
SELECT @dropDefaults = @dropDefaults + N'ALTER TABLE dbo.AspNetUsers DROP CONSTRAINT ' + QUOTENAME(d.name) + N';'
FROM sys.default_constraints d JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
WHERE d.parent_object_id = OBJECT_ID(N'dbo.AspNetUsers') AND c.name IN ('FullName','BirthDate','Gender');
IF @dropDefaults <> N'' EXEC(@dropDefaults);
IF COL_LENGTH('dbo.AspNetUsers','FullName') IS NOT NULL ALTER TABLE dbo.AspNetUsers DROP COLUMN FullName;
IF COL_LENGTH('dbo.AspNetUsers','BirthDate') IS NOT NULL ALTER TABLE dbo.AspNetUsers DROP COLUMN BirthDate;
IF COL_LENGTH('dbo.AspNetUsers','Gender') IS NOT NULL ALTER TABLE dbo.AspNetUsers DROP COLUMN Gender;

-- Restore category/company links before dropping the temporary copies of their old names.
INSERT dbo.Categories (Name)
SELECT DISTINCT LTRIM(RTRIM(old.CategoryName)) FROM dbo.__CvUpgrade_JobPosts old
WHERE old.CategoryId IS NULL AND NULLIF(LTRIM(RTRIM(old.CategoryName)),'') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = LTRIM(RTRIM(old.CategoryName)));
UPDATE j SET CategoryId = c.Id FROM dbo.JobPosts j JOIN dbo.__CvUpgrade_JobPosts old ON old.Id = j.Id
JOIN dbo.Categories c ON c.Name = LTRIM(RTRIM(old.CategoryName)) WHERE j.CategoryId IS NULL;

INSERT dbo.Companies (EmployerId,Name,Description,LogoUrl,Website,Address)
SELECT DISTINCT old.EmployerId, LTRIM(RTRIM(old.CompanyName)), '', '', '', '' FROM dbo.__CvUpgrade_JobPosts old
WHERE old.CompanyId IS NULL AND old.EmployerId IS NOT NULL AND NULLIF(LTRIM(RTRIM(old.CompanyName)),'') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.Companies c WHERE c.EmployerId=old.EmployerId AND c.Name=LTRIM(RTRIM(old.CompanyName)));
UPDATE j SET CompanyId = c.Id FROM dbo.JobPosts j JOIN dbo.__CvUpgrade_JobPosts old ON old.Id = j.Id
JOIN dbo.Companies c ON c.EmployerId=old.EmployerId AND c.Name=LTRIM(RTRIM(old.CompanyName)) WHERE j.CompanyId IS NULL;

-- Every historical submission with an available CV receives its own immutable snapshot.
CREATE TABLE dbo.__CvUpgrade_Submissions (ApplicationId int NOT NULL, OldCVId int NOT NULL, NewCVId int NOT NULL);
MERGE dbo.CVs AS target
USING (
    SELECT a.Id AS ApplicationId, cv.* FROM dbo.Applications a
    LEFT JOIN dbo.Candidates profile ON profile.UserId = a.CandidateId
    JOIN dbo.CVs cv ON cv.Id = COALESCE(a.CVId, profile.DefaultCVId)
    WHERE cv.FileName IS NOT NULL
) AS source ON 1 = 0
WHEN NOT MATCHED THEN INSERT
    (CandidateId,FileName,Title,UploadDate,TemplateId,FullName,JobTitle,Email,Phone,Address,Website,Summary,
     AvatarPath,Activities,Certifications,Interests,ThemeColor,HeaderColor,BackgroundColor,AiScore,AiFeedback,IsSnapshot)
VALUES
    (source.CandidateId,source.FileName,source.Title,source.UploadDate,source.TemplateId,source.FullName,source.JobTitle,
     source.Email,source.Phone,source.Address,source.Website,source.Summary,source.AvatarPath,source.Activities,
     source.Certifications,source.Interests,source.ThemeColor,source.HeaderColor,source.BackgroundColor,source.AiScore,source.AiFeedback,1)
OUTPUT source.ApplicationId,source.Id,inserted.Id INTO dbo.__CvUpgrade_Submissions;
INSERT dbo.Experiences (CVId,SortOrder,Company,Role,Duration,Description)
SELECT m.NewCVId,s.SortOrder,s.Company,s.Role,s.Duration,s.Description FROM dbo.Experiences s JOIN dbo.__CvUpgrade_Submissions m ON m.OldCVId=s.CVId;
INSERT dbo.Educations (CVId,SortOrder,School,Major,Duration)
SELECT m.NewCVId,s.SortOrder,s.School,s.Major,s.Duration FROM dbo.Educations s JOIN dbo.__CvUpgrade_Submissions m ON m.OldCVId=s.CVId;
INSERT dbo.ITSKills (CVId,SortOrder,SkillName,Description)
SELECT m.NewCVId,s.SortOrder,s.SkillName,s.Description FROM dbo.ITSKills s JOIN dbo.__CvUpgrade_Submissions m ON m.OldCVId=s.CVId;
INSERT dbo.Languages (CVId,SortOrder,LanguageName,Level)
SELECT m.NewCVId,s.SortOrder,s.LanguageName,s.Level FROM dbo.Languages s JOIN dbo.__CvUpgrade_Submissions m ON m.OldCVId=s.CVId;
INSERT dbo.[References] (CVId,SortOrder,Name,Position,Company,Phone)
SELECT m.NewCVId,s.SortOrder,s.Name,s.Position,s.Company,s.Phone FROM dbo.[References] s JOIN dbo.__CvUpgrade_Submissions m ON m.OldCVId=s.CVId;
UPDATE a SET CVId = m.NewCVId FROM dbo.Applications a JOIN dbo.__CvUpgrade_Submissions m ON m.ApplicationId=a.Id;

DROP TABLE dbo.__CvUpgrade_Submissions;
DROP TABLE dbo.__CvUpgrade_Map;
DROP TABLE dbo.__CvUpgrade_Experiences;
DROP TABLE dbo.__CvUpgrade_Educations;
DROP TABLE dbo.__CvUpgrade_ITSKills;
DROP TABLE dbo.__CvUpgrade_Languages;
DROP TABLE dbo.__CvUpgrade_References;
DROP TABLE dbo.__CvUpgrade_UserCVs;
DROP TABLE dbo.__CvUpgrade_CVs;
DROP TABLE dbo.__CvUpgrade_Candidates;
DROP TABLE dbo.__CvUpgrade_JobPosts;

UPDATE dbo.AspNetUsers SET LockoutEnabled = 1 WHERE LockoutEnabled = 0;
