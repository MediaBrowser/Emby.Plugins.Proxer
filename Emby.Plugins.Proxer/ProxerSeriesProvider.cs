﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using Emby.Anime;

namespace Emby.Plugins.Proxer
{
    public class ProxerSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder, IHasSupportedExternalIdentifiers
    {
        private readonly ILogger _log;
        private readonly IHttpClient _httpClient;
        public static string provider_name = ProviderNames.Proxer;
        public int Order => 6;
        public string Name => "Proxer";

        private Api _api;

        public ProxerSeriesProvider(IHttpClient httpClient, ILogManager logManager)
        {
            _log = logManager.GetLogger(Name);
            _httpClient = httpClient;

            _api = new Api(_log, httpClient);
        }

        public string[] GetSupportedExternalIdentifiers()
        {
            return new[] {

                provider_name
            };
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var aid = info.GetProviderId(provider_name);
            if (string.IsNullOrEmpty(aid))
            {
                _log.Info("Start Proxer... Searching(" + info.Name + ")");
                aid = await _api.FindSeries(info.Name, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(aid))
            {
                string WebContent = await _api.WebRequestAPI(Api.Proxer_anime_link + aid, cancellationToken).ConfigureAwait(false);
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.SetProviderId(provider_name, aid);
                result.Item.Overview = Api.Get_Overview(WebContent);
                result.ResultLanguage = "ger";
                try
                {
                    result.Item.CommunityRating = float.Parse(Api.Get_Rating(WebContent), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception) { }
                foreach (var genre in Api.Get_Genre(WebContent))
                    result.Item.AddGenre(genre);
                GenreHelper.CleanupGenres(result.Item);
            }
            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, RemoteSearchResult>();

            var aid = searchInfo.GetProviderId(provider_name);
            if (!string.IsNullOrEmpty(aid))
            {
                if (!results.ContainsKey(aid))
                    results.Add(aid, await _api.GetAnime(aid, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false));
            }

            if (!string.IsNullOrEmpty(searchInfo.Name))
            {
                List<string> ids = await _api.Search_GetSeries_list(searchInfo.Name, cancellationToken).ConfigureAwait(false);
                foreach (string a in ids)
                {
                    results.Add(a, await _api.GetAnime(a, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false));
                }
            }

            return results.Values;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }

    public class ProxerSeriesImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private Api _api;

        public ProxerSeriesImageProvider(IHttpClient httpClient, ILogManager logManager)
        {
            _httpClient = httpClient;
            _api = new Api(logManager.GetLogger(Name), httpClient);
        }

        public string Name => "Proxer";

        public bool Supports(BaseItem item) => item is Series || item is Season;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var seriesId = item.GetProviderId(ProxerSeriesProvider.provider_name);
            return GetImages(seriesId, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(string aid, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrEmpty(aid))
            {
                var primary = Api.Get_ImageUrl(await _api.WebRequestAPI(Api.Proxer_anime_link + aid, cancellationToken));
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = primary
                });
            }
            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}