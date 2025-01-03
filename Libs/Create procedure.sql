CREATE PROCEDURE [dbo].[SearchBooks]
(
    @LemmaString    NVARCHAR(MAX),
    @QuoteString    NVARCHAR(MAX), 
    @AuthorString   NVARCHAR(MAX), 
    @GenreIdString  NVARCHAR(MAX), 
    @PageIndex      INT,
    @PageSize       INT
)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #SearchLemmas
    (
        Lemma NVARCHAR(100),
        SeqNo INT
    );

    ;WITH cteLemmas AS
    (
        SELECT 
            TRIM(value) AS Lemma,
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SeqNo
        FROM string_split(@LemmaString, ' ')
        WHERE TRIM(value) <> ''
    )
    INSERT INTO #SearchLemmas(Lemma, SeqNo)
    SELECT Lemma, SeqNo
    FROM cteLemmas;

    DECLARE @CountOfSearchLemmas INT = (SELECT COUNT(*) FROM #SearchLemmas);

    CREATE TABLE #Quotes
    (
        Quote NVARCHAR(MAX)
    );

    INSERT INTO #Quotes (Quote)
    SELECT TRIM(value)
    FROM string_split(@QuoteString, '|')  -- цитати розділено символом '|'
    WHERE TRIM(value) <> '';

    CREATE TABLE #Authors
    (
        Author NVARCHAR(200)
    );

    INSERT INTO #Authors (Author)
    SELECT TRIM(value)
    FROM string_split(@AuthorString, ',')
    WHERE TRIM(value) <> '';

    CREATE TABLE #GenreIds
    (
        GenreID INT
    );

    INSERT INTO #GenreIds (GenreID)
    SELECT CAST(value AS INT)
    FROM string_split(@GenreIdString, ',')
    WHERE TRIM(value) <> '';

    ----------------------------------------------------------------------------
    --  Обчислюємо RelevanceScore
    ----------------------------------------------------------------------------

SELECT * FROM (
	SELECT
        b.BookID,
        b.Title,
        b.Author,
        b.PublishedYear,

        (
            --------------------------------------------------------------------
            -- (1) +30: якщо знайдена хоч одна lemma з пошуку в назві книги
            --------------------------------------------------------------------
            CASE WHEN EXISTS
            (
                SELECT 1
                FROM BookLemmaLinks bl
                JOIN Lemmas l ON l.LemmaID = bl.LemmaID
                JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                WHERE bl.BookID = b.BookID
            )
            THEN 30 ELSE 0 END

            --------------------------------------------------------------------
            -- (2) +50: якщо ВСІ леми пошуку є в назві книги
            --------------------------------------------------------------------
            + CASE WHEN
            (
                SELECT COUNT(DISTINCT l.Lemma)
                FROM BookLemmaLinks bl
                JOIN Lemmas l ON l.LemmaID = bl.LemmaID
                JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                WHERE bl.BookID = b.BookID
            ) = @CountOfSearchLemmas
            THEN 50 ELSE 0 END

            --------------------------------------------------------------------
            -- (3) +100: всі леми підряд у назві
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT 
                        bl.BookID,
                        (bl.Position - sl.SeqNo) AS offsetConsecutive
                    FROM BookLemmaLinks bl
                    JOIN Lemmas l ON l.LemmaID = bl.LemmaID
                    JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                    WHERE bl.BookID = b.BookID
                ) AS X
                GROUP BY X.BookID, X.offsetConsecutive
                HAVING COUNT(*) = @CountOfSearchLemmas
            )
            THEN 100 ELSE 0 END

            --------------------------------------------------------------------
            -- (4) +120: збіг по автору
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1 FROM #Authors a
                WHERE b.Author = a.Author
            )
            THEN 120 ELSE 0 END

            --------------------------------------------------------------------
            -- (5) +100: книга належить до визначеного жанру
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1
                FROM BookCategories bc
                JOIN #GenreIds gi ON bc.CategoryID = gi.GenreID
                WHERE bc.BookID = b.BookID
            )
            THEN 100 ELSE 0 END

            --------------------------------------------------------------------
            -- (6) +20: якщо хоч одна lemma з пошуку у відгуках книги
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1
                FROM Reviews r
                JOIN ReviewLemmaLinks rl ON rl.ReviewID = r.ReviewID
                JOIN Lemmas l ON l.LemmaID = rl.LemmaID
                JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                WHERE r.BookID = b.BookID
            )
            THEN 20 ELSE 0 END

            --------------------------------------------------------------------
            -- (7) +40: якщо ВСІ леми пошуку є у відгуках книги
            --------------------------------------------------------------------
            + CASE WHEN
            (
                SELECT COUNT(DISTINCT l.Lemma)
                FROM Reviews r
                JOIN ReviewLemmaLinks rl ON rl.ReviewID = r.ReviewID
                JOIN Lemmas l ON l.LemmaID = rl.LemmaID
                JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                WHERE r.BookID = b.BookID
            ) = @CountOfSearchLemmas
            THEN 40 ELSE 0 END

            --------------------------------------------------------------------
            -- (8) +80: всі леми підряд у відгуках
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1
                FROM
                (
                    SELECT 
                        r.BookID,
                        (rl.Position - sl.SeqNo) AS offsetConsecutive
                    FROM Reviews r
                    JOIN ReviewLemmaLinks rl ON rl.ReviewID = r.ReviewID
                    JOIN Lemmas l ON l.LemmaID = rl.LemmaID
                    JOIN #SearchLemmas sl ON sl.Lemma = l.Lemma
                    WHERE r.BookID = b.BookID
                ) AS Y
                GROUP BY Y.BookID, Y.offsetConsecutive
                HAVING COUNT(*) = @CountOfSearchLemmas
            )
            THEN 80 ELSE 0 END

            --------------------------------------------------------------------
            -- (9) +120: якщо хоч одна цитата (#Quotes) входить у назву чи відгук
            --------------------------------------------------------------------
            + CASE WHEN EXISTS
            (
                SELECT 1
                FROM #Quotes q
                WHERE b.Title LIKE '%' + q.Quote + '%'
                      OR EXISTS
                         (
                             SELECT 1
                             FROM Reviews r2
                             WHERE r2.BookID = b.BookID
                               AND r2.ReviewText LIKE '%' + q.Quote + '%'
                         )
            )
            THEN 120 ELSE 0 END

        ) AS RelevanceScore

    FROM Books b) r
	WHERE RelevanceScore > 0
    ORDER BY RelevanceScore DESC
         OFFSET (@PageIndex * @PageSize) ROWS
         FETCH NEXT @PageSize ROWS ONLY;


    DROP TABLE #SearchLemmas;
    DROP TABLE #Quotes;
    DROP TABLE #Authors;
    DROP TABLE #GenreIds;
END
GO