# QuestPDF Patterns and Best Practices

**Last Updated**: December 1, 2025

This document contains patterns and examples for creating professional PDF reports in SMU-3.0 using QuestPDF.

---

## Table of Contents

1. [Basic Document Structure](#basic-document-structure)
2. [Tables with Headers and Footers](#tables-with-headers-and-footers)
3. [Romanian Language Support (Fonts with Diacritics)](#romanian-language-support)
4. [Adding Images and Logos](#adding-images-and-logos)
5. [Complete Example: Student Grade Report](#complete-example-student-grade-report)
6. [Best Practices](#best-practices)

---

## Basic Document Structure

### Simple Document with Header, Content, and Footer

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

Document.Create(document =>
{
    document.Page(page =>
    {
        // Page settings
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(12));

        // Header
        page.Header()
            .Text("Student Grade Report")
            .FontSize(24).Bold().FontColor(Colors.Blue.Medium);

        // Content
        page.Content()
            .PaddingVertical(1, Unit.Centimetre)
            .Column(column =>
            {
                column.Spacing(20);
                column.Item().Text("Student Name: John Doe");
                column.Item().Text("Faculty: Computer Science");
                // Add more content here
            });

        // Footer with page numbers
        page.Footer()
            .AlignCenter()
            .Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
    });
})
.GeneratePdf("report.pdf");
```

### Document with Custom Page Sizes and Margins

```csharp
Document.Create(document =>
{
    // Portrait A4 page
    document.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(1, Unit.Inch);
        page.MarginVertical(32);
        page.MarginHorizontal(2, Unit.Centimeter);
        page.Content().AlignCenter().AlignMiddle().Text("A4 PORTRAIT");
    });

    // Landscape A5 page
    document.Page(page =>
    {
        page.Size(PageSizes.A5.Landscape());
        page.Margin(2f, Unit.Centimeter);
        page.Content().AlignCenter().AlignMiddle().Text("A5 LANDSCAPE");
    });
})
.GeneratePdf("multi-page.pdf");
```

### Using IDocument Interface for Structured Documents

```csharp
public class StudentReportDocument : IDocument
{
    public StudentReportModel Model { get; }

    public StudentReportDocument(StudentReportModel model)
    {
        Model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item()
                    .Text($"Student Report #{Model.StudentId}")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                column.Item().Text(text =>
                {
                    text.Span("Generated: ").SemiBold();
                    text.Span($"{DateTime.Now:d}");
                });
            });

            // Logo placeholder (right side)
            row.ConstantItem(100).Height(50).AlignRight().Text("LOGO");
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);
            column.Item().Text($"Student: {Model.StudentName}").FontSize(16);
            column.Item().Text($"Faculty: {Model.FacultyName}");
            // Add more content sections
        });
    }
}
```

---

## Tables with Headers and Footers

### Professional Table with Repeating Headers

```csharp
container
    .Padding(10)
    .MinimalBox()
    .Border(1)
    .Table(table =>
    {
        // Reusable cell style helper
        IContainer DefaultCellStyle(IContainer container, string backgroundColor)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(backgroundColor)
                .PaddingVertical(5)
                .PaddingHorizontal(10)
                .AlignCenter()
                .AlignMiddle();
        }

        // Define column widths
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);      // Course name (wider)
            columns.ConstantColumn(80);     // Credits
            columns.ConstantColumn(80);     // Grade
            columns.ConstantColumn(100);    // Professor
            columns.ConstantColumn(120);    // Date
        });

        // Header (repeats on each page)
        table.Header(header =>
        {
            header.Cell().Element(CellStyle).Text("Course Name");
            header.Cell().Element(CellStyle).Text("Credits");
            header.Cell().Element(CellStyle).Text("Grade");
            header.Cell().Element(CellStyle).Text("Professor");
            header.Cell().Element(CellStyle).Text("Date");

            IContainer CellStyle(IContainer container) =>
                DefaultCellStyle(container, Colors.Grey.Lighten3);
        });

        // Data rows
        foreach (var grade in grades)
        {
            table.Cell().Element(CellStyle).ExtendHorizontal().AlignLeft().Text(grade.CourseName);
            table.Cell().Element(CellStyle).Text(grade.Credits.ToString());
            table.Cell().Element(CellStyle).Text(grade.GradeValue.ToString());
            table.Cell().Element(CellStyle).Text(grade.ProfessorName);
            table.Cell().Element(CellStyle).Text(grade.Date.ToString("dd/MM/yyyy"));

            IContainer CellStyle(IContainer container) =>
                DefaultCellStyle(container, Colors.White).ShowOnce();
        }
    });
