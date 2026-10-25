using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace HouseLedger.Client.Auth
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigation;

        public AuthMessageHandler(IAuthService authService, NavigationManager navigation)
        {
            _authService = authService;
            _navigation = navigation;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            if (IsAuthRequest(request))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            if (_authService.CurrentToken is null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var refreshResult = await _authService.RefreshAsync();
            if (!refreshResult.Success && _authService.CurrentToken is null)
            {
                ClearAndRedirect();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    RequestMessage = request
                };
            }

            AddAccessToken(request);
            var retryRequest = await CloneRequestAsync(request);
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                retryRequest.Dispose();
                return response;
            }

            response.Dispose();
            var retryRefreshResult = await _authService.RefreshAsync(force: true);
            if (!retryRefreshResult.Success || _authService.CurrentToken is null)
            {
                retryRequest.Dispose();
                ClearAndRedirect();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    RequestMessage = request
                };
            }

            AddAccessToken(retryRequest);
            return await base.SendAsync(retryRequest, cancellationToken);
        }

        private void AddAccessToken(HttpRequestMessage request)
        {
            request.Headers.Authorization = _authService.CurrentToken is { } token
                ? new AuthenticationHeaderValue("Bearer", token.AccessToken)
                : null;
        }

        private void ClearAndRedirect()
        {
            _authService.ClearAuthentication();
            _navigation.NavigateTo("/login");
        }

        private static bool IsAuthRequest(HttpRequestMessage request)
        {
            return request.RequestUri?.AbsolutePath.Contains("/api/v1/auth/", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                VersionPolicy = request.VersionPolicy
            };

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(content);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }
}
