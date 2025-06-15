using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;

namespace CLTI.Diagnosis.Client.Services
{
    /// <summary>
    /// Custom Authorization Message Handler для автоматичного додавання токенів до HTTP запитів
    /// </summary>
    public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        private readonly ILogger<CustomAuthorizationMessageHandler> _logger;

        public CustomAuthorizationMessageHandler(
            IAccessTokenProvider provider,
            NavigationManager navigation,
            ILogger<CustomAuthorizationMessageHandler> logger)
            : base(provider, navigation)
        {
            _logger = logger;

            // Налаштовуємо URL-и для яких потрібна автентифікація
            ConfigureHandler(
                authorizedUrls: new[]
                {
                    navigation.BaseUri,
                    "https://localhost:7124",
                    "https://antsdemo02.demo.dragon-cloud.org"
                },
                scopes: new[] { "api" }
            );

            _logger.LogInformation("CustomAuthorizationMessageHandler initialized with BaseUri: {BaseUri}",
                navigation.BaseUri);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing request to: {Uri}", request.RequestUri);

                // Викликаємо базовий метод який автоматично додає токен
                var response = await base.SendAsync(request, cancellationToken);

                _logger.LogDebug("Request completed with status: {StatusCode}", response.StatusCode);

                // Логуємо заголовки авторизації для діагностики
                if (request.Headers.Authorization != null)
                {
                    _logger.LogDebug("Authorization header added: {Scheme}",
                        request.Headers.Authorization.Scheme);
                }
                else
                {
                    _logger.LogWarning("No authorization header added to request: {Uri}",
                        request.RequestUri);
                }

                return response;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available for request to {Uri}. Redirecting to login.",
                    request.RequestUri);

                // Перенаправляємо на сторінку входу
                ex.Redirect();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CustomAuthorizationMessageHandler for request to {Uri}",
                    request.RequestUri);
                throw;
            }
        }
    }
}