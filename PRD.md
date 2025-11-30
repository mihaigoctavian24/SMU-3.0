# PRD: SMU 3.0 - Sistem Management Universitar

## 1. Viziune

SMU 3.0 este un sistem modern de management universitar care oferă o experiență intuitivă și eficientă pentru toate părțile implicate în procesul educațional: studenți, profesori, secretariat, decani, rectori și administratori.

**Misiune**: Digitalizarea completă a proceselor universitare cu o interfață modernă, performantă și ușor de utilizat.

---

## 2. Stack Tehnic

| Componentă | Tehnologie |
|------------|------------|
| **Framework** | Blazor Server (.NET 8) |
| **UI** | Tailwind CSS + Lucide Icons |
| **Charts** | Blazor.ApexCharts |
| **Database** | Supabase PostgreSQL |
| **ORM** | Entity Framework Core |
| **Auth** | ASP.NET Identity |
| **Real-time** | SignalR (built-in) |
| **Export** | QuestPDF + ClosedXML |
| **Hosting** | Azure App Service |
| **CI/CD** | GitHub Actions |

### Avantaje arhitectură:
- **Un singur proiect** - simplitate în dezvoltare și deployment
- **Server-side rendering** - performanță și SEO
- **Real-time updates** - SignalR built-in
- **Auth robust** - Identity framework testat în producție

---

## 3. Roluri și Permisiuni

### 3.1 Tipuri de Utilizatori

| Rol | Cod | Descriere | Scope |
|-----|-----|-----------|-------|
| **Student** | `student` | Accesează propriile note, prezențe, documente | Personal |
| **Profesor** | `professor` | Gestionează cursuri, note, prezențe pentru studenții proprii | Cursuri proprii |
| **Secretariat** | `secretary` | Administrează studenți, documente, exporturi | Facultate |
| **Decan** | `dean` | Supervizează facultatea, aprobă note, rapoarte | Facultate |
| **Rector** | `rector` | Vizualizare globală, decizii strategice | Universitate |
| **Administrator** | `admin` | Configurare sistem, gestiune utilizatori | Global |

### 3.2 Matrice Permisiuni Detaliată

#### Legenda:
- ✓ = Acces complet
- R = Read only
- O = Own data only
- F = Faculty scope
- - = Fără acces

| Modul | Student | Profesor | Secretariat | Decan | Rector | Admin |
|-------|---------|----------|-------------|-------|--------|-------|
| **Dashboard** | O | O | F | F | ✓ | ✓ |
| **Studenți - View** | - | O (cursuri) | F | F | ✓ | ✓ |
| **Studenți - Create** | - | - | ✓ | - | - | ✓ |
| **Studenți - Update** | - | - | ✓ | - | - | ✓ |
| **Studenți - Delete** | - | - | ✓ | - | - | ✓ |
| **Profesori - View** | - | - | F | F | ✓ | ✓ |
| **Profesori - CRUD** | - | - | - | - | - | ✓ |
| **Note - View** | O | O (cursuri) | F | F | ✓ | ✓ |
| **Note - Create** | - | ✓ (cursuri) | - | - | - | ✓ |
| **Note - Approve** | - | - | - | ✓ | - | ✓ |
| **Note - Reject** | - | - | - | ✓ | - | ✓ |
| **Prezențe - View** | O | O (cursuri) | F | F | ✓ | ✓ |
| **Prezențe - CRUD** | - | ✓ (cursuri) | - | - | - | ✓ |
| **Orar - View** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Orar - CRUD** | - | - | ✓ | - | - | ✓ |
| **Facultăți - View** | - | - | F | F | ✓ | ✓ |
| **Facultăți - CRUD** | - | - | - | - | - | ✓ |
| **Rapoarte - View** | - | O | F | F | ✓ | ✓ |
| **Rapoarte - Export** | - | O | F | F | ✓ | ✓ |
| **Documente - Request** | ✓ | - | - | - | - | - |
| **Documente - Process** | - | - | ✓ | R | R | ✓ |
| **Notificări** | O | O | O | O | O | ✓ |
| **Setări Sistem** | - | - | - | - | - | ✓ |
| **Audit Log** | - | - | - | - | R | ✓ |

---

## 4. User Stories Detaliate

### 4.1 Student User Stories

#### Autentificare
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| S-001 | Ca student, vreau să mă autentific cu email și parolă | Email format @stud.rau.ro, redirect la dashboard după login |
| S-002 | Ca student, vreau să îmi recuperez parola uitată | Email cu link reset, link valid 24h |
| S-003 | Ca student, vreau să rămân autentificat între sesiuni | Remember me checkbox, token 30 zile |

