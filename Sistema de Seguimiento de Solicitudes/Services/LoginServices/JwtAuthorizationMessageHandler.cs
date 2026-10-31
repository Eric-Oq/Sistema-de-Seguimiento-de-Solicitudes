using System.Net;
using System.Net.Http.Headers;

namespace Sistema_de_Seguimiento_de_Solicitudes.Services.LoginServices
{
    public sealed class JwtAuthorizationMessageHandler : DelegatingHandler
    {
        private readonly CustomAuthenticationStateProvider _authenticationStateProvider;

        public JwtAuthorizationMessageHandler(CustomAuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isLoginRequest = request.RequestUri?.AbsolutePath.EndsWith("/api/Auth/login", StringComparison.OrdinalIgnoreCase) == true;
            var token = isLoginRequest ? null : await _authenticationStateProvider.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization is not null)
            {
                await _authenticationStateProvider.RemoveToken();
            }

            return response;
        }
    }
}