```

### Table with Header Spanning Multiple Rows/Columns

```csharp
table.Header(header =>
{
    // Row 1: Main header spanning 2 rows and 5 columns
    header.Cell().RowSpan(2).Element(CellStyle)
        .ExtendHorizontal().AlignLeft().Text("Academic Year 2024/2025");

    // Semester 1 header spanning 2 columns
    header.Cell().ColumnSpan(2).Element(CellStyle).Text("Semester 1");

    // Semester 2 header spanning 2 columns
    header.Cell().ColumnSpan(2).Element(CellStyle).Text("Semester 2");

    // Row 2: Sub-headers
    header.Cell().Element(CellStyle).Text("Grade");
    header.Cell().Element(CellStyle).Text("Credits");
    header.Cell().Element(CellStyle).Text("Grade");
    header.Cell().Element(CellStyle).Text("Credits");

    IContainer CellStyle(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(5)
            .PaddingHorizontal(10)
            .AlignCenter()
            .AlignMiddle();
});
```

---

## Romanian Language Support

### Configuring Fonts for Romanian Diacritics (ă, â, î, ș, ț)

QuestPDF supports Unicode and Romanian characters through proper font configuration.

#### Method 1: Using System Fonts with Fallback

```csharp
// Use font that supports Romanian characters
container
    .Text("Cursul de Programare și Baze de Date")
    .FontFamily("Arial"); // Arial supports Romanian diacritics

// With font fallback for special characters
container
    .Text("Facultatea de Științe și Informatică")
    .FontFamily("Lato", "Arial", "Noto Sans");
```

#### Method 2: Registering Custom Fonts

```csharp
// Register a custom font that supports Romanian
public class DocumentConfiguration
{
    public static void ConfigureFonts()
    {
        // Register font from file
        FontManager.RegisterFont(File.OpenRead("fonts/Roboto-Regular.ttf"));
        FontManager.RegisterFont(File.OpenRead("fonts/Roboto-Bold.ttf"));

        // Or from embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SMU.Fonts.Roboto-Regular.ttf");
        FontManager.RegisterFont(stream);
    }
}

// Usage in document
Document.Create(document =>
{
    document.Page(page =>
    {
        page.DefaultTextStyle(x => x.FontFamily("Roboto"));
        page.Content().Text("Universitatea Tehnică București");
    });
});
```

#### Method 3: Enabling Glyph Checking (Development)

```csharp
// Enable glyph availability checking during development
QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;

// This will throw an exception if Romanian characters are not supported by the font
Document.Create(document =>
{
    document.Page(page =>
    {
        page.Content().Text("Științe și Tehnologie"); // Will fail if font doesn't support ș, ț
    });
});
```

### Complete Example with Romanian Text

```csharp
public class RomanianReportDocument : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);

            // Use Arial or another font supporting Romanian
            page.DefaultTextStyle(x => x
                .FontFamily("Arial")
                .FontSize(12));

            page.Header()
                .Text("Situația Școlară")
                .FontSize(24).Bold();

            page.Content().Column(column =>
            {
                column.Spacing(10);
                column.Item().Text("Student: Popescu Ion");
                column.Item().Text("Facultatea: Științe și Informatică");
                column.Item().Text("Program: Tehnologii Informaționale");
                column.Item().Text("Anul: 2024/2025");

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Disciplină");
                        header.Cell().Text("Notă");
                        header.Cell().Text("Credite");
                    });

                    table.Cell().Text("Programare și Algoritmi");
                    table.Cell().Text("9");
                    table.Cell().Text("6");

                    table.Cell().Text("Baze de Date");
                    table.Cell().Text("10");
                    table.Cell().Text("5");
                });
            });

            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" din ");
                    text.TotalPages();
                });
        });
    }
}
```

---

## Adding Images and Logos

### Method 1: Load Image from File

```csharp
// Simple image embedding
container.Image("path/to/university-logo.png");

// With sizing and positioning
container
    .Width(150)
    .Height(75)
    .AlignRight()
    .Image("wwwroot/images/logo.png");
```

### Method 2: Load Image from Byte Array

```csharp
byte[] logoData = File.ReadAllBytes("wwwroot/images/logo.png");
container.Image(logoData);
```

### Method 3: Load Image from Stream

```csharp
using var stream = new FileStream("logo.png", FileMode.Open);
container.Image(stream);
```

### Method 4: Load SVG Logo

```csharp
// From file
container.Svg("wwwroot/images/university-logo.svg");

// From string content
var svgContent = File.ReadAllText("university-logo.svg");
container.Svg(svgContent);

// With sizing
container
    .Width(200)
    .AspectRatio(2.0f) // Width:Height ratio
    .Svg("logo.svg");
