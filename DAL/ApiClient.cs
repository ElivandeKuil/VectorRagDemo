using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.DAL
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly LogboekDbContext _logboekContext;

        public ApiClient(HttpClient httpClient, LogboekDbContext logboekContext)
        {
            _httpClient = httpClient;
            _logboekContext = logboekContext;
        }

        public async Task<HttpResponseMessage> PostAsync(
            string requestUri,
            HttpContent content,
            string? authorizationToken = null,
            Guid? correlationId = null,
            string? clientIP = null)
        {
            return await SendAsync(HttpMethod.Post, requestUri, content, authorizationToken, correlationId, clientIP);
        }

        public async Task<HttpResponseMessage> GetAsync(
            string requestUri,
            string? authorizationToken = null,
            Guid? correlationId = null,
            string? clientIP = null)
        {
            return await SendAsync(HttpMethod.Get, requestUri, null, authorizationToken, correlationId, clientIP);
        }

        public async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string requestUri,
            HttpContent? content = null,
            string? authorizationToken = null,
            Guid? correlationId = null,
            string? clientIP = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var logEntry = new ApiCallLog
            {
                RequestMethod = method.Method,
                RequestUri = requestUri,
                CorrelationId = correlationId ?? Guid.NewGuid(),
                ClientIP = clientIP,
                GemaaktOp = DateTime.UtcNow
            };

            HttpResponseMessage? response = null;
            string? requestContent = null;

            try
            {
                // Build the request
                var request = new HttpRequestMessage(method, requestUri);

                // Set authorization header if provided
                if (!string.IsNullOrEmpty(authorizationToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken);
                }

                // Add content if provided
                if (content != null)
                {
                    request.Content = content;
                    requestContent = await content.ReadAsStringAsync();
                }

                // Capture request headers
                logEntry.RequestHeaders = SerializeHeaders(request.Headers, content?.Headers);
                logEntry.RequestContent = TruncateIfNeeded(requestContent, 10000);

                // Send the request
                response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                // Capture response details
                logEntry.ResponseStatus = (int)response.StatusCode;
                logEntry.ResponseHeaders = SerializeHeaders(response.Headers, response.Content?.Headers);

                var responseContent = await response.Content.ReadAsStringAsync();
                logEntry.ResponseContent = TruncateIfNeeded(responseContent, 10000);
                logEntry.DurationMs = (int)stopwatch.ElapsedMilliseconds;

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logEntry.ErrorMessage = TruncateIfNeeded($"{ex.GetType().Name}: {ex.Message}", 2000);
                logEntry.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                logEntry.ResponseStatus = response?.StatusCode != null ? (int)response.StatusCode : 0;

                throw;
            }
            finally
            {
                await LogToDatabase(logEntry);
            }
        }

        private async Task LogToDatabase(ApiCallLog logEntry)
        {
            _logboekContext.ApiCallLogs.Add(logEntry);
            await _logboekContext.SaveChangesAsync();
        }

        private string SerializeHeaders(HttpHeaders headers, HttpContentHeaders? contentHeaders = null)
        {
            var allHeaders = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                allHeaders[header.Key] = string.Join(", ", header.Value);
            }

            if (contentHeaders != null)
            {
                foreach (var header in contentHeaders)
                {
                    allHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }

            return JsonConvert.SerializeObject(allHeaders);
        }

        private string? TruncateIfNeeded(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "... [truncated]";
        }
    }
}
