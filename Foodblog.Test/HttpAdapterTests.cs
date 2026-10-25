using Microsoft.AspNetCore.Http;

namespace Foodblog.Test;

public class HttpAdapterTests
{
    [Fact]
    public async Task WriteResponse_writes_binary_body_with_content_type()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        var bytes = new byte[] { 1, 2, 3, 4, 250, 99 };

        await HttpAdapter.WriteResponse(http, Response.Bytes("image/png", bytes));

        Assert.Equal("image/png", http.Response.ContentType);
        Assert.Equal(bytes, ((MemoryStream)http.Response.Body).ToArray());
    }

    [Fact]
    public async Task WriteResponse_still_writes_text_bodies()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();

        await HttpAdapter.WriteResponse(http, Response.Text("hallo"));

        var written = System.Text.Encoding.UTF8.GetString(((MemoryStream)http.Response.Body).ToArray());
        Assert.Equal("hallo", written);
    }

    [Fact]
    public async Task ToRequest_reads_uploaded_files()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.Request.Path = "/images";
        var content = new byte[] { 9, 8, 7, 6 };
        using var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.Length, "file", "pic.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };
        http.Request.ContentType = "multipart/form-data; boundary=x";
        http.Request.Form = new FormCollection(new(), new FormFileCollection { file });

        var request = await HttpAdapter.ToRequest(http);

        var uploaded = Assert.Single(request.Files);
        Assert.Equal("file", uploaded.FieldName);
        Assert.Equal("pic.png", uploaded.FileName);
        Assert.Equal("image/png", uploaded.ContentType);
        Assert.Equal(content, uploaded.Content);
    }

    [Fact]
    public async Task ToRequest_has_no_files_for_a_plain_request()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "GET";
        http.Request.Path = "/";

        var request = await HttpAdapter.ToRequest(http);

        Assert.Empty(request.Files);
    }
}
