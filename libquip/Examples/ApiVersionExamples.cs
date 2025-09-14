using libquip.threads;
using libquip.folders;
using libquip.users;
using libquip.messages;

namespace libquip.Examples
{
    /// <summary>
    /// Examples demonstrating how to use different API versions with the Quip API
    /// </summary>
    public static class ApiVersionExamples
    {
        /// <summary>
        /// Example showing how to use API version 1 (default behavior, backward compatible)
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void UseApiV1(string token)
        {
            // These are equivalent and use API version 1
            var threadV1a = new QuipThread(token);                          // Default to V1
            var threadV1b = new QuipThread(token, QuipApiVersion.V1);       // Explicit V1
            var threadV1c = new QuipThread(token, 1);                       // Integer version 1

            var folderV1 = new QuipFolder(token);                           // Default to V1
            var userV1 = new QuipUser(token);                               // Default to V1
            var messageV1 = new QuipMessage(token);                         // Default to V1

            // Check which version is being used
            System.Console.WriteLine($"Thread API: {threadV1a.GetApiInfo()}");

            // Use V1 GetThread method (returns Document object)
            try
            {
                var document = threadV1a.GetThread("your-thread-id");
                System.Console.WriteLine($"V1 Thread Title: {document.thread.title}");
            }
            catch (QuipException ex)
            {
                System.Console.WriteLine($"V1 API Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing how to use API version 2
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void UseApiV2(string token)
        {
            // These are equivalent and use API version 2
            var threadV2a = new QuipThread(token, QuipApiVersion.V2);       // Explicit V2
            var threadV2b = new QuipThread(token, 2);                       // Integer version 2

            var folderV2 = new QuipFolder(token, QuipApiVersion.V2);
            var userV2 = new QuipUser(token, QuipApiVersion.V2);
            var messageV2 = new QuipMessage(token, QuipApiVersion.V2);

            // Check which version is being used
            System.Console.WriteLine($"Thread API: {threadV2a.GetApiInfo()}");

            // Use V2 GetThreadV2 method (returns ThreadResponseV2 object with more detailed information)
            try
            {
                var threadResponse = threadV2a.GetThreadV2("your-thread-id-or-secret-path");
                var thread = threadResponse.thread;
                
                System.Console.WriteLine($"V2 Thread Title: {thread.title}");
                System.Console.WriteLine($"V2 Thread Type: {thread.type}");
                System.Console.WriteLine($"V2 Is Template: {thread.is_template}");
                System.Console.WriteLine($"V2 Secret Path: {thread.secret_path}");
                System.Console.WriteLine($"V2 Owning Company: {thread.owning_company_id}");
                System.Console.WriteLine($"V2 Created: {UnixMicrosecondsToDateTime(thread.created_usec)}");
                System.Console.WriteLine($"V2 Updated: {UnixMicrosecondsToDateTime(thread.updated_usec)}");
            }
            catch (QuipException ex)
            {
                System.Console.WriteLine($"V2 API Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing how to work with both API versions simultaneously
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void UseMixedVersions(string token)
        {
            // You can use different API versions for different operations
            var threadV1 = new QuipThread(token, QuipApiVersion.V1);
            var threadV2 = new QuipThread(token, QuipApiVersion.V2);

            // Each instance uses its own API version
            System.Console.WriteLine($"V1 API: {threadV1.GetApiInfo()}");
            System.Console.WriteLine($"V2 API: {threadV2.GetApiInfo()}");

            // Access version information
            System.Console.WriteLine($"V1 Version: {threadV1.Version}");
            System.Console.WriteLine($"V2 Version: {threadV2.Version}");

            // Compare V1 vs V2 responses for the same thread
            string threadIdOrPath = "your-thread-id";
            
            try
            {
                // V1 API call
                var v1Document = threadV1.GetThread(threadIdOrPath);
                System.Console.WriteLine($"V1 Response - Title: {v1Document.thread.title}, Type: {v1Document.thread.type}");

                // V2 API call (more detailed information)
                var v2Response = threadV2.GetThreadV2(threadIdOrPath);
                System.Console.WriteLine($"V2 Response - Title: {v2Response.thread.title}, Type: {v2Response.thread.type}, Template: {v2Response.thread.is_template}");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing error handling for invalid API versions and method calls
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void ErrorHandlingExample(string token)
        {
            try
            {
                // This will throw an ArgumentException for unsupported version
                var invalidThread = new QuipThread(token, 3);
            }
            catch (System.ArgumentException ex)
            {
                System.Console.WriteLine($"Invalid Version Error: {ex.Message}");
            }

            try
            {
                // This will throw an ArgumentNullException for null token
                var invalidThread = new QuipThread(null);
            }
            catch (System.ArgumentNullException ex)
            {
                System.Console.WriteLine($"Null Token Error: {ex.Message}");
            }

            try
            {
                // This will throw InvalidOperationException when trying to use V2 method on V1 instance
                var threadV1 = new QuipThread(token, QuipApiVersion.V1);
                threadV1.GetThreadV2("some-thread-id");
            }
            catch (System.InvalidOperationException ex)
            {
                System.Console.WriteLine($"Wrong Version Error: {ex.Message}");
            }

            try
            {
                // This will throw ArgumentException for invalid thread ID length
                var threadV2 = new QuipThread(token, QuipApiVersion.V2);
                threadV2.GetThreadV2("short"); // Too short (< 10 characters)
            }
            catch (System.ArgumentException ex)
            {
                System.Console.WriteLine($"Invalid Thread ID Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example demonstrating V2-specific thread information features
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void V2SpecificFeatures(string token)
        {
            var threadV2 = new QuipThread(token, QuipApiVersion.V2);

            try
            {
                // You can use either thread ID or secret path with V2 API
                string secretPath = "3fs7B2leat8"; // Example from API docs
                string threadId = "your-actual-thread-id";

                // Using secret path (from URL)
                var responseByPath = threadV2.GetThreadV2(secretPath);
                System.Console.WriteLine($"Thread by secret path: {responseByPath.thread.title}");

                // Using thread ID
                var responseById = threadV2.GetThreadV2(threadId);
                System.Console.WriteLine($"Thread by ID: {responseById.thread.title}");

                // V2 specific fields
                var thread = responseById.thread;
                System.Console.WriteLine($"Thread Type: {thread.type}"); // DOCUMENT, SPREADSHEET, SLIDES, CHAT
                System.Console.WriteLine($"Is Template: {thread.is_template}");
                System.Console.WriteLine($"Secret Path: {thread.secret_path}");
                System.Console.WriteLine($"Owning Company: {thread.owning_company_id}");

                // Handle different thread types
                switch (thread.type)
                {
                    case ThreadTypeV2.DOCUMENT:
                        System.Console.WriteLine("This is a document thread");
                        break;
                    case ThreadTypeV2.SPREADSHEET:
                        System.Console.WriteLine("This is a spreadsheet thread");
                        break;
                    case ThreadTypeV2.SLIDES:
                        System.Console.WriteLine("This is a slides thread");
                        break;
                    case ThreadTypeV2.CHAT:
                        System.Console.WriteLine("This is a chat thread");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"V2 Features Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to convert Unix microseconds to DateTime
        /// </summary>
        /// <param name="unixMicroseconds">Unix timestamp in microseconds</param>
        /// <returns>DateTime representation</returns>
        private static System.DateTime UnixMicrosecondsToDateTime(long unixMicroseconds)
        {
            var unixSeconds = unixMicroseconds / 1000000;
            return System.DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
        }
    }
}