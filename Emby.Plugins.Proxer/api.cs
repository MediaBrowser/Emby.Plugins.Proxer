using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Anime;
using MediaBrowser.Common.Net;
using System.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Logging;

namespace Emby.Plugins.Proxer
{
    /// <summary>
    /// API for http://proxer.me/ german anime database.
    /// 🛈 Proxer does not have an API interface to work with
    /// </summary>
    internal class Api
    {
        public static List<string> anime_search_names = new List<string>();
        public static List<string> anime_search_ids = new List<string>();
        public static string SearchLink = "http://proxer.me/search?s=search&name={0}&typ=all-anime&tags=&notags=#top";
        public static string Proxer_anime_link = "http://proxer.me/info/";

        private IHttpClient _httpClient;
        private ILogger _logger;

        /// <summary>
        /// WebContent API call to get a anime with id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Api(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// API call to get a anime with the id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RemoteSearchResult> GetAnime(string id, string preferredLanguage, CancellationToken cancellationToken)
        {
            string WebContent = await WebRequestAPI(Proxer_anime_link + id, cancellationToken).ConfigureAwait(false);

            var result = new RemoteSearchResult
            {
                Name = SelectName(WebContent, preferredLanguage)
            };

            result.SearchProviderName = One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"<td><b>Original Titel<\/b><\/td>([\S\s]*?)\/td>"), WebContent));
            result.ImageUrl = Get_ImageUrl(WebContent);
            result.SetProviderId(ProxerSeriesProvider.provider_name, id);
            result.Overview = Get_Overview(WebContent);

            return result;
        }

        /// <summary>
        /// Get the right name lang
        /// </summary>
        /// <param name="WebContent"></param>
        /// <param name="preference"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private static string SelectName(string WebContent, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(preferredLanguage) || preferredLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                var title = Get_title("en", WebContent);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                var title = Get_title("de", WebContent);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            if (string.Equals(preferredLanguage, "ja", StringComparison.OrdinalIgnoreCase))
            {
                var title = Get_title("jap", WebContent);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return Get_title("jap_r", WebContent);
        }

        /// <summary>
        /// API call to get the name in the called lang
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public static string Get_title(string lang, string WebContent)
        {
            switch (lang)
            {
                case "en":
                    return One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"<td><b>Englischer Titel<\/b><\/td>([\S\s]*?)\/td>"), WebContent));

                case "de":

                    return One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"<td><b>Deutscher Titel<\/b><\/td>([\S\s]*?)\/td>"), WebContent));

                case "jap":
                    return One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"<td><b>Japanischer Titel<\/b><\/td>([\S\s]*?)\/td>"), WebContent));

                //Default is jap_r
                default:
                    return One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"<td><b>Original Titel<\/b><\/td>([\S\s]*?)\/td>"), WebContent));
            }
        }

        /// <summary>
        /// API call to get the genres of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public static List<string> Get_Genre(string WebContent)
        {
            List<string> result = new List<string>();
            string Genres = One_line_regex(new Regex(@"<b>Genre<\/b>((?:.*?\r?\n?)*)<\/tr>"), WebContent);
            int x = 1;
            string Proxer_Genre = null;
            while (Proxer_Genre != "")
            {
                Proxer_Genre = One_line_regex(new Regex("\">" + @"((?:.*?\r?\n?)*)<"), Genres, 1, x);
                if (Proxer_Genre != "")
                {
                    result.Add(Proxer_Genre);
                }
                x++;
            }
            return result;
        }

        /// <summary>
        /// API call to get the ratings of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public static string Get_Rating(string WebContent)
        {
            return One_line_regex(new Regex("<span class=\"average\">" + @"(.*?)<"), WebContent);
        }

        /// <summary>
        /// API call to get the ImageUrl if the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public static string Get_ImageUrl(string WebContent)
        {
            string url = "http://" + One_line_regex(new Regex("<img src=\"" + @"\/\/((?:.*?\r?\n?)*)" + "\""), WebContent);

            if (url.Contains("cdn.proxer.me/cover"))
                return url;

            return "";
        }

        /// <summary>
        /// API call to get the description of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public static string Get_Overview(string WebContent)
        {
            return One_line_regex(new Regex(@"Beschreibung:<\/b><br>((?:.*?\r?\n?)*)<\/td>"), WebContent);
        }

        /// <summary>
        /// Search a title and return the right one back
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> Search_GetSeries(string title, CancellationToken cancellationToken, bool bettersearchresults = false)
        {
            anime_search_names.Clear();
            anime_search_ids.Clear();
            string result = null;
            string result_text = null;
            string WebContent = "";
            if (bettersearchresults)
            {
                WebContent = await WebRequestAPI(string.Format(SearchLink, Uri.EscapeUriString(Equals_check.Half_string(title, 4, 60))), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                WebContent = await WebRequestAPI(string.Format(SearchLink, Uri.EscapeUriString(title)), cancellationToken).ConfigureAwait(false);
            }
            int x = 0;
            while (result_text != "")
            {
                result_text = One_line_regex(new Regex("<tr align=\"" + @"left(.*?)tr>"), WebContent, 1, x);
                if (result_text != "")
                {
                    //get id
                    string id = One_line_regex(new Regex("class=\"entry" + @"(.*?)" + "\">"), result_text);
                    string a_name = One_line_regex(new Regex("#top\">" + @"(.*?)</a>"), result_text);
                    if (Equals_check.Compare_strings(a_name, title))
                    {
                        result = id;
                        return result;
                    }
                    if (Int32.TryParse(id, out int n))
                    {
                        anime_search_names.Add(a_name);
                        anime_search_ids.Add(id);
                    }
                }
                x++;
            }

            return result;
        }

        /// <summary>
        /// Search a title and return a list back
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<string>> Search_GetSeries_list(string title, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();
            string result_text = null;
            string WebContent = await WebRequestAPI(string.Format(SearchLink, Uri.EscapeUriString(title)), cancellationToken).ConfigureAwait(false);
            int x = 0;
            while (result_text != "")
            {
                result_text = One_line_regex(new Regex("<tr align=\"" + @"left(.*?)tr>"), WebContent, 1, x);
                if (result_text != "")
                {
                    //get id

                    string id = One_line_regex(new Regex("class=\"entry" + @"(.*?)" + "\">"), result_text);
                    string a_name = One_line_regex(new Regex("#top\">" + @"(.*?)</a>"), result_text);
                    if (Equals_check.Compare_strings(a_name, title))
                    {
                        result.Add(id);
                        return result;
                    }
                    if (Int32.TryParse(id, out int n))
                    {
                        result.Add(id);
                    }
                }
                x++;
            }
            return result;
        }

        /// <summary>
        /// API call too find a series
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> FindSeries(string title, CancellationToken cancellationToken)
        {
            string aid = await Search_GetSeries(title, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }
            else
            {
                int x = 0;

                foreach (string a_name in anime_search_names)
                {
                    if (Equals_check.Compare_strings(a_name, title))
                    {
                        return anime_search_ids[x];
                    }
                    x++;
                }
            }
            aid = await Search_GetSeries(Equals_check.Clear_name(title), cancellationToken, true).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }
            aid = await Search_GetSeries(Equals_check.Clear_name_step2(title), cancellationToken, true).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }
            return null;
        }

        /// <summary>
        /// Simple async regex call
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="match"></param>
        /// <param name="group"></param>
        /// <param name="match_int"></param>
        /// <returns></returns>
        public static string One_line_regex(Regex regex, string match, int group = 1, int match_int = 0)
        {
            int x = 0;
            MatchCollection matches = regex.Matches(match);
            foreach (Match _match in matches)
            {
                if (x == match_int)
                {
                    return _match.Groups[group].Value.ToString();
                }
                x++;
            }
            return "";
        }

        public async Task<string> WebRequestAPI(string link, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = link
            };

            options.RequestHeaders["Cookie"] = "Adult=1";

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }
    }
}