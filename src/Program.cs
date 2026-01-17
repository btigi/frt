using System.Text.Json;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using frt.Model;

Console.WriteLine("OMDB Film Search");
Console.WriteLine("================");
Console.WriteLine();

CreateFilmTable();

Console.WriteLine("1 - Enter film details");
Console.WriteLine("2 - Output film details");
Console.WriteLine("3 - Import films from file");

var choice = Console.ReadLine();

if (choice != "1" && choice != "2" && choice != "3")
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
        
        Console.Write("\nEnter your rating (1-10): ");
        var userRating = Console.ReadLine();
        
        var yearWatched = DateTime.Now.Year.ToString();
        SaveFilmToDatabase(film, userRating, yearWatched);
    }
}
else if (choice == "2")
{
    ExportFilmTitles();
}
else if (choice == "3")
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

    Console.Write("Enter year watched (e.g. 2025): ");
    var yearWatched = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(yearWatched))
    {
        Console.WriteLine("Error: Year watched is required.");
        return;
    }

    await ImportFilmsFromFile(apiKey, yearWatched);
}

static async Task<Rootobject?> GetFilmDetailsAsync(string apiKey, string filmTitle, string? imdbId = null)
{
    string apiUrl = !string.IsNullOrWhiteSpace(imdbId)
        ? $"http://www.omdbapi.com/?apikey={apiKey}&i={Uri.EscapeDataString(imdbId)}"
        : $"http://www.omdbapi.com/?apikey={apiKey}&t={Uri.EscapeDataString(filmTitle)}";

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
                UserRating TEXT,
                YearWatched TEXT,
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

static void SaveFilmToDatabase(Rootobject film, string? userRating, string? yearWatched, bool silent = false)
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
                Production, Website, Response, UserRating, YearWatched
            ) VALUES (
                @Title, @Year, @Rated, @Released, @Runtime, @Genre, @Director, @Writer,
                @Actors, @Plot, @Language, @Country, @Awards, @Poster, @Metascore,
                @imdbRating, @imdbVotes, @imdbID, @Type, @DVD, @BoxOffice,
                @Production, @Website, @Response, @UserRating, @YearWatched
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
        command.Parameters.AddWithValue("@UserRating", string.IsNullOrWhiteSpace(userRating) ? DBNull.Value : userRating);
        command.Parameters.AddWithValue("@YearWatched", string.IsNullOrWhiteSpace(yearWatched) ? DBNull.Value : yearWatched);

        command.ExecuteNonQuery();
        if (!silent)
            Console.WriteLine("\nFilm information has been stored in the database.");
    }
    catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
    {
        if (!silent)
            Console.WriteLine($"\nWarning: A film with IMDB ID '{film.imdbID}' already exists in the database.");
    }
    catch (Exception ex)
    {
        if (!silent)
            Console.WriteLine($"\nError saving film to database: {ex.Message}");
    }
}

static (string AccentColor, string AccentColorDim, string AccentColorLight, string[] Palette) LoadTheme()
{
    const string defaultAccentColor = "#d4af37";
    const string defaultAccentColorDim = "#9a7b2a";
    const string defaultAccentColorLight = "#f0d878";
    var defaultPalette = new[] { "#d4af37", "#f0d878", "#9a7b2a", "#c9a227", "#e6c54b", "#8b6914", "#daa520", "#ffd700", "#b8860b", "#cd950c" };

    try
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        var configuration = builder.Build();

        var accentColor = configuration["Theme:AccentColor"] ?? defaultAccentColor;
        var accentColorDim = configuration["Theme:AccentColorDim"] ?? defaultAccentColorDim;
        var accentColorLight = configuration["Theme:AccentColorLight"] ?? defaultAccentColorLight;
        
        var paletteSection = configuration.GetSection("Theme:Palette");
        var palette = paletteSection.Exists() && paletteSection.GetChildren().Any()
            ? paletteSection.GetChildren().Select(c => c.Value ?? "").Where(v => !string.IsNullOrEmpty(v)).ToArray()
            : defaultPalette;

        return (accentColor, accentColorDim, accentColorLight, palette);
    }
    catch
    {
        // Return defaults if config file doesn't exist or can't be read
        return (defaultAccentColor, defaultAccentColorDim, defaultAccentColorLight, defaultPalette);
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

        // Load theme colors
        var (accentColor, accentColorDim, accentColorLight, palette) = LoadTheme();
        var paletteJson = JsonSerializer.Serialize(palette);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Title, Year, Runtime, Genre, imdbRating, imdbVotes, imdbID, 
                   Director, Actors, Plot, CreatedDate, UserRating, BoxOffice 
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
                ["CreatedDate"] = reader.IsDBNull(10) ? "" : reader.GetString(10),
                ["UserRating"] = reader.IsDBNull(11) ? "" : reader.GetString(11),
                ["BoxOffice"] = reader.IsDBNull(12) ? "" : reader.GetString(12)
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
            .Replace("{{GENERATED_DATE}}", DateTime.Now.ToString("dd MMMM yyyy"))
            .Replace("{{ACCENT_COLOR}}", accentColor)
            .Replace("{{ACCENT_COLOR_DIM}}", accentColorDim)
            .Replace("{{ACCENT_COLOR_LIGHT}}", accentColorLight)
            .Replace("{{COLOR_PALETTE}}", paletteJson);

        File.WriteAllText(outputFile, html);
        Console.WriteLine($"\nExported {films.Count} film(s) to {outputFile}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError exporting films: {ex.Message}");
    }
}

