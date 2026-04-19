using System.Text.RegularExpressions;
using GroupDocs.Watermark;
using GroupDocs.Watermark.Common;
using GroupDocs.Watermark.Search;
using GroupDocs.Watermark.Search.SearchCriteria;
using GroupDocs.Watermark.Watermarks;

namespace WatermarkAuditTool
{
    /// <summary>
    /// Watermark Audit, Verification, and Compliance Tool.
    /// </summary>
    /// <remarks>
    /// Scans documents for existing watermarks, verifies them against expected values,
    /// runs compliance checks, replaces outdated text, removes unwanted marks,
    /// and adds hidden tracking watermarks for leak detection.
    /// </remarks>
    class Program
    {
        private const string LicensePath = "license.lic";
        private static readonly string ResourceDir = Path.Combine(AppContext.BaseDirectory, "Resources");
        private static readonly string SampleDocsDir = Path.Combine(ResourceDir, "SampleDocs");
        private static readonly string OutputDir = Path.Combine(AppContext.BaseDirectory, "Results");
        private static readonly string ExpectedLogoPath = Path.Combine(ResourceDir, "Logos", "expected-logo.png");

        static void Main()
        {
            Console.WriteLine("=== GroupDocs.Watermark Audit Tool ===\n");

            try
            {
                var license = new License();
                license.SetLicense(LicensePath);
                Console.WriteLine("License applied successfully.");
            }
            catch
            {
                Console.WriteLine("Warning: License not found. Running in evaluation mode.");
            }

            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            string sampleFile = Path.Combine(SampleDocsDir, "sample-watermarked.pdf");
            string draftFile = Path.Combine(SampleDocsDir, "sample-draft.pdf");

            Console.WriteLine("--- Mode: scan ---");
            ScanAllWatermarks(sampleFile);

            Console.WriteLine("\n--- Mode: verify (text) ---");
            VerifyTextWatermark(sampleFile, "GroupDocs Confidential");

            if (File.Exists(ExpectedLogoPath))
            {
                Console.WriteLine("\n--- Mode: verify (logo) ---");
                VerifyLogoWatermark(sampleFile, ExpectedLogoPath);
            }

            Console.WriteLine("\n--- Mode: compliance ---");
            RunComplianceReport(sampleFile,
                expectedText: "GroupDocs Confidential",
                expectedFont: "Arial",
                expectedMinSize: 15,
                expectedBold: true);

            Console.WriteLine("\n--- Mode: replace-text ---");
            ReplaceTextWatermark(sampleFile,
                Path.Combine(OutputDir, "text-replaced.pdf"),
                "GroupDocs Confidential",
                "GroupDocs Internal Use Only");

            Console.WriteLine("\n--- Mode: remove ---");
            RemoveWatermarksByCriteria(draftFile,
                Path.Combine(OutputDir, "watermarks-removed.pdf"),
                "GroupDocs Draft");

            Console.WriteLine("\n--- Mode: track (add) ---");
            string trackedPath = Path.Combine(OutputDir, "tracked.pdf");
            AddTrackingWatermark(sampleFile, trackedPath, "RECIPIENT-12345");

            Console.WriteLine("\n--- Mode: track (detect) ---");
            DetectTrackingWatermark(trackedPath);

            Console.WriteLine("\nDone!");
        }

        /// <summary>
        /// Scans a document and lists every watermark found.
        /// </summary>
        /// <remarks>
        /// Uses parameterless Search() to enumerate all possible watermarks with their
        /// text, position, rotation, page number, and image data size.
        /// This is the starting point for any audit workflow.
        /// </remarks>
        public static void ScanAllWatermarks(string filePath)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                PossibleWatermarkCollection possibleWatermarks = watermarker.Search();

                Console.WriteLine($"Found {possibleWatermarks.Count} possible watermark(s) " +
                    $"in '{Path.GetFileName(filePath)}':\n");

                int index = 0;
                foreach (PossibleWatermark watermark in possibleWatermarks)
                {
                    Console.WriteLine($"  Watermark #{++index}");
                    Console.WriteLine($"    Text:        {watermark.Text ?? "(image)"}");
                    Console.WriteLine($"    Position:    X={watermark.X}, Y={watermark.Y}");
                    Console.WriteLine($"    Size:        {watermark.Width} x {watermark.Height}");
                    Console.WriteLine($"    Rotation:    {watermark.RotateAngle} degrees");
                    Console.WriteLine($"    Page:        {watermark.PageNumber}");
                    if (watermark.ImageData != null)
                    {
                        Console.WriteLine($"    Image data:  {watermark.ImageData.Length} bytes");
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that a document contains the expected text watermark.
        /// </summary>
        /// <remarks>
        /// Uses TextSearchCriteria with SkipUnreadableCharacters enabled to detect
        /// obfuscated watermarks. Returns true if at least one match is found.
        /// </remarks>
        public static bool VerifyTextWatermark(string filePath, string expectedText)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                TextSearchCriteria criteria = new TextSearchCriteria(expectedText);
                criteria.SkipUnreadableCharacters = true;

                PossibleWatermarkCollection found = watermarker.Search(criteria);
                bool passed = found.Count > 0;

                Console.WriteLine($"  [{(passed ? "PASS" : "FAIL")}] " +
                    $"expected '{expectedText}', found {found.Count} match(es)");

                return passed;
            }
        }

