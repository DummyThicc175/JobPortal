using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static PortalChecks;

sealed class FakeOllamaHandler : HttpMessageHandler
{
    public int Calls;
    public string LastContext = "";
    public bool InvalidOutput;
    public bool Unavailable;
    public bool EmptyContent;
    public string? LastEvaluation;
    public string[] LastEvidenceOptions = [];
    public bool WrongEvidence;
    public bool InvalidQuestion;
    public TaskCompletionSource<bool>? ChoPhepTraLoi;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        LastContext = body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
        using var context = JsonDocument.Parse(LastContext);
        var number = context.RootElement.GetProperty("turn").GetInt32();
        var grading = context.RootElement.GetProperty("task").GetString() == "evaluate";
        if (grading)
        {
            LastEvaluation = LastContext;
            LastEvidenceOptions = body.RootElement.GetProperty("format").GetProperty("properties").GetProperty("evidence").GetProperty("enum")
                .EnumerateArray().Select(value => value.GetString()!).ToArray();
        }
        if (grading && ChoPhepTraLoi != null) await ChoPhepTraLoi.Task.WaitAsync(cancellationToken);
        var result = JsonSerializer.Serialize(new
        {
            questionNumber = number,
            evidence = WrongEvidence ? "Nội dung ứng viên chưa nói" : grading ? LastEvidenceOptions[0] : "",
            nextQuestion = number > 5 || InvalidQuestion ? "" : $"Câu hỏi {number}: Bạn tổ chức truy vấn SQL như thế nào? <script>bad()</script>",
            nextExpectedPoints = number > 5 ? "" : "Giải thích chỉ mục và kiểm tra kế hoạch thực thi.",
            score = InvalidOutput ? 999 : 75,
            feedback = grading ? $"Nhận xét câu {number}: Bạn đã đề cập SQL." : "",
            improvement = grading ? $"Nên bổ sung ví dụ và kết quả đo ở câu {number}." : ""
        });
        return new HttpResponseMessage(Unavailable ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = JsonContent.Create(new { done = true, done_reason = "stop", message = new { content = EmptyContent ? null : result } }) };
    }
}

