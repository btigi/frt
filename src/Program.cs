using System.Text.Json;
using Microsoft.Data.Sqlite;
using frt.Model;

Console.WriteLine("OMDB Film Search");
Console.WriteLine("================");
Console.WriteLine();

CreateFilmTable();

Console.WriteLine("1 - Enter film details");
Console.WriteLine("2 - Output film details");

var choice = Console.ReadLine();

if (choice != "1" && choice != "2")
{
    Console.WriteLine("Error: Invalid choice.");
    return;
}

if (choice == "1")
{
    var apiKey = Environment.GetEnvironmentVariable("OMDB_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.Write("Enter API key: ");
        apiKey = Console.ReadLine();
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.WriteLine("Error: API key is required.");
        return;
    }

    Console.Write("Enter film title: ");
    var filmTitle = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(filmTitle))
    {
        Console.WriteLine("Error: Film title is required.");
        return;
    }

    Rootobject? film = await GetFilmDetailsAsync(apiKey, filmTitle);

    if (film != null)
    {
        DisplayFilmInfo(film);   
        SaveFilmToDatabase(film);
    }
}
else if (choice == "2")
{
    ExportFilmTitles();
}

static async Task<Rootobject?> GetFilmDetailsAsync(string apiKey, string filmTitle)
{
    string apiUrl = $"http://www.omdbapi.com/?apikey={apiKey}&t={Uri.EscapeDataString(filmTitle)}";

    try
    {
        using var client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(apiUrl);
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error: API request failed with status code {response.StatusCode}");
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var result = JsonSerializer.Deserialize<Rootobject>(jsonResponse, options);

        if (result == null)
        {
            Console.WriteLine("Error: Failed to deserialize API response.");
            return null;
        }

        if (result.Response?.ToLower() == "false")
        {
            Console.WriteLine($"Error: {result.Response}");
            if (!string.IsNullOrWhiteSpace(result.Title))
            {
                Console.WriteLine($"Message: {result.Title}");
            }
            return null;
        }

        return result;
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Error: Failed to connect to OMDB API. {ex.Message}");
        return null;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error: Failed to parse JSON response. {ex.Message}");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return null;
    }
}

static void DisplayFilmInfo(Rootobject film)
{
    Console.WriteLine();
    Console.WriteLine("Film Information");
    Console.WriteLine("================");
    Console.WriteLine($"Title: {film.Title}");
    Console.WriteLine($"Year: {film.Year}");
    Console.WriteLine($"Rated: {film.Rated}");
    Console.WriteLine($"Released: {film.Released}");
    Console.WriteLine($"Runtime: {film.Runtime}");
    Console.WriteLine($"Genre: {film.Genre}");
    Console.WriteLine($"Director: {film.Director}");
    Console.WriteLine($"Writer: {film.Writer}");
    Console.WriteLine($"Actors: {film.Actors}");
    Console.WriteLine($"Plot: {film.Plot}");
    Console.WriteLine($"Language: {film.Language}");
    Console.WriteLine($"Country: {film.Country}");
    Console.WriteLine($"Awards: {film.Awards}");
    Console.WriteLine($"Metascore: {film.Metascore}");
    Console.WriteLine($"IMDB Rating: {film.imdbRating}");
    Console.WriteLine($"IMDB Votes: {film.imdbVotes}");
    Console.WriteLine($"IMDB ID: {film.imdbID}");
    Console.WriteLine($"Box Office: {film.BoxOffice}");
    
    if (film.Ratings != null && film.Ratings.Length > 0)
    {
        Console.WriteLine("\nRatings:");
        foreach (var rating in film.Ratings)
        {
            Console.WriteLine($"  {rating.Source}: {rating.Value}");
        }
    }
}

static void CreateFilmTable()
{
    const string connectionString = "Data Source=films.db";
    
    try
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Films (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT,
                Year TEXT,
                Rated TEXT,
                Released TEXT,
                Runtime TEXT,
                Genre TEXT,
                Director TEXT,
                Writer TEXT,
                Actors TEXT,
                Plot TEXT,
                Language TEXT,
                Country TEXT,
                Awards TEXT,
                Poster TEXT,
                Metascore TEXT,
                imdbRating TEXT,
                imdbVotes TEXT,
                imdbID TEXT UNIQUE,
                Type TEXT,
                DVD TEXT,
                BoxOffice TEXT,
                Production TEXT,
                Website TEXT,
                Response TEXT,
                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        command.ExecuteNonQuery();
        Console.WriteLine("Database table 'Films' created successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating database table: {ex.Message}");
    }
}

