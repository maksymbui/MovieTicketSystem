using Microsoft.VisualBasic;
using RestSharp;
using Newtonsoft.Json;

public class TMDBApi
{
    private string apiKey = "df0b8bc6934d37266ef32754dfa21420";

    public async Task GetMoviesAndSaveToFile(string filePath = "movies.json")
    {
        var options = new RestClientOptions("https://api.themoviedb.org/3");
        var client = new RestClient(options);
        var request = new RestRequest("discover/movie");

        request.AddParameter("api_key", apiKey);
        request.AddParameter("include_adult", "false");
        request.AddParameter("include_video", "false");
        request.AddParameter("language", "en-US");
        request.AddParameter("page", "1");
        request.AddParameter("sort_by", "popularity.desc");
        request.AddParameter("year", "2025");

        request.AddHeader("accept", "application/json");
        var response = await client.GetAsync(request);

        if (response.Content != null)
        {
            var movieResponse = JsonConvert.DeserializeObject<TMDBMovieResponse>(response.Content);
            var movies = movieResponse?.Results ?? new Movie[0];
            
            // Create JSON file with the movies
            var jsonContent = JsonConvert.SerializeObject(movies, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, jsonContent);
            
            Console.WriteLine($"Movies saved to {filePath}");
            Console.WriteLine($"Total movies saved: {movies.Length}");
        }
        else
        {
            throw new Exception("Failed to fetch movies from API");
        }
    }

    public async Task<Image?> LoadMoviePoster(string posterPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(posterPath))
            {
                string posterUrl = $"https://image.tmdb.org/t/p/w500{posterPath}";
                
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(posterUrl);
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        return Image.FromStream(ms);
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load poster: {ex.Message}");
            return null;
        }
    }
}