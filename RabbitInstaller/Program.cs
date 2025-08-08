using System;
using System.Diagnostics;
using System.IO;
namespace RabbitInstaller
{
    class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Instalator RabbitMQ + Erlang ===");

                string tempFolder = Path.Combine(Path.GetTempPath(), "RabbitInstall");
                Directory.CreateDirectory(tempFolder);

                string erlangUrl = "https://github.com/erlang/otp/releases/download/OTP-28.0.2/otp_win64_28.0.2.exe";
                string rabbitUrl = "https://github.com/rabbitmq/rabbitmq-server/releases/download/v3.12.12/rabbitmq-server-3.12.12.exe";

                string erlangInstaller = Path.Combine(tempFolder, "otp_win64_25.3.exe");
                string rabbitInstaller = Path.Combine(tempFolder, "rabbitmq-server-3.12.12.exe");

                // 1. Pobieranie instalatorów
                DownloadFile(erlangUrl, erlangInstaller).Wait();
                DownloadFile(rabbitUrl, rabbitInstaller).Wait();

                // 1. Instalacja Erlanga
                if (!CheckErlangInstalled())
                {
                    Console.WriteLine("[INFO] Instaluję Erlanga...");
                    RunInstaller(erlangInstaller, "/S"); // /S = silent install (opcjonalnie)
                }
                else
                {
                    Console.WriteLine("[INFO] Erlang już jest zainstalowany.");
                }

                // 2. Instalacja RabbitMQ
                if (!CheckRabbitInstalled())
                {
                    Console.WriteLine("[INFO] Instaluję RabbitMQ...");
                    RunInstaller(rabbitInstaller, "/S");
                }
                else
                {
                    Console.WriteLine("[INFO] RabbitMQ już jest zainstalowany.");
                }


                SetErlangHome();

                Console.WriteLine("=== Dodawania ErLang do zmiennych zakonczone ===");

                // 3. Rejestracja RabbitMQ jako usługi
                string rabbitSbin = @"C:\Program Files\RabbitMQ Server\rabbitmq_server-3.12.12\sbin";
                if (Directory.Exists(rabbitSbin))
                {
                    Console.WriteLine("[INFO] Rejestruję usługę RabbitMQ...");
                    RunCommand(Path.Combine(rabbitSbin, "rabbitmq-service.bat"), "install");
                    RunCommand(Path.Combine(rabbitSbin, "rabbitmq-service.bat"), "start");

                    Console.WriteLine("[INFO] Włączam panel webowy...");
                    RunCommand(Path.Combine(rabbitSbin, "rabbitmq-plugins.bat"), "enable rabbitmq_management");
                }
                else
                {
                    Console.WriteLine("[ERROR] Nie znaleziono folderu sbin RabbitMQ!");
                }

                Console.WriteLine("=== Instalacja zakończona ===");

                if (Directory.Exists(tempFolder))
                {
                    Console.WriteLine("🧹 Usuwanie plików instalacyjnych...");
                    Directory.Delete(tempFolder, true);
                }

                Console.WriteLine("=== Czyszczenie plików zakończone ===");

                RabbitTester.TestRabbitAsync().Wait();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }
            Console.ReadLine();
        }

        static async Task DownloadFile(string url, string destination)
        {
            Console.WriteLine($"[INFO] Pobieram {url}...");
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(destination, bytes);
            Console.WriteLine($"[INFO] Zapisano do: {destination}");
        }

        static void RunInstaller(string filePath, string args)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] Brak pliku instalatora: {filePath}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas" // Uruchom jako administrator
            };
            var proc = Process.Start(psi);
            proc.WaitForExit();
        }

        static void SetErlangHome()
        {
            string erlangDir = Directory.GetDirectories(@"C:\Program Files", "Erlang OTP*")[0];
            string erlangBin = Path.Combine(erlangDir, "bin");

            // Ustaw ERLANG_HOME
            Environment.SetEnvironmentVariable("ERLANG_HOME", erlangDir, EnvironmentVariableTarget.Machine);

            // Dodaj do PATH, jeśli nie istnieje
            string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) ?? "";
            if (!path.Contains(erlangBin, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(
                    "Path",
                    path + ";" + erlangBin,
                    EnvironmentVariableTarget.Machine
                );
            }

            Console.WriteLine($"✅ Ustawiono ERLANG_HOME = {erlangDir}");
        }

        static void RunCommand(string filePath, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{filePath}\" {args}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc.WaitForExit();
        }

        static bool CheckErlangInstalled()
        {
            string erlangPath = Environment.GetEnvironmentVariable("ERLANG_HOME", EnvironmentVariableTarget.Machine);
            return !string.IsNullOrEmpty(erlangPath) && Directory.Exists(erlangPath);
        }

        static bool CheckRabbitInstalled()
        {
            string rabbitPath = @"C:\Program Files\RabbitMQ Server";
            return Directory.Exists(rabbitPath);
        }
    }
}
