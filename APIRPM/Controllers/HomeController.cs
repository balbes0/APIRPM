using APIRPM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using APIRPM.Models.JWT;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace APIRPM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly Rpm2testContext _db;
        private JwtSettings _jwtSettings;
        public HomeController(IOptions<JwtSettings> jwtSettings,Rpm2testContext rpm2Context)
        {
            _db = rpm2Context;
            _jwtSettings = jwtSettings.Value;
        }

        public static bool IsPhoneNumberValid(string phoneNumber)
        {
            string pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

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

                foreach (var dto in catalogs)
                {
                    dto.IsInCart = cartProductIds.Contains(dto.IdProduct);
                }
            }

            return Ok(catalogs);
        }

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
                    Rating = r.Rating ?? 0,
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

        [Authorize]
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

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Phone = request.PhoneNumber,
                Email = request.Email,
                Password = hashedPassword,
                RoleId = 2,
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

        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await AuthenticateUser(request.Email, request.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            HttpContext.Session.SetInt32("UserID", user.IdUser);
            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetInt32("RoleID", user.RoleId);

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                user.IdUser,
                user.Email,
                user.Phone,
                user.RoleId,
                token
            });
        }

        private async Task<User> AuthenticateUser(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u=> u.Email == email);

            if (user == null || !user.VerifyPassword(password))
            {
                return null;
            }

            return user;
        }

        [Authorize]
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

        [Authorize]
        [HttpPost("Logout")]
        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Вы успешно вышли." });
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [HttpGet("Error")]
        public ActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return Problem(detail: "Произошла ошибка на сервере.", instance: requestId);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Role", user.RoleId.ToString() ?? "Guest")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.TokenLifetimeMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    #region ==== DTO-классы для запросов/ответов ====

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

        public int ReviewCount { get; set; }   
        public decimal AverageRating { get; set; } 
        public bool IsInCart { get; set; }     
    }

    public class ReviewDto
    {
        public int IdReview { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }       
        public string? ReviewText { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
    }

    public class ProductReviewsViewModelDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class AddReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; } = null!;
    }

    public class RegisterRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

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

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}
