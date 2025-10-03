using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WiimoteLib;

class Program
{
    // --- Fields ---
    private static float tareWeight;
    private static float tareCoPX;
    private static float tareCoPY;
    private static bool isTared = false;
    private static bool running = true;
    private static bool streaming = false;

    private static Wiimote wiimote;
    private static DateTime startTime;
    private static StreamWriter csvWriter;
    private static string participantName;
    private static string filePath;
    private static string taskType;

    static void Main()
    {
        // --- Banner ---
        Console.WriteLine("===========================================");
        Console.WriteLine(" Wii Balance Board Data Recorder ");
        Console.WriteLine("===========================================");
        Console.WriteLine("Usage:");
        Console.WriteLine("  Enter participant name and task type:");
        Console.WriteLine("    Example:  John simple");
        Console.WriteLine("    Example:  Maria complex");
        Console.WriteLine();
        Console.WriteLine("Commands during runtime:");
        Console.WriteLine("  go    -> Start streaming data");
        Console.WriteLine("  stop  -> Stop streaming");
        Console.WriteLine("  res   -> Reset Center of Pressure (CoP)");
        Console.WriteLine("  exit  -> Quit program");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        // --- Participant setup ---
        string[] inputParts = Console.ReadLine()?.Trim().Split(' ');
        if (inputParts == null || inputParts.Length != 2 ||
            (inputParts[1] != "simple" && inputParts[1] != "complex"))
        {
            Console.WriteLine("Invalid input. Please enter in the format: 'Name simple' or 'Name complex'");
            return;
        }

        participantName = inputParts[0];
        taskType = inputParts[1];

        // --- File setup ---
        string folderPath = Path.Combine("data", participantName);
        Directory.CreateDirectory(folderPath);

        filePath = Path.Combine(
            folderPath,
            $"BalanceBoardData_{taskType}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        );

        wiimote = new Wiimote();

        try
        {
            Console.WriteLine("Searching for Wiimote...");
            wiimote.Connect();
            Console.WriteLine("Connected!");

            if (wiimote.WiimoteState.ExtensionType != ExtensionType.BalanceBoard)
            {
                Console.WriteLine("Not a Balance Board. Please connect a Wii Balance Board.");
                return;
            }

            wiimote.SetReportType(InputReport.IRAccel, true);
            wiimote.WiimoteChanged += OnWiimoteChanged;

            using (csvWriter = new StreamWriter(filePath))
            {
                Console.WriteLine("Stabilizing... Please stand still.");
                Thread.Sleep(2000);
                TareWeight();

                Task.Run(ListenForCommands);

                Console.WriteLine("Type 'go' to start streaming, 'stop' to pause, 'res' to reset CoP, and 'exit' to quit.");

                while (running)
                {
                    Thread.Sleep(100); // Keep main thread alive
                }
            }

            wiimote.Disconnect();
            Console.WriteLine($"Wiimote disconnected. Data saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Records the current weight and center of pressure as baseline.
    /// </summary>
    private static void TareWeight()
    {
        var bb = wiimote.WiimoteState.BalanceBoardState;
        tareWeight = bb.WeightKg;
        tareCoPX = bb.CenterOfGravity.X;
        tareCoPY = bb.CenterOfGravity.Y;
        isTared = true;

        Console.WriteLine($"Tare set -> Weight: {tareWeight:F2} kg, CoP: X={tareCoPX:F2} cm, Y={tareCoPY:F2} cm");
    }

    /// <summary>
    /// Resets the center of pressure reference point.
    /// </summary>
    private static void ResetCenterOfPressure()
    {
        var bb = wiimote.WiimoteState.BalanceBoardState;
        tareCoPX = bb.CenterOfGravity.X;
        tareCoPY = bb.CenterOfGravity.Y;

        Console.WriteLine($"Center of Pressure reset -> X={tareCoPX:F2} cm, Y={tareCoPY:F2} cm");
    }

    /// <summary>
    /// Listens for user commands in the console.
    /// </summary>
    private static void ListenForCommands()
    {
        while (running)
        {
            string input = Console.ReadLine()?.Trim().ToLower();
            if (input == null) continue;

            switch (input)
            {
                case "go":
                    StartStreaming();
                    break;

                case "stop":
                    streaming = false;
                    Console.WriteLine(">> Streaming stopped.");
                    break;

                case "res":
                    ResetCenterOfPressure();
                    break;

                case "exit":
                case "quit":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Unknown command. Use 'go', 'stop', 'res', or 'exit'.");
                    break;
            }
        }
    }

    /// <summary>
    /// Handles starting data collection depending on task type.
    /// </summary>
    private static void StartStreaming()
    {
        ResetCenterOfPressure();
        startTime = DateTime.Now;

        csvWriter.WriteLine("RealTimestamp,ElapsedSeconds,TopLeft(kg),TopRight(kg),BottomLeft(kg),BottomRight(kg),TotalWeight(kg),CoPX(cm),CoPY(cm)");

        streaming = true;

        if (taskType == "simple")
        {
            Console.WriteLine(">> Simple task: Streaming for 60 seconds...");
            Task.Run(async () =>
            {
                await Task.Delay(60000);
                streaming = false;
                Console.WriteLine(">> 60 seconds complete. Streaming stopped.");
            });
        }
        else if (taskType == "complex")
        {
            Console.WriteLine(">> Complex task: Streaming for 39 seconds...");
            Task.Run(async () =>
            {
                await Task.Delay(39000);
                streaming = false;
                Console.WriteLine(">> 39 seconds complete. Streaming stopped.");
            });
        }
    }

    /// <summary>
    /// Event triggered when the Wiimote state changes.
    /// </summary>
    private static void OnWiimoteChanged(object sender, WiimoteChangedEventArgs e)
    {
        if (!streaming || !running) return;

        var bb = e.WiimoteState.BalanceBoardState;
        double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

        float adjustedWeight = isTared ? bb.WeightKg - tareWeight : bb.WeightKg;
        float adjustedX = isTared ? bb.CenterOfGravity.X - tareCoPX : bb.CenterOfGravity.X;
        float adjustedY = isTared ? bb.CenterOfGravity.Y - tareCoPY : bb.CenterOfGravity.Y;

        string realTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

        csvWriter.WriteLine($"{realTime},{elapsedSeconds:F3}," +
            $"{bb.SensorValuesKg.TopLeft:F2},{bb.SensorValuesKg.TopRight:F2}," +
            $"{bb.SensorValuesKg.BottomLeft:F2},{bb.SensorValuesKg.BottomRight:F2}," +
            $"{adjustedWeight:F2},{adjustedX:F2},{adjustedY:F2}");
        csvWriter.Flush();  // Save immediately
    }
}
