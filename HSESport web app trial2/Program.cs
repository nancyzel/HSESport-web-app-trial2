using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HSESport_web_app_trial2.Data;
using HSESport_web_app_trial2.Models;
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddDbContext<MyDbContextStudents>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StudentsDBConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")))
    .AddDbContext<MyDbContextTeachers>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TeachersDBConnection") ?? throw new InvalidOperationException("Connection string 'TeacherDBConnection' not found.")));


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");
    app.UseDeveloperExceptionPage();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