```

### Adding Logo to Header

```csharp
void ComposeHeader(IContainer container)
{
    container.Row(row =>
    {
        // Left side: Text information
        row.RelativeItem().Column(column =>
        {
            column.Item()
                .Text("Universitatea Tehnică București")
                .FontSize(18).SemiBold();

            column.Item()
                .Text("Facultatea de Științe și Informatică")
                .FontSize(12);
        });

        // Right side: Logo
        row.ConstantItem(120)
            .Height(60)
            .AlignRight()
            .Image("wwwroot/images/university-logo.png");
    });
}
```

### Dynamic Image Generation (Charts, QR Codes)

```csharp
byte[] GenerateQRCode(Size size)
{
    // Use a QR code library like QRCoder
    using var qrGenerator = new QRCodeGenerator();
    using var qrCodeData = qrGenerator.CreateQrCode(studentData, QRCodeGenerator.ECCLevel.Q);
    using var qrCode = new QRCode(qrCodeData);
    using var bitmap = qrCode.GetGraphic(20);

    // Convert to byte array
    using var ms = new MemoryStream();
    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    return ms.ToArray();
}

// Usage
container
    .Width(150)
    .Height(150)
    .Image(GenerateQRCode);
```

---

## Complete Example: Student Grade Report

This example combines all patterns: Romanian text, tables, images, and professional layout.

```csharp
public class StudentGradeReportDocument : IDocument
{
    public StudentGradeReportModel Model { get; }

    public StudentGradeReportDocument(StudentGradeReportModel model)
    {
        Model = model;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"Situație Școlară - {Model.StudentName}",
        Author = "SMU - University Management System",
        Subject = "Academic Transcript",
        Keywords = "grades, transcript, university",
        Creator = "QuestPDF",
        CreationDate = DateTime.Now
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x
                .FontFamily("Arial")
                .FontSize(11));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            // Top section with logo
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("UNIVERSITATEA TEHNICĂ BUCUREȘTI")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Facultatea de Automatică și Calculatoare")
                        .FontSize(12);
                });

                row.ConstantItem(100)
                    .Height(50)
                    .AlignRight()
                    .Image("wwwroot/images/university-logo.png");
            });

            // Separator line
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Document title
            column.Item().PaddingTop(10).AlignCenter()
                .Text("SITUAȚIE ȘCOLARĂ")
                .FontSize(20).Bold();

            // Student info section
            column.Item().PaddingTop(15).Row(row =>
            {
                row.Spacing(20);

                row.RelativeItem().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Student:").Bold();
                        r.RelativeItem().Text(Model.StudentName);
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Număr Matricol:").Bold();
                        r.RelativeItem().Text(Model.StudentId);
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Program Studiu:").Bold();
                        r.RelativeItem().Text(Model.ProgramName);
                    });
                });

                row.RelativeItem().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Anul Universitar:").Bold();
                        r.RelativeItem().Text(Model.AcademicYear);
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("An Studiu:").Bold();
                        r.RelativeItem().Text(Model.StudyYear.ToString());
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(120).Text("Data Generării:").Bold();
                        r.RelativeItem().Text(DateTime.Now.ToString("dd.MM.yyyy"));
                    });
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingTop(20).Column(column =>
        {
            // Grades table
            column.Item().Element(ComposeGradesTable);

            // Summary section
            column.Item().PaddingTop(20).Element(ComposeSummary);
        });
    }

    void ComposeGradesTable(IContainer container)
    {
        container.Table(table =>
        {
            IContainer CellStyle(IContainer cell, string backgroundColor, bool isHeader = false)
            {
                return cell
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(backgroundColor)
                    .Padding(8)
                    .AlignMiddle()
                    .DefaultTextStyle(x => isHeader ? x.Bold() : x);
            }

            // Column definitions
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);      // Nr. crt.
                columns.RelativeColumn(4);       // Course name
                columns.ConstantColumn(60);      // Semester
                columns.ConstantColumn(70);      // Credits
                columns.ConstantColumn(70);      // Grade
                columns.RelativeColumn(2);       // Professor
                columns.ConstantColumn(90);      // Date
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Nr.");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Disciplină");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Sem.");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Credite");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Notă");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Profesor");
                header.Cell().Element(c => CellStyle(c, Colors.Blue.Lighten3, true))
                    .AlignCenter().Text("Data");
            });

            // Data rows
            int index = 1;
            foreach (var grade in Model.Grades)
            {
                var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignCenter().Text(index.ToString());
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignLeft().Text(grade.CourseName);
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignCenter().Text(grade.Semester.ToString());
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignCenter().Text(grade.Credits.ToString());
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignCenter().Text(grade.GradeValue.ToString())
                    .Bold().FontColor(grade.GradeValue >= 5 ? Colors.Green.Darken2 : Colors.Red.Medium);
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignLeft().Text(grade.ProfessorName);
                table.Cell().Element(c => CellStyle(c, bgColor))
                    .AlignCenter().Text(grade.GradeDate.ToString("dd.MM.yyyy"));

                index++;
            }
        });
    }

    void ComposeSummary(IContainer container)
    {
        container.Background(Colors.Grey.Lighten4)
            .Padding(15)
            .Column(column =>
            {
                column.Spacing(8);

                column.Item().Text("Rezumat").FontSize(14).Bold();

                column.Item().Row(row =>
                {
                    row.ConstantItem(200).Text("Total Credite:").Bold();
                    row.RelativeItem().Text(Model.TotalCredits.ToString());
                });

                column.Item().Row(row =>
                {
                    row.ConstantItem(200).Text("Medie Generală:").Bold();
                    row.RelativeItem().Text($"{Model.GPA:F2}");
                });

                column.Item().Row(row =>
                {
                    row.ConstantItem(200).Text("Discipline Promovate:").Bold();
                    row.RelativeItem().Text($"{Model.PassedCourses} / {Model.TotalCourses}");
                });
            });
    }

    void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem()
                    .Text("Document generat automat de SMU")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" din ");
                        text.TotalPages();
                    })
                    .FontSize(9);

                row.RelativeItem()
                    .AlignRight()
                    .Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }
}

