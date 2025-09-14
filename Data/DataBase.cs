using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeTgBotChecker.Data
{
    public class DataBase : DbContext
    {
        public DbSet<BotInDataBase> Bots { get; set; }
        public DbSet<SettingsInDataBase> Settings { get; set; }

        private static DataBase? _instance;
        public static event Action<DataBase>? OnInitEvent;

        public DataBase(DbContextOptions<DataBase> options) : base(options)
        {
            _instance = this;
            OnInitEvent?.Invoke(this);
        }

        public async Task AddBot(string token)
        {
            await Bots.AddAsync(new BotInDataBase(token));
            Console.WriteLine($"Added bot with token {token} in data base");
        }

        public async Task RemoveBot(string token)
        {
            var findBot = await Bots.FindAsync(token);
            if (findBot != null)
                Bots.Remove(findBot);
            Console.WriteLine($"Removed bot with token {token} in data base");
        }

        public async Task<SettingsInDataBase?> GetSettings()
        {
            var s = await Settings.FirstOrDefaultAsync();
            
            if (s == null)
                await Settings.AddAsync(s = new SettingsInDataBase());
            
            return s;
        }

        public async Task Save()
        {
            await this.SaveChangesAsync();
            Console.WriteLine($"Successful saving data base");
        }

        public string Backup()
        {
            var jsonClass = new
            {
                checkerBot = LifeCheckBotsService.CheckerBot!.Token,
                bots = Bots.ToList()
            };
            string json = JsonSerializer.Serialize(jsonClass, 
                new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles, 
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

            return json;
        }

        public static string? StaticBackup() => _instance?.Backup();
    }
    
    public class BotInDataBase
    {
        [Key]
        public string Token { get; set; }

        public BotInDataBase(string token)
        {
            Token = token;
        }
    }

    public class SettingsInDataBase
    {
        [Key]
        public int Id { get; set; } = 0;
        public string TokenCheckerBot { get; set; }
        public int MaxAnswersInSecFromCheckerBot { get; set; }
        public int MaxGetWorkloadFromBot { get; set; }
        public int KdMilliSecCheck { get; set; }
        public int KdMilliSecCheckAfterFirstCheck { get; set; }
        public int KdMilliSecCheckAfterSendMessage { get; set; }

        public SettingsInDataBase()
        {

        }
    }
}
