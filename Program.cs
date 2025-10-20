using IBM.WMQ;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace IBMMQConnect
{
    /// <summary>
    /// Main entry point and logic for the  IBM MQ Connector application.
    /// Provides methods for connecting to IBM MQ, sending and receiving messages, and file operations.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The IBM MQ queue manager instance.
        /// </summary>
        static MQQueueManager qMgr;

        /// <summary>
        /// The loaded MQ configuration settings.
        /// </summary>
        static MQSettings config;

        /// <summary>
        /// The application version.
        /// </summary>
        const string version = "v1.0.1"; // ✅ You can update this anytime

        /// <summary>
        /// Represents the configuration settings required to connect to IBM MQ.
        /// </summary>
        public class MQSettings
        {
            /// <summary>
            /// The name of the queue manager.
            /// </summary>
            public string QueueManager { get; set; }
            /// <summary>
            /// The name of the queue.
            /// </summary>
            public string QueueName { get; set; }
            /// <summary>
            /// The MQ host address.
            /// </summary>
            public string Host { get; set; }
            /// <summary>
            /// The MQ port number.
            /// </summary>
            public int Port { get; set; }
            /// <summary>
            /// The MQ channel name.
            /// </summary>
            public string Channel { get; set; }
            /// <summary>
            /// The user ID for authentication.
            /// </summary>
            public string UserId { get; set; }
            /// <summary>
            /// The password for authentication.
            /// </summary>
            public string Password { get; set; }
            /// <summary>
            /// Indicates whether SSL is used.
            /// </summary>
            public bool UseSSL { get; set; }
            /// <summary>
            /// The SSL key repository path.
            /// </summary>
            public string SSLKeyRepository { get; set; }
            /// <summary>
            /// The SSL cipher specification.
            /// </summary>
            public string SSLCipherSpec { get; set; }
            /// <summary>
            /// The SSL peer name.
            /// </summary>
            public string SSLPeerName { get; set; }
            /// <summary>
            /// Indicates whether SSL certificate revocation check is enabled.
            /// </summary>
            public bool SSLCertRevocationCheck { get; set; }

            public string MQTransportProperty { get; set; }
        }

        /// <summary>
        /// Main application entry point. Handles user interaction and orchestrates MQ operations.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        static void Main(string[] args)
        {
            Console.Title = " IBM MQ CONNECTOR v1.0.2";

            ShowSplashScreen(); // 👈 Add this first
            PrintBanner();

            try
            {
                Log("Step 1: Loading MQ configuration from mqsettings.json...");
                config = LoadSettings("mqsettings.json");

                string queueName = config.QueueName;

                Log("Step 2: Applying SSL settings...");
                if (config.UseSSL)
                {
                    Log("  -> SSL Enabled");
                    MQEnvironment.SSLKeyRepository = config.SSLKeyRepository;
                    MQEnvironment.SSLCipherSpec = config.SSLCipherSpec;
                    MQEnvironment.SSLCertRevocationCheck = config.SSLCertRevocationCheck;
                    Log($"     - SSLKeyRepository: {config.SSLKeyRepository}");
                    Log($"     - SSLCipherSpec: {config.SSLCipherSpec}");
                    Log($"     - CertRevocationCheck: {config.SSLCertRevocationCheck}");

                    if (!string.IsNullOrEmpty(config.SSLPeerName))
                    {
                        MQEnvironment.SSLPeerName = config.SSLPeerName;
                        Log($"     - SSLPeerName: {config.SSLPeerName}");
                    }

                    LogSuccess("SSL configuration applied successfully.");
                }
                else
                {
                    LogWarning("SSL is disabled. Proceeding with non-SSL connection.");
                }

                Log("Step 3: Preparing MQ connection properties...");
                
                Hashtable properties = null;
                if (config.MQTransportProperty == "TRANSPORT_MQSERIES_CLIENT")
                {
                    properties = new Hashtable
                    {
                        { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_CLIENT },
                        { MQC.HOST_NAME_PROPERTY, config.Host },
                        { MQC.PORT_PROPERTY, config.Port },
                        { MQC.CHANNEL_PROPERTY, config.Channel },
                        { MQC.USER_ID_PROPERTY, config.UserId },
                        { MQC.PASSWORD_PROPERTY, config.Password },
                        { MQC.USE_MQCSP_AUTHENTICATION_PROPERTY, true }
                    };
                }
                else if (config.MQTransportProperty == "TRANSPORT_MQSERIES_MANAGED")
                {
                    properties = new Hashtable
                    {
                        { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
                        { MQC.HOST_NAME_PROPERTY, config.Host },
                        { MQC.PORT_PROPERTY, config.Port },
                        { MQC.CHANNEL_PROPERTY, config.Channel },
                        { MQC.USER_ID_PROPERTY, config.UserId },
                        { MQC.PASSWORD_PROPERTY, config.Password },
                        { MQC.USE_MQCSP_AUTHENTICATION_PROPERTY, true }
                    };
                }
                

                Log($"  -> Host: {config.Host}");
                Log($"  -> Port: {config.Port}");
                Log($"  -> Channel: {config.Channel}");
                Log($"  -> QueueManager: {config.QueueManager}");
                Log($"  -> UserId: {config.UserId}");

                Log("Step 4: Connecting to IBM MQ...");
                qMgr = new MQQueueManager(config.QueueManager, properties);
                LogSuccess($"Connected to Queue Manager: {config.QueueManager}");

                bool exit = false;
                while (!exit)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n========= MENU =========");
                    Console.WriteLine("1. Put a message");
                    Console.WriteLine("2. Get all messages");
                    Console.WriteLine("3. Put & Get message");
                    Console.WriteLine("4. Send files from folder to queue");
                    Console.WriteLine("5. Exit");
                    Console.WriteLine("==========================");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Enter your choice: ");
                    Console.ResetColor();
                    string input = Console.ReadLine();

                    switch (input)
                    {
                        case "1":
                            PutMessage(queueName);
                            break;
                        case "2":
                            GetMessages(queueName);
                            break;
                        case "3":
                            PutMessage(queueName);
                            GetMessages(queueName);
                            break;
                        case "4":
                            SendTextFilesToQueue(queueName);
                            break;
                        case "5":
                            exit = true;
                            break;
                        default:
                            LogWarning("Invalid input. Please choose between 1 and 5.");
                            break;
                    }

                }

                qMgr.Close();
                LogSuccess("MQ connection closed successfully.");
            }
            catch (MQException mqe)
            {
                LogError("MQ ERROR: " + mqe.Message);
                LogError($"Reason Code: {mqe.Reason}, Completion Code: {mqe.CompletionCode}");
                if (mqe.InnerException != null)
                    LogError("Inner Exception: " + mqe.InnerException.Message);
            }
            catch (Exception ex)
            {
                LogError("GENERAL EXCEPTION: " + ex.Message);
                if (ex.InnerException != null)
                    LogError("Inner Exception: " + ex.InnerException.Message);
                LogError("STACKTRACE:\n" + ex.StackTrace);
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\nPress Enter to exit...");
            Console.ResetColor();
            Console.ReadLine();
        }

        /// <summary>
        /// Loads MQ settings from a JSON configuration file.
        /// </summary>
        /// <param name="filePath">The path to the configuration file.</param>
        /// <returns>An instance of <see cref="MQSettings"/> with loaded values.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the configuration file is missing.</exception>
        /// <exception cref="Exception">Thrown if the file cannot be parsed.</exception>
        static MQSettings LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogError($"Configuration file not found: {filePath}");
                throw new FileNotFoundException("Settings file missing", filePath);
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<MQSettings>(json);
            }
            catch (Exception ex)
            {
                LogError("Failed to parse mqsettings.json: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Puts a single message onto the specified IBM MQ queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to put the message on.</param>
        /// <exception cref="MQException">Thrown if an MQ error occurs.</exception>
        /// <exception cref="Exception">Thrown if a general error occurs.</exception>
        static void PutMessage(string queueName)
        {
            try
            {
                Log("Step 1: Preparing to put message...");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter message to send: ");
                Console.ResetColor();
                string message = Console.ReadLine();

                // Validation: prevent blank message
                if (string.IsNullOrWhiteSpace(message))
                {
                    LogWarning("Message cannot be empty or whitespace. Please enter valid content.");
                    return; // Exit without putting message
                }

                Log($"Step 2: Accessing queue '{queueName}' for output...");
                using (MQQueue queue = qMgr.AccessQueue(queueName, MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING))
                {
                    Log("Step 3: Creating MQMessage...");
                    MQMessage mqMessage = new MQMessage();
                    mqMessage.WriteUTF(message);

                    Log($"Step 4: Putting message to queue '{queueName}'...");
                    queue.Put(mqMessage);

                    LogSuccess($"Message successfully sent to queue '{queueName}'.");
                    Log($"Content: {message}");
                }
            }
            catch (MQException mqe)
            {
                LogError($"MQ PUT ERROR: Reason={mqe.Reason}, Message={mqe.Message}");
                if (mqe.Reason == MQC.MQRC_Q_FULL)
                    LogWarning("Queue is full. Cannot put message.");
                throw;
            }
            catch (Exception ex)
            {
                LogError("Failed to put message: " + ex.Message);
                throw;
            }
        }


        /// <summary>
        /// Retrieves all available messages from the specified IBM MQ queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to get messages from.</param>
        /// <exception cref="MQException">Thrown if an MQ error occurs.</exception>
        /// <exception cref="Exception">Thrown if a general error occurs.</exception>
        static void GetMessages(string queueName)
        {
            try
            {
                Log($"Step 1: Accessing queue '{queueName}' for input...");
                using (MQQueue queue = qMgr.AccessQueue(queueName, MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING))
                {
                    MQGetMessageOptions gmo = new MQGetMessageOptions();
                    int count = 0;

                    Log("Step 2: Reading messages from queue...");
                    while (true)
                    {
                        MQMessage mqMessage = new MQMessage();
                        try
                        {
                            queue.Get(mqMessage, gmo);
                            string received = mqMessage.ReadUTF();
                            count++;
                            LogSuccess($"Message {count} received.");
                            Log($"Content: {received}");
                        }
                        catch (MQException mqe) when (mqe.Reason == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            if (count == 0)
                                Log("No messages found in queue.");
                            else
                                LogSuccess($"Done. {count} message(s) retrieved from queue '{queueName}'.");
                            break;
                        }
                    }
                }
            }
            catch (MQException mqe)
            {
                LogError($"MQ GET ERROR: Reason={mqe.Reason}, Message={mqe.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogError("Failed to get messages: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Sends all readable text files from a specified folder to the IBM MQ queue.
        /// Moves processed files to BACKUP and failed files to UNPROCESSED.
        /// </summary>
        /// <param name="queueName">The name of the queue to send files to.</param>
        /// <exception cref="Exception">Thrown if an error occurs during file processing.</exception>
        static void SendTextFilesToQueue(string queueName)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter folder path: ");
                Console.ResetColor();
                string folderPath = Console.ReadLine();

                if (!Directory.Exists(folderPath))
                {
                    LogError("Invalid directory. Folder does not exist.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Enter file extension (e.g., .txt): ");
                Console.ResetColor();
                string extension = Console.ReadLine();

                if (!extension.StartsWith("."))
                    extension = "." + extension;

                string[] files = Directory.GetFiles(folderPath, $"*{extension}");
                if (files.Length == 0)
                {
                    LogWarning($"No '{extension}' files found in the specified folder.");
                    return;
                }

                // Prepare subdirectories
                string backupDir = Path.Combine(folderPath, "BACKUP");
                string unprocessedDir = Path.Combine(folderPath, "UNPROCESSED");

                Directory.CreateDirectory(backupDir);
                Directory.CreateDirectory(unprocessedDir);

                Log($"Found {files.Length} '{extension}' files. Preparing to send to queue...");

                using (MQQueue queue = qMgr.AccessQueue(queueName, MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING))
                {
                    foreach (string filePath in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string fileExt = Path.GetExtension(filePath);
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string renamedFile = $"{fileName}_{timestamp}{fileExt}";

                        // Check if file is readable
                        if (!IsFileReadable(filePath))
                        {
                            LogWarning($"Skipped unreadable file: {fileName + fileExt}");
                            continue;
                        }

                        try
                        {
                            string fileContent = File.ReadAllText(filePath, System.Text.Encoding.ASCII);

                            MQMessage mqMessage = new MQMessage();
                            mqMessage.Format = MQC.MQFMT_STRING;

                            mqMessage.CharacterSet = Convert.ToInt16("1208");
                            mqMessage.WriteString(fileContent);

                            MQPutMessageOptions queuePutMessageOptions = new MQPutMessageOptions();
                            queue.Put(mqMessage, queuePutMessageOptions);

                            string destPath = Path.Combine(backupDir, renamedFile);
                            if (File.Exists(destPath))
                                File.Delete(destPath);
                            File.Move(filePath, destPath);

                            LogSuccess($"Sent file '{fileName + fileExt}' to queue and moved to BACKUP as '{renamedFile}'.");
                        }
                        catch (Exception ex)
                        {
                            string destPath = Path.Combine(unprocessedDir, renamedFile);
                            if (File.Exists(destPath))
                                File.Delete(destPath);
                            File.Move(filePath, destPath);

                            LogError($"Failed to send file '{fileName + fileExt}': {ex.Message}");
                            LogWarning($"Moved file to UNPROCESSED as '{renamedFile}'.");
                        }
                    }
                }

                LogSuccess("File processing complete.");
            }
            catch (Exception ex)
            {
                LogError("Error during file-to-queue operation: " + ex.Message);
                throw;
            }
        }

        // === Logging Helpers ===

        /// <summary>
        /// Logs a standard message to the console with a timestamp.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        static void Log(string msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:tt}] {msg}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs a success message to the console with a timestamp.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        static void LogSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:tt}] [✓] {msg}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs a warning message to the console with a timestamp.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        static void LogWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:tt}] [!] {msg}");
            Console.ResetColor();
        }

        /// <summary>
        /// Logs an error message to the console with a timestamp.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        static void LogError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:tt}] [ERROR] {msg}");
            Console.ResetColor();
        }

        /// <summary>
        /// Checks if a file is readable.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>True if the file is readable; otherwise, false.</returns>
        static bool IsFileReadable(string path)
        {
            try
            {
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Just test access
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Prints the application banner to the console.
        /// </summary>
        static void PrintBanner()
        {
            const string version = "v1.0.1";
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[ MQ CLIENT] Loaded - Version {version}");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays the splash screen to the user at startup.
        /// </summary>
        static void ShowSplashScreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@"
██╗██████╗ ███╗   ███╗    ███╗   ███╗ ██████╗ 
██║██╔══██╗████╗ ████║    ████╗ ████║██╔═══██╗
██║██████╔╝██╔████╔██║    ██╔████╔██║██║   ██║
██║██╔══██╗██║╚██╔╝██║    ██║╚██╔╝██║██║▄▄ ██║
██║██████╔╝██║ ╚═╝ ██║    ██║ ╚═╝ ██║╚██████╔╝
╚═╝╚═════╝ ╚═╝     ╚═╝    ╚═╝     ╚═╝ ╚══▀▀═╝                    
         IBM MQ CONNECTOR v1.0.1");
            Console.ResetColor();

            Console.WriteLine();
            string spinner = @"|/-\";
            for (int i = 0; i < 20; i++)
            {
                Console.Write($"\rStarting up {spinner[i % spinner.Length]}");
                Thread.Sleep(100);
            }

            Console.WriteLine("\rReady to connect!       ");
            Console.WriteLine("======================================");
        }
    }
}