#### Dashboard
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| S-010 | Ca student, vreau să văd media mea generală | Calculată din note aprobate, afișată cu 2 zecimale |
| S-011 | Ca student, vreau să văd rata mea de prezență | Procentaj prezențe/total ore, colorat (roșu <70%, galben 70-85%, verde >85%) |
| S-012 | Ca student, vreau să văd ultimele note primite | Max 5 note, sortate descrescător după dată |
| S-013 | Ca student, vreau să văd notificările necitite | Badge cu număr, click deschide lista |

#### Note
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| S-020 | Ca student, vreau să văd toate notele mele | Tabel cu curs, dată, notă, tip, status |
| S-021 | Ca student, vreau să filtrez notele pe semestru/an | Dropdown semestru 1/2, dropdown an |
| S-022 | Ca student, vreau să văd media pe fiecare curs | Calculată automat, afișată per curs |
| S-023 | Ca student, vreau să export notele în PDF | Buton export, PDF cu semnătură digitală |

#### Prezențe
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| S-030 | Ca student, vreau să văd prezențele mele pe calendar | Calendar lunar, zile colorate (verde/roșu/galben) |
| S-031 | Ca student, vreau să văd detalii prezențe per curs | Click pe curs → lista ore cu status |
| S-032 | Ca student, vreau să văd statistici prezențe | Total prezent/absent/învoit, grafic pie |

#### Documente
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| S-040 | Ca student, vreau să solicit o adeverință de student | Formular cu tip adeverință, motiv |
| S-041 | Ca student, vreau să văd statusul cererilor mele | Lista cereri cu status (pending/approved/rejected) |
| S-042 | Ca student, vreau să descarc documentele aprobate | Buton download pentru cereri aprobate |

---

### 4.2 Profesor User Stories

#### Dashboard
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| P-010 | Ca profesor, vreau să văd cursurile mele active | Lista cursuri cu program, grupă, credite |
| P-011 | Ca profesor, vreau să văd numărul total de studenți | Suma studenților din toate cursurile mele |
| P-012 | Ca profesor, vreau să văd note în așteptare | Counter note cu status pending |

#### Note
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| P-020 | Ca profesor, vreau să adaug note pentru un curs | Selectez curs → lista studenți → input notă |
| P-021 | Ca profesor, vreau să adaug note în bulk | Import CSV cu nr_matricol, notă |
| P-022 | Ca profesor, vreau să editez o notă înainte de aprobare | Edit doar pentru note cu status pending |
| P-023 | Ca profesor, vreau să văd istoricul notelor date | Tabel cu filtru pe curs, dată, status |

#### Prezențe
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| P-030 | Ca profesor, vreau să fac prezența la o oră | Selectez curs, dată, lista studenți cu checkbox |
| P-031 | Ca profesor, vreau să marchez absențe motivate | Checkbox special "motivat" + câmp note |
| P-032 | Ca profesor, vreau să văd statistici prezență per curs | Procent prezență per student, per curs |
| P-033 | Ca profesor, vreau să export prezența în Excel | Buton export cu toți studenții, toate datele |

---

### 4.3 Secretariat User Stories

#### Studenți
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| SEC-010 | Ca secretar, vreau să adaug un student nou | Formular complet, validare CNP, generare nr matricol |
| SEC-011 | Ca secretar, vreau să editez datele unui student | Toate câmpurile editabile, audit log |
| SEC-012 | Ca secretar, vreau să transfer un student la altă grupă | Selectare grupă destinație, păstrare istoric |
| SEC-013 | Ca secretar, vreau să exmatricuez un student | Confirmare dialog, motiv obligatoriu, notificare student |
| SEC-014 | Ca secretar, vreau să import studenți din Excel | Upload file, validare, preview, confirmare |
| SEC-015 | Ca secretar, vreau să export lista studenților | Filtre + export CSV/Excel/PDF |

#### Documente
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| SEC-020 | Ca secretar, vreau să procesez cereri de adeverințe | Lista cereri pending, aprobare/respingere |
| SEC-021 | Ca secretar, vreau să generez documente automat | Template-uri predefinite, date auto-populate |
| SEC-022 | Ca secretar, vreau să înregistrez număr de ieșire | Număr unic, dată, tracking |

---

### 4.4 Decan User Stories

#### Aprobare Note
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| D-010 | Ca decan, vreau să văd notele în așteptarea aprobării | Lista note pending din facultatea mea |
| D-011 | Ca decan, vreau să aprob/resping o notă | Butoane Approve/Reject, comentariu obligatoriu pentru reject |
| D-012 | Ca decan, vreau să aprob note în bulk | Checkbox multiple, aprobare toate selectate |
| D-013 | Ca decan, vreau să văd istoricul aprobărilor mele | Tabel cu data, notă, profesor, student, acțiune |

