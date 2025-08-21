using Fanex.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Settlement.Shared.Services;

public interface IInvokeExternalAPI
{
    Task<HttpResponseMessage> PostAsync(object request, string url);

    Task<T> GetAsync<T>(string url);

    Task<HttpResponseMessage> PutAsync(object request, string url);
}

public class InvokeExternalAPI(IHttpClientFactory httpClientFactory) : IInvokeExternalAPI
{
    public async Task<HttpResponseMessage> PostAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.PostAsync(new Uri(url), requestContent);

        return response;
    }

    public async Task<HttpResponseMessage> PutAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.PutAsync(new Uri(url), requestContent);
        return response;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            await LogService.InfoAsync($"Markets: " + JsonConvert.SerializeObject(responseContent));
            return JsonConvert.DeserializeObject<T>(responseContent);
        }
        catch (HttpRequestException exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Queue Handler (Call API Failed): {url}", exception);
            return default;
        }
    }
}