        /// <summary>
        /// Verifies that a document contains the expected logo watermark.
        /// </summary>
        /// <remarks>
        /// Uses ImageDctHashSearchCriteria with perceptual hashing. The MaxDifference
        /// property controls matching strictness (0 = exact, 1 = very lenient).
        /// </remarks>
        public static bool VerifyLogoWatermark(string filePath, string expectedLogoPath)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                ImageSearchCriteria criteria = new ImageDctHashSearchCriteria(expectedLogoPath);
                criteria.MaxDifference = 0.9;

                PossibleWatermarkCollection found = watermarker.Search(criteria);
                bool passed = found.Count > 0;

                Console.WriteLine($"  [{(passed ? "PASS" : "FAIL")}] " +
                    $"logo match: {found.Count} instance(s)");

                return passed;
            }
        }

        /// <summary>
        /// Runs a multi-rule watermark compliance report.
        /// </summary>
        /// <remarks>
        /// Checks whether the document meets a watermark policy: correct text present,
        /// expected font name, minimum font size, bold formatting, and coverage on every
        /// page. Prints a per-rule PASS/FAIL summary with a final verdict.
        /// </remarks>
        public static void RunComplianceReport(
            string filePath,
            string expectedText,
            string expectedFont,
            int expectedMinSize,
            bool expectedBold)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                int passed = 0, failed = 0;

                // Rule 1: expected text must be present
                var textCriteria = new TextSearchCriteria(expectedText);
                var textMatches = watermarker.Search(textCriteria);
                bool hasText = textMatches.Count > 0;
                Console.WriteLine(
                    $"  [{(hasText ? "PASS" : "FAIL")}] " +
                    $"Text '{expectedText}' present: {textMatches.Count} match(es)");
                if (hasText) passed++; else failed++;

                // Rule 2: text must use the expected font
                var fontCriteria = new TextFormattingSearchCriteria();
                fontCriteria.FontName = expectedFont;
                var fontMatches = watermarker.Search(textCriteria.And(fontCriteria));
                bool hasFont = fontMatches.Count > 0;
                Console.WriteLine(
                    $"  [{(hasFont ? "PASS" : "FAIL")}] " +
                    $"Font '{expectedFont}': {fontMatches.Count} match(es)");
                if (hasFont) passed++; else failed++;

                // Rule 3: minimum font size
                var sizeCriteria = new TextFormattingSearchCriteria();
                sizeCriteria.MinFontSize = expectedMinSize;
                var sizeMatches = watermarker.Search(textCriteria.And(sizeCriteria));
                bool hasSize = sizeMatches.Count > 0;
                Console.WriteLine(
                    $"  [{(hasSize ? "PASS" : "FAIL")}] " +
                    $"Min font size >= {expectedMinSize}: {sizeMatches.Count} match(es)");
                if (hasSize) passed++; else failed++;

                // Rule 4: bold formatting
                var boldCriteria = new TextFormattingSearchCriteria();
                boldCriteria.FontBold = expectedBold;
                var boldMatches = watermarker.Search(textCriteria.And(boldCriteria));
                bool hasBold = boldMatches.Count > 0;
                Console.WriteLine(
                    $"  [{(hasBold ? "PASS" : "FAIL")}] " +
                    $"Bold formatting: {boldMatches.Count} match(es)");
                if (hasBold) passed++; else failed++;

                // Rule 5: watermark on every page
                var allWatermarks = watermarker.Search(textCriteria);
                var pages = new HashSet<int>();
                foreach (PossibleWatermark wm in allWatermarks)
                    if (wm.PageNumber.HasValue)
                        pages.Add(wm.PageNumber.Value);

                var allItems = watermarker.Search();
                int maxPage = 0;
                foreach (PossibleWatermark wm in allItems)
                {
                    int pg = wm.PageNumber ?? 0;
                    if (pg > maxPage)
                        maxPage = pg;
                }
                int totalPages = Math.Max(maxPage, pages.Count);
                bool allPages = totalPages > 0 && pages.Count >= totalPages;
                Console.WriteLine(
                    $"  [{(allPages ? "PASS" : "FAIL")}] " +
                    $"Watermarked pages: {pages.Count}/{totalPages}");
                if (allPages) passed++; else failed++;

                string verdict = failed == 0 ? "COMPLIANT" : "NON-COMPLIANT";
                Console.WriteLine($"\n  Result: {verdict} ({passed} passed, {failed} failed)");
            }
        }

        /// <summary>
        /// Replaces outdated text watermark with new text and formatting.
        /// </summary>
        /// <remarks>
        /// Finds watermarks matching old text via TextSearchCriteria, then replaces
        /// FormattedTextFragments with the new text, font, and color. Useful for
        /// rebranding or policy updates.
        /// </remarks>
        public static void ReplaceTextWatermark(string filePath, string outputPath,
            string oldText, string newText)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                TextSearchCriteria criteria = new TextSearchCriteria(oldText, false);
                PossibleWatermarkCollection watermarks = watermarker.Search(criteria);

                Console.WriteLine($"  Found {watermarks.Count} watermark(s) with text '{oldText}'");

                foreach (PossibleWatermark watermark in watermarks)
                {
                    try
                    {
                        watermark.FormattedTextFragments.Clear();
                        watermark.FormattedTextFragments.Add(
                            newText,
                            new Font("Arial", 19, FontStyle.Bold),
                            Color.DarkBlue,
                            Color.Transparent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"    Warning: Could not edit entity: {ex.Message}");
                    }
                }

                watermarker.Save(outputPath);
                Console.WriteLine($"  Saved updated document to '{Path.GetFileName(outputPath)}'");
            }
        }

        /// <summary>
        /// Selectively removes watermarks by combined text and formatting criteria.
        /// </summary>
        /// <remarks>
        /// Combines TextSearchCriteria with TextFormattingSearchCriteria so only
        /// watermarks matching BOTH the text AND the formatting (font name, minimum
        /// size, bold) are removed. Targeted removal is safer than clearing all
        /// watermarks in the document.
        /// </remarks>
        public static void RemoveWatermarksByCriteria(string filePath, string outputPath,
            string watermarkText)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                TextSearchCriteria textCriteria = new TextSearchCriteria(watermarkText);

                TextFormattingSearchCriteria formatCriteria = new TextFormattingSearchCriteria();
                formatCriteria.FontName = "Arial";
                formatCriteria.MinFontSize = 15;
                formatCriteria.FontBold = true;

                SearchCriteria combinedCriteria = textCriteria.And(formatCriteria);
                PossibleWatermarkCollection watermarks = watermarker.Search(combinedCriteria);

                Console.WriteLine($"  Found {watermarks.Count} watermark(s) matching criteria");

                watermarks.Clear();

                watermarker.Save(outputPath);
                Console.WriteLine($"  Removed watermarks and saved to '{Path.GetFileName(outputPath)}'");
            }
        }

        /// <summary>
        /// Adds a hidden tracking watermark for leak detection.
        /// </summary>
        /// <remarks>
        /// Creates a nearly invisible text watermark containing a unique recipient
        /// identifier positioned in the bottom-right corner. When a document leaks,
        /// scan the leaked copy with DetectTrackingWatermark() to recover the tracking
        /// ID and identify the source.
        /// </remarks>
        public static void AddTrackingWatermark(string filePath, string outputPath, string recipientId)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                string trackingText = $"ID:{recipientId}";

                var watermark = new TextWatermark(trackingText, new Font("Arial", 6))
                {
                    ForegroundColor = Color.FromArgb(5, 200, 200, 200),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Opacity = 0.02
                };

                watermarker.Add(watermark);
                watermarker.Save(outputPath);

                Console.WriteLine($"  Tracking watermark added for recipient '{recipientId}'");
            }
        }

        /// <summary>
        /// Detects a tracking watermark in a leaked document.
        /// </summary>
        /// <remarks>
        /// Uses a regex TextSearchCriteria to find the unique tracking identifier
        /// embedded by AddTrackingWatermark(). Returns the recovered tracking ID
        /// or null if none is found.
        /// </remarks>
        public static string? DetectTrackingWatermark(string filePath)
        {
            using (Watermarker watermarker = new Watermarker(filePath))
            {
                Regex pattern = new Regex(@"ID:[\w-]+");
                TextSearchCriteria criteria = new TextSearchCriteria(pattern);

                PossibleWatermarkCollection found = watermarker.Search(criteria);

                if (found.Count > 0)
                {
                    string trackingId = found[0].Text;
                    Console.WriteLine($"  Tracking watermark found: {trackingId}");
                    return trackingId;
                }

                Console.WriteLine("  No tracking watermark detected");
                return null;
            }
        }
    }
}
