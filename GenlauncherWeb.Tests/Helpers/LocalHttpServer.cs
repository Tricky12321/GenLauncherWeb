using System.Net;

namespace GenlauncherWeb.Tests.Helpers;

/// <summary>
/// Minimal in-process HTTP server for download tests. Serves a single fixed
/// byte array on every GET request, then disposes cleanly.
/// </summary>
public sealed class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly Task _serveLoop;
    private readonly byte[] _content;
    private readonly string _contentType;
    private readonly bool _setContentLength;
    private readonly int _chunkDelay;

    public string Url { get; }

    public LocalHttpServer(
        byte[] content,
        string contentType = "application/octet-stream",
        bool setContentLength = true,
        int chunkDelay = 0)
    {
        _content = content;
        _contentType = contentType;
        _setContentLength = setContentLength;
        _chunkDelay = chunkDelay;

        var port = GetFreePort();
        Url = $"http://localhost:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(Url);
        _listener.Start();
        _serveLoop = ServeAsync();
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }

            _ = Task.Run(async () =>
            {
                var resp = ctx.Response;
                resp.ContentType = _contentType;
                if (_setContentLength) resp.ContentLength64 = _content.Length;

                const int chunkSize = 8192;
                for (int offset = 0; offset < _content.Length; offset += chunkSize)
                {
                    var len = Math.Min(chunkSize, _content.Length - offset);
                    await resp.OutputStream.WriteAsync(_content.AsMemory(offset, len));
                    if (_chunkDelay > 0) await Task.Delay(_chunkDelay);
                }
                resp.OutputStream.Close();
            });
        }
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        try { _listener.Stop(); } catch { /* already stopped */ }
        _serveLoop.Wait(TimeSpan.FromSeconds(3));
    }
}
