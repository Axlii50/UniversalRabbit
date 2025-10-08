using System.Diagnostics;

namespace RabbitInstaller
{
    public class RabbitDiagnosticsWrapper
    {
        /// <summary>
        /// Uruchamia komendę rabbitmq-diagnostics z podanymi argumentami, i zwraca (exitCode, stdout) 
        /// </summary>
        public static async Task<(int ExitCode, string Output)> RunDiagnosticAsync(string arguments, int timeoutMs = 5000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\RabbitMQ Server\rabbitmq_server-3.12.12\sbin\rabbitmq-diagnostics.bat",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();

            // Możesz czekać na zakończenie procesu z limitem czasu
            bool exited = proc.WaitForExit(timeoutMs);
            if (!exited)
            {
                try { proc.Kill(); } catch { }
                return (-1, "Timeout (did not exit in time)");
            }

            string outp = await proc.StandardOutput.ReadToEndAsync();
            string err = await proc.StandardError.ReadToEndAsync();
            int code = proc.ExitCode;

            string full = outp;
            if (!string.IsNullOrWhiteSpace(err))
                full += "\nERR: " + err;

            return (code, full);
        }

        public static async Task TestRabbitDiagnostics()
        {
            var (codePing, outputPing) = await RunDiagnosticAsync("ping -q");
            Console.WriteLine($"ping exit code = {codePing}");
            Console.WriteLine(outputPing);

            var (codeStatus, outputStatus) = await RunDiagnosticAsync("status -q");
            Console.WriteLine($"status exit code = {codeStatus}");
            Console.WriteLine(outputStatus);

            var (codeRunning, outRunning) = await RunDiagnosticAsync("check_running -q");
            Console.WriteLine($"check_running exit code = {codeRunning}");
            Console.WriteLine(outRunning);

            var (codeAlarms, outAlarms) = await RunDiagnosticAsync("check_local_alarms -q");
            Console.WriteLine($"check_local_alarms exit code = {codeAlarms}");
            Console.WriteLine(outAlarms);

            var (codePorts, outPorts) = await RunDiagnosticAsync("check_port_connectivity -q");
            Console.WriteLine($"check_port_connectivity exit code = {codePorts}");
            Console.WriteLine(outPorts);

            // etc., inne checki, np. listeners, virtual_hosts, alarms itp.
        }
    }

}
