namespace ISSS3.Models {
    public class ReviewLemmaLink {
        public int LinkID { get; set; } // Auto-generated in DB
        public int ReviewID { get; set; }
        public int LemmaID { get; set; }
        public int Position { get; set; }
    }
}