SET NOCOUNT ON;

-- Fail before any destructive change if legacy ownership cannot be resolved safely.
IF EXISTS (SELECT 1 FROM dbo.Candidates c WHERE
    (SELECT COUNT(*) FROM dbo.AspNetUsers u WHERE UPPER(u.Email) = UPPER(c.Email)) <> 1)
    THROW 51000, N'Khong the xac dinh tai khoan cua ho so cu. Hay sua email ho so truoc khi chay migration.', 1;
IF EXISTS (SELECT 1 FROM dbo.UserCVs c WHERE
    (SELECT COUNT(*) FROM dbo.AspNetUsers u WHERE UPPER(u.Email) = UPPER(c.Email)) <> 1)
    THROW 51000, N'CV cu chua co email tai khoan hop le. Hay gan chu so huu truoc khi chay migration.', 1;
IF EXISTS (SELECT Email FROM dbo.Candidates GROUP BY UPPER(Email), Email HAVING COUNT(*) > 1)
    THROW 51000, N'Ho so ung vien bi trung email. Can hop nhat truoc khi nang cap.', 1;
IF EXISTS (SELECT 1 FROM dbo.Candidates WHERE NULLIF(MaritalStatus,'') IS NOT NULL
    OR NULLIF(Industry,'') IS NOT NULL OR NULLIF(DesiredLocation,'') IS NOT NULL OR NULLIF(ExpectedSalary,'') IS NOT NULL)
    THROW 51000, N'Cot ho so cu chua du lieu can luu tru. Hay xuat du lieu truoc khi bo cot.', 1;
IF EXISTS (SELECT CandidateId, JobPostId FROM dbo.Applications GROUP BY CandidateId, JobPostId HAVING COUNT(*) > 1)
    THROW 51000, N'Co don ung tuyen trung. Can hop nhat truoc khi tao rang buoc duy nhat.', 1;
IF EXISTS (SELECT CandidateId, JobPostId FROM dbo.SavedJobs GROUP BY CandidateId, JobPostId HAVING COUNT(*) > 1)
    THROW 51000, N'Co viec lam da luu trung. Can hop nhat truoc khi tao rang buoc duy nhat.', 1;
IF EXISTS (SELECT 1 FROM dbo.CVs WHERE AiScore < 0 OR AiScore > 100)
    THROW 51000, N'Co diem AI ngoai khoang 0..100. Can kiem tra du lieu truoc khi nang cap.', 1;
IF EXISTS (SELECT 1 FROM dbo.Applications a JOIN dbo.CVs c ON c.Id=a.CVId WHERE a.CandidateId <> c.CandidateId)
    THROW 51000, N'Co don ung tuyen gan CV cua tai khoan khac. Can sua chu so huu truoc khi nang cap.', 1;
IF EXISTS (SELECT 1 FROM dbo.CVs WHERE
    (NULLIF(ExperiencesJson,'') IS NOT NULL AND ISJSON(ExperiencesJson) <> 1)
    OR (NULLIF(EducationsJson,'') IS NOT NULL AND ISJSON(EducationsJson) <> 1)
    OR (NULLIF(SkillsJson,'') IS NOT NULL AND ISJSON(SkillsJson) <> 1))
    THROW 51000, N'Du lieu JSON CV cu khong hop le. Hay sua truoc khi nang cap.', 1;

-- Staging is in the same migration transaction. It is removed only after all data is copied.
SELECT * INTO dbo.__CvUpgrade_Candidates FROM dbo.Candidates;
SELECT * INTO dbo.__CvUpgrade_CVs FROM dbo.CVs;
SELECT * INTO dbo.__CvUpgrade_UserCVs FROM dbo.UserCVs;
SELECT * INTO dbo.__CvUpgrade_Experiences FROM dbo.Experiences;
SELECT * INTO dbo.__CvUpgrade_Educations FROM dbo.Educations;
SELECT * INTO dbo.__CvUpgrade_ITSKills FROM dbo.ITSKills;
SELECT * INTO dbo.__CvUpgrade_Languages FROM dbo.Languages;
SELECT * INTO dbo.__CvUpgrade_References FROM dbo.[References];
SELECT Id, EmployerId, CompanyName, CompanyId, CategoryName, CategoryId INTO dbo.__CvUpgrade_JobPosts FROM dbo.JobPosts;

DELETE FROM dbo.Experiences;
DELETE FROM dbo.Educations;
DELETE FROM dbo.ITSKills;
DELETE FROM dbo.Languages;
DELETE FROM dbo.[References];
DELETE FROM dbo.Candidates;

-- Invalid old template IDs were never usable by the editor; keep the CV as an uploaded document.
UPDATE c SET TemplateId = NULL FROM dbo.CVs c
WHERE TemplateId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CVTemplates t WHERE t.Id = c.TemplateId);

-- Normalize legacy file paths into storage keys without changing the file content.
UPDATE dbo.CVs SET FileName = RIGHT(REPLACE(FileName, '\', '/'),
    CHARINDEX('/', REVERSE('/' + REPLACE(FileName, '\', '/'))) - 1)
WHERE FileName IS NOT NULL;
UPDATE dbo.CVs SET FileName = NULL WHERE FileName = 'default-cv.pdf';
