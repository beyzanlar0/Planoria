using Microsoft.Extensions.Logging;
using Planoria.Data;
using Planoria.Services;
namespace Planoria;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
        builder.Services.AddDbContext<AppDbContext>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddDbContext<AppDbContext>();
        builder.Services.AddScoped<TaskService>();
        return builder.Build();
	}
}
