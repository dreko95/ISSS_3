using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ISSS3.Models;

namespace ISSS3.Helpers;

public static class BdHelper {
    private static readonly string _connectionString = ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;

    public static void SaveBook(Book book) {
        using (var connection = new SqlConnection(_connectionString)) {
            connection.Open();
            var transaction = connection.BeginTransaction();

            try {
                var command = new SqlCommand(
                                             "INSERT INTO Books (Title, Author, PublishedYear) VALUES (@Title, @Author, @Year); SELECT SCOPE_IDENTITY();",
                                             connection,
                                             transaction);
                command.Parameters.AddWithValue("@Title", book.Title);
                command.Parameters.AddWithValue("@Author", book.Author);
                command.Parameters.AddWithValue("@Year", book.PublishedYear);
                var bookId = Convert.ToInt32(command.ExecuteScalar());

                // Збереження жанрів
                foreach (var genre in book.Genres) {
                    command = new SqlCommand(
                                             "INSERT INTO BookCategories (BookID, CategoryID) VALUES (@BookID, @CategoryID)",
                                             connection,
                                             transaction);
                    command.Parameters.AddWithValue("@BookID", bookId);
                    command.Parameters.AddWithValue("@CategoryID", genre.Id);
                    command.ExecuteNonQuery();
                }

                var lemmas = Tokenizer.Preprocess(book.Title);

                foreach (var lemmaText in lemmas) {
                    command = new SqlCommand("SELECT LemmaID FROM Lemmas WHERE Lemma = @Lemma", connection, transaction);
                    command.Parameters.AddWithValue("@Lemma", lemmaText);
                    var lemmaId = command.ExecuteScalar() ?? SaveLemma(connection, transaction, lemmaText);

                    // Збереження лінку на лему
                    command = new SqlCommand(
                                             "INSERT INTO BookLemmaLinks (BookID, LemmaID, Position) VALUES (@BookID, @LemmaID, @Position)",
                                             connection,
                                             transaction);
                    command.Parameters.AddWithValue("@BookID", bookId);
                    command.Parameters.AddWithValue("@LemmaID", lemmaId);
                    command.Parameters.AddWithValue("@Position", lemmas.IndexOf(lemmaText));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch {
                transaction.Rollback();
                throw;
            }
        }
    }

    public static void SaveReview(Review review) {
        using (var connection = new SqlConnection(_connectionString)) {
            connection.Open();
            var transaction = connection.BeginTransaction();

            try {
                var command =
                    new SqlCommand("INSERT INTO Reviews (BookID, ReviewText) VALUES (@BookID, @ReviewText); SELECT SCOPE_IDENTITY();",
                                   connection,
                                   transaction);
                command.Parameters.AddWithValue("@BookID", review.BookID);
                command.Parameters.AddWithValue("@ReviewText", review.ReviewText);
                var reviewId = Convert.ToInt32(command.ExecuteScalar());

                var lemmas = Tokenizer.Preprocess(review.ReviewText);

                foreach (var lemmaText in lemmas) {
                    command = new SqlCommand("SELECT LemmaID FROM Lemmas WHERE Lemma = @Lemma", connection, transaction);
                    command.Parameters.AddWithValue("@Lemma", lemmaText);
                    var lemmaId = command.ExecuteScalar() ?? SaveLemma(connection, transaction, lemmaText);

                    // Збереження лінку на лему
                    command =
                        new
                            SqlCommand("INSERT INTO ReviewLemmaLinks (ReviewID, LemmaID, Position) VALUES (@ReviewID, @LemmaID, @Position)",
                                       connection,
                                       transaction);
                    command.Parameters.AddWithValue("@ReviewID", reviewId);
                    command.Parameters.AddWithValue("@LemmaID", lemmaId);
                    command.Parameters.AddWithValue("@Position", lemmas.IndexOf(lemmaText));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch {
                transaction.Rollback();
                throw;
            }
        }
    }

    public static int SaveLemma(SqlConnection connection, SqlTransaction transaction, string lemma) {
        var command = new SqlCommand("INSERT INTO Lemmas (Lemma) VALUES (@Lemma); SELECT SCOPE_IDENTITY();", connection, transaction);
        command.Parameters.AddWithValue("@Lemma", lemma);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static List<Book> GetAllBooks() {
        var books = new List<Book>();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand("SELECT BookID, Title, Author, PublishedYear FROM Books", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            books.Add(new Book {
                BookId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                PublishedYear = reader.GetInt32(3)
            });
        }

        return books;
    }

    public static Book GetBookById(int bookId, List<GenreModel> genres) {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand("SELECT BookID, Title, Author, PublishedYear FROM Books WHERE BookID = @BookID", connection);
        command.Parameters.AddWithValue("@BookID", bookId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var book = new Book {
            BookId = reader.GetInt32(0),
            Title = reader.GetString(1),
            Author = reader.GetString(2),
            PublishedYear = reader.GetInt32(3),
            Genres = new List<GenreModel>()
        };

        reader.Close();

        // Load genres for the book
        var genreCommand = new SqlCommand(@"
                            SELECT bc.CategoryID
                            FROM BookCategories bc
                            WHERE bc.BookID = @BookID",
                                          connection);
        genreCommand.Parameters.AddWithValue("@BookID", bookId);

        using var genreReader = genreCommand.ExecuteReader();
        while (genreReader.Read()) {
            var genreId = genreReader.GetInt32(0);
            var genre = genres.FirstOrDefault(g => g.Id == genreId);
            if (genre != null)
                book.Genres.Add(genre);
        }

        return book;
    }

    public static List<Review> GetReviewsByBookId(int bookId) {
        var reviews = new List<Review>();
        using (var connection = new SqlConnection(_connectionString)) {
            connection.Open();
            var command = new SqlCommand("SELECT ReviewID, ReviewText FROM Reviews WHERE BookID = @BookID", connection);
            command.Parameters.AddWithValue("@BookID", bookId);

            using (var reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    reviews.Add(new Review {
                        ReviewID = reader.GetInt32(0),
                        ReviewText = reader.GetString(1)
                    });
                }
            }
        }

        return reviews;
    }

    public static List<Book> SearchBooks(SearchOptions options) {
        var result = new List<Book>();

        var lemmaString = string.Join(" ", options.Lemmas);
        var quoteString = string.Join("|", options.Quotes);
        var authorString = string.Join(",", options.Author);
        var genreIds = options.Genres.Select(g => g.Id)
                              .Distinct()
                              .ToArray();
        var genreIdString = string.Join(",", genreIds);

        // Викликаємо процедуру
        using (var connection = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand("SearchBooks", connection)) {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@LemmaString", lemmaString);
            cmd.Parameters.AddWithValue("@QuoteString", quoteString);
            cmd.Parameters.AddWithValue("@AuthorString", authorString);
            cmd.Parameters.AddWithValue("@GenreIdString", genreIdString);
            cmd.Parameters.AddWithValue("@PageIndex", options.PageNumber - 1);
            cmd.Parameters.AddWithValue("@PageSize", options.PageSize);

            connection.Open();
            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    var book = new Book {
                        BookId = reader.GetInt32(reader.GetOrdinal("BookID")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Author = reader.GetString(reader.GetOrdinal("Author")),
                        PublishedYear = reader.GetInt32(reader.GetOrdinal("PublishedYear")),
                        RelevanceScore = reader.GetInt32(reader.GetOrdinal("RelevanceScore"))
                    };

                    result.Add(book);
                }
            }
        }

        return result;
    }

    public static List<GenreModel> GetGenres() {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand(@"
                    SELECT c.CategoryID, c.CategoryName, ga.AliasName
                    FROM Categories c
                    LEFT JOIN GenreAliases ga ON c.CategoryID = ga.GenreID",
                                     connection);

        using var reader = command.ExecuteReader();
        var genreDict = new Dictionary<int, GenreModel>();

        while (reader.Read()) {
            var genreId = reader.GetInt32(0);
            var genreName = reader.GetString(1);
            var aliasName = reader.IsDBNull(2) ? null : reader.GetString(2);

            if (!genreDict.ContainsKey(genreId)) {
                genreDict[genreId] = new GenreModel {
                    Id = genreId,
                    Name = genreName
                };
            }

            if (aliasName != null)
                genreDict[genreId]
                    .Aliases.Add(aliasName);
        }

        return genreDict.Values.ToList();
    }
}