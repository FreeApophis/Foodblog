using System.Text;

namespace Foodblog;

public static class HttpAdapter
{
    public static async ValueTask<Request> ToRequest(HttpContext http)
    {
        var form = new Dictionary<string, string>();
        var files = new List<UploadedFile>();

        if (http.Request.HasFormContentType)
        {
            var parsed = await http.Request.ReadFormAsync();
            form = parsed.ToDictionary(x => x.Key, x => x.Value.ToString());

            foreach (var file in parsed.Files)
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer);
                files.Add(new UploadedFile(file.Name, file.FileName, file.ContentType, buffer.ToArray()));
            }
        }

        var cookies = http.Request.Cookies.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);

        return new Request(
            Method: HttpMethod.Parse(http.Request.Method),
            Path: http.Request.Path.Value ?? "/",
            Headers: http.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Query: http.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Form: form,
            Cookies: cookies)
        {
            Files = files,
            BaseUrl = $"{http.Request.Scheme}://{http.Request.Host}",
        };
    }

    public static async ValueTask WriteResponse(HttpContext http, Response response)
    {
        http.Response.StatusCode = response.Status;
        http.Response.ContentType = response.ContentType;

        foreach (var (key, value) in response.Headers)
        {
            http.Response.Headers[key] = value;
        }

        foreach (var cookie in response.SetCookies)
        {
            http.Response.Headers.Append("Set-Cookie", cookie);
        }

        if (response.Binary is { } bytes)
        {
            await http.Response.Body.WriteAsync(bytes);
        }
        else
        {
            await http.Response.WriteAsync(response.Body, Encoding.UTF8);
        }
    }
}
