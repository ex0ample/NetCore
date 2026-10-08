using Microsoft.EntityFrameworkCore;
using EBookStoreApi.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. เพิ่ม Controllers
builder.Services.AddControllers();

// 2. ลงทะเบียน DbContext เข้าสู่ Dependency Injection Container
builder.Services.AddDbContext<EBookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. ตั้งค่า Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. เพิ่ม CORS สำหรับกรณีแยก Frontend รันคนละ Origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5500",     // VS Code Live Server
                "http://127.0.0.1:5500",
                "http://localhost:5173",     // Vite Dev Server
                "http://localhost:3000"      // React / Next.js
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ใช้งาน Swagger บน Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// เปิดให้เสิร์ฟ index.html เมื่อเปิดเข้ามาที่ Root URL (/) — ต้องเรียกก่อน UseStaticFiles
app.UseDefaultFiles();

// เปิดให้เสิร์ฟ Static Files จากโฟลเดอร์ wwwroot
app.UseStaticFiles();

// เปิดใช้งาน CORS Middleware (ต้องวางไว้ก่อน app.MapControllers())
app.UseCors("AllowFrontendApp");

app.UseAuthorization();
app.MapControllers();

app.Run();