// Model classes
public class StudentGradeReportModel
{
    public string StudentId { get; set; }
    public string StudentName { get; set; }
    public string ProgramName { get; set; }
    public string AcademicYear { get; set; }
    public int StudyYear { get; set; }
    public List<GradeInfo> Grades { get; set; }
    public int TotalCredits { get; set; }
    public decimal GPA { get; set; }
    public int PassedCourses { get; set; }
    public int TotalCourses { get; set; }
}

public class GradeInfo
{
    public string CourseName { get; set; }
    public int Semester { get; set; }
    public int Credits { get; set; }
    public int GradeValue { get; set; }
    public string ProfessorName { get; set; }
    public DateTime GradeDate { get; set; }
}
```

---

## Best Practices

### 1. Font Management

- **Always use fonts that support Romanian characters** (Arial, Times New Roman, Roboto)
- **Enable glyph checking during development** to catch missing character support early
- **Use font fallback** for special characters or emojis
- **Register custom fonts** at application startup if using embedded fonts

```csharp
// Application startup
public static void ConfigurePDF()
{
    // Enable glyph checking in development
    #if DEBUG
    QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;
    #endif

    // Register custom fonts
    FontManager.RegisterFont(File.OpenRead("fonts/Roboto-Regular.ttf"));
    FontManager.RegisterFont(File.OpenRead("fonts/Roboto-Bold.ttf"));
}
```

### 2. Performance Optimization

- **Reuse image instances** when the same image appears multiple times
- **Use appropriate image formats**: PNG for logos, JPEG for photos
- **Optimize image resolution** before embedding (300 DPI for print, 72-96 DPI for screen)
- **Use shared images** for logos and watermarks

```csharp
// Shared image example
var logo = Image.FromFile("logo.png");
container.Image(logo); // Reused multiple times without reloading
```

### 3. Table Design

- **Use consistent cell styling** with helper methods
- **Define explicit column widths** for predictable layouts
- **Enable ShowOnce()** for data cells to prevent duplication across pages
- **Use repeating headers** for multi-page tables

### 4. Document Structure

- **Implement IDocument interface** for complex reports
- **Separate concerns** with methods like ComposeHeader, ComposeContent, ComposeFooter
- **Use local helper functions** for reusable styling patterns
- **Set document metadata** for better PDF organization

### 5. Error Handling

```csharp
try
{
    var document = new StudentGradeReportDocument(model);
    var pdfBytes = document.GeneratePdf();
    return File(pdfBytes, "application/pdf", "student-report.pdf");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to generate PDF report for student {StudentId}", studentId);
    throw;
}
```

### 6. Testing

- **Use document previewer** during development
- **Test with real Romanian text** including all diacritics: ă, â, î, ș, ț, Ă, Â, Î, Ș, Ț
- **Verify multi-page behavior** with repeating headers/footers
- **Check print output** if PDFs are intended for printing

```csharp
#if DEBUG
    .GeneratePdfAndShow(); // Opens preview window
#else
    .GeneratePdf("output.pdf");
#endif
```

---

## References

- [QuestPDF Official Documentation](https://www.questpdf.com/)
- [QuestPDF GitHub Repository](https://github.com/QuestPDF/QuestPDF)
- [API Reference](https://www.questpdf.com/api-reference/)
- [Design Patterns](https://www.questpdf.com/design-patterns/)
