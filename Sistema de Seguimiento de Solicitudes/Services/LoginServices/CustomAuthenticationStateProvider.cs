using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace Sistema_de_Seguimiento_de_Solicitudes.Services.LoginServices
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private const string TokenKey = "authToken";
        private readonly IJSRuntime _jsRuntime;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
        private string? _currentToken;
        private CancellationTokenSource? _expirationCancellation;
        private bool _initialized;

        public CustomAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            await EnsureInitializedAsync();
            return new AuthenticationState(_currentUser);
        }

        public async Task<string?> GetTokenAsync()
        {
            await EnsureInitializedAsync();
            return _currentToken;
        }

        public async Task SetToken(string? token)
        {
            await _initializationLock.WaitAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
                    ApplyAuthenticationState(null, null, null);
                }
                else
                {
                    if (!TryParseToken(token, out var user, out var expiresAt))
                    {
                        throw new InvalidOperationException("The authentication token is invalid or expired.");
                    }

                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
                    ApplyAuthenticationState(token, user, expiresAt);
                }

                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public Task RemoveToken() => SetToken(null);

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
                if (TryParseToken(token, out var user, out var expiresAt))
                {
                    ApplyAuthenticationState(token, user, expiresAt);
                }
                else
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
                    }

                    ApplyAuthenticationState(null, null, null);
                }

                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private void ApplyAuthenticationState(string? token, ClaimsPrincipal? user, DateTimeOffset? expiresAt)
        {
            _expirationCancellation?.Cancel();
            _expirationCancellation?.Dispose();
            _expirationCancellation = null;
            _currentToken = token;
            _currentUser = user ?? new ClaimsPrincipal(new ClaimsIdentity());

            if (token is not null && expiresAt.HasValue)
            {
                _expirationCancellation = new CancellationTokenSource();
                _ = RemoveTokenWhenExpiredAsync(token, expiresAt.Value, _expirationCancellation.Token);
            }
        }

        private async Task RemoveTokenWhenExpiredAsync(string token, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            try
            {
                var delay = expiresAt - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                if (_currentToken == token)
                {
                    await RemoveToken();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private static bool TryParseToken(string? token, out ClaimsPrincipal user, out DateTimeOffset expiresAt)
        {
            user = new ClaimsPrincipal(new ClaimsIdentity());
            expiresAt = default;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    return false;
                }

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
                if (!document.RootElement.TryGetProperty("exp", out var expiration)
                    || !long.TryParse(expiration.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixExpiration))
                {
                    return false;
                }

                expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixExpiration);
                if (expiresAt <= DateTimeOffset.UtcNow)
                {
                    return false;
                }

                var claims = new List<Claim>();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.NameEquals("exp") || property.NameEquals("iat") || property.NameEquals("nbf"))
                    {
                        continue;
                    }

                    AddClaims(claims, property.Name, property.Value);
                }

                user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));
                return user.Identity?.IsAuthenticated == true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static void AddClaims(List<Claim> claims, string claimType, JsonElement value)
        {
            var normalizedType = claimType switch
            {
                "name" or "unique_name" => ClaimTypes.Name,
                "sub" => ClaimTypes.NameIdentifier,
                "role" or "roles" or ClaimTypes.Role => ClaimTypes.Role,
                _ => claimType
            };

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    claims.Add(new Claim(normalizedType, item.ToString()));
                }

                return;
            }

            claims.Add(new Claim(normalizedType, value.ToString()));
        }
    }
}
