using APIRPM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace APIRPM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly Rpm2testContext _db;

        public HomeController(Rpm2testContext rpm2Context)
        {
            _db = rpm2Context;
        }

        // =================== УТИЛИТЫ ===================

        // Хеширование SHA256
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        // Валидация телефонного номера
        public static bool IsPhoneNumberValid(string phoneNumber)
        {
            string pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        // =================== 1) GET: Список всех товаров ===================
        // GET /api/Home
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatalogDto>>> Index()
        {
            var catalogs = await _db.Catalogs
                .Select(c => new CatalogDto
                {
                    IdProduct = c.IdProduct,
                    ProductName = c.ProductName,
                    Description = c.Description,
                    Price = c.Price,
                    Weight = c.Weight,
                    Stock = c.Stock,
                    CategoryName = c.CategoryName,
                    PathToImage = c.PathToImage,
                    // Обратите внимание: без Reviews, без вычисления рейтинга/количества
                    ReviewCount = c.Reviews.Count(r => r.Rating.HasValue),
                    AverageRating = c.Reviews.Any(r => r.Rating.HasValue)
                        ? c.Reviews
                            .Where(r => r.Rating.HasValue)
                            .Select(r => (decimal)r.Rating!.Value)
                            .Average()
                        : 0m

                })
                .ToListAsync();

            // Если нужно пометить IsInCart (для залогиненного пользователя), делаем 2-й проход:
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId != null)
            {
                var cartProductIds = await _db.Carts
                    .Where(c => c.UserId == userId.Value)
                    .Select(c => c.ProductId)
                    .ToListAsync();

                foreach (var dto in catalogs)
                {
                    dto.IsInCart = cartProductIds.Contains(dto.IdProduct);
                }
            }

            return Ok(catalogs);
        }

        // =================== 2) GET: Фильтрация/поиск/сортировка каталога ===================
        // GET /api/Home/Catalog?filter=...&search=...&sort=...
        [HttpGet("Catalog")]
        public async Task<ActionResult<IEnumerable<CatalogDto>>> Catalog(
            [FromQuery] string? filter,
            [FromQuery] string? search,
            [FromQuery] string? sort)
        {
            var query = _db.Catalogs.Include(c => c.Reviews).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
                query = query.Where(c => c.CategoryName == filter);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.ProductName.Contains(search) || c.Description!.Contains(search));

            if (!string.IsNullOrWhiteSpace(sort))
            {
                query = sort switch
                {
                    "price-asc" => query.OrderBy(c => c.Price),
                    "price-desc" => query.OrderByDescending(c => c.Price),
                    _ => query
                };
            }

            var catalogList = await query
                .Select(c => new CatalogDto
                {
                    IdProduct = c.IdProduct,
                    ProductName = c.ProductName,
                    Description = c.Description,
                    Price = c.Price,
                    Weight = c.Weight,
                    Stock = c.Stock,
                    CategoryName = c.CategoryName,
                    PathToImage = c.PathToImage,
                    ReviewCount = c.Reviews.Count(r => r.Rating.HasValue),
                    AverageRating = c.Reviews.Any(r => r.Rating.HasValue)
                    ? c.Reviews
                        .Where(r => r.Rating.HasValue)
                        .Select(r => (decimal)r.Rating!.Value)
                        .Average()
                    : 0m
                })
                .ToListAsync();

            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId != null)
            {
                var cartProductIds = await _db.Carts
                    .Where(c => c.UserId == userId.Value)
                    .Select(c => c.ProductId)
                    .ToListAsync();

                foreach (var dto in catalogList)
                    dto.IsInCart = cartProductIds.Contains(dto.IdProduct);
            }

            return Ok(catalogList);
        }

        // =================== 3) GET: Отзывы для товара ===================
        // GET /api/Home/Reviews/{id}
        [HttpGet("Reviews/{id:int}")]
        public async Task<ActionResult<ProductReviewsViewModelDto>> Reviews(int id)
        {
            var product = await _db.Catalogs
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProduct == id);
            if (product == null)
                return NotFound(new { message = "Товар не найден." });

            var reviews = await _db.Reviews
                .Where(r => r.ProductId == id)
                .Select(r => new ReviewDto
                {
                    IdReview = r.IdReview,
                    UserId = r.UserId,
                    ProductId = r.ProductId,
                    Rating = r.Rating ?? 0,                       // r.Rating — int?
                    ReviewText = r.ReviewText,
                    CreatedDate = r.CreatedDate,
                    FirstName = r.User.FirstName,
                    LastName = r.User.LastName
                })
                .ToListAsync();

            var model = new ProductReviewsViewModelDto
            {
                ProductId = product.IdProduct,
                ProductName = product.ProductName,
                Reviews = reviews
            };

            return Ok(model);
        }

        // =================== 4) POST: Добавить отзыв ===================
        // POST /api/Home/AddReview
        [HttpPost("AddReview")]
        public async Task<ActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            bool productExists = await _db.Catalogs.AnyAsync(p => p.IdProduct == request.ProductId);
            if (!productExists)
                return NotFound(new { message = "Товар не найден." });

            var review = new Review
            {
                UserId = userId.Value,
                ProductId = request.ProductId,
                Rating = request.Rating,
                ReviewText = request.ReviewText,
                CreatedDate = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Reviews), new { id = request.ProductId }, null);
        }

        // =================== 5) POST: Регистрация пользователя ===================
        // POST /api/Home/Register
        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Пароли не совпадают." });

            if (!IsPhoneNumberValid(request.PhoneNumber))
                return BadRequest(new { message = "Неверный номер телефона." });

            bool alreadyExists = await _db.Users
                .AnyAsync(u => u.Email == request.Email || u.Phone == request.PhoneNumber);
            if (alreadyExists)
                return BadRequest(new { message = "Пользователь с таким email или номером телефона уже зарегистрирован." });

            string hashedPassword = ComputeSha256Hash(request.Password);

            var user = new User
            {
                Phone = request.PhoneNumber,
                Email = request.Email,
                Password = hashedPassword,
                RoleId = 2, // по умолчанию “обычный” пользователь
                RegistrationDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Profile), new { id = user.IdUser }, new
            {
                user.IdUser,
                user.Email,
                user.Phone,
                user.RoleId
            });
        }

        // =================== 6) POST: Логин ===================
        // POST /api/Home/Login
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound(new { message = "Пользователь с таким email не найден." });

            string hashedPassword = ComputeSha256Hash(request.Password);
            if (hashedPassword != user.Password)
                return BadRequest(new { message = "Неверный пароль." });

            HttpContext.Session.SetInt32("UserID", user.IdUser);
            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetInt32("RoleID", user.RoleId);

            return Ok(new
            {
                user.IdUser,
                user.Email,
                user.Phone,
                user.RoleId
            });
        }

        // =================== 7) GET: Профиль текущего пользователя ===================
        // GET /api/Home/Profile
        [HttpGet("Profile")]
        public async Task<ActionResult<UserProfileDto>> Profile()
        {
            var isAuth = HttpContext.Session.GetString("IsAuthenticated");
            if (isAuth != "true")
                return Unauthorized(new { message = "Не авторизован." });

            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Неизвестный пользователь." });

            var user = await _db.Users
                .AsNoTracking()
                .Select(u => new UserProfileDto
                {
                    IdUser = u.IdUser,
                    Email = u.Email,
                    Phone = u.Phone,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Address = u.Address,
                    RegistrationDate = u.RegistrationDate,
                    RoleId = u.RoleId
                })
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден." });

            return Ok(user);
        }

        // =================== 8) POST: Logout ===================
        // POST /api/Home/Logout
        [HttpPost("Logout")]
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Вы успешно вышли." });
        }

        // =================== 9) GET: EasyData (только для RoleId == 1) ===================
        // GET /api/Home/EasyData
        [HttpGet("EasyData")]
        public async Task<ActionResult> EasyData()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUser == userId.Value);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден." });

            if (user.RoleId == 1)
            {
                return Ok(new { message = "Добро пожаловать в EasyData, администратор!" });
            }
            else
            {
                return Forbid();
            }
        }

        // =================== 10) GET: Корзина текущего пользователя ===================
        // GET /api/Home/Cart
        [HttpGet("Cart")]
        public async Task<ActionResult<IEnumerable<CartItemDto>>> Cart()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            var cartItems = await _db.Carts
                .Where(c => c.UserId == userId.Value)
                .Include(c => c.Product)
                .Select(c => new CartItemDto
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product.ProductName,
                    Description = c.Product.Description,
                    PathToImage = c.Product.PathToImage,
                    Stock = c.Product.Stock ?? 0,
                    Quantity = c.Quantity ?? 0,
                    Price = c.Product.Price,
                    Total = (c.Quantity ?? 0) * c.Product.Price
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        // =================== 11) POST: Добавить товар в корзину ===================
        // POST /api/Home/AddToCart
        [HttpPost("AddToCart")]
        public async Task<ActionResult> AddToCart(AddToCartRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            bool productExists = await _db.Catalogs.AnyAsync(p => p.IdProduct == request.ProductId);
            if (!productExists)
                return NotFound(new { message = "Товар не найден." });

            bool alreadyInCart = await _db.Carts
                .AnyAsync(c => c.UserId == userId.Value && c.ProductId == request.ProductId);
            if (alreadyInCart)
                return Conflict(new { message = "Товар уже в корзине." });

            var cartItem = new Cart
            {
                UserId = userId.Value,
                ProductId = request.ProductId,
                Quantity = request.Quantity > 0 ? request.Quantity : 1
            };

            _db.Carts.Add(cartItem);
            await _db.SaveChangesAsync();

            return Created(string.Empty, new { message = "Товар добавлен в корзину." });
        }

        // =================== 12) PUT: Обновить количество в корзине ===================
        // PUT /api/Home/UpdateCartQuantity
        [HttpPut("UpdateCartQuantity")]
        public async Task<ActionResult> UpdateCartQuantity([FromBody] UpdateCartRequest request)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            var cartItem = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == request.ProductId);
            if (cartItem == null)
                return NotFound(new { message = "Элемент корзины не найден." });

            cartItem.Quantity = request.Quantity;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Количество обновлено." });
        }

        // =================== 13) DELETE: Удалить товар из корзины ===================
        // DELETE /api/Home/RemoveCartItem/{productId}
        [HttpDelete("RemoveCartItem/{productId:int}")]
        public async Task<ActionResult> RemoveCartItem(int productId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            var cartItem = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);
            if (cartItem == null)
                return NotFound(new { message = "Элемент корзины не найден." });

            _db.Carts.Remove(cartItem);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Товар удалён из корзины." });
        }

        // =================== 14) POST: Оформление заказа ===================
        // POST /api/Home/Order
        [HttpPost("Order")]
        public ActionResult Order()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized(new { message = "Не авторизован." });

            // Здесь может быть любая логика создания заказа
            return Ok(new { message = "Order endpoint: реализуйте свою логику оформления." });
        }

        // =================== 15) GET: О нас (AboutUs) ===================
        // GET /api/Home/AboutUs
        [HttpGet("AboutUs")]
        public ActionResult AboutUs()
        {
            var info = new
            {
                Company = "Ваша компания",
                Description = "Немного информации о нас...",
                Contact = "contact@company.com"
            };
            return Ok(info);
        }

        // =================== 16) GET: Политика конфиденциальности ===================
        // GET /api/Home/Privacy
        [HttpGet("Privacy")]
        public ActionResult Privacy()
        {
            var text = "Политика конфиденциальности вашего приложения...";
            return Ok(new { PrivacyPolicy = text });
        }

        // =================== 17) GET: Обработка ошибок ===================
        // GET /api/Home/Error
        [HttpGet("Error")]
        public ActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return Problem(detail: "Произошла ошибка на сервере.", instance: requestId);
        }
    }

    #region ==== DTO-классы для запросов/ответов ====

    // DTO для отображения товара в каталоге
    public class CatalogDto
    {
        public int IdProduct { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int? Weight { get; set; }
        public int? Stock { get; set; }
        public string? CategoryName { get; set; }
        public string? PathToImage { get; set; }

        // Вычисляемые поля
        public int ReviewCount { get; set; }      // общее количество рейтингованных отзывов
        public decimal AverageRating { get; set; } // средний рейтинг (0, если нет отзывов)
        public bool IsInCart { get; set; }        // заполняется отдельно, если пользователь залогинен
    }

    // DTO для одиночного отзыва с именем/фамилией пользователя
    public class ReviewDto
    {
        public int IdReview { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }         // r.Rating может быть null, но в DTO кладём 0 по умолчанию
        public string? ReviewText { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? FirstName { get; set; }  // подтягивается через r.User.FirstName
        public string? LastName { get; set; }   // через r.User.LastName
    }

    // ViewModel-DTO для ответа GET /Reviews/{id}
    public class ProductReviewsViewModelDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    // DTO для запроса добавления нового отзыва
    public class AddReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; } = null!;
    }

    // DTO для регистрации
    public class RegisterRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    // DTO для логина
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    // DTO для профиля пользователя (ответ на GET /Profile)
    public class UserProfileDto
    {
        public int IdUser { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public int RoleId { get; set; }
    }

    // DTO для одного элемента корзины (ответ GET /Cart)
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string PathToImage { get; set; } = null!;
        public int Stock { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    // DTO для запроса “добавить в корзину”
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // DTO для запроса “обновить количество”
    public class UpdateCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}
