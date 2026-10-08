# EBookStoreApi

โปรเจกต์ตัวอย่างจากหลักสูตร `ebook-mssql-dotnet-crud-course.md` (ASP.NET Core 8 Web API + MS SQL Server + EF Core / ADO.NET + Bootstrap 5 UI)

## วิธีรัน

1. เตรียมฐานข้อมูล (เลือกอย่างใดอย่างหนึ่ง)
   - รันสคริปต์ `Database/01-create-ebookstore-db.sql` ใน SSMS / Azure Data Studio (มี Seed Data)
     ```bash
     sqlcmd -S localhost -E -C -i Database/01-create-ebookstore-db.sql
     ```
   - หรือใช้ EF Core Migrations (ไม่มี Seed Data)
     ```bash
     dotnet ef migrations add InitialCreate
     dotnet ef database update
     ```
2. ตรวจ `ConnectionStrings:DefaultConnection` ใน `appsettings.json` ให้ตรงกับ SQL Server ของคุณ
3. รันโปรเจกต์
   ```bash
   dotnet run --launch-profile http
   ```
4. เปิด
   - หน้าเว็บ: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - ทดสอบ API: `EBookStoreApi.http`

## API

| Method | Endpoint | คำอธิบาย |
| --- | --- | --- |
| GET | `/api/Books` | หนังสือทั้งหมด (EF Core) |
| GET | `/api/Books/search?search=&categoryId=&authorId=&pageNumber=&pageSize=` | ค้นหา + Pagination |
| GET / POST / PUT / DELETE | `/api/Books/{id}` | CRUD (EF Core) |
| GET / POST / PUT / DELETE | `/api/AdoBooks/{id}` | CRUD (ADO.NET) |
| GET | `/api/Categories`, `/api/Authors` | Lookup สำหรับ Dropdown |
