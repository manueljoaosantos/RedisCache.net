using HtmlAgilityPack;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace API.Controllers
{
    public class AnalyticsController : BaseApiController
    {
        readonly CultureInfo culture = new("pt-PT");
        private readonly FeedContext _context;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private static readonly object _lockObj = new();
        public AnalyticsController(FeedContext context, IConfiguration configuration, IDistributedCache cache){
            _context = context;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpPost]
        [Route("CreatePosts/{authorId}")]
        public async Task<bool> CreatePosts(string authorId)
        {
            try
            {
                string localRSS = _configuration.GetConnectionString("LocalizationRSS");
                XDocument doc = XDocument.Load(localRSS + authorId + "/rss");
                if (doc == null)
                {
                    return false;
                }
                var entries = from item in doc.Root!.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")
                              select new Feed
                              {
                                  Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                                  Link = (item.Elements().First(i => i.Name.LocalName == "link").Value).StartsWith("/") ? "https://www.c-sharpcorner.com" + item.Elements().First(i => i.Name.LocalName == "link").Value : item.Elements().First(i => i.Name.LocalName == "link").Value,
                                  PubDate = Convert.ToDateTime(item.Elements().First(i => i.Name.LocalName == "pubDate").Value, culture),
                                  Title = item.Elements().First(i => i.Name.LocalName == "title").Value,
                                  FeedType = (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("blog") ? "Blog" : (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("news") ? "News" : "Article",
                                  Author = item.Elements().First(i => i.Name.LocalName == "author").Value
                              };

                List<Feed> feeds = entries.OrderByDescending(o => o.PubDate).ToList();
                string urlAddress = string.Empty;
                List<ArticleMatrix> articleMatrices = new();
                _ = int.TryParse(_configuration["ParallelTasksCount"], out int parallelTasksCount);

                Parallel.ForEach(feeds, new ParallelOptions { MaxDegreeOfParallelism = parallelTasksCount }, feed =>
                {
                    urlAddress = feed.Link;

                    var httpClient = new HttpClient
                    {
                        BaseAddress = new Uri(urlAddress)
                    };
                    var result = httpClient.GetAsync("").Result;

                    string strData = "";

                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        strData = result.Content.ReadAsStringAsync().Result;

                        HtmlDocument htmlDocument = new();
                        htmlDocument.LoadHtml(strData);

                        ArticleMatrix articleMatrix = new()
                        {
                            AuthorId = authorId,
                            Author = feed.Author,
                            Type = feed.FeedType,
                            Link = feed.Link,
                            Title = feed.Title,
                            PubDate = feed.PubDate
                        };

                        string category = "Videos";
                        if (htmlDocument.GetElementbyId("ImgCategory") != null)
                        {
                            category = htmlDocument.GetElementbyId("ImgCategory").GetAttributeValue("title", "");
                        }

                        articleMatrix.Category = category;

                        var view = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='ViewCounts']");
                        if (view != null)
                        {
                            articleMatrix.Views = view.InnerText;

                            if (articleMatrix.Views.Contains('m'))
                            {
                                articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000000;
                            }
                            else if (articleMatrix.Views.Contains('k'))
                            {
                                articleMatrix.ViewsCount = 9;//decimal.Parse(articleMatrix.Views[0..^1]) * 1000;
                            }
                            else
                            {
                                _ = decimal.TryParse(articleMatrix.Views, out decimal viewCount);
                                articleMatrix.ViewsCount = viewCount;
                            }
                        }
                        else
                        {
                            var newsView = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='spanNewsViews']");
                            if (newsView != null)
                            {
                                articleMatrix.Views = newsView.InnerText;

                                if (articleMatrix.Views.Contains('m'))
                                {
                                    articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000000;
                                }
                                else if (articleMatrix.Views.Contains('k'))
                                {
                                    articleMatrix.ViewsCount = 9;//decimal.Parse(articleMatrix.Views[0..^1]) * 1000;
                                }
                                else
                                {
                                    _ = decimal.TryParse(articleMatrix.Views, out decimal viewCount);
                                    articleMatrix.ViewsCount = viewCount;
                                }
                            }
                            else
                            {
                                articleMatrix.ViewsCount = 0;
                            }
                        }
                        var like = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='LabelLikeCount']");
                        if (like != null)
                        {
                            _ = int.TryParse(like.InnerText, out int likes);
                            articleMatrix.Likes = likes;
                        }

                        lock (_lockObj)
                        {
                            articleMatrices.Add(articleMatrix);
                        }
                    }
                });

                _context.ArticleMatrices!.RemoveRange(_context.ArticleMatrices.Where(x => x.AuthorId == authorId));

                foreach (ArticleMatrix articleMatrix in articleMatrices)
                {
                    if (articleMatrix.Category == "Videos")
                    {
                        articleMatrix.Type = "Video";
                    }
                    articleMatrix.Category = articleMatrix.Category!.Replace("&amp;", "&");
                    await _context.ArticleMatrices.AddAsync(articleMatrix);
                }

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(authorId);
                return true;
            }
            catch
            {
                return false;
            }
        }
        [HttpGet]
        [Route("GetAll/{authorId}/{enableCache}")]
        public async Task<List<ArticleMatrix>> GetAll(string authorId, bool enableCache)        
        {
            if (!enableCache)
            {
                return _context.ArticleMatrices!.Where(x => x.AuthorId == authorId).OrderByDescending(x => x.PubDate).ToList();
            }

            string cacheKey = authorId;

            // Trying to get data from the Redis cache
            byte[] cachedData = await _cache.GetAsync(cacheKey);
            List<ArticleMatrix> articleMatrices = new();
            if (cachedData != null)
            {
                // If the data is found in the cache, encode and deserialize cached data.
                var cachedDataString = Encoding.UTF8.GetString(cachedData);
                articleMatrices = JsonSerializer.Deserialize<List<ArticleMatrix>>(cachedDataString);
            }
            else
            {
                // If the data is not found in the cache, then fetch data from database
                articleMatrices = _context.ArticleMatrices!.Where(x => x.AuthorId == authorId).OrderByDescending(x => x.PubDate).ToList();

                // Serializing the data
                string cachedDataString = JsonSerializer.Serialize(articleMatrices);
                var dataToCache = Encoding.UTF8.GetBytes(cachedDataString);

                // Setting up the cache options 
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                // Add the data into the cache
                await _cache.SetAsync(cacheKey, dataToCache, options);
            }
            return articleMatrices;
        }
    }
}