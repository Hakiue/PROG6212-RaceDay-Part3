namespace RaceDayMvc.Infrastructure;

/// <summary>
/// Carries the logged-in user's API session across HTTP calls. The API identifies a user by a
/// session cookie, but this app calls it server-to-server, so the browser never sees that
/// cookie. This handler stores it in the MVC session when the API issues it (at login) and
/// sends it back on every later request for that same user.
/// </summary>
public class ApiSessionHandler : DelegatingHandler
{
    private const string ApiSessionCookiePrefix = ".AspNetCore.Session=";
    private readonly IHttpContextAccessor _accessor;

    public ApiSessionHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;

        var cookie = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var apiSession = setCookies.FirstOrDefault(c => c.StartsWith(ApiSessionCookiePrefix, StringComparison.Ordinal));
            if (apiSession is not null)
            {
                session.SetString(SessionKeys.ApiCookie, apiSession.Split(';')[0]);
            }
        }

        return response;
    }
}
