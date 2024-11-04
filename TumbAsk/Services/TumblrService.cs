using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TumbAsk.Helper;
using TumbAsk.Hubs;
using TumbAsk.Models;
using TumbAsk.Repositories;

namespace TumbAsk.Services
{
    public class TumblrService
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly HttpClient _httpClient;
        private readonly IHubContext<TumblrStatusHub> _hubContext;
        private readonly IHubContext<BotStatusHub> _botStatusHubContext;
        private readonly IMemoryCache _cache;

        private const string CsrfTokenCacheKey = "Tumblr_CsrfToken";
        private const string AuthorizationTokenCacheKey = "Tumblr_AuthorizationToken";
        private const string ObfuscatedFeaturesCacheKey = "Tumblr_ObfuscatedFeaturesToken";
        private const string SenderAccountNameCachekey = "Tumblr_SenderAccountName";

        private const string LoginedUserCacheKey = "Local_LoginedUserToken";

        public TumblrService(HttpClient httpClient, IHubContext<TumblrStatusHub> hubContext, IMemoryCache cache, IUserAccountRepository userAccountRepository, IHubContext<BotStatusHub> botStatusHubContext)
        {
            _httpClient = httpClient;
            _hubContext = hubContext;
            _cache = cache;
            _userAccountRepository = userAccountRepository;
            _botStatusHubContext = botStatusHubContext;
        }

 
        private async Task<bool> EnsureTokensAsync()
        {
            
            if (_cache.TryGetValue(CsrfTokenCacheKey, out string csrfToken) &&
                _cache.TryGetValue(AuthorizationTokenCacheKey, out string authorizationToken) &&
                _cache.TryGetValue(ObfuscatedFeaturesCacheKey, out string obfuscatedFeaturesToken))
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Using cached tokens.");
                return true;
            }

          
            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Fetching new tokens...");
            return await FetchHeaderInfoAsync();
        }

        private async Task<bool> IsUserLogined(string username)
        {
          
            if (_cache.TryGetValue(LoginedUserCacheKey, out string loginedUser) &&
                 loginedUser == username)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Using cached tokens.");
                return true;
            }

            return false;
        }

        
        public async Task<bool> FetchHeaderInfoAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.tumblr.com/login");

            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();

                
                var csrfTokenMatch = Regex.Match(htmlContent, "\"csrfToken\":\"(?<csrfToken>[^\"]+)\"");
                var apiTokenMatch = Regex.Match(htmlContent, "\"API_TOKEN\":\"(?<apiToken>[^\"]+)\"");
                var obfuscatedFeaturesMatch = Regex.Match(htmlContent, "\"obfuscatedFeatures\":\"(?<obfuscatedFeatures>[^\"]+)\"");
                
                

                if (csrfTokenMatch.Success && apiTokenMatch.Success)
                {
                    string csrfToken = csrfTokenMatch.Groups["csrfToken"].Value;
                    string authorizationToken = apiTokenMatch.Groups["apiToken"].Value;

                     
                    _cache.Set(CsrfTokenCacheKey, csrfToken, TimeSpan.FromMinutes(30));
                    _cache.Set(AuthorizationTokenCacheKey, authorizationToken, TimeSpan.FromMinutes(30));

                    if (obfuscatedFeaturesMatch.Success)
                    {
                        string obfuscatedFeaturesToken = obfuscatedFeaturesMatch.Groups["obfuscatedFeatures"].Value;
                         
                        _cache.Set(ObfuscatedFeaturesCacheKey, obfuscatedFeaturesToken, TimeSpan.FromMinutes(30));
                    }

                    await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Tokens fetched and cached successfully.");
                    return true;
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Failed to retrieve tokens.");
                    return false;
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", $"Failed to fetch header info. Status code: {response.StatusCode}");
                return false;
            }
        }

        public async Task<bool> FetchHeaderInfoForLoginedAsync()
        {
            
            var csrfToken = _cache.Get<string>(CsrfTokenCacheKey);
            var authorizationToken = _cache.Get<string>(AuthorizationTokenCacheKey);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.tumblr.com/");

            request.Headers.Add("accept", "application/json;format=camelcase");
            request.Headers.Add("authorization", $"Bearer {authorizationToken}");
            request.Headers.Add("origin", "https://www.tumblr.com/login");
            request.Headers.Add("referer", "https://www.tumblr.com/login");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.5;) AppleWebKit/535.25 (KHTML, like Gecko) Chrome/51.0.2713.143 Safari/533");
            request.Headers.Add("x-csrf", csrfToken);
            request.Headers.Add("x-version", "redpop/3/0//redpop/");
            request.Headers.Add("x-ad-blocker-enabled", "0");
            request.Headers.Add("priority", "u=1, i");
            request.Headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-fetch-dest", "empty");
            request.Headers.Add("sec-fetch-mode", "cors");
            request.Headers.Add("sec-fetch-site", "same-origin");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();

               
                var userInfoMatch = Regex.Match(htmlContent, "\"user\":\"(?<user>[^\"]+)\"");

                var userMatch = Regex.Match(htmlContent, "\"isLoggedIn\":true,\"user\":\\{.*?\"name\":\"(?<name>[^\"]+)\",\"likes\":(?<likes>\\d+),\"following\":(?<following>\\d+),\"defaultPostFormat\":\"(?<defaultPostFormat>[^\"]+)\",\"email\":\"(?<email>[^\"]+)\"");

                if (userMatch.Success)
                {
                    var userInfo = new TumblrUserInfo
                    {
                        Name = userMatch.Groups["name"].Value,
                        Likes = int.Parse(userMatch.Groups["likes"].Value),
                        Following = int.Parse(userMatch.Groups["following"].Value),
                        DefaultPostFormat = userMatch.Groups["defaultPostFormat"].Value,
                        Email = userMatch.Groups["email"].Value
                    };

                    UserInfoCache.SetUserInfo(userInfo);

                    return true;
                }


                return false;
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", $"Failed to fetch header info. Status code: {response.StatusCode}");
                return false;
            }
        }

       
        public async Task<bool> AuthenticateAsync(string username, string password, bool isBotRedirect = false)
        {
            if (isBotRedirect) return true;

            
            bool tokensAvailable = await EnsureTokensAsync();
            if (!tokensAvailable)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Unable to retrieve tokens for authentication.");
                return false;
            }

            
            var csrfToken = _cache.Get<string>(CsrfTokenCacheKey);
            var authorizationToken = _cache.Get<string>(AuthorizationTokenCacheKey);

            await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Authenticating with Tumblr...");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.tumblr.com/api/v2/oauth2/token");

            request.Headers.Add("accept", "application/json;format=camelcase");
            request.Headers.Add("authorization", $"Bearer {authorizationToken}");
            request.Headers.Add("origin", "https://www.tumblr.com/login");
            request.Headers.Add("referer", "https://www.tumblr.com/login");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.5;) AppleWebKit/535.25 (KHTML, like Gecko) Chrome/51.0.2713.143 Safari/533");
            request.Headers.Add("x-csrf", csrfToken);
            request.Headers.Add("x-version", "redpop/3/0//redpop/");
            request.Headers.Add("x-ad-blocker-enabled", "0");
            request.Headers.Add("priority", "u=1, i");
            request.Headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-fetch-dest", "empty");
            request.Headers.Add("sec-fetch-mode", "cors");
            request.Headers.Add("sec-fetch-site", "same-origin");

            var payload = new
            {
                username = username,
                password = password,
                grant_type = "password"
            };

            request.Content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            
            await StoreCookiesAsync(response);

            if (response.IsSuccessStatusCode)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", "Authentication successful.");
                _cache.Set(LoginedUserCacheKey, username, TimeSpan.FromMinutes(30));

                await FetchHeaderInfoForLoginedAsync();

                return true;
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", $"Authentication failed: {response.StatusCode}");
                return false;
            }
        }


        public async Task<List<string>> SearchUsersAsync(Bot bot, CancellationToken token)
        {
            List<string> userNames = new List<string>();
            int collectedAccounts = 0;
            string nextCursor = null;

            try
            {
                if (bot.Status == BotStatus.Completed) return userNames;

                bool isLoginedUser = await IsUserLogined(bot.Username);

                if (!isLoginedUser)
                {
                    
                    bool isAuthenticated = await AuthenticateAsync(bot.Username, bot.Password);
                    if (!isAuthenticated)
                    {
                        await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Authentication failed for bot '{bot.Username}'.");
                        return userNames;
                    }
                }


 
                var csrfToken = _cache.Get<string>(CsrfTokenCacheKey);
                var authorizationToken = _cache.Get<string>(AuthorizationTokenCacheKey);
                var obfuscatedFeaturesToken = _cache.Get<string>(ObfuscatedFeaturesCacheKey);

                await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Bot '{bot.Username}' started search for keyword '{bot.Keyword}'...");

                do
                {
                    token.ThrowIfCancellationRequested();

                    int getTotalAccount = await _userAccountRepository.GetTotalAccountsCountAsync(bot.Id);
                    if (getTotalAccount >= bot.MaxAccounts) break;

                    string url = $"https://www.tumblr.com/api/v2/timeline/search?limit=20&days=0&query={bot.Keyword}&mode=top&timeline_type=post&skip_component=related_tags%2Cblog_search&reblog_info=true&query_source=search_box_typed_query&fields%5Bblogs%5D=%3Fadvertiser_name%2C%3Favatar%2C%3Fblog_view_url%2C%3Fcan_be_booped%2C%3Fcan_be_followed%2C%3Fcan_show_badges%2C%3Fdescription_npf%2C%3Ffollowed%2C%3Fis_adult%2C%3Fis_member%2C%3Fis_paywall_on%2Cname%2C%3Fpaywall_access%2C%3Fprimary%2C%3Fsubscription_plan%2C%3Ftheme%2C%3Ftitle%2C%3Ftumblrmart_accessories%2Curl%2C%3Fuuid%2C%3Fshare_following%2C%3Fshare_likes%2C%3Fask";
                    if (!string.IsNullOrEmpty(nextCursor))
                    {
                        url += $"&cursor={nextCursor}";
                    }

                    var request = new HttpRequestMessage(HttpMethod.Get, url);

                    
                    request.Headers.Add("accept", "application/json;format=camelcase");
                    request.Headers.Add("authorization", $"Bearer {authorizationToken}");
                    request.Headers.Add("referer", $"https://www.tumblr.com/search/{bot.Keyword}?src=typed_query");
                    request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.5;) AppleWebKit/535.25 (KHTML, like Gecko) Chrome/51.0.2713.143 Safari/533");
                    request.Headers.Add("x-csrf", csrfToken);
                    request.Headers.Add("x-version", "redpop/3/0//redpop/");
                    request.Headers.Add("x-ad-blocker-enabled", "0");
                    request.Headers.Add("priority", "u=1, i");
                    request.Headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                    request.Headers.Add("sec-ch-ua-mobile", "?0");
                    request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.Add("sec-fetch-dest", "empty");
                    request.Headers.Add("sec-fetch-mode", "cors");
                    request.Headers.Add("sec-fetch-site", "same-origin");

                   
                    var cookies = _cache.Get<string>("TumblrCookies");
                    if (!string.IsNullOrEmpty(cookies))
                    {
                        request.Headers.Add("cookie", cookies);
                    }

                    var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, "Search failed. Please try again.");
                        return userNames;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    var posts = json["response"]["timeline"]["elements"]
                                    .Where(e => (string)e["objectType"] == "post")
                                    .Select(e => (string)e["blog"]["name"])
                                    .Distinct();

                    foreach (var blogName in posts)
                    {
                        if (!await _userAccountRepository.UserExistsAsync(blogName))
                        {
                            var userAccount = new UserAccount
                            {
                                BlogName = blogName,
                                CollectedAt = DateTime.UtcNow,
                                BotId = bot.Id  
                            };

                            await _userAccountRepository.AddUserAccountAsync(userAccount);
                            userNames.Add(blogName);
                            collectedAccounts++;
                        }
                    }

                    nextCursor = json["response"]["timeline"]["links"]?["next"]?["queryParams"]?["cursor"]?.ToString();

                    await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Bot '{bot.Username}' collected {collectedAccounts}/{bot.MaxAccounts} accounts so far...");
                    await Task.Delay(5000);

                } while (collectedAccounts < bot.MaxAccounts && !string.IsNullOrEmpty(nextCursor));

                await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"Search completed by bot '{bot.Username}'. Collected {userNames.Count} accounts.");

                return userNames;
            }
            catch (Exception ex)
            {
                await _botStatusHubContext.Clients.All.SendAsync("ReceiveStatusUpdate", bot.Id, $"STOP CAN. "+ex.Message);
            }

            return new();
           
        }

        public async Task StoreCookiesAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode) return;

            var cookieList = new List<string>();
 
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var cookie in setCookieHeaders)
                {
                    
                    var cookieParts = cookie.Split(';');
                    var cookieMainPart = cookieParts[0].Trim(); 
                    cookieList.Add(cookieMainPart);
                }
            }

         
            if (cookieList.Any())
            {
                var combinedCookies = string.Join("; ", cookieList);
                _cache.Set("TumblrCookies", combinedCookies, TimeSpan.FromMinutes(30));
            }
        }
         
        public async Task<bool> SendQuestionAsync(Bot bot, string blogName, string questionContent, string questionUrl)
        {
          
             
            var csrfToken = _cache.Get<string>(CsrfTokenCacheKey);
            var authorizationToken = _cache.Get<string>(AuthorizationTokenCacheKey);
            var cookies = _cache.Get<string>("TumblrCookies");

           
            bool isLoginedUser = await IsUserLogined(bot.Username);
            if (!isLoginedUser)
            {
                bool isAuthenticated = await AuthenticateAsync(bot.Username, bot.Password);
                if (!isAuthenticated)
                {
                    return false;
                }

                
                csrfToken = _cache.Get<string>(CsrfTokenCacheKey);
                authorizationToken = _cache.Get<string>(AuthorizationTokenCacheKey);
                cookies = _cache.Get<string>("TumblrCookies");
            }

            TumblrUserInfo? cachedUserInfo = UserInfoCache.GetUserInfo();
 
            var payload = new QuestionPayload
            {
                Content = new[]
                {
                    new Content
                    {
                        Text = questionContent,
                        Formatting = new[]
                        {
                            new Models.Formatting
                            {
                                Start = 62,
                                End = 90,
                                Url = questionUrl
                            }
                        }
                    }
                },
                            Layout = new[]
                {
                    new Layout
                    {
                        Blocks = new[] { 0 },
                        Attribution = new Attribution
                        {
                            Blog = new BlogInfo
                            {
                                Name = cachedUserInfo.Name,
                                Avatar = new[]
                                {
                                    new Avatar { Width = 512, Height = 512, Url = "https://assets.tumblr.com/images/default_avatar/octahedron_closed_512.png" }
                                },
                                Url = $"https://{cachedUserInfo.Name}.tumblr.com/"
                            }
                        }
                    }
                }
            };

           
        
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.tumblr.com/api/v2/blog/{blogName}/posts");
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


      
            request.Headers.Add("accept", "application/json;format=camelcase");
            request.Headers.Add("authorization", $"Bearer {authorizationToken}");
            request.Headers.Add("x-csrf", csrfToken);
            request.Headers.Add("origin", "https://www.tumblr.com");
            request.Headers.Add("referer", $"https://www.tumblr.com/new/ask/{blogName}");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            request.Headers.Add("x-ad-blocker-enabled", "0");
            request.Headers.Add("priority", "u=1, i");
            request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"130\", \"Google Chrome\";v=\"130\", \"Not?A_Brand\";v=\"99\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-fetch-dest", "empty");
            request.Headers.Add("sec-fetch-mode", "cors");
            request.Headers.Add("sec-fetch-site", "same-origin");
 
            if (!string.IsNullOrEmpty(cookies))
            {
                request.Headers.Add("cookie", cookies);
            }

            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
