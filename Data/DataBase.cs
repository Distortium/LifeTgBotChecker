using Microsoft.AspNetCore.Identity;
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
        private static bool isInitialized = false;
        public static event Action<DataBase>? OnInitEvent;

        public DataBase(DbContextOptions<DataBase> options) : base(options)
        {
            if (!isInitialized)
            {
                try
                {
                    Database.EnsureCreated();

                    // Инициализируем данные
                    InitDB(this);
                    isInitialized = true;
                }
                catch (Exception ex)
                {
                    // Логируем ошибку
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                }
            }

            _instance = this;

            if (isInitialized)
                OnInitEvent?.Invoke(this);
        }

        private void InitDB(DataBase db)
        {
            try
            {
                try
                {
                    if (!db.Settings.Any())
                    {
                        var defaultSettings = new SettingsInDataBase
                        {
                            TokenCheckerBot = "",
                            MaxAnswersInSecFromCheckerBot = 10,
                            MaxGetWorkloadFromBot = 5,
                            KdMilliSecCheck = 5000,
                            KdMilliSecCheckAfterFirstCheck = 3000,
                            KdMilliSecCheckAfterSendMessage = 2000
                        };

                        db.Settings.Add(defaultSettings);
                        db.SaveChanges();
                        Console.WriteLine("Default settings created");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Settings initialization failed (tables might not exist): {ex.Message}");
                    // Таблицы не существуют, но EnsureCreated уже должен был их создать
                    // Продолжаем выполнение
                }

                try
                {
                    // Просто проверяем таблицу Bots
                    var botsCount = db.Bots.Count();
                    Console.WriteLine($"Bots records count: {botsCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bots check failed: {ex.Message}");
                }

                Console.WriteLine("Database initialization completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database initialization: {ex.Message}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка таблицы Settings - гарантируем что будет только одна запись
            modelBuilder.Entity<SettingsInDataBase>()
                .HasKey(s => s.Id);

            // Настройка таблицы Bots
            modelBuilder.Entity<BotInDataBase>()
                .HasKey(b => b.Token);

            Console.WriteLine("Database model configured");
        }

        public async Task AddBot(string token, string name = "Бот")
        {
            await Bots.AddAsync(new BotInDataBase(token, name));
            await SaveChangesAsync();
            Console.WriteLine($"Added bot with token {token} in data base");
        }

        public async Task RemoveBot(string token)
        {
            var findBot = await Bots.FindAsync(token);
            if (findBot != null)
            {
                Bots.Remove(findBot);
                await SaveChangesAsync();
            }
            Console.WriteLine($"Removed bot with token {token} in data base");
        }

        public async Task<SettingsInDataBase?> GetSettings()
        {
            var s = await Settings.FirstOrDefaultAsync();

            if (s == null)
            {
                s = new SettingsInDataBase();
                await Settings.AddAsync(s);
                //await SaveChangesAsync();
            }

            return s;
        }

        public async Task Save()
        {
            await SaveChangesAsync();
            Console.WriteLine($"Successful saving data base");
        }

        public string Backup()
        {
            JsonDataBots jsonClass = new(LifeCheckBotsService.CheckerBot?.Token ?? "not_set", Bots.ToList());
            string json = JsonSerializer.Serialize(jsonClass,
                new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            return json;
        }

        public async Task InputBackup(string json)
        {
            var outputClass = JsonSerializer.Deserialize<JsonDataBots>(json);
            if (outputClass != null)
            {
                await Database.ExecuteSqlRawAsync("DELETE FROM Bots");
                if (outputClass.Bots != null)
                    await Bots.AddRangeAsync(outputClass.Bots);
                var settings = await GetSettings();
                if (settings != null)
                    settings.TokenCheckerBot = outputClass.CheckerBot;
                await SaveChangesAsync();
                OnInitEvent?.Invoke(this);
                Console.WriteLine("Success deserialize file and import data base");
            }
            else Console.WriteLine("Cant deserialize file");
        }

        public static string? StaticBackup() => _instance?.Backup();
    }

    public class BotInDataBase
    {
        [Key]
        public string Token { get; set; }
        public string Name { get; set; }

        public BotInDataBase(string token, string name)
        {
            Token = token;
            Name = name;
        }
    }

    public class SettingsInDataBase
    {
        [Key]
        public int Id { get; set; } = 0;
        public string TokenCheckerBot { get; set; } = string.Empty;
        public int MaxAnswersInSecFromCheckerBot { get; set; } = 10;
        public int MaxGetWorkloadFromBot { get; set; } = 5;
        public int KdMilliSecCheck { get; set; } = 5000;
        public int KdMilliSecCheckAfterFirstCheck { get; set; } = 3000;
        public int KdMilliSecCheckAfterSendMessage { get; set; } = 2000;

        public SettingsInDataBase()
        {
        }
    }

    [System.Serializable]
    public class JsonDataBots
    {
        [JsonInclude]
        public string CheckerBot;
        [JsonInclude]
        public List<BotInDataBase> Bots;

        public JsonDataBots(string checkerBot, List<BotInDataBase> bots)
        {
            CheckerBot = checkerBot;
            Bots = bots;
        }
    }
}