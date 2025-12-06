using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Service for generating PDF documents for student document requests
/// </summary>
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private const string UniversityName = "Universitatea Româno-Americană";
    private const string UniversityAddress = "Bd. Expoziției nr. 1B, București, România";
    private const string UniversityPhone = "Tel: +40 21 200 1000";
    private const string UniversityEmail = "secretariat@rau.ro";

    public DocumentGenerationService(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateStudentCertificateAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student cu ID {studentId} nu a fost găsit.");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName)
                        .FontSize(16).Bold();
                    column.Item().AlignCenter().Text(UniversityAddress)
                        .FontSize(10);
                    column.Item().AlignCenter().Text(UniversityPhone)
                        .FontSize(9);
                    column.Item().AlignCenter().Text(UniversityEmail)
                        .FontSize(9).Italic();
                    column.Item().PaddingTop(15).LineHorizontal(2);
                });

                // Content
                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Item().AlignCenter().PaddingBottom(30).Text("ADEVERINȚĂ")
                        .FontSize(18).Bold();

                    column.Item().PaddingBottom(20).Text(text =>
                    {
                        text.Span("Prin prezenta se adeverește că domnul/doamna ");
                        text.Span($"{student.User.LastName} {student.User.FirstName}").Bold();
                        text.Span(", este student(ă) în anul universitar ");
                        text.Span($"{DateTime.Now.Year}-{DateTime.Now.Year + 1}").Bold();
                        text.Span(".");
                    });

                    column.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Număr Matricol:").Bold();
                        table.Cell().Text(student.StudentNumber);

                        table.Cell().Text("Facultatea:").Bold();
                        table.Cell().Text(student.Group?.Program.Faculty.Name ?? "N/A");

                        table.Cell().Text("Programul de studiu:").Bold();
                        table.Cell().Text(student.Group?.Program.Name ?? "N/A");

                        table.Cell().Text("Forma de învățământ:").Bold();
                        table.Cell().Text(student.Group?.Program.Type switch
                        {
                            ProgramType.Bachelor => "Licență",
                            ProgramType.Master => "Master",
                            ProgramType.PhD => "Doctorat",
                            _ => "N/A"
                        });

                        table.Cell().Text("Anul de studiu:").Bold();
                        table.Cell().Text($"{student.Group?.Year ?? 0}");

                        table.Cell().Text("Grupa:").Bold();
                        table.Cell().Text(student.Group?.Name ?? "N/A");

                        table.Cell().Text("Status:").Bold();
                        table.Cell().Text(student.Status switch
                        {
                            StudentStatus.Active => "Activ",
                            StudentStatus.Inactive => "Inactiv",
                            StudentStatus.Graduated => "Absolvent",
                            StudentStatus.Suspended => "Suspendat",
                            StudentStatus.Expelled => "Exmatriculat",
                            _ => "Necunoscut"
                        });
                    });

                    column.Item().PaddingTop(20).Text(
                        "Adeverința se eliberează la cererea celui interesat pentru a-i servi la nevoie.");

                    column.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Data emiterii:");
                            col.Item().Text($"{DateTime.Now:dd.MM.yyyy}").Bold();
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("SECRETARIAT");
                            col.Item().PaddingTop(30).AlignRight().Text("_________________");
                            col.Item().AlignRight().Text("(semnătură și ștampilă)").FontSize(9);
                        });
                    });
                });

                // Footer
                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).Italic()).Text(text =>
                {
                    text.Span("Document generat electronic la ");
                    text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateGradeReportAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .Include(s => s.Grades.Where(g => g.Status == GradeStatus.Approved))
                .ThenInclude(g => g.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student cu ID {studentId} nu a fost găsit.");

        var approvedGrades = student.Grades.OrderBy(g => g.CreatedAt).ToList();
        var averageGrade = approvedGrades.Any() ? approvedGrades.Average(g => (double)g.Value) : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName)
                        .FontSize(14).Bold();
                    column.Item().AlignCenter().Text(UniversityAddress)
                        .FontSize(9);
                    column.Item().PaddingTop(10).AlignCenter().Text("ADEVERINȚĂ CU NOTE")
                        .FontSize(14).Bold();
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                // Content
                page.Content().PaddingVertical(15).Column(column =>
                {
                    // Student information
                    column.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Nume Student:").Bold();
                        table.Cell().Text($"{student.User.LastName} {student.User.FirstName}");

                        table.Cell().Text("Nr. Matricol:").Bold();
                        table.Cell().Text(student.StudentNumber);

                        table.Cell().Text("Facultate:").Bold();
                        table.Cell().Text(student.Group?.Program.Faculty.Name ?? "N/A");

                        table.Cell().Text("Program:").Bold();
                        table.Cell().Text(student.Group?.Program.Name ?? "N/A");

                        table.Cell().Text("Anul:").Bold();
                        table.Cell().Text($"{student.Group?.Year ?? 0}");

                        table.Cell().Text("Grupa:").Bold();
                        table.Cell().Text(student.Group?.Name ?? "N/A");
                    });

                    column.Item().PaddingTop(10).Text("SITUAȚIA NOTELOR").FontSize(12).Bold();

                    // Grades table
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Nr.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Disciplina").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tip Evaluare").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Nota").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Data").Bold();
                        });

                        // Rows
                        int index = 1;
                        foreach (var grade in approvedGrades)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{index++}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(grade.Course.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(grade.Type switch
                            {
                                GradeType.Exam => "Examen",
                                GradeType.Project => "Proiect",
                                GradeType.Lab => "Laborator",
                                GradeType.Seminar => "Seminar",
                                GradeType.Final => "Finală",
                                _ => grade.Type.ToString()
                            });
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{grade.Value:F2}").Bold();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{grade.CreatedAt:dd.MM.yyyy}");
                        }
                    });

                    // Summary
                    column.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).Column(col =>
                        {
                            col.Item().BorderTop(2).PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem().Text("Media generală:").Bold();
                                r.ConstantItem(60).AlignRight().Text($"{averageGrade:F2}").Bold().FontSize(12);
                            });
                        });
                    });

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).Column(col =>
                        {
                            col.Item().AlignRight().Text("SECRETARIAT");
                            col.Item().PaddingTop(20).AlignRight().Text("_________________");
                            col.Item().AlignRight().Text("(semnătură și ștampilă)").FontSize(8);
                        });
                    });
                });

                // Footer
                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).Italic()).Text(text =>
                {
                    text.Span("Document generat electronic la ");
                    text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateEnrollmentProofAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .Include(s => s.Grades.Where(g => g.Status == GradeStatus.Approved))
                .ThenInclude(g => g.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student cu ID {studentId} nu a fost găsit.");

        var approvedGrades = student.Grades.OrderBy(g => g.CreatedAt).ToList();
        var totalCredits = approvedGrades.Sum(g => g.Course.Credits);
        var averageGrade = approvedGrades.Any() ? approvedGrades.Average(g => (double)g.Value) : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName)
                        .FontSize(14).Bold();
                    column.Item().AlignCenter().Text(UniversityAddress)
                        .FontSize(9);
                    column.Item().PaddingTop(10).AlignCenter().Text("FOAIE MATRICOLĂ")
                        .FontSize(14).Bold();
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                // Content
                page.Content().PaddingVertical(15).Column(column =>
                {
                    // Student information
                    column.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Nume și Prenume:").Bold();
                        table.Cell().Text($"{student.User.LastName} {student.User.FirstName}");

                        table.Cell().Text("Nr. Matricol:").Bold();
                        table.Cell().Text(student.StudentNumber);

                        table.Cell().Text("Email:").Bold();
                        table.Cell().Text(student.User.Email ?? "N/A");

                        table.Cell().Text("Telefon:").Bold();
                        table.Cell().Text(student.User.PhoneNumber ?? "N/A");

                        table.Cell().Text("Facultate:").Bold();
                        table.Cell().Text(student.Group?.Program.Faculty.Name ?? "N/A");

                        table.Cell().Text("Program de studiu:").Bold();
                        table.Cell().Text(student.Group?.Program.Name ?? "N/A");

                        table.Cell().Text("Forma de învățământ:").Bold();
                        table.Cell().Text(student.Group?.Program.Type switch
                        {
                            ProgramType.Bachelor => "Licență",
                            ProgramType.Master => "Master",
                            ProgramType.PhD => "Doctorat",
                            _ => "N/A"
                        });

                        table.Cell().Text("Anul curent:").Bold();
                        table.Cell().Text($"{student.Group?.Year ?? 0}");

                        table.Cell().Text("Grupa:").Bold();
                        table.Cell().Text(student.Group?.Name ?? "N/A");

                        table.Cell().Text("Status:").Bold();
                        table.Cell().Text(student.Status switch
                        {
                            StudentStatus.Active => "Activ",
                            StudentStatus.Inactive => "Inactiv",
                            StudentStatus.Graduated => "Absolvent",
                            StudentStatus.Suspended => "Suspendat",
                            StudentStatus.Expelled => "Exmatriculat",
                            _ => "Necunoscut"
                        });
                    });

                    column.Item().PaddingTop(15).Text("SITUAȚIA ACADEMICĂ").FontSize(12).Bold();

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Total credite acumulate:").Bold();
                        table.Cell().Text($"{totalCredits}");

                        table.Cell().Text("Medie generală:").Bold();
                        table.Cell().Text($"{averageGrade:F2}");

                        table.Cell().Text("Număr examene promovate:").Bold();
                        table.Cell().Text($"{approvedGrades.Count(g => g.Value >= 5)}");

                        table.Cell().Text("Număr examene nepromovate:").Bold();
                        table.Cell().Text($"{approvedGrades.Count(g => g.Value < 5)}");
                    });

                    column.Item().PaddingTop(20).Text("OBSERVAȚII").FontSize(11).Bold();
                    column.Item().PaddingTop(5).Text("Documentul este valabil în vederea continuării studiilor și dovedește situația academică a studentului la data emiterii.");

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Data emiterii:");
                            col.Item().Text($"{DateTime.Now:dd.MM.yyyy}").Bold();
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("SECRETARIAT");
                            col.Item().PaddingTop(20).AlignRight().Text("_________________");
                            col.Item().AlignRight().Text("(semnătură și ștampilă)").FontSize(8);
                        });
                    });
                });

                // Footer
                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).Italic()).Text(text =>
                {
                    text.Span("Document generat electronic la ");
                    text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<string> SaveDocumentAsync(byte[] pdfBytes, RequestType type, string studentNumber)
    {
        var documentsPath = Path.Combine(_environment.WebRootPath, "documents");

        // Ensure directory exists
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }

        var typePrefix = type switch
        {
            RequestType.StudentCertificate => "StudentCertificate",
            RequestType.GradeReport => "GradeReport",
            RequestType.EnrollmentProof => "EnrollmentProof",
            RequestType.Other => "Document",
            _ => "Document"
        };

        var fileName = $"{typePrefix}_{studentNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(documentsPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return $"/documents/{fileName}";
    }
}