static async Task ImportFilmsFromFile(string apiKey, string yearWatched)
{
    const string filmsFile = "films.txt";

    if (!File.Exists(filmsFile))
    {
        Console.WriteLine($"\nError: File '{filmsFile}' not found.");
        return;
    }

    var lines = File.ReadAllLines(filmsFile);
    var filmEntries = new List<(string Title, string Rating, string? ImdbId)>();

    foreach (var line in lines)
    {
        if (string.IsNullOrWhiteSpace(line))
            continue;

		// "film title     rating     # tt1234567" (# and IMDB ID are optional)
		var trimmedLine = line.TrimEnd();
        
        var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"^(.+?)\s+(\d+)(?:\s*#\s*(tt\d+))?\s*$");
        
        if (match.Success)
        {
            var title = match.Groups[1].Value.Trim();
            var rating = match.Groups[2].Value;
            var imdbId = match.Groups[3].Success ? match.Groups[3].Value : null;
            filmEntries.Add((title, rating, imdbId));
        }
        else
        {
            Console.WriteLine($"Warning: Could not parse line: '{line}'");
        }
    }

    if (filmEntries.Count == 0)
    {
        Console.WriteLine("\nNo valid film entries found in the file.");
        return;
    }

    Console.WriteLine($"\nFound {filmEntries.Count} film(s) to import.");
    Console.WriteLine();
    Console.WriteLine("Validating films...");
    Console.WriteLine();

    var validatedFilms = new List<(Rootobject Film, string Rating)>();
    var failedFilms = new List<string>();

    // Phase 1: Validate all films by fetching from API
    foreach (var (title, rating, imdbId) in filmEntries)
    {
        if (!string.IsNullOrWhiteSpace(imdbId))
            Console.WriteLine($"Validating: {title} (ID: {imdbId})...");
        else
            Console.WriteLine($"Validating: {title}...");
        
        var film = await GetFilmDetailsAsync(apiKey, title, imdbId);

        if (film != null)
        {
            Console.WriteLine($"  Found: {film.Title} ({film.Year})");
            validatedFilms.Add((film, rating));
        }
        else
        {
            failedFilms.Add(title);
        }

        // Basic rate limit compliance
        await Task.Delay(250);
    }

    Console.WriteLine();
    Console.WriteLine($"Validation complete. Found: {validatedFilms.Count}, Failed: {failedFilms.Count}");

    if (failedFilms.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Failed films (fix these in films.txt and try again):");
        foreach (var title in failedFilms)
        {
            Console.WriteLine($"  - {title}");
        }
        Console.WriteLine();
        Console.WriteLine("Import aborted. No films were added to the database.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("All films validated. Importing to database...");

    foreach (var (film, rating) in validatedFilms)
    {
        SaveFilmToDatabase(film, rating, yearWatched, silent: true);
    }

    Console.WriteLine($"Successfully imported {validatedFilms.Count} film(s) to the database.");
}