using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ISSS3.Helpers;
using ISSS3.Models;

namespace ISSS3 {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const int _pageSize = 10;

        private int _currentPage = 1;

        public MainWindow() {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Libs\refined-ensign-446604-b4-3bd4c29a1eaa.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
            InitializeComponent();
            LoadBooks();
            LoadGenres();
        }

        private List<Book> Books { get; set; }
        private List<GenreModel> Genres { get; set; } = new();
        private string SearchQuery { get; set; }
        private SearchOptions SearchOptions { get; set; }
        private List<Book> SearchResult { get; set; } = new();


        private void LoadGenres()
        {
            Genres = BdHelper.GetGenres();
            GenreListBox.ItemsSource = Genres;
        }

        private void LoadBooks() {
            try {
                Books = BdHelper.GetAllBooks();
                BookComboBox.ItemsSource = Books;
            }
            catch (Exception ex) {
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}");
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e) {
            try {
                var selectedGenres = GenreListBox.SelectedItems.Cast<GenreModel>().ToList();
                var book = new Book {
                    Title = BookTitle.Text,
                    Author = BookAuthor.Text,
                    PublishedYear = int.Parse(PublishedYear.Text),
                    Genres = selectedGenres
                };

                BdHelper.SaveBook(book);
                MessageBox.Show("Книгу додано успішно!");
                BookTitle.Text = string.Empty;
                BookAuthor.Text = string.Empty;
                PublishedYear.Text = string.Empty;
                GenreListBox.SelectedItems.Clear();
            }
            catch (Exception ex) {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void BookComboBox_SelectionChanged(object sender, RoutedEventArgs e) {
            if (BookComboBox.SelectedItem == null)
                return;

            var selected = BookComboBox.SelectedItem;
            var review = new BookReviewWindow(selected as Book, Genres);
            review.Show();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) {
            _currentPage = 1;
            SearchQuery = SearchTextBox.Text;
            SearchBooks();
        }

        private void SearchBooks() {
            if (SearchQuery.IsNullOrWhiteSpace())
                return;

            try {
                SearchOptions = SearchOptions != null && SearchOptions.OriginalQuery == SearchQuery
                                    ? SearchOptions
                                    : SearchPreprocessingHelper.PrepareSearchOptions(SearchQuery, Genres);
                SearchOptions.PageNumber = _currentPage;
                SearchOptions.PageSize = _pageSize;

                SearchResult = BdHelper.SearchBooks(SearchOptions) ?? new List<Book>();

                SearchResultsListView.ItemsSource = SearchResult;
                UpdatePageInfo();
            }
            catch (Exception ex) {
                MessageBox.Show($"Помилка пошуку: {ex.Message}");
            }
        }

        private void UpdatePageInfo() {
            PageInfoTextBlock.Text = $"Сторінка {_currentPage}";
            NextPageButton.Visibility = (!SearchResult.IsEmpty()) && SearchResult.Count == _pageSize ? Visibility.Visible : Visibility.Collapsed;
            PreviousPageButton.Visibility = _currentPage > 1 ? Visibility.Visible : Visibility.Collapsed;
            SearchResultsListView.Visibility = Visibility.Visible;
            PageInfoTextBlock.Visibility = Visibility.Visible;
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e) {
            if (_currentPage <= 1)
                return;

            _currentPage--;
            SearchBooks();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e) {
            if (SearchResult.IsEmpty())
                return;

            _currentPage++;
            SearchBooks();
        }

        private void SearchResultsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (!(SearchResultsListView.SelectedItem is Book selectedBook))
                return;

            var bookReviewWindow = new BookReviewWindow(selectedBook, Genres);
            bookReviewWindow.Show();
        }
    }
}