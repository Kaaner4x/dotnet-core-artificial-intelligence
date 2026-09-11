using DotnetCoreAI.Project03_RapidApi.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;
var client = new HttpClient();
List<ApiSeriesViewModel> apiSeriesViewModels = new List<ApiSeriesViewModel>();
var request = new HttpRequestMessage
{
    Method = HttpMethod.Get,
    RequestUri = new Uri("https://imdb-top-100-movies.p.rapidapi.com/series/"),
    Headers =
    {
        { "x-rapidapi-key", "5088c917d6msh6dbc7b8055deeedp198d06jsnaaddf6399d5f" },
        { "x-rapidapi-host", "imdb-top-100-movies.p.rapidapi.com" },
    },
};
using (var response = await client.SendAsync(request))
{
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    apiSeriesViewModels = JsonConvert.DeserializeObject<List<ApiSeriesViewModel>>(body);
    foreach(var series in apiSeriesViewModels)
    {
        Console.WriteLine($"Rank: {series.rank} - Title: {series.title} - Rating: {series.rating} - Year: {series.year}");
    }
}
Console.ReadLine();