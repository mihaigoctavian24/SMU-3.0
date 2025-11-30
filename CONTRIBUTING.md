# Ghid de Contributie

Multumim pentru interesul de a contribui la SMU 3.0! Acest document ofera ghiduri pentru a contribui eficient la proiect.

## Cuprins

- [Cod de Conduita](#cod-de-conduita)
- [Cum Pot Contribui?](#cum-pot-contribui)
- [Setup Development](#setup-development)
- [Workflow Git](#workflow-git)
- [Standarde Cod](#standarde-cod)
- [Commit Messages](#commit-messages)
- [Pull Requests](#pull-requests)
- [Raportare Bug-uri](#raportare-bug-uri)

---

## Cod de Conduita

Prin participarea la acest proiect, te angajezi sa mentii un mediu respectuos si profesional. Asteptari:

- Foloseste limbaj incluziv si respectuos
- Accepta feedback constructiv
- Concentreaza-te pe ceea ce e mai bun pentru comunitate
- Arata empatie fata de alti contributori

---

## Cum Pot Contribui?

### Tipuri de Contributii

1. **Bug Reports** - Raporteaza bug-uri cu pasi de reproducere
2. **Feature Requests** - Sugereaza functionalitati noi
3. **Code** - Implementeaza features sau fix-uri
4. **Documentation** - Imbunatateste documentatia
5. **Testing** - Adauga teste sau raporteaza rezultate testare
6. **UI/UX** - Sugereaza imbunatatiri de design

### Unde Incep?

- Cauta issues cu label-ul `good first issue` pentru inceput
- Issues cu `help wanted` sunt potrivite pentru contributori externi
- Verifica [Projects](../../projects) pentru roadmap-ul curent

---

## Setup Development

### Cerinte

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Git](https://git-scm.com/)
- Editor recomandat: [Visual Studio Code](https://code.visualstudio.com/) sau [Visual Studio 2022](https://visualstudio.microsoft.com/)
- Extensii VS Code recomandate:
  - C# Dev Kit
  - Tailwind CSS IntelliSense
  - GitLens

### Pasi Setup

```bash
# 1. Fork repository-ul pe GitHub

# 2. Cloneaza fork-ul local
git clone https://github.com/YOUR-USERNAME/SMU-3.0.git
cd SMU-3.0

# 3. Adauga upstream remote
git remote add upstream https://github.com/ORIGINAL-OWNER/SMU-3.0.git

# 4. Instaleaza dependentele .NET
dotnet restore

# 5. Instaleaza dependentele Node
npm install

# 6. Copiaza si configureaza settings
cp appsettings.Example.json appsettings.Development.json
# Editeaza appsettings.Development.json cu connection string-ul tau

# 7. Aplica migrarile bazei de date
dotnet ef database update

# 8. Porneste aplicatia in development
dotnet watch run
```

### Baza de Date Locala

Pentru development local, poti folosi:

**Optiunea 1: Supabase (recomandat)**
- Creaza cont gratuit pe [supabase.com](https://supabase.com)
- Creaza proiect nou
- Copiaza connection string-ul PostgreSQL

**Optiunea 2: Docker**
```bash
docker run --name smu-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=smu \
  -p 5432:5432 \
  -d postgres:15
```

---

## Workflow Git

### Branch Naming

Foloseste urmatoarea conventie pentru branch-uri:

```
feature/descriere-scurta    # Feature nou
bugfix/descriere-bug        # Fix pentru bug
hotfix/descriere-urgenta    # Fix urgent pentru production
docs/ce-documentezi         # Documentatie
refactor/ce-refactorizezi   # Refactoring cod
```

Exemple:
```bash
git checkout -b feature/add-grade-export
git checkout -b bugfix/fix-login-redirect
git checkout -b docs/update-api-docs
```

### Workflow Standard

```bash
# 1. Asigura-te ca esti pe main actualizat
git checkout main
git pull upstream main

# 2. Creaza branch pentru feature
git checkout -b feature/my-feature

# 3. Lucreaza, commit-uri frecvente
git add .
git commit -m "feat: add grade export button"

# 4. Push la fork
git push origin feature/my-feature

# 5. Deschide Pull Request pe GitHub
```

### Sincronizare cu Upstream

```bash
# Periodic, sincronizeaza fork-ul
git fetch upstream
git checkout main
git merge upstream/main
git push origin main
```

---

## Standarde Cod

### C# / .NET

Urmeaza [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// DO: PascalCase pentru clase, metode, proprietati publice
public class StudentService
{
    public async Task<Student> GetStudentByIdAsync(Guid id)
    {
        // ...
    }
}

// DO: camelCase pentru parametri si variabile locale
public void ProcessGrade(Grade grade)
{
    var gradeValue = grade.Value;
    var isApproved = grade.Status == GradeStatus.Approved;
}

// DO: _camelCase pentru campuri private
private readonly IStudentRepository _studentRepository;

// DO: Foloseste var cand tipul e evident
var students = await _context.Students.ToListAsync();
var count = students.Count;

// DON'T: var cand tipul nu e clar
Student student = GetStudent(); // OK
var student = GetStudent();      // Ambiguu - ce tip returneaza?
```

### Blazor Components

```razor
@* Componente in fisiere separate .razor *@
@* Logica complexa in @code block sau code-behind .razor.cs *@

@page "/students"
@attribute [Authorize(Roles = "Admin,Secretary")]

<PageTitle>Studenti - SMU</PageTitle>

<div class="container mx-auto p-4">
    @if (_loading)
    {
        <LoadingSpinner />
    }
    else
    {
        <DataTable Items="_students" Context="student">
            <HeaderTemplate>
                <th>Nume</th>
                <th>Email</th>
            </HeaderTemplate>
            <RowTemplate>
                <td>@student.FullName</td>
                <td>@student.Email</td>
            </RowTemplate>
        </DataTable>
    }
</div>

@code {
    private List<Student> _students = new();
    private bool _loading = true;

    [Inject] private IStudentService StudentService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _students = await StudentService.GetAllAsync();
        _loading = false;
    }
}
```

### Tailwind CSS

```html
<!-- DO: Foloseste clase Tailwind, evita CSS custom -->
<div class="bg-white rounded-lg shadow-md p-6">
    <h2 class="text-xl font-semibold text-gray-800 mb-4">Titlu</h2>
    <p class="text-gray-600">Continut</p>
</div>

<!-- DO: Extrage componente pentru pattern-uri repetate -->
<!-- Creaza componenta Card.razor in loc sa repeti acelasi markup -->

<!-- DON'T: Stiluri inline -->
<div style="background: white; padding: 20px;">Bad</div>
```

### Entity Framework

```csharp
// DO: Foloseste async/await
public async Task<List<Student>> GetAllAsync()
{
    return await _context.Students
        .Include(s => s.Group)
        .Where(s => s.IsActive)
        .OrderBy(s => s.LastName)
        .ToListAsync();
}

// DO: Proiectii pentru performanta
public async Task<List<StudentDto>> GetStudentNamesAsync()
{
    return await _context.Students
        .Select(s => new StudentDto
        {
            Id = s.Id,
            FullName = $"{s.FirstName} {s.LastName}"
        })
        .ToListAsync();
}

// DON'T: N+1 queries
foreach (var student in students)
{
    var grades = await _context.Grades
        .Where(g => g.StudentId == student.Id) // BAD!
        .ToListAsync();
}
```

---

## Commit Messages

Folosim [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | Descriere |
|------|-----------|
| `feat` | Feature nou |
| `fix` | Bug fix |
| `docs` | Documentatie |
| `style` | Formatare, missing semi-colons (nu afecteaza logica) |
| `refactor` | Refactoring cod |
| `test` | Adaugare sau modificare teste |
| `chore` | Maintenance tasks, build changes |

### Exemple

```bash
# Feature nou
git commit -m "feat(grades): add PDF export functionality"

# Bug fix
git commit -m "fix(auth): resolve login redirect loop"

# Documentatie
git commit -m "docs(readme): update installation instructions"

# Refactoring
git commit -m "refactor(services): extract common validation logic"

# Cu body pentru context aditional
git commit -m "feat(notifications): add real-time notification bell

- Implement SignalR hub for notifications
- Add NotificationBell component
- Update MainLayout to include bell

Closes #123"
```

---

## Pull Requests

### Inainte de PR

- [ ] Codul compileaza fara erori
- [ ] Toate testele trec (`dotnet test`)
- [ ] Ai testat manual functionalitatea
- [ ] Ai actualizat documentatia daca e necesar
- [ ] Commit messages urmeaza conventiile

### Template PR

Cand deschizi un PR, completeaza:

```markdown
## Descriere
Descrie schimbarile facute si de ce.

## Tip schimbare
- [ ] Bug fix
- [ ] Feature nou
- [ ] Breaking change
- [ ] Documentatie

## Cum a fost testat?
Descrie testele efectuate.

## Checklist
- [ ] Codul meu urmeaza style guidelines
- [ ] Am facut self-review
- [ ] Am adaugat comentarii pentru cod complex
- [ ] Am actualizat documentatia
- [ ] Schimbarile nu genereaza warnings noi
- [ ] Am adaugat teste pentru schimbari
- [ ] Toate testele trec local
```

### Review Process

1. Minimum 1 approval necesar pentru merge
2. CI checks trebuie sa treaca
3. Branch-ul trebuie sa fie up-to-date cu main
4. Squash merge pentru istoric curat

---

## Raportare Bug-uri

### Template Issue

```markdown
## Descriere Bug
Descriere clara si concisa.

## Pasi de Reproducere
1. Mergi la '...'
2. Click pe '...'
3. Scroll pana la '...'
4. Vezi eroarea

## Comportament Asteptat
Ce te asteptai sa se intample.

## Screenshots
Daca e aplicabil, adauga screenshots.

## Environment
- OS: [e.g., Windows 11, macOS 14]
- Browser: [e.g., Chrome 120, Safari 17]
- .NET Version: [e.g., 8.0.1]

## Context Aditional
Orice alt context despre problema.
```

---

## Intrebari?

Daca ai intrebari:

1. Verifica [Discussions](../../discussions) pentru intrebari anterioare
2. Deschide o noua discutie pentru intrebari generale
3. Deschide un issue pentru bug-uri sau feature requests
4. Contacteaza maintainerii pe [email]

---

Multumim pentru contributie!