static class AiInterviewChecks
{
    public static async Task ChayKiemTraAsync(PortalFactory factory, Actor candidate, Actor other, Actor employer)
    {
        int applicationId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cv = new CV { CandidateId = candidate.Id, IsSnapshot = true, Title = "CV luyện tập", Summary = "Kinh nghiệm C# SQL tối ưu truy vấn và phát triển ứng dụng web." };
            var job = new JobPost { Title = "Kỹ sư dữ liệu", Description = "Yêu cầu SQL indexing và phân tích dữ liệu.", EmployerId = employer.Id,
                Category = new Category { Name = "Chuyên ngành dữ liệu AI" }, LocationId = 1, IsApproved = true };
            var application = new Application { JobPost = job, CandidateId = candidate.Id, CV = cv };
            db.Add(application); await db.SaveChangesAsync(); applicationId = application.Id;
        }
        await TaiTrangAsync(candidate.Client, "/Candidate/AiInterview", "Luyện phỏng vấn AI", "Kỹ sư dữ liệu");
        using (var missingCsrf = await candidate.Client.PostAsync("/Candidate/AiInterview/Start", new FormUrlEncodedContent(new Dictionary<string,string>{{"applicationId",applicationId.ToString()}})))
            KiemTra(missingCsrf.StatusCode == HttpStatusCode.BadRequest, "AI start requires CSRF");
        using (var forbidden = await GuiBieuMauAsync(other, "/Candidate/AiInterview/Start", "/Candidate/AiInterview", new() { ["applicationId"] = applicationId.ToString() }))
            KiemTra(forbidden.StatusCode == HttpStatusCode.NotFound, "Other candidate cannot start a private application");
        KiemTra(factory.Ollama.Calls == 0, "Access is checked before invoking local AI");
        string path;
        using (var start = await GuiBieuMauAsync(candidate, "/Candidate/AiInterview/Start", "/Candidate/AiInterview", new() { ["applicationId"] = applicationId.ToString() }))
        { KiemTra(start.StatusCode == HttpStatusCode.Redirect, "AI session starts: " + start.StatusCode + ": " + await start.Content.ReadAsStringAsync()); path = start.Headers.Location!.ToString(); }
        var id = Guid.Parse(path.Split('/').Last());
        KiemTra(factory.Ollama.LastContext.Contains("Chuyên ngành dữ liệu AI") && !factory.Ollama.LastContext.Contains("\\u"), "Prompt keeps readable Vietnamese rather than literal Unicode escapes");
        using (var prompt = JsonDocument.Parse(factory.Ollama.LastContext))
        {
            KiemTra(prompt.RootElement.GetProperty("job").GetProperty("specialty").GetString() == "Chuyên ngành dữ liệu AI", "Server includes the job specialty");
            KiemTra(prompt.RootElement.GetProperty("job").GetProperty("requirements").GetString()!.Contains("indexing"), "Server includes job requirements");
            KiemTra(prompt.RootElement.GetProperty("cv").GetString()!.Contains("C# SQL"), "Server includes submitted CV content");
        }
        using (var repeat = await GuiBieuMauAsync(candidate, "/Candidate/AiInterview/Start", "/Candidate/AiInterview", new() { ["applicationId"] = applicationId.ToString() }))
            KiemTra(repeat.Headers.Location!.ToString() == path && factory.Ollama.Calls == 1, "Repeated start resumes without another AI call");
        var html = await TaiTrangAsync(candidate.Client, path, "Bắt đầu ghi âm");
        KiemTra(!html.Contains("<script>bad()") && !WebUtility.HtmlDecode(html).Contains("Giải thích chỉ mục"), "Question is escaped and hidden rubric is not exposed");
        foreach (var address in new[] { path, $"/Candidate/AiInterviewReport/Details/{id}" })
            KiemTra((await other.Client.GetAsync(address)).StatusCode == HttpStatusCode.NotFound, "Other candidate cannot read AI history");
        KiemTra((await employer.Client.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Employer cannot enter practice sessions");
        var csrf = LayMaChongGiaMao(html);
        const string cauTraLoi = "Tôi dùng SQL indexing và kiểm tra execution plan.";
        string LayCauTraLoiTheoLuot(int number) => $"{cauTraLoi} Câu trả lời riêng số {number}.";
        async Task<HttpResponseMessage> GuiTraLoiAsync(Actor actor, int number, string? answer = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/Candidate/AiInterviewAnswer/Submit/{id}")
            { Content = JsonContent.Create(new { number, answer = answer ?? cauTraLoi }) };
            request.Headers.Add("X-CSRF-TOKEN", actor == candidate ? csrf : LayMaChongGiaMao(await TaiTrangAsync(actor.Client, "/Candidate/AiInterview")));
            return await actor.Client.SendAsync(request);
        }
        async Task<JsonDocument> ChoKetQuaTraLoiAsync(string statusUrl)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                using var response = await candidate.Client.GetAsync(statusUrl);
                KiemTra(response.StatusCode == HttpStatusCode.OK && response.Headers.CacheControl?.NoStore == true, "Answer job polling is private and cannot be cached");
                var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var status = result.RootElement.GetProperty("status").GetString();
                if (status is "completed" or "failed") return result;
                KiemTra(status is "pending" or "running", "Answer job exposes a known processing state");
                result.Dispose();
                await Task.Delay(20);
            }
            throw new InvalidOperationException("Background answer processing did not finish.");
        }
        using (var missingCsrf = await candidate.Client.PostAsJsonAsync($"/Candidate/AiInterviewAnswer/Submit/{id}", new { number = 1, answer = cauTraLoi }))
            KiemTra(missingCsrf.StatusCode == HttpStatusCode.BadRequest, "Answer queue requires CSRF before invoking AI");
        using (var wrongOwner = await GuiTraLoiAsync(other, 1)) KiemTra(wrongOwner.StatusCode == HttpStatusCode.NotFound, "Cannot answer another person's session");
        using (var invalid = await GuiTraLoiAsync(candidate, 0)) KiemTra(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid turn number rejected");
        using (var invalid = await GuiTraLoiAsync(candidate, 1, " ")) KiemTra(invalid.StatusCode == HttpStatusCode.BadRequest, "Empty answer is rejected before queueing");
        factory.Ollama.InvalidOutput = true;
        using (var invalid = await GuiTraLoiAsync(candidate, 1))
        {
            KiemTra(invalid.StatusCode == HttpStatusCode.Accepted, "Answer submission returns immediately before slow AI evaluation");
            using var queued = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            using var result = await ChoKetQuaTraLoiAsync(queued.RootElement.GetProperty("statusUrl").GetString()!);
            KiemTra(result.RootElement.GetProperty("status").GetString() == "failed" && result.RootElement.GetProperty("statusCode").GetInt32() == 502,
                "Out of range AI scores become a controlled background failure");
            KiemTra(result.RootElement.GetProperty("completed").ValueKind == JsonValueKind.Null && result.RootElement.GetProperty("url").ValueKind == JsonValueKind.Null,
                "Failed answer jobs cannot redirect as if the answer was saved");
        }
        using (var scope = factory.Services.CreateScope())
            KiemTra(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewTurns.CountAsync(t => t.SessionId == id && t.AnsweredAt != null) == 0, "Failed AI evaluation does not commit an answer");
        factory.Ollama.InvalidOutput = false;
        factory.Ollama.WrongEvidence = true;
        try
        {
            using var scope = factory.Services.CreateScope();
            var session = await scope.ServiceProvider.GetRequiredService<AiInterviewService>().LayPhienAsync(id, candidate.Id, default);
            await scope.ServiceProvider.GetRequiredService<OllamaInterviewClient>().TaoLuotAsync(session!, "Tôi dùng SQL indexing.", default);
            KiemTra(false, "AI accepted fabricated evidence");
        }
        catch (AiInterviewException ex) { KiemTra(ex.StatusCode == 502, "AI cannot quote words absent from the candidate answer"); }
        finally { factory.Ollama.WrongEvidence = false; }
        using (var scope = factory.Services.CreateScope())
        {
            var session = await scope.ServiceProvider.GetRequiredService<AiInterviewService>().LayPhienAsync(id, candidate.Id, default);
            var client = scope.ServiceProvider.GetRequiredService<OllamaInterviewClient>();
            foreach (var length in new[] { 202, 4000 })
            {
                var answer = TaoCauTraLoiGiaLap(length);
                var result = await client.TaoLuotAsync(session!, answer, default);
                using var evaluation = JsonDocument.Parse(factory.Ollama.LastEvaluation!);
                var grade = evaluation.RootElement;
                var choices = grade.GetProperty("evidenceOptions").EnumerateArray().Select(value => value.GetString()!).ToArray();
                KiemTra(answer.Length == length && grade.GetProperty("current").GetProperty("answer").GetString() == answer,
                    "Grading preserves the complete synthetic Unicode answer at " + length + " characters");
                KiemTra(choices.Length is > 0 and <= 8 && choices.All(value => value.Length is > 0 and <= 80 && answer.Contains(value, StringComparison.Ordinal)),
                    "Evidence choices are bounded exact excerpts, including for long Unicode answers");
                KiemTra(choices.Any(value => answer.EndsWith(value, StringComparison.Ordinal)), "Evidence choices also cover the end of the answer");
                KiemTra(factory.Ollama.LastEvidenceOptions.SequenceEqual(choices), "JSON schema restricts evidence to the exact excerpts supplied in grading context");
                KiemTra(!grade.TryGetProperty("job", out _) && !grade.TryGetProperty("cv", out _) && !grade.TryGetProperty("goal", out _) && !grade.TryGetProperty("previousQuestions", out _),
                    "Evidence selection never reintroduces unrelated question goals, CV or job context");
                KiemTra(result.Score == 75 && !string.IsNullOrWhiteSpace(result.Feedback), "Schema-selected evidence allows a valid Unicode answer to be graded");
            }
        }
        factory.Ollama.InvalidQuestion = true;
        try
        {
            using var invalid = await GuiTraLoiAsync(candidate, 1);
            KiemTra(invalid.StatusCode == HttpStatusCode.Accepted, "Retry after failed grading creates a new answer job");
            using var queued = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            using var result = await ChoKetQuaTraLoiAsync(queued.RootElement.GetProperty("statusUrl").GetString()!);
            KiemTra(result.RootElement.GetProperty("status").GetString() == "failed" && result.RootElement.GetProperty("statusCode").GetInt32() == 502,
                "Failure to generate the next question is reported as a controlled job failure");
            using var scope = factory.Services.CreateScope();
            var turns = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewTurns.Where(t => t.SessionId == id).ToListAsync();
            KiemTra(turns.Count == 1 && turns[0].AnsweredAt == null && turns[0].Answer == null && turns[0].Score == null,
                "Successful grading followed by failed question generation cannot commit half an answer");
        }
        finally { factory.Ollama.InvalidQuestion = false; }
        for (var number = 1; number <= 5; number++)
        {
            var callsBefore = factory.Ollama.Calls;
            if (number == 1) factory.Ollama.ChoPhepTraLoi = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using var submission = await GuiTraLoiAsync(candidate, number, LayCauTraLoiTheoLuot(number));
            KiemTra(submission.StatusCode == HttpStatusCode.Accepted, "Each validated turn is queued without waiting for AI");
            using var queued = JsonDocument.Parse(await submission.Content.ReadAsStringAsync());
            var statusUrl = queued.RootElement.GetProperty("statusUrl").GetString()!;
            if (number == 1)
            {
                try
                {
                    using var otherStatus = await other.Client.GetAsync(statusUrl);
                    KiemTra(otherStatus.StatusCode == HttpStatusCode.NotFound, "Another candidate cannot read private answer processing results");
                    using var employerStatus = await employer.Client.GetAsync(statusUrl);
                    KiemTra(employerStatus.StatusCode == HttpStatusCode.Redirect, "Employer cannot poll a candidate answer job");
                    using var wrongSession = await candidate.Client.GetAsync(statusUrl.Replace(id.ToString(), Guid.NewGuid().ToString()));
                    KiemTra(wrongSession.StatusCode == HttpStatusCode.NotFound, "Answer job is bound to its exact interview session");
                    using var pending = await candidate.Client.GetAsync(statusUrl);
                    using var state = JsonDocument.Parse(await pending.Content.ReadAsStringAsync());
                    KiemTra(pending.Headers.CacheControl?.NoStore == true && (state.RootElement.GetProperty("status").GetString() is "pending" or "running"),
                        "Slow AI answers remain pollable without caching");
                    using var duplicate = await GuiTraLoiAsync(candidate, 1, LayCauTraLoiTheoLuot(1));
                    using var duplicateBody = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
                    KiemTra(duplicate.StatusCode == HttpStatusCode.Accepted && duplicateBody.RootElement.GetProperty("jobId").GetGuid() == queued.RootElement.GetProperty("jobId").GetGuid(),
                        "Repeated submission while processing resumes the same job");
                    using var changed = await GuiTraLoiAsync(candidate, 1, "Đây là câu trả lời khác không được ghi đè.");
                    KiemTra(changed.StatusCode == HttpStatusCode.Conflict, "Changed answer cannot silently replace an answer already being evaluated");
                    KiemTra(factory.Ollama.Calls <= callsBefore + 1, "Pending duplicate cannot invoke a second AI evaluation");
                }
                finally { factory.Ollama.ChoPhepTraLoi?.TrySetResult(true); }
            }
            using var body = await ChoKetQuaTraLoiAsync(statusUrl);
            factory.Ollama.ChoPhepTraLoi = null;
            KiemTra(body.RootElement.GetProperty("status").GetString() == "completed", "Each validated turn commits successfully in the worker");
            KiemTra(body.RootElement.GetProperty("completed").GetBoolean() == (number == 5), "Only the fifth answer completes the session");
            KiemTra(body.RootElement.GetProperty("url").GetString()!.Contains(number == 5 ? "AiInterviewReport/Details" : "AiInterview/Room"), "Completed job returns the correct next page");
            using (var evaluation = JsonDocument.Parse(factory.Ollama.LastEvaluation!))
            {
                var grade = evaluation.RootElement;
                KiemTra(grade.GetProperty("turn").GetInt32() == number
                    && grade.GetProperty("current").GetProperty("question").GetString()!.Contains($"Câu hỏi {number}:")
                    && grade.GetProperty("current").GetProperty("answer").GetString() == LayCauTraLoiTheoLuot(number),
                    "The model receives the matching question and distinct answer for turn " + number);
            }
            if (number == 1)
            {
                using var evaluation = JsonDocument.Parse(factory.Ollama.LastEvaluation!);
                var grade = evaluation.RootElement;
                KiemTra(grade.GetProperty("current").GetProperty("question").GetString()!.Contains("Câu hỏi 1"), "Grading is pinned to the question being answered");
                KiemTra(!grade.TryGetProperty("job", out _) && !grade.TryGetProperty("cv", out _) && !grade.TryGetProperty("goal", out _) && !grade.TryGetProperty("previousQuestions", out _), "Grading never receives next question goals, CV or unrelated job context");
                var calls = factory.Ollama.Calls;
                using var duplicate = await GuiTraLoiAsync(candidate, 1);
                KiemTra(duplicate.StatusCode == HttpStatusCode.Conflict && factory.Ollama.Calls == calls, "Duplicate answer does not invoke AI");
                AiInterviewTurn answered;
                using (var scope = factory.Services.CreateScope())
                    answered = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewTurns.AsNoTracking()
                        .SingleAsync(t => t.SessionId == id && t.Number == 1);
                using (var resume = await GuiBieuMauAsync(candidate, "/Candidate/AiInterview/Start", "/Candidate/AiInterview", new() { ["applicationId"] = applicationId.ToString() }))
                    KiemTra(resume.StatusCode == HttpStatusCode.Redirect && resume.Headers.Location!.ToString() == path && factory.Ollama.Calls == calls,
                        "Starting again after an answer resumes the existing unfinished session without another AI call");
                using (var scope = factory.Services.CreateScope())
                {
                    var saved = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewTurns.AsNoTracking()
                        .SingleAsync(t => t.SessionId == id && t.Number == 1);
                    KiemTra(saved.Answer == answered.Answer && saved.Feedback == answered.Feedback && saved.Improvement == answered.Improvement
                        && saved.Score == answered.Score && saved.AnsweredAt == answered.AnsweredAt && saved.Version == answered.Version,
                        "Resuming preserves the saved answer and its original grading instead of silently regrading history");
                }
            }
        }
        var report = WebUtility.HtmlDecode(await TaiTrangAsync(candidate.Client, $"/Candidate/AiInterviewReport/Details/{id}", "75", "SQL indexing", "Nên bổ sung"));
        var cards = System.Text.RegularExpressions.Regex.Matches(report, @"<article\b[^>]*>[\s\S]*?</article>")
            .Select(match => match.Value).ToArray();
        KiemTra(cards.Length == 5, "The report renders exactly one card for each answered turn");
        for (var number = 1; number <= 5; number++)
        {
            var card = cards[number - 1];
            KiemTra(card.Contains($"Câu {number}</span>") && card.Contains($"Câu hỏi {number}:")
                && card.Contains(LayCauTraLoiTheoLuot(number)) && card.Contains($"Nhận xét câu {number}: Bạn đã đề cập SQL.")
                && card.Contains($"Nên bổ sung ví dụ và kết quả đo ở câu {number}."),
                "Report card keeps the question, answer, feedback and improvement together for turn " + number);
            KiemTra(Enumerable.Range(1, 5).Where(otherNumber => otherNumber != number)
                .All(otherNumber => !card.Contains(LayCauTraLoiTheoLuot(otherNumber)) && !card.Contains($"Nhận xét câu {otherNumber}:")
                    && !card.Contains($"kết quả đo ở câu {otherNumber}.")),
                "Report card cannot include another turn's answer or grading for turn " + number);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var session = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewSessions.Include(s => s.Turns).SingleAsync(s => s.Id == id);
            KiemTra(session.EndedAt != null && session.Turns.Count == 5 && session.Turns.All(t => t.Score == 75 && t.Answer == LayCauTraLoiTheoLuot(t.Number)),
                "All five distinct original answers and scores persist without duplicate overwrites");
            KiemTra(session.Turns.All(t => t.Question.Contains($"Câu hỏi {t.Number}:")
                && t.Feedback == $"Nhận xét câu {t.Number}: Bạn đã đề cập SQL."
                && t.Improvement == $"Nên bổ sung ví dụ và kết quả đo ở câu {t.Number}."),
                "Database binds each evaluation to its original question number rather than the generated next question");
        }
        KiemTra((await candidate.Client.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Completed room redirects to its report");
        KiemTraAmThanh(new byte[32044], 400);
        var valid = TaoBanGhiWav(); InterviewSpeechService.KiemTraAmThanh(valid);
        KiemTra(true, "Canonical browser PCM16 WAV is accepted");
        Array.Clear(valid, 44, valid.Length - 44); KiemTraAmThanh(valid, 422);
        using var audio = new MultipartFormDataContent(); audio.Add(new ByteArrayContent(TaoBanGhiWav()), "audio", "test.wav");
        using var speechRequest = new HttpRequestMessage(HttpMethod.Post, $"/Candidate/AiInterviewSpeech/Transcribe/{id}") { Content = audio };
        speechRequest.Headers.Add("X-CSRF-TOKEN", LayMaChongGiaMao(await TaiTrangAsync(other.Client, "/Candidate/AiInterview")));
        using var speechResponse = await other.Client.SendAsync(speechRequest);
        KiemTra(speechResponse.StatusCode == HttpStatusCode.NotFound, "Transcription checks ownership before reading audio");
        factory.Ollama.Unavailable = true;
        using (var unavailable = await GuiBieuMauAsync(candidate, "/Candidate/AiInterview/Start", "/Candidate/AiInterview", new() { ["applicationId"] = applicationId.ToString() }))
            KiemTra(unavailable.StatusCode == HttpStatusCode.Redirect && unavailable.Headers.Location!.ToString().EndsWith("AiInterview"), "Unavailable model returns candidate to a useful error page");
        using (var scope = factory.Services.CreateScope())
            KiemTra(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewSessions.CountAsync(s => s.ApplicationId == applicationId) == 1, "Unavailable model does not create a fake or empty interview");
        factory.Ollama.Unavailable = false;
        Guid earlyId;
        using (var scope = factory.Services.CreateScope())
            earlyId = await scope.ServiceProvider.GetRequiredService<AiInterviewService>().BatDauAsync(candidate.Id, applicationId, default);
        KiemTra(earlyId != id, "A new practice session can start after completion");
        using (var scope = factory.Services.CreateScope())
        {
            var fresh = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiInterviewSessions.Include(s => s.Turns).SingleAsync(s => s.Id == earlyId);
            KiemTra(fresh.EndedAt == null && fresh.Turns.Count == 1 && fresh.Turns.Single().Number == 1,
                "A genuinely new practice session starts with only its first question");
            var turn = fresh.Turns.Single();
            KiemTra(turn.Answer == null && turn.Feedback == null && turn.Improvement == null && turn.AnsweredAt == null && turn.Score == null,
                "A new session cannot inherit saved answers or grading from the previous session");
        }
        var earlyPath = $"/Candidate/AiInterview/Room/{earlyId}";
        async Task<HttpResponseMessage> GuiAmThanhAsync(byte[] bytes)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "audio", "test.wav");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/Candidate/AiInterviewSpeech/Transcribe/{earlyId}") { Content = form };
            request.Headers.Add("X-CSRF-TOKEN", csrf);
            return await candidate.Client.SendAsync(request);
        }
        using (var invalidAudio = await GuiAmThanhAsync(new byte[32044]))
            KiemTra(invalidAudio.StatusCode == HttpStatusCode.BadRequest, "Speech upload validates WAV before queueing");
        using (var queued = await GuiAmThanhAsync(TaoBanGhiWav()))
        {
            KiemTra(queued.StatusCode == HttpStatusCode.Accepted, "Speech returns a short 202 response for slow inference");
            using var content = JsonDocument.Parse(await queued.Content.ReadAsStringAsync());
            var statusUrl = content.RootElement.GetProperty("statusUrl").GetString()!;
            using var pending = await candidate.Client.GetAsync(statusUrl);
            KiemTra(pending.IsSuccessStatusCode && pending.Headers.CacheControl?.NoStore == true, "Owner can poll a non-cacheable speech status");
            using var privateResult = await other.Client.GetAsync(statusUrl);
            KiemTra(privateResult.StatusCode == HttpStatusCode.NotFound, "Another candidate cannot read speech job results");
            using var wrongSession = await candidate.Client.GetAsync(statusUrl.Replace(earlyId.ToString(), Guid.NewGuid().ToString()));
            KiemTra(wrongSession.StatusCode == HttpStatusCode.NotFound, "Speech job is bound to its exact interview session");
            using var busy = await GuiAmThanhAsync(TaoBanGhiWav());
            KiemTra(busy.StatusCode == HttpStatusCode.TooManyRequests, "A second recording cannot overload the speech worker");
            factory.Speech.Result.SetResult("mình từng làm 1 dự án về một website phỏng vấn");
            var completed = false;
            for (var attempt = 0; attempt < 50; attempt++)
            {
                using var response = await candidate.Client.GetAsync(statusUrl);
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (result.RootElement.GetProperty("status").GetString() == "completed")
                {
                    KiemTra(result.RootElement.GetProperty("text").GetString()!.Contains("website phỏng vấn") && !result.RootElement.TryGetProperty("audio", out _), "Speech polling returns transcript without exposing recorded audio");
                    completed = true; break;
                }
                await Task.Delay(20);
            }
            KiemTra(completed, "Background speech completes independently of the upload request");
        }
        using (var otherEnd = await GuiBieuMauAsync(other, $"/Candidate/AiInterview/End/{earlyId}", "/Candidate/AiInterview"))
            KiemTra(otherEnd.StatusCode == HttpStatusCode.NotFound, "Another candidate cannot end a private practice session");
        using (var earlyEnd = await GuiBieuMauAsync(candidate, $"/Candidate/AiInterview/End/{earlyId}", earlyPath))
            KiemTra(earlyEnd.StatusCode == HttpStatusCode.Redirect, "Candidate can finish early without inventing an answer");
        var emptyReport = await TaiTrangAsync(candidate.Client, $"/Candidate/AiInterviewReport/Details/{earlyId}", "Chưa có câu trả lời");
        KiemTra(!WebUtility.HtmlDecode(emptyReport).Contains("Giải thích chỉ mục"), "Unanswered question rubric is not revealed in a partial report");
        factory.Ollama.EmptyContent = true;
        try
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<OllamaInterviewClient>().TaoLuotAsync(new AiInterviewSession { JobTitle = "C#" }, null, default);
            KiemTra(false, "Empty upstream message accepted");
        }
        catch (AiInterviewException ex) { KiemTra(ex.StatusCode == 502, "Null model content is a controlled retryable error"); }
        finally { factory.Ollama.EmptyContent = false; }
    }

    private static string TaoCauTraLoiGiaLap(int length)
    {
        const string repeated = "Tôi viết module tuyển dụng bằng C#, SQL và ha\u0300m kiểm tra quyền; ứng viên chỉ đọc hồ sơ của mình. ";
        const string ending = "Phần cuối: kiểm thử quyền truy cập và đối chiếu kết quả.";
        var text = new StringBuilder();
        while (text.Length < length - ending.Length) text.Append(repeated);
        return text.ToString()[..(length - ending.Length)] + ending;
    }

    private static void KiemTraAmThanh(byte[] bytes, int status)
    {
        try { InterviewSpeechService.KiemTraAmThanh(bytes); KiemTra(false, "Invalid recording accepted"); }
        catch (AiInterviewException ex) { KiemTra(ex.StatusCode == status, "Invalid/silent audio gets a useful response"); }
    }

    private static byte[] TaoBanGhiWav()
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(32036); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(16000); writer.Write(32000); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(32000);
        for (var i = 0; i < 16000; i++) writer.Write((short)(Math.Sin(i * .1) * 3000));
        return stream.ToArray();
    }
}