#### Rapoarte
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| D-020 | Ca decan, vreau să văd statistici facultate | Dashboard cu KPIs facultate |
| D-021 | Ca decan, vreau raport medii pe programe | Grafic bar medii per program |
| D-022 | Ca decan, vreau raport rate promovare | Procent promovați/respinși per an |

---

### 4.5 Admin User Stories

#### Utilizatori
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| A-010 | Ca admin, vreau să creez utilizatori noi | Formular cu rol, email, date personale |
| A-011 | Ca admin, vreau să dezactivez un utilizator | Soft delete, păstrare date |
| A-012 | Ca admin, vreau să resetez parola unui utilizator | Generare parolă temporară, forțare schimbare |
| A-013 | Ca admin, vreau să schimb rolul unui utilizator | Dropdown roluri, confirmare |

#### Sistem
| ID | Story | Criterii de Acceptare |
|----|-------|----------------------|
| A-020 | Ca admin, vreau să configurez anii universitari | CRUD ani, semestre, date start/end |
| A-021 | Ca admin, vreau să văd audit log | Filtru utilizator, acțiune, dată, export |
| A-022 | Ca admin, vreau să fac backup la date | Trigger manual, download |

---

## 5. Fluxuri de Business

### 5.1 Flux Înregistrare Notă

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Profesor   │────▶│ Creare Notă │────▶│   Status:   │────▶│  Notificare │
│ adaugă notă │     │  în sistem  │     │   PENDING   │     │   Decan     │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                                                                   │
                    ┌──────────────────────────────────────────────┘
                    │
                    ▼
              ┌─────────────┐
              │    Decan    │
              │  reviewează │
              └─────────────┘
                    │
         ┌─────────┴─────────┐
         │                   │
         ▼                   ▼
   ┌───────────┐       ┌───────────┐
   │  APPROVED │       │  REJECTED │
   └───────────┘       └───────────┘
         │                   │
         ▼                   ▼
   ┌───────────┐       ┌───────────┐
   │ Notificare│       │ Notificare│
   │  Student  │       │ Profesor  │
   └───────────┘       └───────────┘
```

### 5.2 Flux Cerere Document

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Student   │────▶│   Cerere    │────▶│   Status:   │
│   trimite   │     │  înregistr. │     │   PENDING   │
└─────────────┘     └─────────────┘     └─────────────┘
                                               │
                    ┌──────────────────────────┘
                    ▼
              ┌─────────────┐
              │ Secretariat │
              │  procesează │
              └─────────────┘
                    │
         ┌─────────┴─────────┐
         │                   │
         ▼                   ▼
   ┌───────────┐       ┌───────────┐
   │  APPROVED │       │  REJECTED │
   │  + Nr.Reg │       │  + Motiv  │
   └───────────┘       └───────────┘
         │                   │
         ▼                   ▼
   ┌───────────┐       ┌───────────┐
   │ Document  │       │ Notificare│
   │ disponibil│       │  Student  │
   └───────────┘       └───────────┘
```

### 5.3 Flux Prezență

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Profesor   │────▶│  Selectare  │────▶│   Lista     │
│  inițiază   │     │  Curs/Data  │     │  Studenți   │
└─────────────┘     └─────────────┘     └─────────────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │  Marcare    │
                                        │  P/A/M      │
                                        └─────────────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │   Salvare   │
                                        │   + Audit   │
                                        └─────────────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │  Notificare │
                                        │  Studenți   │
                                        │  (absenți)  │
                                        └─────────────┘
