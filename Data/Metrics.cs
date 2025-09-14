using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LifeTgBotChecker.Data
{
    [ApiController]
    [Route("[controller]")]
    public class Metrics : ControllerBase
    {
        private readonly LifeCheckBotsService LifeCheckerService;
        public Metrics(LifeCheckBotsService checker)
        {
            LifeCheckerService = checker;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var sb = new StringBuilder();
            int countActiveBots = 0;
            double averageWorkloadBots = 0;

            foreach (var bot in LifeCheckerService.Bots)
            {
                if (bot.Value.IsLife)
                {
                    countActiveBots++;
                    averageWorkloadBots += bot.Value.Workload;
                }
            }
            if (countActiveBots != 0)
                averageWorkloadBots /= countActiveBots;

            // Всего ботов
            sb.AppendLine("# HELP all_count_bots Всего ботов");
            sb.AppendLine("# TYPE all_count_bots counter");
            sb.AppendLine($"all_count_bots {LifeCheckerService.Bots.Count}");

            // Количество активных ботов
            sb.AppendLine("# HELP count_active_bots Количество активных ботов");
            sb.AppendLine("# TYPE count_active_bots counter");
            sb.AppendLine($"count_active_bots {countActiveBots}");

            // Средняя нагруженность ботов
            sb.AppendLine("# HELP average_workload_bots Средняя нагруженность ботов");
            sb.AppendLine("# TYPE average_workload_bots counter");
            sb.AppendLine($"average_workload_bots {averageWorkloadBots}");

            // Активен ли определённый бот
            sb.AppendLine($"# HELP active_bot Is bot active (1 = active, 0 = inactive)");
            sb.AppendLine($"# TYPE active_bot gauge");
            foreach (var bot in LifeCheckerService.Bots)
                sb.AppendLine($"active_bot{{bot_id=\"{bot.Key}\"}} {(bot.Value.IsLife ? 1 : 0)}");

            // Загруженность определённого бота
            sb.AppendLine($"# HELP workload_bot Bot load");
            sb.AppendLine($"# TYPE workload_bot gauge");
            foreach (var bot in LifeCheckerService.Bots)
                sb.AppendLine($"workload_bot{{bot_id=\"{bot.Key}\"}} {bot.Value.Workload}");

            return Content(sb.ToString(), "text/plain; version=0.0.4");
        }
    }
}
