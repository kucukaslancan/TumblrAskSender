using Hangfire;
using Microsoft.AspNetCore.SignalR;
using TumbAsk.Hubs;
using TumbAsk.Models;
using TumbAsk.Repositories;

namespace TumbAsk.Services
{
    public class BotService : IBotService
    {
        private readonly Dictionary<int, CancellationTokenSource> _cancellationTokens = new();

        private readonly IBotRepository _botRepository;
        private readonly IHubContext<BotStatusHub> _hubContext;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly TumblrService _tumblrService;
        private readonly BotLogService _botLogService;
        public BotService(IBotRepository botRepository, IHubContext<BotStatusHub> hubContext, TumblrService tumblrService, IUserAccountRepository userAccountRepository, BotLogService botLogService)
        {
            _botRepository = botRepository;
            _hubContext = hubContext;
            _tumblrService = tumblrService;
            _userAccountRepository = userAccountRepository;
            _botLogService = botLogService;
        }
        public async Task<List<Bot>> GetAllBotsAsync() => await _botRepository.GetAllBotsAsync();

        public void ScheduleBotTask(Bot bot, CancellationToken token)
        {
            //RecurringJob.AddOrUpdate(bot.RecurringJobId, () => RunBotAsync(bot, token), Cron.MinuteInterval(5));
            BackgroundJob.Enqueue(() => RunBotAsync(bot, token));
        }
        public async Task<bool> AddBotAsync(Bot bot)
        {
            try
            {
                // bumblr login kontrolü
                bool isAuthenticated = await _tumblrService.AuthenticateAsync(bot.Username, bot.Password);
                if (!isAuthenticated)
                {
                    return false; 
                }
                 
                await _botRepository.AddBotAsync(bot);
                return true;
            }
            catch
            {
                return false; 
            }
        }

        public async Task StartBotAsync(int botId, CancellationToken cancellationToken)
        {
            var bot = await _botRepository.GetBotByIdAsync(botId);
            if (bot != null && (bot.Status == BotStatus.Idle || bot.Status == BotStatus.Paused || bot.Status == BotStatus.Stopped))
            {

                bot.Status = BotStatus.Running;
                await _botRepository.UpdateBotAsync(bot);
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, "Bot started");

                
                ScheduleBotTask(bot, cancellationToken);

            }
        }
        public async Task StopBotAsync(int botId, CancellationToken cancellationToken)
        {
            var bot = await _botRepository.GetBotByIdAsync(botId);
            if (bot != null)
            {
                RecurringJob.RemoveIfExists(bot.RecurringJobId); 

                bot.Status = BotStatus.Stopped;
                await _botRepository.UpdateBotAsync(bot);
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, "Bot stopped");
            }
        }


        public async Task RunBotAsync(Bot bot, CancellationToken cancellationToken)
        {
            bot.Status = BotStatus.Running;
            await _botRepository.UpdateBotAsync(bot);

            // Aramayı başlatır.
            await _tumblrService.SearchUsersAsync(bot, cancellationToken);

            // Toplam sayıyı alır.
            int getTotalAccount = await _userAccountRepository.GetTotalAccountsCountAsync(bot.Id);

            if (getTotalAccount >= bot.MaxAccounts)
            {
                bot.Status = BotStatus.Completed;
                RecurringJob.RemoveIfExists(bot.RecurringJobId);
                await _botRepository.UpdateBotAsync(bot);
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Bot completed. {getTotalAccount} collected.");
            }
            else
            {
                bot.Status = BotStatus.Error;
                //RecurringJob.RemoveIfExists(bot.RecurringJobId);
                await _botRepository.UpdateBotAsync(bot);
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Bot failed.");
            }

        }

        public async Task PauseBotAsync(int botId, CancellationToken cancellationToken)
        {
            var bot = await _botRepository.GetBotByIdAsync(botId);
            if (bot != null)
            {
                RecurringJob.RemoveIfExists(bot.RecurringJobId); 

                bot.Status = BotStatus.Paused;
                await _botRepository.UpdateBotAsync(bot);
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, "Bot paused");
            }
        }

        public async Task DeleteBotAsync(int botId)
        {
            await _botRepository.DeleteBotAsync(botId);
            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", botId, "Bot deleted");
        }

        
     
        private readonly object _lock = new object();
        public async Task StartSendingQuestionsAsync(int botId)
        {
            int failRequest = 0;

            
            if (_cancellationTokens.ContainsKey(botId)) return;

            
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            lock (_lock)
            {
                _cancellationTokens.Add(botId, cancellationTokenSource);
            }


            await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, "Starting to send questions...", SenderStatusEnum.Info);

            var usersToSendQuestion = await _userAccountRepository.GetUnsendedUsersAsync(botId);
            Bot? bot = await _botRepository.GetBotByIdAsync(botId);
           
            foreach (var user in usersToSendQuestion)
            {
                bot = await _botRepository.GetBotByIdAsync(botId);
                lock (_lock)
                {
                    if (bot.Status == BotStatus.Stopped || bot.Status == BotStatus.Paused)
                    {
                        break;
                    }
                }
                

                if (failRequest == 10)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, $"[SPAM ALERT] Wait for 5 minute.", SenderStatusEnum.Info);
                    await Task.Delay(300000);

                    failRequest = 0;
                }

                if (cancellationTokenSource.Token.IsCancellationRequested) break;

                await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, $"Question sent start to {user.BlogName}.", SenderStatusEnum.Info);

                string testContent = "hello, visit my modular house web site please :) \nmy website: https://modernmodularny.com/";
                string testUrl = "https://modernmodularny.com/";
                var success = await _tumblrService.SendQuestionAsync(bot, user.BlogName, testContent, testUrl);


                if (success)
                {
                    user.IsQuestionSent = true;
                    await _userAccountRepository.UpdateUserAccountAsync(user);

                    await _botLogService.LogResultAsync(botId, $"Question sent successfully to {user.BlogName}", true);
                    await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, $"[Success] Question sent to {user.BlogName}." , SenderStatusEnum.Success);
                }
                else
                {
                    failRequest++;
                    await _botLogService.LogResultAsync(botId, $"Failed to send question to {user.BlogName}.", false);
                    await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, $"[Error] Failed to send question to {user.BlogName}.", SenderStatusEnum.Error);
                }

                await Task.Delay(10000);   
            }

            await _hubContext.Clients.All.SendAsync("ReceiveLogUpdate", botId, "All questions sent.");
            bot.Status = BotStatus.Completed;
            await _botRepository.UpdateBotAsync(bot);

            lock (_lock)
            {
                _cancellationTokens.Remove(botId);
            }
        }

       

        public async Task StopQuestion(int botId)
        {
            lock (_lock)
            {
                if (_cancellationTokens.TryGetValue(botId, out var cts))
                {
                    cts.Cancel();
                    _cancellationTokens.Remove(botId);
                }
            }
        }
    }
}
