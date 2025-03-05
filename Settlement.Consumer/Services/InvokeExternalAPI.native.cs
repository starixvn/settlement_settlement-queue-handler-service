using Newtonsoft.Json;
using Settlement.Consumer.Models;
using System.Text;

namespace Settlement.Consumer.Services;
public interface IInvokeExternalAPI
{
    Task PostAsync(object request, string url);

    Task<T?> GetAsync<T>(string url);

    Task<Response> PutAsync(object request, string url);
}

public class InvokeExternalAPI(IHttpClientFactory httpClientFactory) : IInvokeExternalAPI
{
    public async Task PostAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        try
        {
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = httpClientFactory.CreateClient();

            var response = await httpClient.PostAsync(new Uri(url), requestContent);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Queue Handler (Call API Failed): {url} : {json}", exception);
        }
    }

    public async Task<Response> PutAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        try
        {
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.PutAsync(new Uri(url), requestContent);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Response>(responseContent)!;

        }
        catch (HttpRequestException exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Queue Handler (Call API Failed): {url} : {json}", exception);
        }

        return new Response { IsSuccess = true, Message = "" };
    }


    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseContent);
        }
        catch (HttpRequestException exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Queue Handler (Call API Failed): {url}", exception);
            return default;
        }
    }
}
