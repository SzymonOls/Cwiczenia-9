internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        
        var app = builder.Build();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            Console.WriteLine("Mapped endpoints:");
        });
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}