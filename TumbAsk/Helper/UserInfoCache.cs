using TumbAsk.Models;

namespace TumbAsk.Helper
{
    public static class UserInfoCache
    {
        private static TumblrUserInfo _cachedUserInfo;
        private static DateTime _cacheExpiration;

        public static void SetUserInfo(TumblrUserInfo userInfo, int cacheDurationInMinutes = 30)
        {
            _cachedUserInfo = userInfo;
            _cacheExpiration = DateTime.Now.AddMinutes(cacheDurationInMinutes);
        }

        public static TumblrUserInfo GetUserInfo()
        {
            if (_cachedUserInfo != null && DateTime.Now < _cacheExpiration)
            {
                return _cachedUserInfo;
            }

            return null; 
        }
    }

}
