using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using libquip.threads;

namespace libquip.Examples
{
    /// <summary>
    /// Demonstration of the V2 Get Thread HTML API methods
    /// </summary>
    public static class GetThreadHtmlV2Demo
    {
        /// <summary>
        /// Demonstrates basic HTML retrieval with pagination
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path to retrieve</param>
        public static void DemonstrateBasicHtmlRetrieval(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Basic HTML Retrieval Demo ===");
            Console.WriteLine();

            try
            {
                // Create a V2 API instance
                var quipThread = new QuipThread(token, QuipApiVersion.V2);
                Console.WriteLine($"API Info: {quipThread.GetApiInfo()}");
                Console.WriteLine();

                // Get first page of HTML content
                Console.WriteLine("Getting first page of HTML content...");
                var response = quipThread.GetThreadHtmlV2(threadIdOrSecretPath);

                Console.WriteLine($"HTML Content Length: {response.html.Length} characters");
                Console.WriteLine($"Content Preview: {GetPreview(response.html, 200)}");

                // Check for pagination
                if (!string.IsNullOrEmpty(response.response_metadata?.next_cursor))
                {
                    Console.WriteLine($"? More content available (next cursor: {response.response_metadata.next_cursor.Substring(0, Math.Min(20, response.response_metadata.next_cursor.Length))}...)");
                }
                else
                {
                    Console.WriteLine("? This is all the content (no pagination needed)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates complete HTML retrieval using the convenience method
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path to retrieve</param>
        public static void DemonstrateCompleteHtmlRetrieval(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Complete HTML Retrieval Demo ===");
            Console.WriteLine();

            try
            {
                var quipThread = new QuipThread(token, QuipApiVersion.V2);

                Console.WriteLine("Getting complete HTML content (all pages automatically)...");
                var startTime = DateTime.Now;
                var completeHtml = quipThread.GetCompleteThreadHtmlV2(threadIdOrSecretPath, 100);
                var elapsed = DateTime.Now - startTime;

                Console.WriteLine($"? Complete HTML retrieved in {elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"Total Content Length: {completeHtml.Length} characters");
                Console.WriteLine($"Estimated Word Count: {EstimateWordCount(completeHtml)}");

                // Analyze content
                AnalyzeHtmlContent(completeHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates manual pagination for fine-grained control
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path to retrieve</param>
        public static void DemonstrateManualPagination(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Manual Pagination Demo ===");
            Console.WriteLine();

            try
            {
                var quipThread = new QuipThread(token, QuipApiVersion.V2);
                var allHtml = new List<string>();
                string cursor = null;
                int pageCount = 0;
                int totalLength = 0;

                Console.WriteLine("Processing pages manually with cursor-based pagination...");

                do
                {
                    pageCount++;
                    Console.WriteLine($"Fetching page {pageCount}...");

                    var response = quipThread.GetThreadHtmlV2(threadIdOrSecretPath, cursor, 50);
                    allHtml.Add(response.html);
                    totalLength += response.html.Length;

                    Console.WriteLine($"  Page {pageCount}: {response.html.Length} characters");

                    cursor = response.response_metadata?.next_cursor;
                    if (!string.IsNullOrEmpty(cursor))
                    {
                        Console.WriteLine($"  Next cursor: {cursor.Substring(0, Math.Min(15, cursor.Length))}...");
                    }
                }
                while (!string.IsNullOrEmpty(cursor));

                var completeHtml = string.Join("", allHtml);
                Console.WriteLine();
                Console.WriteLine($"? Manual pagination complete:");
                Console.WriteLine($"  Total pages: {pageCount}");
                Console.WriteLine($"  Total content: {totalLength} characters");
                Console.WriteLine($"  Average page size: {totalLength / pageCount} characters");

                // Demonstrate section extraction from paginated content
                ExtractSectionIds(completeHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates error handling for various scenarios
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void DemonstrateErrorHandling(string token)
        {
            Console.WriteLine("=== Error Handling Demo ===");
            Console.WriteLine();

            var quipThread = new QuipThread(token, QuipApiVersion.V2);

            // Test 1: Invalid thread ID length
            Console.WriteLine("Test 1: Invalid thread ID length");
            try
            {
                quipThread.GetThreadHtmlV2("short");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }

            // Test 2: Empty thread ID
            Console.WriteLine("\nTest 2: Empty thread ID");
            try
            {
                quipThread.GetThreadHtmlV2("");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"? Expected error caught: {ex.Message}");
            }

            // Test 3: Invalid cursor (simulated)
            Console.WriteLine("\nTest 3: Invalid/expired cursor");
            try
            {
                quipThread.GetThreadHtmlV2("validthreadid123", "invalid-cursor");
            }
            catch (QuipException ex)
            {
                Console.WriteLine($"? Expected API error caught: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error caught (cursor validation): {ex.Message}");
            }

            Console.WriteLine("\nAll error handling tests completed!");
        }

        /// <summary>
        /// Demonstrates working with different thread identifier types
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        public static void DemonstrateIdentifierTypes(string token)
        {
            Console.WriteLine("=== Thread Identifier Types Demo ===");
            Console.WriteLine();

            var quipThread = new QuipThread(token, QuipApiVersion.V2);

            // Example identifiers (replace with real ones)
            var threadId = "threadAbc123456";
            var secretPath = "3fs7B2leat8";

            try
            {
                Console.WriteLine("1. Using Thread ID:");
                var htmlById = quipThread.GetThreadHtmlV2(threadId, limit: 1);
                Console.WriteLine($"   Retrieved {htmlById.html.Length} characters");

                Console.WriteLine("\n2. Using Secret Path:");
                var htmlByPath = quipThread.GetThreadHtmlV2(secretPath, limit: 1);
                Console.WriteLine($"   Retrieved {htmlByPath.html.Length} characters");

                Console.WriteLine("\n? Both identifier types work with the V2 API");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error (use real thread identifiers): {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates performance comparison between different approaches
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path to retrieve</param>
        public static void DemonstratePerformanceComparison(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Performance Comparison Demo ===");
            Console.WriteLine();

            try
            {
                var quipThread = new QuipThread(token, QuipApiVersion.V2);

                // Test complete method
                Console.WriteLine("Testing GetCompleteThreadHtmlV2 method...");
                var start1 = DateTime.Now;
                var completeHtml = quipThread.GetCompleteThreadHtmlV2(threadIdOrSecretPath);
                var elapsed1 = DateTime.Now - start1;

                // Test manual pagination
                Console.WriteLine("Testing manual pagination...");
                var start2 = DateTime.Now;
                var allHtml = new List<string>();
                string cursor = null;
                int pageCount = 0;

                do
                {
                    var response = quipThread.GetThreadHtmlV2(threadIdOrSecretPath, cursor, 100);
                    allHtml.Add(response.html);
                    cursor = response.response_metadata?.next_cursor;
                    pageCount++;
                }
                while (!string.IsNullOrEmpty(cursor));

                var manualHtml = string.Join("", allHtml);
                var elapsed2 = DateTime.Now - start2;

                // Results
                Console.WriteLine("\n=== Performance Results ===");
                Console.WriteLine($"Complete method:");
                Console.WriteLine($"  Time: {elapsed1.TotalSeconds:F2} seconds");
                Console.WriteLine($"  Content: {completeHtml.Length} characters");

                Console.WriteLine($"Manual pagination:");
                Console.WriteLine($"  Time: {elapsed2.TotalSeconds:F2} seconds");
                Console.WriteLine($"  Content: {manualHtml.Length} characters");
                Console.WriteLine($"  Pages: {pageCount}");

                Console.WriteLine($"\nContent length match: {completeHtml.Length == manualHtml.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to get a preview of HTML content
        /// </summary>
        private static string GetPreview(string html, int maxLength)
        {
            if (string.IsNullOrEmpty(html))
                return "[No content]";

            // Strip HTML tags for preview
            var plainText = Regex.Replace(html, "<[^>]+>", "");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            if (plainText.Length <= maxLength)
                return plainText;

            return plainText.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Estimates word count from HTML content
        /// </summary>
        private static int EstimateWordCount(string html)
        {
            if (string.IsNullOrEmpty(html))
                return 0;

            // Strip HTML tags and count words
            var plainText = Regex.Replace(html, "<[^>]+>", "");
            var words = Regex.Split(plainText, @"\s+", RegexOptions.IgnoreCase);
            
            int count = 0;
            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Analyzes HTML content for various elements
        /// </summary>
        private static void AnalyzeHtmlContent(string html)
        {
            Console.WriteLine("\n=== Content Analysis ===");

            var analysis = new Dictionary<string, int>
            {
                ["Tables"] = Regex.Matches(html, "<table", RegexOptions.IgnoreCase).Count,
                ["Images"] = Regex.Matches(html, "<img", RegexOptions.IgnoreCase).Count,
                ["Links"] = Regex.Matches(html, "<a href", RegexOptions.IgnoreCase).Count,
                ["Headings"] = Regex.Matches(html, "<h[1-6]", RegexOptions.IgnoreCase).Count,
                ["Lists"] = Regex.Matches(html, "<[ou]l", RegexOptions.IgnoreCase).Count,
                ["Sections"] = Regex.Matches(html, @"id=""[a-zA-Z0-9]{10,}""").Count
            };

            foreach (var item in analysis)
            {
                Console.WriteLine($"  {item.Key}: {item.Value}");
            }
        }

        /// <summary>
        /// Extracts and displays section IDs from HTML content
        /// </summary>
        private static void ExtractSectionIds(string html)
        {
            Console.WriteLine("\n=== Section ID Extraction ===");

            var sectionMatches = Regex.Matches(html, @"id=""([a-zA-Z0-9]{10,})""");
            var sectionIds = new List<string>();
            var seen = new HashSet<string>();

            // Extract unique section IDs
            foreach (Match match in sectionMatches)
            {
                var sectionId = match.Groups[1].Value;
                if (seen.Add(sectionId) && sectionIds.Count < 10) // Show first 10 unique
                {
                    sectionIds.Add(sectionId);
                }
            }

            if (sectionIds.Count > 0)
            {
                Console.WriteLine($"Found {sectionMatches.Count} total sections, showing first {sectionIds.Count}:");
                foreach (var sectionId in sectionIds)
                {
                    Console.WriteLine($"  - {sectionId}");
                }

                if (sectionMatches.Count > 10)
                {
                    Console.WriteLine($"  ... and {sectionMatches.Count - 10} more sections");
                }
            }
            else
            {
                Console.WriteLine("No section IDs found in the HTML content");
            }
        }

        /// <summary>
        /// Demonstrates practical use case: exporting thread to file
        /// </summary>
        /// <param name="token">Your Quip API token</param>
        /// <param name="threadIdOrSecretPath">Thread ID or secret path</param>
        public static void DemonstrateExportToFile(string token, string threadIdOrSecretPath)
        {
            Console.WriteLine("=== Export to File Demo ===");
            Console.WriteLine();

            try
            {
                var quipThread = new QuipThread(token, QuipApiVersion.V2);

                // Get thread info and HTML content
                Console.WriteLine("Getting thread information and HTML content...");
                var threadInfo = quipThread.GetThreadV2(threadIdOrSecretPath);
                var htmlContent = quipThread.GetCompleteThreadHtmlV2(threadIdOrSecretPath);

                // Create filename
                var safeTitle = Regex.Replace(threadInfo.thread.title, @"[^\w\s]", "")
                                    .Replace(" ", "_");
                var filename = $"{threadInfo.thread.id}_{safeTitle}.html";

                // Create complete HTML document
                var completeHtml = $@"<!DOCTYPE html>
<html>
<head>
    <title>{threadInfo.thread.title}</title>
    <meta charset=""UTF-8"">
    <meta name=""quip-thread-id"" content=""{threadInfo.thread.id}"">
    <meta name=""quip-secret-path"" content=""{threadInfo.thread.secret_path}"">
    <meta name=""quip-type"" content=""{threadInfo.thread.type}"">
    <meta name=""quip-is-template"" content=""{threadInfo.thread.is_template}"">
    <meta name=""export-date"" content=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .export-info {{ background: #f5f5f5; padding: 10px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class=""export-info"">
        <h2>{threadInfo.thread.title}</h2>
        <p><strong>Thread ID:</strong> {threadInfo.thread.id}</p>
        <p><strong>Type:</strong> {threadInfo.thread.type}</p>
        <p><strong>Is Template:</strong> {threadInfo.thread.is_template}</p>
        <p><strong>Exported:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    {htmlContent}
</body>
</html>";

                // In a real application, you would save to file:
                // File.WriteAllText(filename, completeHtml);

                Console.WriteLine($"? HTML document prepared for export:");
                Console.WriteLine($"  Filename: {filename}");
                Console.WriteLine($"  Content length: {completeHtml.Length} characters");
                Console.WriteLine($"  Thread title: {threadInfo.thread.title}");
                Console.WriteLine($"  Thread type: {threadInfo.thread.type}");

                // Show content preview
                Console.WriteLine($"\nDocument preview:");
                Console.WriteLine(GetPreview(htmlContent, 300));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
            }
        }
    }
}

// Extension methods to make LINQ work with older .NET versions
namespace System.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                if (predicate(item))
                    yield return item;
            }
        }

        public static IEnumerable<TResult> Select<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
        {
            foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source)
        {
            var seen = new HashSet<T>();
            foreach (var item in source)
            {
                if (seen.Add(item))
                    yield return item;
            }
        }

        public static IEnumerable<T> Take<T>(this IEnumerable<T> source, int count)
        {
            int taken = 0;
            foreach (var item in source)
            {
                if (taken >= count) yield break;
                yield return item;
                taken++;
            }
        }

        public static bool Any<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
                return true;
            return false;
        }

        public static int Count<T>(this IEnumerable<T> source)
        {
            int count = 0;
            foreach (var item in source)
                count++;
            return count;
        }
    }
}