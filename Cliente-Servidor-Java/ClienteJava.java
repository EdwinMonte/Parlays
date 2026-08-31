import java.io.*;
import java.net.*;
import java.util.Scanner;

public class ClienteJava {
    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);
        System.out.println("===================================================");
        System.out.println("            CLIENTE TCP EN JAVA");
        System.out.println("===================================================\n");

        System.out.print("Ingrese la IP del Servidor (o presione ENTER para 127.0.0.1): ");
        String ip = scanner.nextLine().trim();
        if (ip.isEmpty()) ip = "127.0.0.1";

        int puerto = 5000;

        try {
            System.out.println("\n[>] Conectando al servidor " + ip + ":" + puerto + "...");
            Socket socket = new Socket(ip, puerto);
            System.out.println("[✔] ¡Conectado exitosamente con el servidor!\n");

            PrintWriter out = new PrintWriter(new OutputStreamWriter(socket.getOutputStream(), "UTF-8"), true);
            BufferedReader in = new BufferedReader(new InputStreamReader(socket.getInputStream(), "UTF-8"));

            System.out.print("Escribe el mensaje a enviar: ");
            String mensaje = scanner.nextLine();
            if (mensaje.isEmpty()) mensaje = "Hola desde Cliente Java";

            out.println(mensaje);
            System.out.println("[→] Mensaje enviado: '" + mensaje + "'");

            String response = in.readLine();
            System.out.println("\n[←] Respuesta del Servidor: " + response);

            socket.close();
            System.out.println("[✔] Flujo completado y conexión cerrada ordenadamente.");
        } catch (UnknownHostException e) {
            System.err.println("\n[✖] Host desconocido o no alcanzable: " + ip);
        } catch (IOException e) {
            System.err.println("\n[✖] Error de comunicación de red: " + e.getMessage());
        }

        System.out.println("\nFin del programa.");
    }
}
