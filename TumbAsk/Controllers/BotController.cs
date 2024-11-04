using Microsoft.AspNetCore.Mvc;
using TumbAsk.Models;
using TumbAsk.Models.ViewModels;
using TumbAsk.Repositories;
using TumbAsk.Services;

namespace TumbAsk.Controllers
{
    public class BotController : Controller
    {
        private readonly IBotService _botService;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IBotRepository _botRepository;
        private readonly BotLogService _logService;
        public BotController(IBotService botService, IUserAccountRepository userAccountRepository, IBotRepository botRepository, BotLogService blogLogService)
        {
            _botService = botService;
            _userAccountRepository = userAccountRepository;
            _botRepository = botRepository;
            _logService = blogLogService;
        }

        public async Task<IActionResult> Index()
        {
            List<Bot>? bots = await _botRepository.GetAllBotsAsync();

            List<BotListingVM>? botViewModels = new List<BotListingVM>();
            foreach (var bot in bots)
            {
                var userCount = await _userAccountRepository.GetTotalAccountsCountAsync(bot.Id);
                int usersToSendQuestion = _userAccountRepository.GetUnsendedUsersAsync(bot.Id).Result.Count();
                botViewModels.Add(new BotListingVM
                {
                    Bot = bot,
                    UserCount = userCount,
                    UnsendedUsersCount = usersToSendQuestion
                });
            }

            return View(botViewModels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> ViewAccounts(int botId, int pageNumber = 1, int pageSize = 10)
        {
            var accounts = await _userAccountRepository.GetAccountsByBotIdAsync(botId, pageNumber, pageSize);
            var totalAccounts = await _userAccountRepository.GetTotalAccountsCountAsync(botId);
            var bot = await _botRepository.GetBotByIdAsync(botId);

            ViewBag.BotName = bot?.Username ?? "Bot";
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalAccounts / (double)pageSize);
            ViewBag.BlogSubject = bot?.Keyword ?? "-";
            ViewBag.BotId = bot?.Id;

            return View(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BotSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data. Please try again." });
            }

            var bot = new Bot
            {
                Username = settings.Username,
                Password = settings.Password,
                Keyword = settings.Keyword,
                ThreadCount = settings.ThreadCount,
                MaxAccounts = settings.MaxAccounts,
                MaxMessages = settings.MaxMessages,
                Status = BotStatus.Idle  
            };

            try
            {
                bool isAdded = await _botService.AddBotAsync(bot);  
                return Json(new { success = isAdded, message = isAdded ? "Bot created successfully." : "Authentication failed." });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while creating the bot. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Start(int botId, CancellationToken cancellationToken)
        {
            await _botService.StartBotAsync(botId, cancellationToken);
            TempData["SuccessMessage"] = "Bot started successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Stop(int botId, CancellationToken cancellationToken)
        {
            await _botService.StopBotAsync(botId, cancellationToken);
            TempData["SuccessMessage"] = "Bot stopped successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Pause(int botId, CancellationToken cancellationToken)
        {
            await _botService.PauseBotAsync(botId, cancellationToken);
            TempData["SuccessMessage"] = "Bot paused successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int botId, CancellationToken cancellationToken)
        {
            await _botService.DeleteBotAsync(botId);
            TempData["SuccessMessage"] = "Bot deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> StartQuestion(int botId, CancellationToken cancellationToken)
        {
            await _botService.StartSendingQuestionsAsync(botId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> StopQuestion(int botId)
        {
            await _botService.StopQuestion(botId);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ShowQuestionLogs(int botId)
        {
            var logs = await _logService.GetQuestionLogsByBotIdAsync(botId);
            return View("QuestionLogs", logs);  
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAllLogs()
        {
            await _logService.DeleteAllLogsAsync();

            return Json(new { success = true });
        }
    }
}