```

---

## 6. Module Funcționale

### 6.1 Autentificare & Landing

**Landing Page** (public):
- Hero section cu branding universitate
- Secțiune "Conturi Demo" pentru testare rapidă
- Link către login

**Login Page**:
- Formular email + parolă
- Opțiune "Ai uitat parola?"
- Quick-login pentru demo (în development)

**Flux autentificare**:
```
Landing → Login → [Auth] → Dashboard (role-specific)
```

### 6.2 Dashboard

**Student Dashboard**:
- Card bun-venit cu gradient
- Stats: Medie generală, Note primite, Rată prezență, Cursuri active
- Grid: Note recente, Notificări
- Secțiune: Prezențe recente

**Professor Dashboard**:
- Stats: Cursuri predate, Studenți total, Note de aprobat
- Grid: Cursuri active, Note în așteptare
- Calendar săptămânal cu ore

**Admin/Dean/Rector Dashboard**:
- Stats globale: Studenți, Profesori, Cursuri, Facultăți
- Grafice: Evoluție înscrieri, Distribuție pe facultăți
- Alerts: Acțiuni necesare

### 6.3 Gestionare Studenți

**Features**:
- Tabel cu paginare, sortare, filtrare
- Căutare: nume, email, nr. matricol
- Filtre: status (activ/inactiv/absolvent/exmatriculat), grupă, an
- Export CSV/Excel
- CRUD complet cu modal

**Câmpuri student**:
- Prenume, Nume
- Email (@stud.rau.ro)
- CNP
- Nr. Matricol (auto-generat)
- Grupă
- An înmatriculare
- Status

### 6.4 Catalog Note

**Student View**:
- Lista note proprii cu curs, dată, tip, status
- Medie pe semestru/an
- Istoric complet

**Professor View**:
- Selectare curs → studenți → adăugare note
- Tipuri: examen, laborator, seminar, proiect
- Status: pending → approved (necesită aprobare decan)
- Bulk import note

**Dean View**:
- Note în așteptarea aprobării
- Approve/Reject cu comentarii
- Istoric aprobări

### 6.5 Prezențe

**Professor View**:
- Selectare curs/dată
- Lista studenți cu checkbox prezent/absent/învoit
- Note opționale
- Istoric prezențe per curs

**Student View**:
- Calendar cu prezențe colorate
- Statistici: total prezențe, absențe, învoire
- Rată prezență per curs

### 6.6 Orar

**Vizualizare**:
- View săptămânal cu ore
- Filtrare: facultate, program, grupă, profesor
- Export PDF/iCal

**Management** (secretariat/admin):
- Drag & drop pentru programare
- Conflict detection
- Alocări săli

### 6.7 Facultăți & Programe

**Structură**:
```
Facultate
└── Program (Licență/Master/Doctorat)
    └── Grupă
        └── Studenți
```

**Management**:
- CRUD facultăți
- CRUD programe cu durata și tip
- CRUD grupe cu an de studiu
- Alocare decani

### 6.8 Rapoarte

**Tipuri**:
- Situație școlară per student
- Medii pe grupă/program/facultate
- Statistici prezențe
- Evoluție înscrieri
- Export complet

**Formate**: PDF, Excel, CSV

### 6.9 Documente

**Pentru studenți**:
- Adeverințe de student
- Situații școlare
- Cereri diverse

**Management**:
- Templates documente
- Generare automată
- Istoric cereri

### 6.10 Notificări

**Tipuri**:
- Note noi
- Prezențe înregistrate
- Aprobare necesară
- Anunțuri generale
- Termen limită

**Canale**:
- In-app (real-time via SignalR)
- Email (opțional)

---

## 7. Design System

### 7.1 Culori

```css
/* Primary - Indigo */
--primary-50: #eef2ff;
--primary-100: #e0e7ff;
--primary-500: #6366f1;
--primary-600: #4f46e5;
--primary-700: #4338ca;

/* Neutral - Gray */
--gray-50: #f9fafb;
--gray-100: #f3f4f6;
--gray-200: #e5e7eb;
--gray-500: #6b7280;
--gray-700: #374151;
--gray-900: #111827;

/* Semantic */
--success: #22c55e;
--warning: #f59e0b;
--error: #ef4444;
--info: #3b82f6;
```

### 7.2 Componente UI

**Layout**:
- Sidebar fix 256px, collapsible pe mobile
- Header cu search, notificări, data curentă
- Main content cu padding 24px
- Cards cu rounded-xl, border subtle, elevation on hover

**Elemente**:
- Butoane: filled primary, outlined, text
- Inputs: outlined cu focus ring indigo
- Tables: striped hover, sticky header
- Badges: rounded-full pentru status
- Avatare: inițiale cu background colorat

**Responsive**:
- Desktop: sidebar permanent
- Tablet: sidebar collapsible
- Mobile: sidebar drawer

### 7.3 Iconografie

**Set**: Lucide Icons (similar design-ului demo)

**Principalele**:
- `LayoutDashboard` - Dashboard
- `Users` - Studenți
- `GraduationCap` - Profesori/Educație
- `BookOpen` - Note/Cursuri
- `ClipboardList` - Prezențe
- `Calendar` - Orar
- `Building2` - Facultăți
- `BarChart3` - Rapoarte
- `FileText` - Documente
- `Bell` - Notificări
- `Settings` - Setări
- `LogOut` - Deconectare

---

## 8. Model Date (Entity Framework)

### 8.1 Entități Core

```csharp
// Identity User extins
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public UserRole Role { get; set; }
    public Guid? FacultyId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class Faculty
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string? Description { get; set; }
    public string? DeanId { get; set; }
    public ApplicationUser? Dean { get; set; }
    public ICollection<Program> Programs { get; set; }
}

