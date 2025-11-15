using Application.Common.Interfaces;
using Domain.Entities;
using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Services;
namespace WebAPI.Controllers
{
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        public FileController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost("File/UploadResume")]
        public async Task<ContentResult> UploadResume()
        {
            return await BaseUpload("Resumes\\");
        }

        [HttpPost("File/UploadDocument")]
        public async Task<ContentResult> UploadSignatureFile()
        {
            return await BaseUpload("Documents\\");
        }

        [HttpPost("File/UploadTicketFile")]
        public async Task<ContentResult> UploadTicketFile()
        {
            return await BaseUpload("TicketFiles\\");
        }

        [HttpPost("File/UploadProfile")]
        public async Task<ContentResult> UploadProfile()
        {
            return await BaseUpload("Images\\ProfileImages\\");
        }

        private async Task<ContentResult> BaseUpload(string FolderName)
        {
            var fileName = Guid.NewGuid().ToString();
            var file = Request.Form.Files[0];
            var extension = GetDefaultExtension(file.ContentType.ToLower());
            var path = _hostingEnvironment.WebRootPath + "\\Content\\" + FolderName + fileName + extension;

            if (!Directory.Exists(_hostingEnvironment.WebRootPath + "\\Content\\" + FolderName))
            {
                Directory.CreateDirectory(path);
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Content(fileName + extension);
        }

        public static string GetDefaultExtension(string mimeType)
        {
            string result = "";
            if (mimeType.Contains("pdf"))
            {
                result = ".pdf";
            }
            else if (mimeType.Contains("jpeg"))
            {
                result = ".jpg";
            }
            else if (mimeType.Contains("png"))
            {
                result = ".png";
            }
            else if (mimeType.Contains("zip"))
            {
                result = ".zip";
            }
            else if (mimeType.Contains("mp4"))
            {
                result = ".mp4";
            }
            else if (mimeType.Contains("mp3") || mimeType.Contains("mpeg"))
            {
                result = ".mp3";
            }
            else if (mimeType.Contains("msdownload"))
            {
                result = ".exe";
            }
            else if (mimeType.Contains("sheet"))
            {
                result = ".xlsx";
            }
            else if (mimeType.Contains("json"))
            {
                result = ".json";
            }
            return result;
        }

        //

        [HttpGet("DownloadResume")]
        public async Task<FileContentResult> DownloadResume(string Email)
        {
            IApplicationDbContext DbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

            var user = await DbContext.Users.AsNoTracking()
                .Where(m => m.Email == Email)
                .SingleOrDefaultAsync();

            var userImageName = user.ImageName != null
                ? (_hostingEnvironment.WebRootPath + "\\Content\\Images\\ProfileImages\\" + user.ImageName)
                : "";
            userImageName = userImageName.Replace("\\", "/");

            var skills = await DbContext.Skills.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == user.Id)
                .Select(m => new
                {
                    m.Title,
                    //Value = Convert.ToInt32(m.Value),
                })
                .ToListAsync();

            var experiences = await DbContext.Experiences.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == user.Id)
                .Select(m => new
                {
                    m.Id,
                    m.Project,
                    m.Company,
                    m.Role,
                })
                .ToListAsync();

            var educations = await DbContext.Educations.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == user.Id)
                .Select(m => new
                {
                    m.Academy,
                    m.Degree,
                    //m.Score,
                })
                .ToListAsync();

            var certificates = await DbContext.Certificates.AsNoTracking()
                .Where(m => !m.Deleted)
                .Where(m => m.UserId == user.Id)
                .Select(m => new
                {
                    m.Title,
                    m.Academy,
                    m.Skill,
                })
                .ToListAsync();

            Report rpt = new Report();
            rpt.RegisterData(skills, "skills");
            rpt.RegisterData(experiences, "experiences");
            rpt.RegisterData(educations, "educations");
            rpt.RegisterData(certificates, "certificates");
            //rpt.Design();
            rpt.Load(_hostingEnvironment.WebRootPath + "\\Content\\Resume\\Resume.frx");

            rpt.SetParameterValue("UserFullName", user.FirstName + " " + user.LastName);
            rpt.SetParameterValue("UserImageName", userImageName);
            rpt.SetParameterValue("Email", user.Email);
            rpt.SetParameterValue("Phone", user.PhoneNumber);
            rpt.SetParameterValue("Job", user.NeededJob);
            rpt.SetParameterValue("ProfileText", user.AboutMe);

            rpt.Prepare();

            #region PDF
            using (MemoryStream ms = new MemoryStream())
            {
                PDFSimpleExport pdfExport = new PDFSimpleExport();
                pdfExport.Export(rpt, ms);
                ms.Flush();
                return File(ms.ToArray(), "application/pdf", Guid.NewGuid().ToString() + ".pdf");
            }
            #endregion
        }
    }
}
