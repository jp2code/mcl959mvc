# MCL959MVC

## About the Project

This project is the official website and member management system for Marine Corps League Detachment 959 (MCL959). The application provides tools for roster management, event scheduling, messaging, and memorials, supporting both public visitors and registered members. The system is designed to streamline communication, record-keeping, and engagement for the organization.

## About the Organization

Marine Corps League Detachment 959 is a non-profit veterans organization dedicated to supporting Marines, FMF Corpsmen, and their families. The detachment promotes camaraderie, community service, and the preservation of Marine Corps traditions.

## Technologies Used

- **.NET 8** (ASP.NET Core)
- **Razor Pages** and **MVC**
- **Entity Framework Core** (SQL Server)
- **Identity** for authentication and authorization
- **Bootstrap** for responsive UI
- **Mailgun** for email delivery
- **jQuery** for client-side interactivity

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or use the provided connection string for remote DB)
- [Git](https://git-scm.com/)

### Setup Instructions

1. **Clone the repository:**
`git clone https://github.com/jp2code/mcl959mvc.git`
   - Or download the ZIP file and extract it.

2. **Configure secrets:**
- Copy `appsettings.Secrets.json.example` to `appsettings.Secrets.json` (if provided), or create your own.
- Fill in sensitive values (database connection, SMTP credentials, API keys).

3. **Restore NuGet packages:**
`dotnet restore`

4. **Apply database migrations (if needed):**
`dotnet ef database update`

5. **Run the application:**
`dotnet run`

The site will be available at `https://localhost:7180/` by default.

### Development Notes

- Use `git add .`, `git commit -m "message"`, and `git push` to manage changes.
- The project uses both MVC controllers and Razor Pages (for Identity).
- Static files (images, uploads) are served from the `wwwroot` directory.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for review.

---

For more information, visit [mcl959.com](https://www.mcl959.com).