public class Program
{
    public Guid Id { get; set; }
    public Guid FacultyId { get; set; }
    public Faculty Faculty { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int Duration { get; set; } // ani
    public ProgramType Type { get; set; }
    public ICollection<Group> Groups { get; set; }
    public ICollection<Course> Courses { get; set; }
}

public class Group
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Program Program { get; set; }
    public string Name { get; set; }
    public int Year { get; set; }
    public ICollection<Student> Students { get; set; }
}

public class Student
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; }
    public string MatriculationNumber { get; set; }
    public string CNP { get; set; }
    public StudentStatus Status { get; set; }
    public int EnrollmentYear { get; set; }
    public ICollection<Grade> Grades { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
}

public class Professor
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public Guid FacultyId { get; set; }
    public Faculty Faculty { get; set; }
    public string Department { get; set; }
    public string Title { get; set; }
    public ICollection<Course> Courses { get; set; }
}

public class Course
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public Guid ProgramId { get; set; }
    public Program Program { get; set; }
    public Guid ProfessorId { get; set; }
    public Professor Professor { get; set; }
    public int Credits { get; set; }
    public int Semester { get; set; }
    public int Year { get; set; }
    public ICollection<Grade> Grades { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
}

public class Grade
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; }
    public decimal Value { get; set; }
    public DateTime Date { get; set; }
    public GradeType Type { get; set; }
    public GradeStatus Status { get; set; }
    public Guid ProfessorId { get; set; }
    public Professor Professor { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class Attendance
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid ProfessorId { get; set; }
    public Professor Professor { get; set; }
    public string? Notes { get; set; }
}

