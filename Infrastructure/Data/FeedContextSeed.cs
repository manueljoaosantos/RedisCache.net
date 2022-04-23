using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public class FeedContextSeed
    {
        public static async Task SeedAsync(FeedContext context, ILoggerFactory loggerFactory)
        {
            try
            {
                if (!context.ArticleMatrices!.Any())
                {
                    var ArticleMatricesData = File.ReadAllText("../Infrastructure/Data/SeedData/feedrss.json");
                    var ArticleMatrices = JsonSerializer.Deserialize<List<ArticleMatrix>>(ArticleMatricesData);

                    foreach (var item in ArticleMatrices)
                    {
                        context.ArticleMatrices!.Add(item);
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<FeedContextSeed>();
                logger.LogError(ex.Message);
            }
        }
    }
}
