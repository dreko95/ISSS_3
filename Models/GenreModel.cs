using System.Collections.Generic;

namespace ISSS3.Models {
    public class GenreModel {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; } = new();
    }
}