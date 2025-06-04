using APIRPM.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем EF Core DbContext
builder.Services.AddDbContext<Rpm2testContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("con")));

// 2. Подключаем MVC-controllers как Web API
builder.Services.AddControllers();

// 3. Настраиваем кэш и сессии
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Время жизни сессии (необязательно — можно оставить по умолчанию)
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Swagger (для тестирования API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Если в окружении Development — подключаем swagger-ui
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. Перенаправляем HTTP → HTTPS (если нужно)
app.UseHttpsRedirection();

// 6. Обязательно: перед контроллерами вызываем UseSession()
app.UseSession();

// 7. Если у вас появится аутентификация/авторизация — подключите и их:
// app.UseAuthentication();
// app.UseAuthorization();

// 8. Роутинг к контроллерам
app.MapControllers();

app.Run();
