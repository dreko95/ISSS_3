using System.Collections.Generic;

namespace ISSS3.Models {
    public class Book {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int PublishedYear { get; set; }
        public List<GenreModel> Genres { get; set; } = new();

        public double RelevanceScore { get; set; }
    }
}