static void SaveFilmToDatabase(Rootobject film)
{
    const string connectionString = "Data Source=films.db";
    
    try
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Films (
                Title, Year, Rated, Released, Runtime, Genre, Director, Writer,
                Actors, Plot, Language, Country, Awards, Poster, Metascore,
                imdbRating, imdbVotes, imdbID, Type, DVD, BoxOffice,
                Production, Website, Response
            ) VALUES (
                @Title, @Year, @Rated, @Released, @Runtime, @Genre, @Director, @Writer,
                @Actors, @Plot, @Language, @Country, @Awards, @Poster, @Metascore,
                @imdbRating, @imdbVotes, @imdbID, @Type, @DVD, @BoxOffice,
                @Production, @Website, @Response
            )";

        command.Parameters.AddWithValue("@Title", (object?)film.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("@Year", (object?)film.Year ?? DBNull.Value);
        command.Parameters.AddWithValue("@Rated", (object?)film.Rated ?? DBNull.Value);
        command.Parameters.AddWithValue("@Released", (object?)film.Released ?? DBNull.Value);
        command.Parameters.AddWithValue("@Runtime", (object?)film.Runtime ?? DBNull.Value);
        command.Parameters.AddWithValue("@Genre", (object?)film.Genre ?? DBNull.Value);
        command.Parameters.AddWithValue("@Director", (object?)film.Director ?? DBNull.Value);
        command.Parameters.AddWithValue("@Writer", (object?)film.Writer ?? DBNull.Value);
        command.Parameters.AddWithValue("@Actors", (object?)film.Actors ?? DBNull.Value);
        command.Parameters.AddWithValue("@Plot", (object?)film.Plot ?? DBNull.Value);
        command.Parameters.AddWithValue("@Language", (object?)film.Language ?? DBNull.Value);
        command.Parameters.AddWithValue("@Country", (object?)film.Country ?? DBNull.Value);
        command.Parameters.AddWithValue("@Awards", (object?)film.Awards ?? DBNull.Value);
        command.Parameters.AddWithValue("@Poster", (object?)film.Poster ?? DBNull.Value);
        command.Parameters.AddWithValue("@Metascore", (object?)film.Metascore ?? DBNull.Value);
        command.Parameters.AddWithValue("@imdbRating", (object?)film.imdbRating ?? DBNull.Value);
        command.Parameters.AddWithValue("@imdbVotes", (object?)film.imdbVotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@imdbID", (object?)film.imdbID ?? DBNull.Value);
        command.Parameters.AddWithValue("@Type", (object?)film.Type ?? DBNull.Value);
        command.Parameters.AddWithValue("@DVD", (object?)film.DVD ?? DBNull.Value);
        command.Parameters.AddWithValue("@BoxOffice", (object?)film.BoxOffice ?? DBNull.Value);
        command.Parameters.AddWithValue("@Production", (object?)film.Production ?? DBNull.Value);
        command.Parameters.AddWithValue("@Website", (object?)film.Website ?? DBNull.Value);
        command.Parameters.AddWithValue("@Response", (object?)film.Response ?? DBNull.Value);

        command.ExecuteNonQuery();
        Console.WriteLine("\nFilm information has been stored in the database.");
    }
    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
    {
        Console.WriteLine($"\nWarning: A film with IMDB ID '{film.imdbID}' already exists in the database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError saving film to database: {ex.Message}");
    }
}

static void ExportFilmTitles()
{
    const string connectionString = "Data Source=films.db";
    const string templateFile = "template.html";
    const string outputFile = "output.html";
    
    try
    {
        if (!File.Exists(templateFile))
        {
            Console.WriteLine($"\nError: Template file '{templateFile}' not found.");
            return;
        }

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Title, Year, Runtime, Genre, imdbRating, imdbVotes, imdbID, 
                   Director, Actors, Plot, CreatedDate 
            FROM Films 
            ORDER BY CreatedDate DESC";

        var films = new List<Dictionary<string, string>>();
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var film = new Dictionary<string, string>
            {
                ["Title"] = reader.IsDBNull(0) ? "" : reader.GetString(0),
                ["Year"] = reader.IsDBNull(1) ? "" : reader.GetString(1),
                ["Runtime"] = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ["Genre"] = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ["imdbRating"] = reader.IsDBNull(4) ? "" : reader.GetString(4),
                ["imdbVotes"] = reader.IsDBNull(5) ? "" : reader.GetString(5),
                ["imdbID"] = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ["Director"] = reader.IsDBNull(7) ? "" : reader.GetString(7),
                ["Actors"] = reader.IsDBNull(8) ? "" : reader.GetString(8),
                ["Plot"] = reader.IsDBNull(9) ? "" : reader.GetString(9),
                ["CreatedDate"] = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };
            films.Add(film);
        }

        if (films.Count == 0)
        {
            Console.WriteLine("\nNo films found in the database.");
            return;
        }

        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        var filmsJson = JsonSerializer.Serialize(films, jsonOptions);

        var template = File.ReadAllText(templateFile);
        var html = template
            .Replace("{{FILM_DATA}}", filmsJson)
            .Replace("{{GENERATED_DATE}}", DateTime.Now.ToString("dd MMMM yyyy"));

        File.WriteAllText(outputFile, html);
        Console.WriteLine($"\nExported {films.Count} film(s) to {outputFile}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError exporting films: {ex.Message}");
    }
}