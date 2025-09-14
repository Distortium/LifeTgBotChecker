using Microsoft.AspNetCore.Mvc;

namespace LifeTgBotChecker.Data
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileBackup : ControllerBase
    {
        [HttpGet]
        public IActionResult DownloadJson()
        {
            byte[] bytes = Array.Empty<byte>();
            var json = DataBase.StaticBackup();
            if (json != null)
                bytes = System.Text.Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", $"Backup {DateTime.Now:u}.json");
        }
    }
}
