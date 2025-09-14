using System;
using libquip.threads;

namespace libquip.Examples
{
    /// <summary>
    /// Demonstration of the new V2 Get Thread API method
    /// </summary>
    public static class GetThreadV2Demo
    {
        /// <summary>
        /// Demonstrates how to use the new GetThreadV2 method
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path to retrieve</param>
        public static void DemonstrateGetThreadV2(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Quip API V2 Get Thread Demo ===");
            Console.WriteLine();

            try
            {
                // Create a V2 API instance
                var quipThreadV2 = new QuipThread(token, QuipApiVersion.V2);
                Console.WriteLine($"API Info: {quipThreadV2.GetApiInfo()}");
                Console.WriteLine();

                // Call the V2 Get Thread method
                Console.WriteLine($"Getting thread information for: {threadIdOrSecretPath}");
                var response = quipThreadV2.GetThreadV2(threadIdOrSecretPath);
                var thread = response.thread;

                // Display the enhanced V2 information
                Console.WriteLine("=== Thread Information (V2) ===");
                Console.WriteLine($"ID: {thread.id}");
                Console.WriteLine($"Title: {thread.title}");
                Console.WriteLine($"Type: {thread.type}");
                Console.WriteLine($"Author ID: {thread.author_id}");
                Console.WriteLine($"Is Template: {thread.is_template}");
                Console.WriteLine($"Secret Path: {thread.secret_path}");
                Console.WriteLine($"Link: {thread.link}");
                Console.WriteLine($"Owning Company ID: {thread.owning_company_id}");
                Console.WriteLine($"Created: {UnixMicrosecondsToDateTime(thread.created_usec)}");
                Console.WriteLine($"Last Updated: {UnixMicrosecondsToDateTime(thread.updated_usec)}");
                
                if (thread.sharing != null)
                {
                    Console.WriteLine("=== Sharing Information ===");
                    Console.WriteLine($"Company ID: {thread.sharing.company_id}");
                    Console.WriteLine($"Company Mode: {thread.sharing.company_mode}");
                }

                // Demonstrate thread type handling
                Console.WriteLine();
                Console.WriteLine("=== Thread Type Analysis ===");
                switch (thread.type)
                {
                    case ThreadTypeV2.DOCUMENT:
                        Console.WriteLine("?? This is a document thread - contains rich text content");
                        break;
                    case ThreadTypeV2.SPREADSHEET:
                        Console.WriteLine("?? This is a spreadsheet thread - contains tabular data");
                        break;
                    case ThreadTypeV2.SLIDES:
                        Console.WriteLine("??? This is a slides thread - contains presentation slides");
                        break;
                    case ThreadTypeV2.CHAT:
                        Console.WriteLine("?? This is a chat thread - contains conversation messages");
                        break;
                    default:
                        Console.WriteLine($"? Unknown thread type: {thread.type}");
                        break;
                }

                Console.WriteLine();
                if (thread.is_template)
                {
                    Console.WriteLine("?? This thread is a template and can be used to create new documents");
                }
                else
                {
                    Console.WriteLine("?? This is a regular thread (not a template)");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"? Version Error: {ex.Message}");
                Console.WriteLine("?? Make sure you're using a QuipThread instance created with API Version 2");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Parameter Error: {ex.Message}");
                Console.WriteLine("?? Check that your thread ID or secret path is between 10-32 characters");
            }
            catch (QuipException ex)
            {
                Console.WriteLine($"? Quip API Error: {ex.Message}");
                Console.WriteLine("?? Check your API token and thread permissions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Unexpected Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Compares V1 and V2 API responses for the same thread
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadId">Thread ID to compare (must be thread ID, not secret path for V1)</param>
        public static void CompareV1AndV2Responses(string token, string threadId)
        {
            Console.WriteLine("=== Comparing V1 vs V2 API Responses ===");
            Console.WriteLine();

            try
            {
                // V1 API call
                var quipThreadV1 = new QuipThread(token, QuipApiVersion.V1);
                var v1Response = quipThreadV1.GetThread(threadId);
                
                Console.WriteLine("=== V1 API Response ===");
                Console.WriteLine($"ID: {v1Response.thread.id}");
                Console.WriteLine($"Title: {v1Response.thread.title}");
                Console.WriteLine($"Type: {v1Response.thread.type}");
                Console.WriteLine($"Author ID: {v1Response.thread.author_id}");
                Console.WriteLine($"Link: {v1Response.thread.link}");
                Console.WriteLine($"Created: {UnixMicrosecondsToDateTime(v1Response.thread.created_usec)}");
                Console.WriteLine($"Updated: {UnixMicrosecondsToDateTime(v1Response.thread.updated_usec)}");
                Console.WriteLine($"HTML Content Length: {v1Response.html?.Length ?? 0} characters");
                Console.WriteLine();

                // V2 API call
                var quipThreadV2 = new QuipThread(token, QuipApiVersion.V2);
                var v2Response = quipThreadV2.GetThreadV2(threadId);

                Console.WriteLine("=== V2 API Response ===");
                Console.WriteLine($"ID: {v2Response.thread.id}");
                Console.WriteLine($"Title: {v2Response.thread.title}");
                Console.WriteLine($"Type: {v2Response.thread.type}");
                Console.WriteLine($"Author ID: {v2Response.thread.author_id}");
                Console.WriteLine($"Link: {v2Response.thread.link}");
                Console.WriteLine($"Created: {UnixMicrosecondsToDateTime(v2Response.thread.created_usec)}");
                Console.WriteLine($"Updated: {UnixMicrosecondsToDateTime(v2Response.thread.updated_usec)}");
                Console.WriteLine($"Is Template: {v2Response.thread.is_template}");
                Console.WriteLine($"Secret Path: {v2Response.thread.secret_path}");
                Console.WriteLine($"Owning Company: {v2Response.thread.owning_company_id}");
                Console.WriteLine();

                Console.WriteLine("=== Key Differences ===");
                Console.WriteLine("? V1 includes full HTML content");
                Console.WriteLine("? V2 includes template status, secret path, and company information");
                Console.WriteLine("? V2 has more detailed thread type enum (includes SLIDES)");
                Console.WriteLine("? V2 supports secret path as identifier (not just thread ID)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error during comparison: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates error handling for the V2 Get Thread method
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void DemonstrateErrorHandling(string token)
        {
            Console.WriteLine("=== V2 Get Thread Error Handling Demo ===");
            Console.WriteLine();

            // Test 1: Using V2 method on V1 instance
            Console.WriteLine("Test 1: Calling GetThreadV2 on V1 instance");
            try
            {
                var threadV1 = new QuipThread(token, QuipApiVersion.V1);
                threadV1.GetThreadV2("some-thread-id");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }
            Console.WriteLine();

            // Test 2: Invalid thread ID length (too short)
            Console.WriteLine("Test 2: Thread ID too short");
            try
            {
                var threadV2 = new QuipThread(token, QuipApiVersion.V2);
                threadV2.GetThreadV2("short");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }
            Console.WriteLine();

            // Test 3: Invalid thread ID length (too long)
            Console.WriteLine("Test 3: Thread ID too long");
            try
            {
                var threadV2 = new QuipThread(token, QuipApiVersion.V2);
                threadV2.GetThreadV2("this-thread-id-is-way-too-long-for-the-api-specification");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }
            Console.WriteLine();

            // Test 4: Null or empty thread ID
            Console.WriteLine("Test 4: Null or empty thread ID");
            try
            {
                var threadV2 = new QuipThread(token, QuipApiVersion.V2);
                threadV2.GetThreadV2("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("All error handling tests completed successfully!");
        }

        /// <summary>
        /// Helper method to convert Unix microseconds to DateTime
        /// </summary>
        /// <param name="unixMicroseconds">Unix timestamp in microseconds</param>
        /// <returns>DateTime representation</returns>
        private static DateTime UnixMicrosecondsToDateTime(long unixMicroseconds)
        {
            var unixSeconds = unixMicroseconds / 1000000;
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
        }
    }
}