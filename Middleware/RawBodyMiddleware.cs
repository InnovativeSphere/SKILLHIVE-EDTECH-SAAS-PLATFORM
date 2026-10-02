namespace SkillHive.Middleware
{
    /// <summary>
    /// Enables request body buffering so controllers can read the raw bytes
    /// for HMAC signature verification (Paystack webhook).
    /// Without this, the body stream is one-shot and we can't verify the signature.
    /// </summary>
    public class RawBodyMiddleware
    {
        private readonly RequestDelegate _next;

        public RawBodyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            await _next(context);
        }
    }
}