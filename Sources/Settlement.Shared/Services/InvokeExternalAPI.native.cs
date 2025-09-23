using System.Text;
using Newtonsoft.Json;
using Polly;

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
        response.EnsureSuccessStatusCode();

        return response;
    }

    public async Task<HttpResponseMessage> PutAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.PutAsync(new Uri(url), requestContent);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var policy = Policy
           .Handle<HttpRequestException>()
           .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        var httpClient = httpClientFactory.CreateClient();

        var json = await policy.ExecuteAsync(async () =>
        {
            var response = await httpClient.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        });

        return JsonConvert.DeserializeObject<T>(json);
    }
}