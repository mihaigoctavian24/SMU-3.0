using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using SMU.Data;
using SMU.Data.Entities;

namespace SMU.Services;

/// <summary>
/// Service for exporting data to PDF and Excel formats
/// Uses QuestPDF for PDF generation and ClosedXML for Excel
/// </summary>
public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IExportHistoryService? _exportHistoryService;
    private const string UniversityName = "Universitatea Româno-Americană";
    private const string UniversityAddress = "Bd. Expoziției nr. 1B, București";

    public ExportService(
        ApplicationDbContext context,
        IExportHistoryService? exportHistoryService = null)
    {
        _context = context;
        _exportHistoryService = exportHistoryService;

        // Set QuestPDF license (Community license for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ========================================
    // PDF Exports - Academic Documents
    // ========================================

    public async Task<byte[]> ExportSituatieScolara(Guid studentId)
    {
        // Fetch student data with all related entities
        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .Include(s => s.Grades)
                .ThenInclude(g => g.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            throw new InvalidOperationException($"Student cu ID {studentId} nu a fost găsit.");

        // Calculate statistics
        var approvedGrades = student.Grades.Where(g => g.Status == GradeStatus.Approved).ToList();
        var totalCredits = approvedGrades.Sum(g => g.Course.Credits);
        var averageGrade = approvedGrades.Any() ? approvedGrades.Average(g => (double)g.Value) : 0;
        var passedCourses = approvedGrades.Count(g => g.Value >= 5);
        var totalCourses = approvedGrades.Count;
        var passRate = totalCourses > 0 ? (double)passedCourses / totalCourses * 100 : 0;

        // Generate PDF
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
                    column.Item().PaddingTop(10).AlignCenter().Text("SITUAȚIE ȘCOLARĂ")
                        .FontSize(12).Bold();
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                // Content
                page.Content().PaddingVertical(15).Column(column =>
                {
                    // Student information
                    column.Item().Table(table =>
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

                        table.Cell().Text("Grupă:").Bold();
                        table.Cell().Text(student.Group?.Name ?? "N/A");

                        table.Cell().Text("An studiu:").Bold();
                        table.Cell().Text(student.Group?.Year.ToString() ?? "N/A");

                        table.Cell().Text("Status:").Bold();
                        table.Cell().Text(GetStudentStatusRomanian(student.Status));
                    });

                    column.Item().PaddingTop(20).Text("Situație Note:")
                        .FontSize(11).Bold();

                    // Grades table
                    column.Item().PaddingTop(5).Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Course
                            columns.RelativeColumn(1);  // Credits
                            columns.RelativeColumn(1);  // Grade
                            columns.RelativeColumn(1);  // Status
                            columns.RelativeColumn(1);  // Date
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                .Text("Disciplină").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                .Text("Credite").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                .Text("Notă").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                .Text("Status").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                .Text("Data").Bold();
                        });

                        // Data rows
                        foreach (var grade in approvedGrades.OrderBy(g => g.Course.Year)
                            .ThenBy(g => g.Course.Semester)
                            .ThenBy(g => g.Course.Name))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .Text(grade.Course.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(grade.Course.Credits.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(grade.Value.ToString("0.00"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(grade.Value >= 5 ? "Promovat" : "Nepromovat");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(grade.ExamDate.ToString("dd.MM.yyyy"));
                        }
                    });

                    // Summary statistics
                    column.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text("Total Credite:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text(totalCredits.ToString());

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text("Medie Generală:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text(averageGrade.ToString("0.00"));

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text("Disciplini Promovate:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text($"{passedCourses} / {totalCourses}");

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text("Procent Promovare:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                            .Text($"{passRate:0.00}%");
                    });
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Document generat la data: ");
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).Bold();
                    text.Span(" | Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        // Log export to history
        if (_exportHistoryService != null)
        {
            var fileName = $"situatie_scolara_{student.StudentNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            await _exportHistoryService.LogExportAsync(
                student.UserId,
                ExportType.SituatieScolara,
                fileName,
                pdfBytes.Length,
                new { studentId });
        }

        return pdfBytes;
    }

    public async Task<byte[]> ExportAdeverintaStudent(Guid studentId, string purpose)
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

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName)
                        .FontSize(14).Bold();
                    column.Item().AlignCenter().Text(UniversityAddress)
                        .FontSize(9);
                    column.Item().PaddingTop(20).LineHorizontal(1);
                });

                page.Content().PaddingVertical(30).Column(column =>
                {
                    column.Item().AlignCenter().PaddingBottom(30).Text("ADEVERINȚĂ")
                        .FontSize(16).Bold();

                    column.Item().PaddingVertical(10).Text(text =>
                    {
                        text.Span("        Prin prezenta se adeverește că ");
                        text.Span($"{student.User.LastName} {student.User.FirstName}").Bold();
                        text.Span($", identificat(ă) cu numărul matricol ");
                        text.Span(student.StudentNumber).Bold();
                        text.Span(", este student(ă) înscris(ă) în anul academic ");
                        text.Span($"{DateTime.Now.Year - 1}/{DateTime.Now.Year}").Bold();
                        text.Span($", anul {student.Group?.Year ?? 0} de studii, ");
                        text.Span("la programul de studii universitare ");
                        text.Span(student.Group?.Program.Name ?? "N/A").Bold();
                        text.Span($", Facultatea {student.Group?.Program.Faculty.Name ?? "N/A"}");
                        text.Span(", în cadrul ");
                        text.Span(UniversityName).Bold();
                        text.Span(".");
                    });

                    if (!string.IsNullOrWhiteSpace(purpose))
                    {
                        column.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("        Adeverința se eliberează la cerere pentru ");
                            text.Span(purpose).Bold();
                            text.Span(".");
                        });
                    }

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Data:");
                            col.Item().PaddingTop(5).Text(DateTime.Now.ToString("dd.MM.yyyy")).Bold();
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("Semnătura și ștampila");
                            col.Item().AlignRight().PaddingTop(30).Text("_________________");
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"Nr. înregistrare: ADV-{DateTime.Now:yyyyMMdd}-{studentId.ToString()[..8]}")
                    .FontSize(8);
            });
        });

        var pdfBytes = document.GeneratePdf();

        // Log export to history
        if (_exportHistoryService != null)
        {
            var fileName = $"adeverinta_{student.StudentNumber}_{DateTime.Now:yyyyMMdd}.pdf";
            await _exportHistoryService.LogExportAsync(
                student.UserId,
                ExportType.AdeverintaStudent,
                fileName,
                pdfBytes.Length,
                new { studentId, purpose });
        }

        return pdfBytes;
    }

    public async Task<byte[]> ExportCatalogNote(Guid courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Professor)
                .ThenInclude(p => p!.User)
            .Include(c => c.Program)
                .ThenInclude(p => p.Faculty)
            .Include(c => c.Grades)
                .ThenInclude(g => g.Student)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            throw new InvalidOperationException($"Curs cu ID {courseId} nu a fost găsit.");

        var approvedGrades = course.Grades
            .Where(g => g.Status == GradeStatus.Approved)
            .OrderBy(g => g.Student.User.LastName)
            .ThenBy(g => g.Student.User.FirstName)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName).FontSize(12).Bold();
                    column.Item().AlignCenter().Text($"Facultatea {course.Program.Faculty.Name}").FontSize(10);
                    column.Item().PaddingTop(10).AlignCenter().Text("CATALOG NOTE").FontSize(11).Bold();
                    column.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    // Course information
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Disciplină: {course.Name}").Bold();
                        row.RelativeItem().AlignRight().Text($"Cod: {course.Code}");
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Profesor: {course.Professor?.User.LastName} {course.Professor?.User.FirstName}");
                        row.RelativeItem().AlignRight().Text($"An: {course.Year} | Semestru: {course.Semester}");
                    });

                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);   // Nr
                            columns.ConstantColumn(100);  // Matricol
                            columns.RelativeColumn(2);    // Nume
                            columns.ConstantColumn(60);   // Notă
                            columns.ConstantColumn(80);   // Status
                            columns.ConstantColumn(90);   // Data
                            columns.ConstantColumn(100);  // Observații
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Nr.").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Matricol").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Nume Student").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Notă").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Status").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Data Examen").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Observații").FontColor(Colors.White).Bold();
                        });

                        int index = 1;
                        foreach (var grade in approvedGrades)
                        {
                            var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .AlignCenter().Text(index.ToString());
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(grade.Student.StudentNumber);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text($"{grade.Student.User.LastName} {grade.Student.User.FirstName}");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .AlignCenter().Text(grade.Value.ToString("0.00")).Bold();
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .AlignCenter().Text(grade.Value >= 5 ? "Promovat" : "Nepromovat");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .AlignCenter().Text(grade.ExamDate.ToString("dd.MM.yyyy"));
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(grade.Notes ?? "");

                            index++;
                        }
                    });

                    column.Item().PaddingTop(10).Text($"Total studenți: {approvedGrades.Count}").Bold();

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Data:");
                            col.Item().PaddingTop(5).Text(DateTime.Now.ToString("dd.MM.yyyy")).Bold();
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("Semnătura Profesor:");
                            col.Item().AlignRight().PaddingTop(5).Text("_________________");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generat la: ");
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).Bold();
                    text.Span(" | Pagina ");
                    text.CurrentPageNumber();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        // Log export to history
        if (_exportHistoryService != null && course.Professor != null)
        {
            var fileName = $"catalog_{course.Code}_{DateTime.Now:yyyyMMdd}.pdf";
            await _exportHistoryService.LogExportAsync(
                course.Professor.UserId,
                ExportType.CatalogNote,
                fileName,
                pdfBytes.Length,
                new { courseId });
        }

        return pdfBytes;
    }

    public async Task<byte[]> ExportRaportFacultate(Guid facultyId)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Programs)
                .ThenInclude(p => p.Groups)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(s => s.Grades.Where(gr => gr.Status == GradeStatus.Approved))
            .FirstOrDefaultAsync(f => f.Id == facultyId);

        if (faculty == null)
            throw new InvalidOperationException($"Facultate cu ID {facultyId} nu a fost găsită.");

        // Calculate statistics per program
        var programStats = faculty.Programs.Select(p => new
        {
            Program = p,
            TotalStudents = p.Groups.SelectMany(g => g.Students).Count(),
            ActiveStudents = p.Groups.SelectMany(g => g.Students).Count(s => s.Status == StudentStatus.Active),
            AverageGrade = p.Groups.SelectMany(g => g.Students)
                .SelectMany(s => s.Grades)
                .Where(g => g.Status == GradeStatus.Approved)
                .Select(g => (double)g.Value)
                .DefaultIfEmpty(0)
                .Average(),
            PassRate = CalculatePassRate(p.Groups.SelectMany(g => g.Students).SelectMany(s => s.Grades).ToList())
        }).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text(UniversityName).FontSize(14).Bold();
                    column.Item().AlignCenter().PaddingTop(5).Text($"RAPORT FACULTATE: {faculty.Name}").FontSize(12).Bold();
                    column.Item().AlignCenter().PaddingTop(3).Text($"An academic {DateTime.Now.Year - 1}/{DateTime.Now.Year}").FontSize(10);
                    column.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Item().Text("Statistici Generale:").FontSize(11).Bold();

                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        var totalStudents = programStats.Sum(p => p.TotalStudents);
                        var totalActive = programStats.Sum(p => p.ActiveStudents);
                        var overallAverage = programStats
                            .Where(p => p.AverageGrade > 0)
                            .Select(p => p.AverageGrade)
                            .DefaultIfEmpty(0)
                            .Average();

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Total Studenți:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(totalStudents.ToString());

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Studenți Activi:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(totalActive.ToString());

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Medie Generală:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(overallAverage.ToString("0.00"));

                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Programe Active:").Bold();
                        table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(faculty.Programs.Count(p => p.IsActive).ToString());
                    });

                    column.Item().PaddingTop(20).Text("Detalii pe Program:").FontSize(11).Bold();

                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Program
                            columns.RelativeColumn(1);  // Students
                            columns.RelativeColumn(1);  // Average
                            columns.RelativeColumn(1);  // Pass Rate
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Program Studii").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Studenți").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Medie").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Promovare %").FontColor(Colors.White).Bold();
                        });

                        foreach (var stat in programStats.OrderBy(s => s.Program.Name))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .Text(stat.Program.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(stat.TotalStudents.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(stat.AverageGrade.ToString("0.00"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                                .AlignCenter().Text(stat.PassRate.ToString("0.00"));
                        }
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().AlignCenter().Text(text =>
                    {
                        text.Span("Generat la: ");
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).Bold();
                        text.Span(" | Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        // Log export to history (use Dean's user ID if available)
        if (_exportHistoryService != null && faculty.DeanId.HasValue)
        {
            var fileName = $"raport_facultate_{faculty.Code}_{DateTime.Now:yyyyMMdd}.pdf";
            await _exportHistoryService.LogExportAsync(
                faculty.DeanId.Value,
                ExportType.RaportFacultate,
                fileName,
                pdfBytes.Length,
                new { facultyId });
        }

        return pdfBytes;
    }

    // ========================================
    // Excel Exports
    // ========================================

    public async Task<byte[]> ExportStudentsToExcel(Guid? facultyId = null, Guid? programId = null)
    {
        var query = _context.Students
            .Include(s => s.User)
            .Include(s => s.Group)
                .ThenInclude(g => g!.Program)
                    .ThenInclude(p => p.Faculty)
            .AsQueryable();

        if (programId.HasValue)
        {
            query = query.Where(s => s.Group!.ProgramId == programId.Value);
        }
        else if (facultyId.HasValue)
        {
            query = query.Where(s => s.Group!.Program.FacultyId == facultyId.Value);
        }

        var students = await query
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Studenți");

        // Header row
        var headers = new[]
        {
            "Nr. Matricol", "Nume", "Prenume", "Email", "Facultate",
            "Program", "Grupă", "An", "Status", "Bursier", "Data Înscriere"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196); // #4472C4
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = 2;
        foreach (var student in students)
        {
            worksheet.Cell(row, 1).Value = student.StudentNumber;
            worksheet.Cell(row, 2).Value = student.User.LastName;
            worksheet.Cell(row, 3).Value = student.User.FirstName;
            worksheet.Cell(row, 4).Value = student.User.Email;
            worksheet.Cell(row, 5).Value = student.Group?.Program.Faculty.Name ?? "N/A";
            worksheet.Cell(row, 6).Value = student.Group?.Program.Name ?? "N/A";
            worksheet.Cell(row, 7).Value = student.Group?.Name ?? "N/A";
            worksheet.Cell(row, 8).Value = student.Group?.Year ?? 0;
            worksheet.Cell(row, 9).Value = GetStudentStatusRomanian(student.Status);
            worksheet.Cell(row, 10).Value = student.ScholarshipHolder ? "DA" : "NU";
            worksheet.Cell(row, 11).Value = student.EnrollmentDate.ToString("dd.MM.yyyy");

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Return as byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportGradesToExcel(Guid? courseId = null, Guid? studentId = null)
    {
        var query = _context.Grades
            .Include(g => g.Student)
                .ThenInclude(s => s.User)
            .Include(g => g.Course)
                .ThenInclude(c => c.Professor)
                    .ThenInclude(p => p!.User)
            .Where(g => g.Status == GradeStatus.Approved)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(g => g.CourseId == courseId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(g => g.StudentId == studentId.Value);
        }

        var grades = await query
            .OrderBy(g => g.Student.User.LastName)
            .ThenBy(g => g.Course.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Note");

        // Header row
        var headers = new[]
        {
            "Student", "Nr. Matricol", "Disciplină", "Cod Disciplină",
            "Notă", "Tip", "Status", "Profesor", "Data Examen"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = 2;
        foreach (var grade in grades)
        {
            worksheet.Cell(row, 1).Value = $"{grade.Student.User.LastName} {grade.Student.User.FirstName}";
            worksheet.Cell(row, 2).Value = grade.Student.StudentNumber;
            worksheet.Cell(row, 3).Value = grade.Course.Name;
            worksheet.Cell(row, 4).Value = grade.Course.Code;
            worksheet.Cell(row, 5).Value = grade.Value;
            worksheet.Cell(row, 6).Value = GetGradeTypeRomanian(grade.Type);
            worksheet.Cell(row, 7).Value = grade.Value >= 5 ? "Promovat" : "Nepromovat";
            worksheet.Cell(row, 8).Value = grade.Course.Professor != null
                ? $"{grade.Course.Professor.User.LastName} {grade.Course.Professor.User.FirstName}"
                : "N/A";
            worksheet.Cell(row, 9).Value = grade.ExamDate.ToString("dd.MM.yyyy");

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportAttendanceToExcel(Guid? courseId = null, Guid? studentId = null)
    {
        var query = _context.Attendances
            .Include(a => a.Student)
                .ThenInclude(s => s.User)
            .Include(a => a.Course)
            .AsQueryable();

        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(a => a.StudentId == studentId.Value);
        }

        var attendances = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Student.User.LastName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Prezență");

        // Header row
        var headers = new[]
        {
            "Student", "Nr. Matricol", "Disciplină", "Data",
            "Status", "Observații"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = 2;
        foreach (var attendance in attendances)
        {
            worksheet.Cell(row, 1).Value = $"{attendance.Student.User.LastName} {attendance.Student.User.FirstName}";
            worksheet.Cell(row, 2).Value = attendance.Student.StudentNumber;
            worksheet.Cell(row, 3).Value = attendance.Course.Name;
            worksheet.Cell(row, 4).Value = attendance.Date.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 5).Value = GetAttendanceStatusRomanian(attendance.Status);
            worksheet.Cell(row, 6).Value = attendance.Notes ?? "";

            // Color code attendance status
            var statusCell = worksheet.Cell(row, 5);
            switch (attendance.Status)
            {
                case AttendanceStatus.Present:
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    break;
                case AttendanceStatus.Absent:
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                    break;
                case AttendanceStatus.Excused:
                    statusCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    break;
            }

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ========================================
    // Helper Methods
    // ========================================

    private static string GetStudentStatusRomanian(StudentStatus status)
    {
        return status switch
        {
            StudentStatus.Active => "Activ",
            StudentStatus.Inactive => "Inactiv",
            StudentStatus.Graduated => "Absolvent",
            StudentStatus.Expelled => "Exmatriculat",
            StudentStatus.Suspended => "Suspendat",
            _ => status.ToString()
        };
    }

    private static string GetGradeTypeRomanian(GradeType type)
    {
        return type switch
        {
            GradeType.Exam => "Examen",
            GradeType.Lab => "Laborator",
            GradeType.Seminar => "Seminar",
            GradeType.Project => "Proiect",
            GradeType.Final => "Final",
            _ => type.ToString()
        };
    }

    private static string GetAttendanceStatusRomanian(AttendanceStatus status)
    {
        return status switch
        {
            AttendanceStatus.Present => "Prezent",
            AttendanceStatus.Absent => "Absent",
            AttendanceStatus.Excused => "Motivat",
            _ => status.ToString()
        };
    }

    private static double CalculatePassRate(List<Grade> grades)
    {
        if (!grades.Any())
            return 0;

        var approvedGrades = grades.Where(g => g.Status == GradeStatus.Approved).ToList();
        if (!approvedGrades.Any())
            return 0;

        var passed = approvedGrades.Count(g => g.Value >= 5);
        return (double)passed / approvedGrades.Count * 100;
    }
}
