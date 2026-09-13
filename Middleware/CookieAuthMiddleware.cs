namespace SkillHive.Middleware
{
    public class CookieAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                if (context.Request.Cookies.TryGetValue("AuthToken", out var token)
                    && !string.IsNullOrEmpty(token))
                {
                    context.Request.Headers.Append("Authorization", $"Bearer {token}");
                }
            }

            await _next(context);
        }
    }
}