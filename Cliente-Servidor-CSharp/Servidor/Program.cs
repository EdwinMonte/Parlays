using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ServidorCSharp
{
    private const int Puerto = 5000;

    static async Task Main(string[] args)
    {
        Console.Title = "Servidor TCP Concurrente - C# (.NET 10)";
        var ipAddress = IPAddress.Any; // Escucha en todas las interfaces de red
        var listener = new TcpListener(ipAddress, Puerto);

        listener.Start();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"===================================================");
        Console.WriteLine($" [SERVIDOR C#] Iniciado y escuchando en el puerto: {Puerto}");
        Console.WriteLine($" Listo para conexiones locales y remotas (VM/LAN)");
        Console.WriteLine($"===================================================\n");
        Console.ResetColor();

        try
        {
            while (true)
            {
                // Espera asíncrona de clientes sin bloquear el hilo principal
                TcpClient client = await listener.AcceptTcpClientAsync();
                var endpoint = client.Client.RemoteEndPoint?.ToString();
                Console.WriteLine($"[+] Nuevo cliente conectado desde: {endpoint}");

                // Atiende al cliente en una tarea en segundo plano (Concurrencia)
                _ = Task.Run(() => AtenderClienteAsync(client, endpoint));
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Error en el servidor: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task AtenderClienteAsync(TcpClient client, string? endpoint)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];

            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string mensajeRecibido = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"[📥 Mensaje de {endpoint}]: {mensajeRecibido}");

                    // Respuesta ECO interoperable
                    string textoRespuesta = $"ECO DESDE C#: {mensajeRecibido}\n";
                    byte[] response = Encoding.UTF8.GetBytes(textoRespuesta);
                    await stream.WriteAsync(response, 0, response.Length);
                    Console.WriteLine($"[📤 Respuesta enviada a {endpoint}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Cliente {endpoint} desconectado: {ex.Message}");
            }
        }
        Console.WriteLine($"[-] Conexión cerrada con: {endpoint}\n");
    }
}
