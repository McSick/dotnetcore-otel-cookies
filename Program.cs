using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var honeycombOptions = builder.Configuration.GetHoneycombOptions();

// Set up OpenTelemetry Tracing
builder.Services.AddOpenTelemetry().WithTracing(otelBuilder =>
{
    otelBuilder
        .AddHoneycomb(honeycombOptions)
        .AddCommonInstrumentations()
        .AddAspNetCoreInstrumentation ( o =>
        {
            o.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("requestProtocol", httpRequest.Protocol);
                int clientcookiecount = 0;
                string listofkeys = "";
                foreach (var cookie in httpRequest.Cookies)
                {
                    string cookieKey = "clientcookie." + cookie.Key;
                    string cookieValue = cookie.Value;
                    listofkeys += (cookie.Key + ",");
                    activity.SetTag(cookieKey, cookieValue);
                    clientcookiecount++;
                }
                activity.SetTag("clientcookie.count", clientcookiecount);
                activity.SetTag("clientcookie.listofkeys", listofkeys);
            };
            o.EnrichWithHttpResponse = (activity, httpResponse) =>
            {

                activity.SetTag("responseLength", httpResponse.ContentLength);

                int servercookiecount = 0;
                var setCookieHeaders = httpResponse.GetTypedHeaders().SetCookie;
                string listofkeys = "";
                foreach (var cookieHeader in setCookieHeaders) 
                {
                    var cookieKey = "servercookie." + cookieHeader.Name;
                    var cookieValue = cookieHeader.Value.Value; // This gets only the value of the cookie, excluding other properties.
                    activity.SetTag(cookieKey, cookieValue);
                    servercookiecount++;
                    listofkeys += (cookieHeader.Name + ",");
                }
                activity.SetTag("servercookie.count", servercookiecount);
                activity.SetTag("servercookie.listofkeys", listofkeys);
            };
            o.EnrichWithException = (activity, exception) =>
            {
                activity.SetTag("exceptionType", exception.GetType().ToString());
            };
        });
});

// Register a Tracer, so it can be injected into other components (for example, Controllers)
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(honeycombOptions.ServiceName));

var app = builder.Build();

app.MapGet("/", (HttpContext context) => 
{
    context.Response.Cookies.Append("SampleCookie", "SampleValue", new CookieOptions
    {
        Path = "/",
        HttpOnly = true,
        Secure = false,
    });
    return "Hello World!";
});

app.Run();
