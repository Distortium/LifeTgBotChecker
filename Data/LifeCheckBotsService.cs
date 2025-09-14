using Telegram.Bot;

namespace LifeTgBotChecker.Data
{
    public class LifeCheckBotsService
    {
        private TelegramBotClient? checkerBot;
        private static LifeCheckBotsService? _instance;
        private int lastUpdateId = -1;

        // Parameters
        public int MaxAnswersInSecFromCheckerBot = 10;
        public int MaxGetWorkloadFromBot = 30;
        public int KdMilliSecCheck = 2000;
        public int KdMilliSecCheckAfterFirstCheck = 500;
        public int KdMilliSecCheckAfterSendMessage = 200;
        public string TokenCheckerBot = "";

        // Storages for check bots
        public Dictionary<string, Bot> Bots = new();
        public Dictionary<long, int> ChatIds = new();
        public Dictionary<long, int> TempChatIds = new();
        public DataBase? DB { get; set; }

        public event Func<Task>? OnUpdateUIEvent;
        public static TelegramBotClient? CheckerBot
        {
            get { return _instance?.checkerBot; }
        }

        public LifeCheckBotsService()
        {
            _instance = this;
            DataBase.OnInitEvent += InitDB;

            Task.Run(Main);
        }

        private async Task Main()
        {
            while (true)
            {
                if (KdMilliSecCheck < 1000)
                    KdMilliSecCheck = 1000;
                await Task.Delay(KdMilliSecCheck);
                await CheckLifeBots();
            }
        }

        private void InitDB(DataBase db)
        {
            DataBase.OnInitEvent -= InitDB;

            DB = db;
            if (DB != null)
            {
                LoadBots(DB.Bots.ToList());
                Console.WriteLine("Load bots from data base");
                LoadSettings(DB);
            }
        }

        public void AddBot(string token)
        {
            if (!string.IsNullOrWhiteSpace(token) && !Bots.ContainsKey(token))
            {
                Bots.Add(token, new Bot(token));
                OnUpdateUIEvent?.Invoke();

                if (DB == null)
                    Console.WriteLine($"Added bot with token {token}");
                DB?.AddBot(token);
            }
        }

        public void RemoveBot(string token)
        {
            if (Bots.ContainsKey(token))
            {
                Bots.Remove(token);
                OnUpdateUIEvent?.Invoke();

                if (DB == null)
                    Console.WriteLine($"Removed bot with token {token}");
                DB?.RemoveBot(token);
            }
        }

        public void RefreshTokenCheckerBot()
        {
            try
            {
                checkerBot = new TelegramBotClient(TokenCheckerBot);
            }
            catch (Exception)
            {
                checkerBot = null;
            }
        }

