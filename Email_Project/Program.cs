using Email_Project.Context;
using Email_Project.Entities;
using Email_Project.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EmailContext>();

builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<EmailContext>().AddErrorDescriber<CustomIdentityValidator>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/UserLogin"; 
    options.AccessDeniedPath = "/Login/UserLogin";
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Data Section
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
    if (!context.Categories.Any())
    {
        context.Categories.AddRange(new List<Category>
        {
            new Category { CategoryName = "Education", Keywords = "exam,lesson,homework,midterm,final,school,university" },
            new Category { CategoryName = "Finance", Keywords = "invoice,payment,debt,receipt,statement,bank,credit" },
            new Category { CategoryName = "Social", Keywords = "follow,like,comment,friend,invite,event" },
            new Category { CategoryName = "Promotions", Keywords = "discount,opportunity,campaign,buy,free,deal" }
        });
        context.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
