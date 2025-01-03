using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LemmaSharp.Classes;

namespace ISSS3.Helpers {
    public static class Tokenizer {
        private static readonly Lemmatizer lemmatizer;

        private static readonly HashSet<string> stopWords = new HashSet<string> {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "her", "his", "i", "in", "is", "it", "its",
            "of", "on", "or", "that", "the", "to", "was", "we", "were", "with",
            "you"
        };

        static Tokenizer() {
            var stream = File.OpenRead($"{AppDomain.CurrentDomain.BaseDirectory}Libs\\full7z-mlteast-en.lem");
            lemmatizer = new Lemmatizer(stream);
        }

        // Токенізуємо текст. Прибираємо все крім букв і цифр та приводимо до нижнього регістру
        private static List<string> Tokenize(string text) {
            return Regex.Split(text.ToLower(), @"\W+")
                        .Where(token => token.Length > 1)
                        .ToList();
        }

        // Видаляємо стоп слова
        private static List<string> RemoveStopWords(List<string> tokens) {
            return tokens.Where(token => !stopWords.Contains(token))
                         .ToList();
        }

        // Лематизація
        private static List<string> Lemmatize(List<string> tokens) {
            // Для лиматизації слів використав бібліотеку LemmaGenerator 
            return tokens.Select(token => lemmatizer.Lemmatize(token))
                         .ToList();
        }

        public static List<string> Preprocess(string text) {
            var tokens = Tokenize(text);
            tokens = RemoveStopWords(tokens);
            return Lemmatize(tokens);
        }
    }
}