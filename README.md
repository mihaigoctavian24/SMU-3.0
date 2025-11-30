# SMU 3.0 - Student Management University

<div align="center">

![SMU Logo](docs/assets/logo-placeholder.png)

**Sistem Modern de Management Universitar**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://blazor.net/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4-38B2AC?logo=tailwindcss)](https://tailwindcss.com/)
[![Supabase](https://img.shields.io/badge/Supabase-PostgreSQL-3ECF8E?logo=supabase)](https://supabase.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

[Demo](https://smu3.azurewebsites.net) | [Documentatie](docs/) | [PRD](PRD.md) | [Raportare Bug](issues)

</div>

---

## Despre Proiect

SMU 3.0 este un sistem complet de management universitar construit cu tehnologii moderne. Aplicatia ofera o interfata eleganta si intuitiva pentru gestionarea studentilor, notelor, prezentei si cererilor administrative.

### Caracteristici Principale

- **Multi-rol**: Student, Profesor, Secretariat, Decan, Rector, Administrator
- **Dashboard Personalizat**: Fiecare rol vede informatii relevante
- **Catalog Note**: Sistem complet cu workflow de aprobare
- **Evidenta Prezenta**: Tracking automat cu statistici
- **Cereri Administrative**: Workflow digital pentru adeverinte
- **Notificari Real-time**: Alerte instantanee prin SignalR
- **Design Modern**: UI/UX elegant cu Tailwind CSS

## Stack Tehnologic

| Component | Tehnologie |
|-----------|------------|
| **Frontend** | Blazor Server + Tailwind CSS |
| **Backend** | ASP.NET Core 8.0 |
| **Baza de Date** | Supabase PostgreSQL |
| **ORM** | Entity Framework Core 8.0 |
| **Autentificare** | ASP.NET Identity |
| **Icons** | Lucide Icons |
| **Deployment** | Azure App Service |

## Quick Start

### Cerinte

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (pentru Tailwind)
- [PostgreSQL](https://www.postgresql.org/) sau cont [Supabase](https://supabase.com/)

### Instalare

```bash
# Cloneaza repository-ul
git clone https://github.com/your-username/SMU-3.0.git
cd SMU-3.0

# Restaureaza dependentele .NET
dotnet restore

# Instaleaza dependentele Node (pentru Tailwind)
npm install

# Configureaza connection string-ul
cp appsettings.Example.json appsettings.Development.json
# Editeaza appsettings.Development.json cu datele tale Supabase

# Aplica migrarile EF Core
dotnet ef database update

# Porneste aplicatia
dotnet run
```

Aplicatia va fi disponibila la `https://localhost:5001`

### Conturi Demo

| Rol | Email | Parola |
|-----|-------|--------|
| Admin | admin@smu.edu | Admin123! |
| Decan | decan@smu.edu | Decan123! |
| Profesor | profesor@smu.edu | Prof123! |
| Secretariat | secretar@smu.edu | Secr123! |
| Student | student@smu.edu | Stud123! |

## Structura Proiect

```
SMU-3.0/
├── src/
│   └── SMU.Web/                 # Aplicatie Blazor Server
│       ├── Components/          # Componente Blazor reutilizabile
│       │   ├── Layout/          # MainLayout, NavMenu, Header
│       │   ├── Shared/          # Componente comune
│       │   └── Pages/           # Pagini pe module
│       ├── Data/                # DbContext, Entities, Configurations
│       ├── Services/            # Business logic services
│       ├── wwwroot/             # Static files, CSS
│       └── Program.cs           # Entry point
├── tests/
│   ├── SMU.UnitTests/           # Unit tests
│   └── SMU.IntegrationTests/    # Integration tests
├── docs/                        # Documentatie
├── .github/workflows/           # CI/CD pipelines
├── PRD.md                       # Product Requirements Document
└── README.md                    # Acest fisier
```

## Module

### Dashboard
Vizualizare personalizata pe rol cu:
- Statistici relevante
- Actiuni rapide
- Notificari recente
- Calendar evenimente

### Catalog Note
- Adaugare/editare note (Profesor)
- Workflow aprobare (Decan)
- Vizualizare note proprii (Student)
- Export PDF/Excel

### Evidenta Prezenta
- Marcare prezenta (Profesor)
- Justificare absente (Student)
- Rapoarte prezenta
- Alerte prag absente

### Cereri Administrative
- Creare cereri (Student)
- Procesare cereri (Secretariat)
- Aprobare cereri (Decan)
- Istoric si tracking

### Administrare
- Gestionare utilizatori
- Gestionare facultati/programe
- Configurare sistem
- Audit logs

## Documentatie

- [Arhitectura Tehnica](docs/ARCHITECTURE.md)
- [Ghid API](docs/API.md)
- [Ghid Deployment](docs/DEPLOYMENT.md)
- [PRD Complet](PRD.md)

## Contribuie

Contributiile sunt binevenite! Vezi [CONTRIBUTING.md](CONTRIBUTING.md) pentru ghidul complet.

```bash
# Fork repository
# Creaza branch pentru feature
git checkout -b feature/nume-feature

# Commit schimbari
git commit -m "Add: descriere feature"

# Push la fork
git push origin feature/nume-feature

# Deschide Pull Request
```

## Roadmap

- [x] PRD si Arhitectura
- [ ] Setup proiect Blazor Server
- [ ] Integrare Supabase PostgreSQL
- [ ] Sistem autentificare
- [ ] Module Dashboard
- [ ] Module Catalog Note
- [ ] Module Prezenta
- [ ] Module Cereri
- [ ] Real-time Notifications
- [ ] Deployment Azure

## Licenta

Distribuit sub licenta MIT. Vezi [LICENSE](LICENSE) pentru detalii.

## Contact

- **Autor**: [Nume]
- **Email**: contact@smu.edu
- **Project Link**: [https://github.com/your-username/SMU-3.0](https://github.com/your-username/SMU-3.0)

---

<div align="center">
Construit cu pasiune pentru educatie
</div>
