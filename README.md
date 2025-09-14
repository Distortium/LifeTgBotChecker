# LifeTgBotChecker
Collects statistics on tg bots (whether online and queue load)
Собирает статистику по тг-ботам (будь то онлайн или загрузка в очереди)

## En / Английский 🇬🇧

Parameters:
- The interval between checks (ml. sec.) <br>The time of the call between the main method of checking all bots
- The resulting queue length for bots <br>The maximum possible queue length received from bots for verification (greatly affects the speed because this operation is waiting for a response from the telegram API)
is the maximum number of bot actions per second. <br>Due to the protection of telegram from spam by bots, it is advisable to set no more than 30, otherwise the bot may be blocked
- The time before the start of verification after sending the message (ml. sec.) <br>After sending a message to check the queue, the bot will have a given time to wait for telegram and the bot to process this message.
- Time to re-check after the first one (ml. sec.) <br>If there are messages waiting in the queue for the bot, after the first check, then we give another given time for the bot to process the queue and then look at the length
- Token of the checking bot <br>With the help of this bot, messages will be sent to channels for verification.

Buttons:
- Save <br>Saving all parameters and bots to the database, as well as initializing the checking bot using the entered token
- Backup <br>Starts downloading the json file of the bot list
- Add <br>Adds a token from the "Enter token" field to the local list of bots

API:
- /api/health <br>Returns the 200 code and a json message - status: Healthy, about the health of the service
- /api/backup <br>Returns a json message with a list of bots
- /metrics <br>Returns data for Prometheus:
  - all_count_bots (counter) Total bots
  - count_active_bots (counter) Number of active bots
  - average_workload_bots (counter) Average bot load
  - active_bot {bot_id} (gauge) Is the bot active (1 = active, 0 = inactive)
  - workload_bot {bot_id} (gauge) Bot load

## Ru / Русский 🇷🇺

Параметры:
- Промежуток между проверками (мл. сек.) <br>Время вызова между основного метода проверки всех ботов
- Получаемая длина очереди у ботов <br>Максимальная возможная длина очереди получаемая от ботов для проверки (сильно влияет на скорость т.к. данная операция ждёт ответ от телеграм API)
- Максимальное количество действий бота в сек. <br>Из за защиты телеграма от спама ботами, желательно ставить не больше 30 иначе бота могут заблокировать
- Время до начала проверки после отправки сообщения (мл. сек.) <br>После отправки сообщения для проверки очереди у бота будет ждать данное время для ожидания обработки телеграмом и ботом этого сообщения
- Время до повторной проверки после первой (мл. сек.) <br>Если в очереди ожидают сообщения для бота, после первой проверки, то даём ещё данное время для обработки очереди ботом и после смотрим длину
- Токен проверяющего бота <br>С помощью данного бота будут оформляться отправки сообщений в каналы для проверки

Кнопки:
- Save <br>Сохранение всех параметров и ботов в базу данных, так же инициализация проверяющего бота с помощью введённого токена
- Backup <br>Начинает скачивание json файла списка ботов
- Add <br>Добавляет токен из поля "Введите токен" в локальный список ботов

API:
- /api/health <br>Возвращает 200 код и json сообщение - status: Healthy, о работоспособности сервиса
- /api/backup <br>Возвращает json сообщение с списком ботов
- /metrics <br>Возвращает данные для Prometheus:
  - all_count_bots (counter) Всего ботов
  - count_active_bots (counter) Количество активных ботов
  - average_workload_bots (counter) Средняя нагруженность ботов
  - active_bot {bot_id} (gauge) Активен ли бот (1 = active, 0 = inactive)
  - workload_bot {bot_id} (gauge) Загруженность ботов
