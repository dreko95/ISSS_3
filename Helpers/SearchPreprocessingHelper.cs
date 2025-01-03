using Google.Cloud.Translation.V2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Cloud.Language.V1;
using ISSS3.Models;

namespace ISSS3.Helpers
{
    public static class SearchPreprocessingHelper
    {
        static SearchPreprocessingHelper()
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Libs\refined-ensign-446604-b4-3bd4c29a1eaa.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
        }

        public static SearchOptions PrepareSearchOptions(string query, List<GenreModel> genresList) {
            var options = new SearchOptions(query);
            var quotes = GetQuotes(query);

            foreach (var quote in quotes) {
                query = query.Replace($"\"{quote}\"", "")
                             .Trim();
            }

            query = TransformToEnglish(query);
            options.Query = query;
            var lemmas = Tokenizer.Preprocess(query);

            options.Lemmas.AddRange(lemmas);
            options.Quotes.AddRange(quotes);

            options.UpdateContextOptions(genresList);


            return options;
        }

        private static string TransformToEnglish(string query) {
            var client = TranslationClient.Create();
            var detection = client.DetectLanguage(query);

            if (detection.Language == "en")
                return query;

            var translation = client.TranslateText(query, "en");
            query = translation.TranslatedText;

            return query;
        }

        private static List<string> GetQuotes(string query) {
            var quotes = new List<string>();
            var regex = new Regex("\"([^\"]*)\"");
            var matches = regex.Matches(query);

            foreach (Match match in matches) {
                quotes.Add(match.Groups[1].Value);
            }

            return quotes;
        }

        private static void UpdateContextOptions(this SearchOptions options, List<GenreModel> genresList) {
            var client = LanguageServiceClient.Create();
            var response = client.AnalyzeEntities(new Document
            {
                Content = options.Query,
                Type = Document.Types.Type.PlainText
            });

            var authors = response.Entities
                .Where(e => e.Type == Entity.Types.Type.Person)
                .Select(e => e.Name)
                .ToList();
            options.Author.AddRange(authors);

            var years = GetYearsFromQuery(options.Query);
            options.PublishedYear.AddRange(years);

            var matchedGenres = genresList
                                .Where(genre => options.Lemmas.Contains(genre.Name, StringComparer.OrdinalIgnoreCase) ||
                                                genre.Aliases.Any(alias => options.Lemmas.Contains(alias, StringComparer.OrdinalIgnoreCase)))
                                .ToList();
            options.Genres.AddRange(matchedGenres);
        }

        private static List<int> GetYearsFromQuery(string query) {
            var years = new List<int>();
            var regex = new Regex(@"\b(18|19|20)\d{2}\b");
            var matches = regex.Matches(query);

            foreach (Match match in matches) {
                if (int.TryParse(match.Value, out var year))
                    years.Add(year);
            }

            return years;
        }


    }

}