        public async Task CheckLifeBots()
        {
            if (checkerBot == null)
                return;

            await CheckNewChatForCheckBots();

            // В массиве нету ни одного чата для проверки
            if (ChatIds.Count == 0)
                return;

            // Сбор статистики до проверки
            int countActiveBots = 0, countErrorBots = 0;
            foreach (var bot in Bots)
            {
                if (bot.Value.IsLife)
                    countActiveBots++;
                else if (bot.Value.Workload < 0)
                    countErrorBots++;
            }

            // Отправка сообщения проверяющего активность ботов
            foreach (var id in ChatIds)
            {
                var members = await checkerBot.GetChatAdministrators(id.Key);
                if (members.Any(m => m.User.Id == checkerBot.BotId))
                {
                    try
                    {
                        if (id.Value != 0)
                            await checkerBot.DeleteMessage(id.Key, id.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while deleting a previous message: {ex}");
                    }

                    var m = await checkerBot.SendMessage(id.Key,
                        $"Check, before that, {countActiveBots}/{Bots.Count} was active and {countErrorBots} with errors",
                        disableNotification: true);
                    TempChatIds.Add(id.Key, m.Id);
                }
            }

            // Перезапись id на новое отправленное сообщение
            foreach (var tempId in TempChatIds)
            {
                ChatIds.Remove(tempId.Key);
                ChatIds.Add(tempId.Key, tempId.Value);
            }
            TempChatIds.Clear();

            await Task.Delay(KdMilliSecCheckAfterSendMessage);

            int lastCountQueue;
            foreach (var bot in Bots)
            {
                var b = bot.Value;
                TelegramBotClient botClient;
                try
                {
                    botClient = new TelegramBotClient(b.Token);
                }
                catch
                {
                    b.IsLife = false;
                    b.Workload = -1;
                    continue;
                }

                try
                {
                    var webhookInfo = await botClient.GetWebhookInfo();
                    if (!string.IsNullOrEmpty(webhookInfo.Url))
                    {
                        // Бот имеет webhook, нужно отправить кастомный запрос на проверку
                        
                        if (webhookInfo.PendingUpdateCount > 0)
                        {
                            b.IsLife = false;
                            lastCountQueue = webhookInfo.PendingUpdateCount;

                            await Task.Delay(KdMilliSecCheckAfterFirstCheck);
                            webhookInfo = await botClient.GetWebhookInfo();
                            if (webhookInfo.PendingUpdateCount < lastCountQueue)
                                b.IsLife = true;
                        }
                        else
                            b.IsLife = true;

                        b.Workload = webhookInfo.PendingUpdateCount;
                        continue;
                    }

                    // Проверка, активен ли бот и не забанен
                    //var me = await botClient.GetMe();

                    // Проверка, слушает ли он сообщения (получение обновлений)
                    var updates = await botClient.GetUpdates(offset: 0, limit: MaxGetWorkloadFromBot);
                    if (updates.Length > 0)
                    {
                        lastCountQueue = updates.Length;
                        b.IsLife = false;

                        await Task.Delay(KdMilliSecCheckAfterFirstCheck);
                        updates = await botClient.GetUpdates(offset: 0, limit: MaxGetWorkloadFromBot);
                        if (updates.Length < lastCountQueue)
                            b.IsLife = true;
                    }
                    else
                        b.IsLife = true;

                    b.Workload = updates.Length;
                }
                catch (Exception ex)
                {
                    b.IsLife = false;
                    b.Workload = -1;
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    // Возможные причины: бот забанен, токен неверный, сервер недоступен
                }
            }
            OnUpdateUIEvent?.Invoke();
        }

        private async Task AddChat(long chatId)
        {
            if (checkerBot != null && !ChatIds.ContainsKey(chatId))
            {
                var messageId =
                    await checkerBot.SendMessage(chatId, "This chat was recorded", disableNotification: true);
                ChatIds.Add(chatId, messageId.Id);
            }
        }

        // Поиск новых чатов для работы
        private async Task CheckNewChatForCheckBots()
        {
            if (checkerBot != null)
            {
                try
                {
                    var updates = await checkerBot.GetUpdates(++lastUpdateId);
                    int countAnswer = MaxAnswersInSecFromCheckerBot;

                    foreach (var update in updates)
                    {
                        if (countAnswer-- <= 0)
                        {
                            countAnswer = MaxAnswersInSecFromCheckerBot;
                            await Task.Delay(1000);
                        }

                        // Обработка каналов куда пригласили 'checker' бота
                        if (update.ChannelPost?.NewChatMembers != null)
                        {
                            for (int i = 0; i < update.ChannelPost.NewChatMembers.Length; i++)
                            {
                                if (!update.ChannelPost.NewChatMembers[i].IsBot)
                                    continue;

                                if (update.ChannelPost.NewChatMembers[i].Id == checkerBot.BotId)
                                {
                                    if (!ChatIds.ContainsKey(update.ChannelPost.Chat.Id))
                                        await AddChat(update.ChannelPost.Chat.Id);
                                }
                            }
                        }
                        // Обработка ввода от пользователей сообщения '/start' в каналах
                        else if (update.ChannelPost?.Text != null &&
                            update.ChannelPost.Text.Contains("/start", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (!ChatIds.ContainsKey(update.ChannelPost.Chat.Id))
                                await AddChat(update.ChannelPost.Chat.Id);
                        }
                        lastUpdateId = update.Id;
                    }
                }
                catch { }
            }
        }

        // Загрузка токенов ботов из БД
        private void LoadBots(IEnumerable<BotInDataBase> bots)
        {
            foreach (var bot in bots)
                Bots.Add(bot.Token, new Bot(bot.Token));
        }

        // Загрузка настроек из БД
        private async void LoadSettings(DataBase db)
        {
            var settings = await db.GetSettings();
            if (settings == null) return;

            MaxAnswersInSecFromCheckerBot = settings.MaxAnswersInSecFromCheckerBot;
            MaxGetWorkloadFromBot = settings.MaxGetWorkloadFromBot;
            KdMilliSecCheck = settings.KdMilliSecCheck;
            KdMilliSecCheckAfterFirstCheck = settings.KdMilliSecCheckAfterFirstCheck;
            KdMilliSecCheckAfterSendMessage = settings.KdMilliSecCheckAfterSendMessage;
            TokenCheckerBot = settings.TokenCheckerBot;
            Console.WriteLine("Load settings from data base");
        }
    }
}
