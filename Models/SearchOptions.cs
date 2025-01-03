using System.Collections.Generic;

namespace ISSS3.Models;

public class SearchOptions {
    public SearchOptions(string originalQuery = null)
    {
        OriginalQuery = originalQuery;
    }

    public SearchOptions(List<string> lemmas, List<string> quotes, List<string> author, List<int> publishedYear) {
        Lemmas = lemmas;
        Quotes = quotes;
        Author = author;
        PublishedYear = publishedYear;
    }

    public string OriginalQuery { get; set; }
    public string Query { get; set; }
    public List<string> Lemmas { get; set; } = new();
    public List<string> Quotes { get; set; } = new();

    public List<string> Author { get; set; } = new();
    public List<int> PublishedYear { get; set; } = new();
    public List<GenreModel> Genres { get; set; } = new();


    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}