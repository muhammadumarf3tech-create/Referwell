using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReferWell.Application.Auth;
using ReferWell.Application.Config;
using ReferWell.Application.MassComm;
using ReferWell.Application.MenuAccess;
using ReferWell.Application.Patients;
using ReferWell.Application.ReferralImport;
using ReferWell.Application.Referrals;
using ReferWell.Application.Users;

namespace ReferWell.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IMenuAccessService, MenuAccessService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IMassCommService, MassCommService>();
        services.AddScoped<IReferralImportService, ReferralImportService>();
        return services;
    }
}