public class Notification
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Link { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
```

### 8.2 Enums

```csharp
public enum UserRole { Student, Professor, Secretary, Dean, Rector, Admin }
public enum StudentStatus { Active, Inactive, Graduated, Expelled }
public enum ProgramType { Bachelor, Master, PhD }
public enum GradeType { Exam, Lab, Seminar, Project, Final }
public enum GradeStatus { Pending, Approved, Rejected }
public enum AttendanceStatus { Present, Absent, Excused }
public enum NotificationType { Info, Success, Warning, Error }
```

---

## 9. Structură Proiect

```
SMU-3.0/
├── .github/
│   └── workflows/
│       └── deploy.yml
├── docs/
│   ├── ARCHITECTURE.md
│   ├── API.md
│   └── DEPLOYMENT.md
├── src/
│   └── UniversityManagement/
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   ├── Sidebar.razor
│       │   │   └── Header.razor
│       │   ├── Pages/
│       │   │   ├── Home.razor
│       │   │   ├── Login.razor
│       │   │   ├── Dashboard/
│       │   │   │   ├── StudentDashboard.razor
│       │   │   │   ├── ProfessorDashboard.razor
│       │   │   │   ├── DeanDashboard.razor
│       │   │   │   ├── RectorDashboard.razor
│       │   │   │   └── AdminDashboard.razor
│       │   │   ├── Students/
│       │   │   ├── Grades/
│       │   │   ├── Attendance/
│       │   │   ├── Faculties/
│       │   │   ├── Analytics/
│       │   │   │   ├── RiskDashboard.razor
│       │   │   │   └── TrendAnalysis.razor
│       │   │   └── Reports/
│       │   │       ├── ReportGenerator.razor
│       │   │       └── ExportPage.razor
│       │   ├── Shared/
│       │   │   ├── Card.razor
│       │   │   ├── Button.razor
│       │   │   ├── Table.razor
│       │   │   ├── Modal.razor
│       │   │   ├── Badge.razor
│       │   │   └── ...
│       │   └── Charts/
│       │       ├── LineChart.razor
│       │       ├── BarChart.razor
│       │       ├── DonutChart.razor
│       │       ├── RadarChart.razor
│       │       ├── HeatmapChart.razor
│       │       └── GaugeChart.razor
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Entities/
│       │   │   ├── Core/
│       │   │   │   ├── ApplicationUser.cs
│       │   │   │   ├── Student.cs
│       │   │   │   ├── Professor.cs
│       │   │   │   └── ...
│       │   │   └── Analytics/
│       │   │       ├── DailySnapshot.cs
│       │   │       ├── GradeSnapshot.cs
│       │   │       ├── AttendanceStats.cs
│       │   │       ├── StudentRiskScore.cs
│       │   │       ├── StudentEngagement.cs
│       │   │       └── HistoricalMetric.cs
│       │   └── Migrations/
│       ├── Services/
│       │   ├── Core/
│       │   │   ├── StudentService.cs
│       │   │   ├── GradeService.cs
│       │   │   ├── AttendanceService.cs
│       │   │   └── NotificationService.cs
│       │   ├── Analytics/
│       │   │   ├── RiskScoringService.cs
│       │   │   ├── SnapshotService.cs
│       │   │   └── TrendAnalysisService.cs
│       │   ├── Export/
│       │   │   ├── PdfExportService.cs
│       │   │   └── ExcelExportService.cs
│       │   └── AuditService.cs
│       ├── Hubs/
│       │   ├── NotificationHub.cs
│       │   └── DashboardHub.cs
│       ├── Jobs/
│       │   ├── DailySnapshotJob.cs
│       │   └── RiskCalculationJob.cs
│       ├── wwwroot/
│       │   ├── css/
│       │   │   └── app.css
│       │   └── js/
│       ├── tailwind.config.js
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── UniversityManagement.Tests/
├── CREDENTIALS/
├── .gitignore
├── LICENSE
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
└── PRD.md
```

---

## 10. Roadmap Implementare

### Faza 1: Foundation (Core) - Sprint 1-2
- [ ] Setup proiect Blazor Server
- [ ] Configurare Tailwind CSS
- [ ] Conexiune Supabase PostgreSQL
- [ ] Entity Framework + Migrations
- [ ] ASP.NET Identity setup
- [ ] Layout base (Sidebar, Header)
- [ ] Pagină Login funcțională
- [ ] Routing + Autorizare per rol

### Faza 2: Dashboards - Sprint 3
- [ ] Student Dashboard
- [ ] Professor Dashboard
- [ ] Admin Dashboard
- [ ] Componente shared (Card, Stats, etc.)

### Faza 3: Core Modules - Sprint 4-6
- [ ] Gestionare Studenți (CRUD)
- [ ] Catalog Note (CRUD + Aprobare)
- [ ] Prezențe (CRUD)
- [ ] Facultăți & Programe

### Faza 4: Advanced Features - Sprint 7-8
- [ ] Notificări real-time (SignalR)
- [ ] Orar
- [ ] Documente
- [ ] Dean/Rector Dashboards

### Faza 5: Analytics & Charts - Sprint 9-10
- [ ] Integrare Blazor.ApexCharts
- [ ] Student Charts (evoluție medie, prezențe)
- [ ] Professor Charts (distribuție note, corelații)
- [ ] Dean Charts (treemap, funnel, KPIs)
- [ ] Rector Charts (trends, comparații facultăți)
- [ ] Real-time dashboard updates (SignalR)

### Faza 6: Export & Rapoarte - Sprint 11-12
- [ ] Setup QuestPDF pentru PDF export
- [ ] Setup ClosedXML pentru Excel export
- [ ] Template-uri rapoarte (situație școlară, catalog)
- [ ] Export dashboard charts ca imagine
- [ ] Rapoarte programate (opțional)

### Faza 7: Predictive Analytics - Sprint 13-14
- [ ] Implementare Risk Scoring System
- [ ] Entități analytics (GradeSnapshot, AttendanceStats)
- [ ] Calculare automată risk scores (background job)
- [ ] Early Warning Dashboard pentru Decan
- [ ] Alert system pentru studenți la risc
- [ ] Trend analysis & predictions

### Faza 8: Polish & Deploy - Sprint 15-16
- [ ] Responsive design
- [ ] Dark mode (opțional)
- [ ] Optimizare performanță
- [ ] Azure deployment
- [ ] CI/CD pipeline
- [ ] Load testing & optimizations

---

## 11. Convenții Cod

### Naming
- **Componente**: PascalCase (`StudentDashboard.razor`)
- **Servicii**: PascalCase cu sufix Service (`StudentService.cs`)
- **Metode**: PascalCase, verbe (`GetStudentById`, `CreateGrade`)
- **Variabile**: camelCase (`currentUser`, `isLoading`)
- **CSS Classes**: kebab-case Tailwind (`bg-indigo-600`, `rounded-xl`)

### Structură Componentă Blazor
```razor
@* Directives *@
@page "/students"
@attribute [Authorize(Roles = "Admin,Secretary")]

@* Injects *@
@inject StudentService StudentService
@inject NavigationManager Navigation

@* Markup *@
<div class="space-y-6">
    ...
</div>

@* Code block *@
@code {
    // Properties
    private List<Student> students = new();
    private bool isLoading = true;

    // Lifecycle
    protected override async Task OnInitializedAsync()
    {
        students = await StudentService.GetAllAsync();
        isLoading = false;
    }

    // Methods
    private async Task DeleteStudent(Guid id) { ... }
}
```

---

## 12. Metrici Succes

| Metric | Target |
|--------|--------|
| Timp încărcare pagină | < 2s |
| Time to Interactive | < 3s |
| Uptime | 99.9% |
| User satisfaction | > 4.5/5 |
| Code coverage | > 80% |
| Lighthouse Score | > 90 |

---

## 13. Security Considerations

- **Autentificare**: ASP.NET Identity cu password hashing (PBKDF2)
- **Autorizare**: Role-based + Resource-based pentru granularitate
- **Data Protection**: Encryption at rest pentru CNP
- **HTTPS**: Obligatoriu în producție
- **CORS**: Configurare strictă
- **Rate Limiting**: Pentru prevenire brute-force
- **Audit Log**: Toate acțiunile sensibile logate
- **Session Management**: Timeout configurabil, logout pe inactivitate

---

## 14. Analytics & Raportare

### 14.1 Vizualizări per Rol (Blazor.ApexCharts)

#### Student Dashboard Charts
| Tip Grafic | Descriere | Actualizare |
|------------|-----------|-------------|
| **Line Chart** | Evoluție medie pe semestre | La fiecare notă nouă |
| **Radar Chart** | Performanță pe categorii (Exam/Lab/Seminar/Proiect) | Semestrial |
| **Donut Chart** | Distribuție prezențe (Prezent/Absent/Motivat) | Real-time |
| **Heatmap** | Calendar prezențe lunar | Zilnic |
| **Progress Bars** | Credite acumulate vs. necesare | La fiecare notă aprobată |

#### Professor Dashboard Charts
| Tip Grafic | Descriere | Actualizare |
|------------|-----------|-------------|
| **Box Plot** | Distribuție note per curs (min/max/median/quartile) | După sesiune |
| **Bar Chart** | Comparație medii între grupe | Semestrial |
| **Scatter Plot** | Corelație prezențe vs. note | Semestrial |
| **Stacked Bar** | Breakdown note per tip (Exam/Lab/etc.) | La cerere |
| **Line Chart** | Trend prezență pe parcursul semestrului | Săptămânal |
| **Heatmap** | Matricea prezențelor (studenți x date) | Real-time |

#### Dean Dashboard Charts
| Tip Grafic | Descriere | Actualizare |
|------------|-----------|-------------|
| **Treemap** | Overview facultate (programe → grupe → studenți) | Zilnic |
| **Multi-line Chart** | Evoluție medii pe programe (comparativ) | Semestrial |
| **Funnel Chart** | Rata promovare per an (An I → II → III → Absolvire) | Anual |
| **Bar Chart** | Top/Bottom 10 cursuri după medie | Semestrial |
| **Donut Chart** | Distribuție studenți pe statusuri | Real-time |
| **Gauge Chart** | KPIs facultate (rata prezență, rata promovare) | Zilnic |

#### Rector Dashboard Charts
| Tip Grafic | Descriere | Actualizare |
|------------|-----------|-------------|
| **Choropleth/Bubble Map** | Distribuție studenți pe origine geografică | Anual |
| **Multi-series Line** | Trend înscrieri pe 5 ani | Anual |
| **Sunburst Chart** | Ierarhie completă universitate | Real-time |
| **Radar Chart** | Comparație facultăți (multipli KPIs) | Semestrial |
| **Stacked Area** | Evoluție buget/cheltuieli | Lunar |
| **Sankey Diagram** | Flow studenți (înscriere → absolvire → angajare) | Anual |

### 14.2 Export Capabilități

**Formate suportate**:
- **PDF** (QuestPDF): Rapoarte formatate profesional cu branding universitate
- **Excel** (ClosedXML): Date tabulare cu formule și formatare
- **CSV**: Export raw pentru analize externe

**Rapoarte predefinite**:
| Raport | Format | Accesibil pentru |
|--------|--------|------------------|
| Situație școlară student | PDF | Student, Secretariat, Decan |
| Catalog note per curs | PDF, Excel | Profesor, Secretariat |
| Statistici facultate | PDF, Excel | Decan, Rector |
| Raport prezențe | Excel, CSV | Profesor, Secretariat |
| Analiza performanță | PDF | Decan, Rector |
| Export complet studenți | Excel, CSV | Secretariat, Admin |

### 14.3 Real-time Updates (SignalR)

**Evenimente live**:
- Notă nouă adăugată → Dashboard student se actualizează
- Prezență marcată → Calendar student se colorează
- Aprobare notă → Notificare instant profesor + student
- KPIs facultate → Dashboard decan refresh automat

**Implementare**:
```csharp
public class DashboardHub : Hub
{
    public async Task JoinStudentGroup(string studentId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"student-{studentId}");

    public async Task JoinFacultyGroup(string facultyId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"faculty-{facultyId}");
}

// Usage în servicii
await _hubContext.Clients.Group($"student-{studentId}")
    .SendAsync("GradeAdded", gradeDto);
```

---

## 15. Predictive Analytics & ML

### 15.1 Risk Scoring System

**Scopul**: Identificare proactivă a studenților cu risc de eșec academic sau abandon.

**Factori de risc analizați**:
| Factor | Pondere | Descriere |
|--------|---------|-----------|
| Media curentă | 25% | Sub 5.0 = risc ridicat |
| Trend note | 20% | Scădere progresivă = semnal de alarmă |
| Rata prezență | 20% | Sub 70% = risc ridicat |
| Absențe consecutive | 15% | >3 absențe consecutive = flag |
| Istoric restanțe | 10% | Număr restanțe anterioare |
| Engagement platforma | 10% | Frecvența accesării sistemului |

**Risk Levels**:
```
🟢 LOW (0-30): Student performant, fără acțiune necesară
🟡 MEDIUM (31-60): Monitorizare, notificare tutore
🟠 HIGH (61-80): Intervenție necesară, întâlnire consiliere
🔴 CRITICAL (81-100): Acțiune imediată, risc abandon/exmatriculare
```

### 15.2 Predicții disponibile

| Predicție | Model | Acuratețe țintă | Utilizatori |
|-----------|-------|-----------------|-------------|
| **Risc abandon** | Logistic Regression | >85% | Decan, Secretariat |
| **Risc picare examen** | Random Forest | >80% | Profesor, Student |
| **Estimare notă finală** | Linear Regression | ±0.5 puncte | Student |
| **Rata promovare curs** | Classification | >75% | Profesor |

### 15.3 Alerts & Notifications

**Sistem de alertare automată**:
```
Student Risk Score > 60 → Notificare tutore + secretariat
Student Risk Score > 80 → Notificare decan + email student
Absențe consecutive > 5 → Alert profesor + secretariat
Media sub 5.0 la mid-term → Warning student + părinte (opțional)
```

### 15.4 Dashboard ML (Decan/Rector)

**Widgets**:
- **Risk Distribution**: Pie chart cu distribuția studenților pe nivele de risc
- **Early Warning List**: Tabel top 20 studenți cu cel mai mare risc
- **Trend Analysis**: Evoluție risk scores pe ultimele 6 luni
- **Intervention Tracking**: Status acțiuni întreprinse pentru studenți la risc
- **Prediction Accuracy**: Metrici model (accuracy, precision, recall)

---

## 16. Entități Analytics (Model Date Extins)

### 16.1 Entități pentru Analytics

```csharp
// Snapshot zilnic pentru trend analysis
public class DailySnapshot
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? ProgramId { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal AttendanceRate { get; set; }
    public int GradesSubmitted { get; set; }
    public int GradesApproved { get; set; }
}

// Pre-calculat pentru performanță
public class GradeSnapshot
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public int AcademicYear { get; set; }
    public int Semester { get; set; }
    public decimal SemesterAverage { get; set; }
    public decimal CumulativeAverage { get; set; }
    public int TotalCredits { get; set; }
    public int PassedCredits { get; set; }
    public int FailedCourses { get; set; }
    public DateTime CalculatedAt { get; set; }
}

