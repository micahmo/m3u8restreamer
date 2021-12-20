using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using EmbedIO;
using EmbedIO.Actions;
using Swan.Logging;

namespace m3u8restreamer
{
    public class Program
    {
        private static void Main()
        {
            var pipProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "pip3",
                Arguments = "install --upgrade yt-dlp",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            pipProcess?.StandardOutput.ReadToEnd().Log(nameof(Main), LogLevel.Info);
            
            StartServer();
            Thread.Sleep(Timeout.Infinite);
        }

        private static void StartServer()
        {
            string server = $"http://+:{11034}";

            $"Starting server on {server}".Log(nameof(StartServer), LogLevel.Info);

            WebServer webServer = new WebServer(o => o
                    .WithUrlPrefix(server)
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithModule(new ActionModule("/getStream", HttpVerbs.Any, GetStream));

            // Important: Do not ignore write exceptions, but let them bubble up.
            // This allows us to see when a client disconnects, so that we can stop streaming.
            // (Otherwise we could stream to a disconnected client indefinitely.)
            webServer.Listener.IgnoreWriteExceptions = false;

            webServer.RunAsync();

            "Server is started and ready to receive connections.".Log(nameof(StartServer), LogLevel.Info);
        }

        private static async Task GetStream(IHttpContext context)
        {
            string m3u8 = HttpUtility.UrlDecode(context.RequestedPath.TrimStart('/'));

            context.Response.ContentType = "video/mp2t";
            context.Response.SendChunked = true;
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            await context.Response.OutputStream.FlushAsync();

            string command = $"-q --no-warnings --downloader ffmpeg {m3u8} -o -";

            $"Got request to play stream {m3u8}. Starting now with command {command}".Log(nameof(GetStream), LogLevel.Info);

            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            try
            {
                await process.StandardOutput.BaseStream.CopyToAsync(context.Response.OutputStream);
                //await process.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput());

                var errors = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errors))
                {
                    $"There were errors playing {m3u8}: {errors}".Log(nameof(GetStream), LogLevel.Error);
                }

                // Handle graceful shutdown (natural end-of-stream)
                // Next, wait for the stream playing process to end (with a timeout, in case it hangs)
                if (process.WaitForExit((int)TimeSpan.FromSeconds(5).TotalMilliseconds))
                {
                    $"Stream {m3u8} finished. Stream process exited gracefully is {process.HasExited}.".Log(nameof(GetStream), LogLevel.Info);
                }
                else
                {
                    // The streaming process failed to exit gracefully, so kill it.
                    process.Kill();
                    await process.WaitForExitAsync();

                    $"Stream {m3u8} finished. Stream process exited successfully (ungracefully) is {process.HasExited}.".Log(nameof(GetStream), LogLevel.Info);
                }
            }
            catch
            {
                // Handle forceful shutdown (client disconnection)

                process.Kill();
                await process.WaitForExitAsync();

                $"Client disconnected. Killing stream {m3u8}. Stream process exited successfully is {process.HasExited}.".Log(nameof(GetStream), LogLevel.Info);
            }
        }
    }
}
