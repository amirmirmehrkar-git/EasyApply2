using Application.CQRS.Suggest_CQRS;
using Domain.Entities;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using System.Linq.Dynamic.Core;
using WebAPI.CustomFilters;
using WebAPI.ViewModels;
using WebUI.Services;

namespace WebAPI.Controllers;

public class SuggestController : ApiControllerBase
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IConfiguration configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _openAiKey;
    private readonly string _chatModel;
    protected readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager;

    public SuggestController(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> UserManager,
        IHttpClientFactory httpClientFactory, IConfiguration config, IWebHostEnvironment hostingEnvironment)
    {
        _httpClientFactory = httpClientFactory;
        _openAiKey = config.GetConnectionString("OpenAiKey");
        _chatModel = config["OPENAI_CHAT_MODEL"] ?? "gpt-4o-mini";
        configuration = config;
        userManager = UserManager;
        _hostingEnvironment = hostingEnvironment;
    }

    [CustomAuthorize("Admin,User")]
    [HttpPost("AddResponse")]
    public async Task<IActionResult> AddResponse(CreateResponseCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = CurrentUserService.GetUserId(HttpContext);

            var entity = new ResponseSuggest();
            entity.ResponseText = command.ResponseText;
            entity.SuggestId = command.SuggestId;

            var ai = new AI(_httpClientFactory, _chatModel, _openAiKey);
            var ret = await ai.GenerateResponse(command.ResponseText);
            entity.AiResult = ret;

            DbContext.ResponseSuggests.Add(entity);

            await DbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpPost("UpdateQuestions")]
    public async Task<IActionResult> AddQuestions(UpdateQuestionsCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var li = await DbContext.Questions
                    .Where(m => m.SuggestId == command.SuggestId)
                    .ToListAsync();

            foreach (var item in command.Questions)
            {
                var q = li.SingleOrDefault(m => m.Id == item.Id);
                q.Answer = item.Answer;
            }

            await DbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpGet("GetAllSentSuggestByUserId")]
    public async Task<IActionResult> GetAllSentSuggestByUserId()
    {
        try
        {
            var userId = CurrentUserService.GetUserId(HttpContext);

            var query = await DbContext.Suggests.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == userId)
                .Where(m => m.SuggestType == Domain.Enums.SuggestType.Sent)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Company,
                    m.About,
                    m.Description,

                    Responses = (m.Responses == null || m.Responses.Count() == 0) ? null
                    : m.Responses.Select(r => new
                    {
                        r.ResponseText,
                        r.AiResult,
                        CreateDate = r.CreateDate.Value.ToLongDateString(),
                    }).ToList(),

                    CreateDate = m.CreateDate.Value.ToShortDateString(),
                })
                .ToListAsync();

            return Ok(query);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpGet("GetAllWaitingJobsByUserId")]
    public async Task<IActionResult> GetAllWaitingJobsByUserId()
    {
        try
        {
            var userId = CurrentUserService.GetUserId(HttpContext);

            var query = await DbContext.Suggests.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == userId)
                .Where(m => m.SuggestType == Domain.Enums.SuggestType.NewQuestions)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Company,
                    m.About,
                    m.Description,

                    Questions = (m.Questions == null || m.Questions.Count() == 0) ? null
                    : m.Questions.Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.InputType,
                        r.Options,
                        r.Answer,
                    }).ToList(),

                    CreateDate = m.CreateDate.Value.ToShortDateString(),
                })
                .ToListAsync();

            return Ok(query);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpGet("GetAllNewJobsByUserId")]
    public async Task<IActionResult> GetAllNewJobsByUserId()
    {
        try
        {
            var userId = CurrentUserService.GetUserId(HttpContext);
            var jobs = new List<JobInfoViewModel>();

            var neededJob = DbContext.Users.AsNoTracking()
                .Where(m => m.Id == userId)
                .SingleOrDefault()
                .NeededJob;
            string jobTitle = neededJob;
            var playwrightPath = configuration.GetConnectionString("PlaywrightPath");

            var apply = new Apply(_httpClientFactory, _chatModel, _openAiKey, jobTitle, playwrightPath);

            #region دریافت آگهی های لینکدین
            var linkedins = await apply.GetLinkedInJobs();
            jobs.AddRange(linkedins);
            #endregion

            #region دریافت آگهی های زینگ
            var xings = await apply.GetXingJobs();
            jobs.AddRange(xings);
            #endregion

            #region دریافت آگهی های استپ استون
            var stepStones = await apply.GetStepStoneJobs();
            jobs.AddRange(stepStones);
            #endregion

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpPost("SendApplication")]
    public async Task<IActionResult> SendApplication(SendApplicationCommand command)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = CurrentUserService.GetUserId(HttpContext);
            var user = await userManager.FindByIdAsync(userId);
            var playwrightPath = configuration.GetConnectionString("PlaywrightPath");

            var entity = new Suggest();
            entity.Title = command.Title;
            entity.Company = command.Company;
            //entity.About = command.Link;
            //entity.Description = command.Location;
            entity.UserId = userId;
            entity.CovertLetter = command.CoverLetter;
            entity.Link = command.Link;
            entity.Location = command.Location;
            entity.JobType = command.JobType;
            entity.SuggestType = Domain.Enums.SuggestType.Sent;
            DbContext.Suggests.Add(entity);

            await DbContext.SaveChangesAsync();

            var sendApplication = new SendApplication
               (
               DbContext,
               user,
               command,
               _openAiKey,
               playwrightPath,
               _hostingEnvironment.WebRootPath,
               entity
               );

            try
            {
                await sendApplication.Send();
            }
            catch { }

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [CustomAuthorize("Admin,User")]
    [HttpPost("GenerateCoverLetter")]
    public async Task<IActionResult> GenerateCoverLetter(GenerateCoverLetterCommand command)
    {
        var playwrightPath = configuration.GetConnectionString("PlaywrightPath");
        var jobs = new List<JobInfoViewModel>();
        var userId = CurrentUserService.GetUserId(HttpContext);
        var user = await userManager.FindByIdAsync(userId);
        string jobTitle = user.NeededJob;

        var skills = await DbContext.Skills.AsNoTracking()
            .Where(m => !m.Deleted)
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var Experiences = await DbContext.Experiences.AsNoTracking()
            .Where(m => !m.Deleted)
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var Educations = await DbContext.Educations.AsNoTracking()
            .Where(m => !m.Deleted)
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var Certificates = await DbContext.Certificates.AsNoTracking()
            .Where(m => !m.Deleted)
            .Where(m => m.UserId == userId)
            .ToListAsync();

        var li = new ResumeResult();
        li.Skills = skills;
        li.Experiences = Experiences;
        li.Educations = Educations;
        li.Certificates = Certificates;

        var apply = new Apply(_httpClientFactory, _chatModel, _openAiKey, jobTitle, playwrightPath);

        foreach (var item in command.Jobs)
        {
            jobs.Add(new JobInfoViewModel
            {
                Company = item.Company,
                CoverLetter = "",
                Link = item.Link,
                Location = item.Location,
                Title = item.Title,
                Type = (JobType)item.Type,
            });
        }

        #region بررسی شرکت ها و تولید کاورلتر
        var res = await apply.GenerateCoverLetter(jobs, li, user);
        #endregion

        return Ok(res);
    }

    [HttpPost("GenerateResumeAndGetNewJobs")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> GenerateResumeAndGetNewJobs([FromForm] IFormFile pdf, [FromForm] string TokenId)
    {
        if (pdf == null || pdf.Length == 0)
            return BadRequest("هیچ فایل PDF ارسال نشده");

        var userId = CurrentUserService.GetUserId(TokenId);

        var ai = new AI(_httpClientFactory, _chatModel, _openAiKey);
        string text;
        using (var ms = new MemoryStream())
        {
            await pdf.CopyToAsync(ms);
            ms.Position = 0;
            text = ai.ExtractTextFromPdf(ms);
        }

        var li = await ai.ExtractResumeJsonAsync(text);

        #region پر کردن اطلاعات از خروجی رزومه
        foreach (var item in li.Skills)
        {
            var hasExist = await DbContext.Skills.AsNoTracking()
                .Where(m => !m.Deleted)
                .AnyAsync(m => m.Title == item.Title);

            if (!hasExist)
            {
                var entity = new Skill();
                entity.Title = item.Title;
                entity.UserId = userId;

                DbContext.Skills.Add(entity);
            }
        }

        foreach (var item in li.Experiences)
        {
            var hasExist = await DbContext.Experiences.AsNoTracking()
                .Where(m => !m.Deleted)
                .AnyAsync(m => m.Project == item.Project && m.Company == item.Company
                            && m.Role == item.Role && m.Activity == item.Activity && m.Benefit == item.Benefit);

            if (!hasExist)
            {
                var entity = new Experience();
                entity.Project = item.Project;
                entity.Company = item.Company;
                entity.Role = item.Role;
                entity.Activity = item.Activity;
                entity.Benefit = item.Benefit;
                entity.UserId = userId;

                DbContext.Experiences.Add(entity);
            }
        }

        foreach (var item in li.Educations)
        {
            var hasExist = await DbContext.Educations.AsNoTracking()
                .Where(m => !m.Deleted)
                .AnyAsync(m => m.Academy == item.Academy && m.Degree == item.Degree && m.Score == item.Score);

            if (!hasExist)
            {
                var entity = new Education();
                entity.Academy = item.Academy;
                entity.Degree = item.Degree;
                entity.UserId = userId;

                DbContext.Educations.Add(entity);
            }
        }

        foreach (var item in li.Certificates)
        {
            var hasExist = await DbContext.Certificates.AsNoTracking()
                .Where(m => !m.Deleted)
                .AnyAsync(m => m.Academy == item.Academy && m.Academy == item.Academy && m.Skill == item.Skill);

            if (!hasExist)
            {
                var entity = new Certificate();
                entity.Title = item.Title;
                entity.Academy = item.Academy;
                entity.Skill = item.Skill;
                entity.UserId = userId;

                DbContext.Certificates.Add(entity);
            }
        }

        var user = await userManager.FindByIdAsync(userId);
        user.NeededJob = li.JobTitle;
        //query.AboutMe = "";
        await userManager.UpdateAsync(user);
        #endregion

        await DbContext.SaveChangesAsync();

        //var skills = await DbContext.Skills.AsNoTracking()
        //       .Where(m => !m.Deleted)
        //       .Where(m => m.UserId == userId)
        //       .ToListAsync();
        var skills = li.Skills;

        var playwrightPath = configuration.GetConnectionString("PlaywrightPath");
        var jobs = new List<JobInfoViewModel>();
        string jobTitle = user.NeededJob;

        var apply = new Apply(_httpClientFactory, _chatModel, _openAiKey, jobTitle, playwrightPath);

        #region دریافت آگهی های لینکدین
        var linkedins = await apply.GetLinkedInJobs();
        jobs.AddRange(linkedins);
        #endregion

        #region دریافت آگهی های زینگ
        var xings = await apply.GetXingJobs();
        jobs.AddRange(xings);
        #endregion

        #region دریافت آگهی های استپ استون
        var stepStones = await apply.GetStepStoneJobs();
        jobs.AddRange(stepStones);
        #endregion

        #region بررسی شرکت ها و تولید کاورلتر
        var res = await apply.GenerateCoverLetter(jobs, li, user);
        #endregion

        return Ok(new { skills, res });
    }
}