// Statistici prezență per curs/student
public class AttendanceStats
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; }
    public int TotalClasses { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public int ConsecutiveAbsences { get; set; }
    public DateTime LastUpdated { get; set; }
}

// ML Risk Scoring
public class StudentRiskScore
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public int OverallScore { get; set; } // 0-100
    public RiskLevel Level { get; set; }
    public decimal GradeRiskFactor { get; set; }
    public decimal AttendanceRiskFactor { get; set; }
    public decimal TrendRiskFactor { get; set; }
    public decimal EngagementRiskFactor { get; set; }
    public string? RiskFactors { get; set; } // JSON array
    public string? Recommendations { get; set; } // JSON array
    public DateTime CalculatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
}

// Engagement tracking
public class StudentEngagement
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; }
    public DateTime Date { get; set; }
    public int LoginCount { get; set; }
    public int GradesViewed { get; set; }
    public int AttendanceViewed { get; set; }
    public int DocumentsRequested { get; set; }
    public int MinutesActive { get; set; }
}

// Historical metrics pentru trend analysis
public class HistoricalMetric
{
    public Guid Id { get; set; }
    public string MetricType { get; set; } // "enrollment", "graduation", "dropout"
    public Guid? FacultyId { get; set; }
    public Guid? ProgramId { get; set; }
    public int AcademicYear { get; set; }
    public decimal Value { get; set; }
    public string? Metadata { get; set; } // JSON pentru context adițional
}
```

### 16.2 Enums Adiționale

```csharp
public enum RiskLevel
{
    Low = 0,      // 0-30
    Medium = 1,   // 31-60
    High = 2,     // 61-80
    Critical = 3  // 81-100
}

public enum MetricType
{
    Enrollment,
    Graduation,
    Dropout,
    AverageGrade,
    AttendanceRate,
    PassRate
}
```

---

*Document creat: 30 Noiembrie 2024*
*Versiune: 2.0*
*Autor: ATLAS*
