DECLARE @candidate nvarchar(450) = N'd83b7113-a8f8-4035-99a7-a20dd400f6a4';
DECLARE @employer nvarchar(450) = N'c9284cfc-2dc1-460d-8521-396c0966a4f1';
INSERT dbo.Candidates (Email,FullName,Phone,Address,CVUrl)
VALUES (N'candidate@jobhub.com',N'Legacy Candidate',N'0123456789',N'Legacy address',N'/uploads/cvs/legacy-upload.pdf');
UPDATE dbo.AspNetUsers SET FullName=N'Old identity name', Gender=N'Nam', BirthDate='2000-01-01' WHERE Id=@candidate;
INSERT dbo.CVs (CandidateId,FileName,Title,UploadDate,FullName,Email,TemplateId,ExperiencesJson,EducationsJson,SkillsJson,AiScore)
VALUES (@candidate,N'/uploads/cvs/json-cv.pdf',N'JSON CV',SYSUTCDATETIME(),N'JSON Candidate',N'candidate@jobhub.com',1,
N'[{"Company":"JSON Company","Role":"Developer","Duration":"2024","Description":"Saved experience"}]',
N'[{"School":"JSON School","Major":"IT","Duration":"2023"}]',N'["SQL",{"skillName":"C#","description":"Advanced"}]',88);
DECLARE @cv int=SCOPE_IDENTITY();
INSERT dbo.UserCVs (TemplateId,FullName,JobTitle,Email,Phone,Address,Summary,Website,Interests,Activities,Certifications)
VALUES(1,N'Builder Candidate',N'Builder Developer',N'candidate@jobhub.com',N'0123456789',N'Builder address',N'Summary',N'https://example.test',N'Interests',N'Activities',N'Certification');
DECLARE @builder int=SCOPE_IDENTITY();
INSERT dbo.Experiences(EditCVViewModelId,Company,Role,Duration,Description) VALUES(@builder,N'Builder Company',N'Dev',N'2024',N'Work');
INSERT dbo.Educations(EditCVViewModelId,School,Major,Duration) VALUES(@builder,N'Builder School',N'IT',N'2023');
INSERT dbo.ITSKills(EditCVViewModelId,SkillName,Description) VALUES(@builder,N'C#',N'Good');
INSERT dbo.Languages(EditCVViewModelId,LanguageName,Level) VALUES(@builder,N'English',N'B2');
INSERT dbo.[References](EditCVViewModelId,Name,Position,Company,Phone) VALUES(@builder,N'Referee',N'Manager',N'Company',N'0123456789');
INSERT dbo.JobPosts(Title,Description,EmployerId,CompanyName,CategoryName,CreatedAt,IsApproved)
VALUES(N'Legacy job A',N'Description',@employer,N'Legacy Company',N'Legacy Category',SYSUTCDATETIME(),1);
DECLARE @jobA int=SCOPE_IDENTITY();
INSERT dbo.JobPosts(Title,Description,EmployerId,CompanyName,CategoryName,CreatedAt,IsApproved)
VALUES(N'Legacy job B',N'Description',@employer,N'Legacy Company',N'Legacy Category',SYSUTCDATETIME(),1);
DECLARE @jobB int=SCOPE_IDENTITY();
INSERT dbo.Applications(CandidateId,JobPostId,CVId,AppliedDate,Status) VALUES(@candidate,@jobA,@cv,SYSUTCDATETIME(),0),(@candidate,@jobB,NULL,SYSUTCDATETIME(),0);
INSERT dbo.SavedJobs(CandidateId,JobPostId,SavedDate) VALUES(@candidate,@jobA,SYSUTCDATETIME());
INSERT dbo.ChatMessages(SenderId,ReceiverId,Content,Timestamp,IsRead) VALUES(@candidate,@employer,N'Historical message',SYSUTCDATETIME(),0);
