# Watermark Audit Tool for .NET

[![Product Page](https://img.shields.io/badge/Product%20Page-2865E0?style=for-the-badge&logo=appveyor&logoColor=white)](https://github.com/groupdocs-watermark/GroupDocs.Watermark-Docs) 
[![Docs](https://img.shields.io/badge/Docs-2865E0?style=for-the-badge&logo=Hugo&logoColor=white)](https://docs.groupdocs.com/watermark/net/) 
[![Blog](https://img.shields.io/badge/Blog-2865E0?style=for-the-badge&logo=WordPress&logoColor=white)](https://blog.groupdocs.com/categories/groupdocs.watermark-product-family/) 
[![Free Support](https://img.shields.io/badge/Free%20Support-2865E0?style=for-the-badge&logo=Discourse&logoColor=white)](https://forum.groupdocs.com/c/watermark/19) 
[![Temporary License](https://img.shields.io/badge/Temporary%20License-2865E0?style=for-the-badge&logo=rocket&logoColor=white)](https://purchase.groupdocs.com/temp-license/100354)

## 📖 About This Repository

This repository demonstrates watermark auditing and compliance reporting using GroupDocs.Watermark for .NET. It provides a set of utilities to scan, verify, replace, and track watermarks that help ensure document integrity and policy adherence. The examples are designed for developers and compliance engineers who need to audit documents for required watermarks and enforce branding policies.

## The Challenge

Many organizations rely on watermarks to protect intellectual property, enforce branding, and trace document distribution. However, developers often struggle to **detect** every watermark variant—text, image, rotated, or hidden—across diverse file formats and page layouts. When a watermark policy changes, locating and updating existing marks can become a time‑consuming manual process, increasing the risk of non‑compliant releases.

Typical challenges include:
* **Inconsistent watermark formats** – text may be split, rotated, or rendered as an image.
* **Large document collections** – scanning thousands of files requires an automated, performant approach.
* **Policy enforcement** – verifying that every page contains the correct watermark, font, size, and style.

GroupDocs.Watermark for .NET addresses these problems by offering a comprehensive API that can **search**, **edit**, **replace**, and **remove** watermarks with fine‑grained criteria. The library abstracts the complexity of different file formats, allowing developers to focus on compliance logic rather than low‑level document handling.

Key capabilities demonstrated in this repository are:
* Search for any watermark (text or image) with optional filtering such as font name, size, or rotation.
* Validate watermark presence and formatting against a predefined policy.
* Replace outdated watermarks with new text, fonts, and colors in bulk.
* Add invisible tracking watermarks for leak detection and later recovery.
* Generate a detailed compliance report summarising PASS/FAIL results for each rule.

**What is GroupDocs.Watermark?**

GroupDocs.Watermark is a powerful API for adding, searching, editing, and removing watermarks in over 100 document formats. Key features include:

- Add text, image, and PDF watermarks
- Search for existing watermarks with flexible criteria
- Edit watermark properties such as text, font, color, and position
- Remove watermarks selectively or entirely
- Generate compliance reports to validate watermark policies

GroupDocs.Watermark also supports advanced image‑hash matching for logo detection, OCR‑based text extraction, and batch processing for large document sets, making it ideal for enterprise‑grade compliance workflows.

## Prerequisites

- **Runtime** – .NET 6.0 or later
- **Library** – GroupDocs.Watermark for .NET (NuGet package `GroupDocs.Watermark`
- **License** – Temporary license key (optional for evaluation)

## Repository Structure

```
watermark-audit-tool/
│
├── WatermarkAuditTool/
│   ├── Program.cs
│   ├── WatermarkAuditTool.csproj
│   ├── Resources/
│   │   ├── Logos/
│   │   │   └── expected-logo.png
│   │   └── SampleDocs/
│   │       ├── sample-draft.pdf
│   │       └── sample-watermarked.pdf
│   └── Results/
│       ├── text-replaced.pdf
│       ├── tracked.pdf
│       └── watermarks-removed.pdf
```

- **Program.cs** — Entry point with all audit methods: scan, verify, replace, remove, track, and compliance report
- **WatermarkAuditTool.csproj** — Project file targeting .NET 6.0 with GroupDocs.Watermark dependency
- **Resources/Logos/expected-logo.png** — Reference logo image for perceptual hash verification
- **Resources/SampleDocs/** — Pre-built PDF files used as input for all audit operations
- **Results/** — Output directory for processed documents (replaced, tracked, cleaned)

## Code Examples

### Scans a document and lists every watermark found.

The example enumerates all possible watermarks using the parameterless `Search()` method, printing details such as text, position, size, rotation, page number, and image data length. This forms the basis of any audit workflow.

```csharp
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
```

This snippet shows how to retrieve a complete watermark catalogue, which is essential for auditors who need to verify every embedded mark.

### Verifies that a document contains the expected text watermark.

The method builds a `TextSearchCriteria` with `SkipUnreadableCharacters` enabled, allowing detection of obfuscated or fragmented watermarks. It returns `true` when at least one matching watermark is found.

```csharp
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
```

The code demonstrates a reliable way to confirm that mandated text watermarks are present, even when they have been intentionally disguised.

### Verifies that a document contains the expected logo watermark.

Using `ImageDctHashSearchCriteria`, this snippet performs perceptual hashing on the expected logo image. The `MaxDifference` property controls how strict the match is, enabling tolerant detection of resized or slightly altered logos.

```csharp
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
```

This example is useful for verifying the presence of corporate logos or brand images as part of a compliance check.

### Runs a multi‑rule watermark compliance report.

The routine evaluates five independent rules: required text, font name, minimum font size, bold formatting, and coverage on every page. It prints a PASS/FAIL line for each rule and a final verdict indicating overall compliance.

First, open the document and check the four formatting rules — text presence, font, size, and bold style. Each rule combines `TextSearchCriteria` with a `TextFormattingSearchCriteria` using the `.And()` operator:

```csharp
using (Watermarker watermarker = new Watermarker(filePath))
{
    int passed = 0, failed = 0;

    var textCriteria = new TextSearchCriteria(expectedText);

    // Rule 1: expected text must be present
    CheckRule(watermarker, textCriteria,
        $"Text '{expectedText}' present", ref passed, ref failed);

    // Rule 2: correct font
    var fontCriteria = new TextFormattingSearchCriteria { FontName = expectedFont };
    CheckRule(watermarker, textCriteria.And(fontCriteria),
        $"Font '{expectedFont}'", ref passed, ref failed);

    // Rule 3: minimum font size
    var sizeCriteria = new TextFormattingSearchCriteria { MinFontSize = expectedMinSize };
    CheckRule(watermarker, textCriteria.And(sizeCriteria),
        $"Min font size >= {expectedMinSize}", ref passed, ref failed);

    // Rule 4: bold formatting
    var boldCriteria = new TextFormattingSearchCriteria { FontBold = expectedBold };
    CheckRule(watermarker, textCriteria.And(boldCriteria),
        "Bold formatting", ref passed, ref failed);
```

Next, verify that the watermark appears on every page and print the final verdict:

```csharp
    // Rule 5: watermark on every page
    CheckPageCoverage(watermarker, textCriteria, ref passed, ref failed);

    string verdict = failed == 0 ? "COMPLIANT" : "NON-COMPLIANT";
    Console.WriteLine(
        $"\n  Result: {verdict} ({passed} passed, {failed} failed)");

```

The `CheckRule` helper searches with the given criteria and prints a PASS/FAIL line. `CheckPageCoverage` collects watermarked page numbers and compares them against the total page count. Together they produce a concise audit trail suitable for CI pipelines or compliance dashboards.

### Replaces outdated text watermark with new text and formatting.

This routine locates watermarks that match an old text string, clears their existing formatting, and injects new text with a specified font, size, and color. It is ideal for re‑branding or policy updates.

```csharp
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
```

By updating the text and style in‑place, the sample shows how to keep document branding consistent without recreating the whole file.

![Text replacement result — the old watermark text is replaced with new text, font, and color](text-replaced-example.png)

### Selectively removes watermarks by combined text and formatting criteria.

The example combines a `TextSearchCriteria` with a `TextFormattingSearchCriteria` so that only watermarks matching both the specified text **and** the required font, size, and boldness are removed. This targeted approach minimizes the risk of unintentionally deleting legitimate marks.

```csharp
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
```

The code demonstrates fine‑grained control, ensuring that only non‑compliant or outdated marks are stripped away.

![Watermark removal result — targeted watermarks are removed while other content remains intact](watermarks-removed-example.png)

### Adds a hidden tracking watermark for leak detection.

A barely visible text watermark containing a unique identifier is placed in the document’s bottom‑right corner. If the document is later leaked, the identifier can be extracted to pinpoint the source.

```csharp
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
```

This snippet is useful for document distribution tracking without affecting the visual appearance.

### Detects a tracking watermark in a leaked document.

The method builds a regular‑expression based `TextSearchCriteria` to locate the tracking identifier added by the previous example. It returns the recovered ID or `null` when no tracking watermark is present.

```csharp
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
```

The example shows a practical way to trace document leaks back to a specific recipient using the hidden identifier.

## Related Topics to Explore

If you're working with watermark auditing and compliance, the following articles may be helpful:

* **Step‑by‑Step Guide to Find and Remove Watermarks from Documents Using C#** – Learn techniques to locate and programmatically delete unwanted watermarks in a variety of file formats: [Read the article →](https://blog.groupdocs.com/watermark/find-and-remove-watermarks-from-documents-in-csharp/)

* **How to Skip Unreadable Characters in Watermark Searches with GroupDocs.Watermark for .NET 18.8** – Discover how enabling `SkipUnreadableCharacters` improves detection of obfuscated watermarks: [Read the article →](https://blog.groupdocs.com/watermark/skip-unreadable-characters-during-watermark-search-using-groupdocs.watermark-for-.net-18.8/)

* **Edit Watermark Text and Images with GroupDocs.Watermark for Java 18.3** – Although Java‑focused, this post outlines API patterns that translate to .NET for editing watermark content: [Read the article →](https://blog.groupdocs.com/watermark/edit-watermark-text-and-image-using-groupdocs.watermark-for-java-18.3/)

## Keywords

`GroupDocs.Watermark`, `.NET`, `watermark audit`, `document compliance`, `text watermark`, `image watermark`, `logo detection`, `tracking watermark`, `watermark replacement`, `watermark removal`, `PDF watermark`, `DOCX watermark`, `office documents`, `branding`, `security`, `metadata protection`, `API`, `C#`
