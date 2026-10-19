using Library.Application.Common.Interfaces;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories;
using Library.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString = "Data Source=library.db")
    {
        services.AddDbContext<LibraryDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookCopyRepository, BookCopyRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IFineRepository, FineRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        // Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IBarcodeGenerator, BarcodeGenerator>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IPdfService, PdfService>();
        var dbFilePath = connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim().Trim('"');
        services.AddSingleton<IBackupService>(sp => new BackupService(dbFilePath));
        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
