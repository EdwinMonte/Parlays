using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ClienteCSharp
{
    static async Task Main(string[] args)
    {
        Console.Title = "Cliente TCP - C# (.NET 10)";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===================================================");
        Console.WriteLine("        CLIENTE TCP EN C# (.NET 10)");
        Console.WriteLine("===================================================\n");
        Console.ResetColor();

        Console.Write("Ingrese la IP del Servidor (o presione ENTER para 127.0.0.1): ");
        string? ip = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        int puerto = 5000;

        try
        {
            Console.WriteLine($"\n[>] Conectando al servidor {ip}:{puerto}...");
            using var client = new TcpClient();
            await client.ConnectAsync(ip, puerto);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[✔] ¡Conexión establecida exitosamente con el servidor!\n");
            Console.ResetColor();

            await using var stream = client.GetStream();

            Console.Write("Escribe el mensaje a enviar: ");
            string? mensaje = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(mensaje)) mensaje = "Hola desde Cliente C# (.NET 10)";

            // Enviar mensaje con salto de línea para compatibilidad total con Java
            byte[] data = Encoding.UTF8.GetBytes(mensaje + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"[→] Mensaje enviado: '{mensaje}'");

            // Recibir respuesta ECO
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string respuesta = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[←] Respuesta del Servidor: {respuesta}");
            Console.ResetColor();

            Console.WriteLine("\n[✔] Flujo completado y conexión cerrada ordenadamente.");
        }
        catch (SocketException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[✖] Error de red: No se pudo conectar al servidor en {ip}:{puerto}");
            Console.WriteLine($"    Detalle: {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[✖] Error inesperado: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\nPresione cualquier tecla para salir...");
        Console.ReadKey();
    }
}
