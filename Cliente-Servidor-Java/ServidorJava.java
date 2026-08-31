import java.io.*;
import java.net.*;

public class ServidorJava {
    private static final int PUERTO = 5000;

    public static void main(String[] args) {
        System.out.println("===================================================");
        System.out.println(" [SERVIDOR JAVA] Iniciado en el puerto: " + PUERTO);
        System.out.println(" Esperando conexiones concurrentes...");
        System.out.println("===================================================\n");

        try (ServerSocket serverSocket = new ServerSocket(PUERTO)) {
            while (true) {
                Socket clientSocket = serverSocket.accept();
                String remoteIp = clientSocket.getRemoteSocketAddress().toString();
                System.out.println("[+] Nuevo cliente conectado: " + remoteIp);

                // Manejo concurrente con hilos para no bloquear el bucle principal
                new Thread(() -> atenderCliente(clientSocket, remoteIp)).start();
            }
        } catch (IOException e) {
            System.err.println("[!] Error en el servidor Java: " + e.getMessage());
        }
    }

    private static void atenderCliente(Socket socket, String remoteIp) {
        try (socket;
             BufferedReader in = new BufferedReader(new InputStreamReader(socket.getInputStream(), "UTF-8"));
             PrintWriter out = new PrintWriter(new OutputStreamWriter(socket.getOutputStream(), "UTF-8"), true)) {

            String inputLine = in.readLine();
            if (inputLine != null) {
                System.out.println("[📥 Mensaje de " + remoteIp + "]: " + inputLine);
                
                // Respuesta de ECO interoperable
                String respuesta = "ECO DESDE JAVA: " + inputLine;
                out.println(respuesta);
                System.out.println("[📤 Respuesta enviada a " + remoteIp + "]");
            }
        } catch (IOException e) {
            System.err.println("[-] Cliente " + remoteIp + " desconectado: " + e.getMessage());
        }
        System.out.println("[-] Conexión cerrada con: " + remoteIp + "\n");
    }
}
