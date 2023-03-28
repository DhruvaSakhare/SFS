using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace StupidFileStore_API
{
    class Program
    {
        private static int uploadCount = 0;

        static void Main()
        {
            // Start a thread to print 'upload count' every 10 seconds
            Thread Counter = new Thread(PrintUploadCount);
            Counter.Start();

            var port = 7913; //port number of StupidFileStore(SFS)

            // Set up web server
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:5000") // to host our API server
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        // Check if request is for /api/store and is a POST
                        if (context.Request.Path == "/api/store" && context.Request.Method == HttpMethods.Post)
                        {
                            // Read string from request body
                            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                            {
                                string content = await reader.ReadToEndAsync();

                                // Try connecting to StupidFileStore server and send string
                                try
                                {
                                    using (TcpClient StupidFileStore = new TcpClient("localhost", port))
                                    using (NetworkStream stream = StupidFileStore.GetStream())
                                    {
                                        //Make a byte to store the Hello Message in
                                        byte[] hello = Encoding.ASCII.GetBytes("Hello\n");

                                        // Read response from server
                                        byte[] responseBytes = new byte[hello.Length];
                                        await stream.ReadAsync(responseBytes, 0, responseBytes.Length);

                                        //Write string send by the client
                                        byte[] stringBytes = Encoding.ASCII.GetBytes(content + "\n\n");
                                        await stream.WriteAsync(stringBytes, 0, stringBytes.Length);

                                        // Read UUID from response
                                        byte[] uuidBytes = new byte[36];
                                        await stream.ReadAsync(uuidBytes, 0, uuidBytes.Length);

                                        string uuid = Encoding.ASCII.GetString(uuidBytes);

                                        // Return UUID to client
                                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                                        context.Response.ContentType = "text/plain";
                                        await context.Response.WriteAsync(uuid);
                                    }

                                    // Increment upload count
                                    Interlocked.Increment(ref uploadCount);
                                }
                                catch (Exception ex)
                                {
                                    // Handling the exception
                                    await context.Response.WriteAsync("Cannot connect to StupidFileStore: " + ex.Message);

                                }
                            }
                        }
                        else
                        {
                            await context.Response.WriteAsync("Request path or METHOD inccorect. Please use the path: /api/store and the METHOD : POST");
                        }
                    });
                })
                .Build();

            host.Run();
        }

        private static void PrintUploadCount()
        {
            int timeSeconds = 10; //How long to wait until next print
            int time = timeSeconds * 1000;
            while (true)
            {
                Thread.Sleep(time);
                Console.WriteLine($"Uploaded {uploadCount} strings since last print out.");
                Interlocked.Exchange(ref uploadCount, 0);
            }
        }
    }
}
