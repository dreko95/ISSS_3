using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ISSS3.Helpers;
using ISSS3.Models;

namespace ISSS3;

public partial class BookReviewWindow {
    public BookReviewWindow(Book book, List<GenreModel> genres) {
        Genres = genres;
        InitializeComponent();
        SelectedBook = LoadBookInfo(book);
        LoadReviewsForBook(SelectedBook.BookId);
    }

    private Book SelectedBook { get; }
    private List<GenreModel> Genres { get; }

    private Book LoadBookInfo(Book book) {
        var currentBook = BdHelper.GetBookById(book.BookId, Genres) ?? book;
        BookTitleTextBlock.Text = currentBook.Title;
        BookAuthorTextBlock.Text = currentBook.Author;
        PublishedYearTextBlock.Text = currentBook.PublishedYear.ToString();
        GenresTextBlock.Text = string.Join(", ", currentBook.Genres.Select(g => g.Name));
        return currentBook;
    }

    private void AddReview_Click(object sender, RoutedEventArgs e) {
        try {
            var review = new Review {
                BookID = SelectedBook.BookId,
                ReviewText = ReviewText.Text
            };

            BdHelper.SaveReview(review);
            MessageBox.Show("Рецензію додано успішно!");
            ReviewText.Text = string.Empty;
            LoadReviewsForBook(SelectedBook.BookId);
        }
        catch (Exception ex) {
            MessageBox.Show($"Помилка: {ex.Message}");
        }
    }

    private void LoadReviewsForBook(int bookId) {
        try {
            var reviews = BdHelper.GetReviewsByBookId(bookId);
            ReviewListView.ItemsSource = reviews;
        }
        catch (Exception ex) {
            MessageBox.Show($"Помилка завантаження рецензій: {ex.Message}");
        }
    }
}