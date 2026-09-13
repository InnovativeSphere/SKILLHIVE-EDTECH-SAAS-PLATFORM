using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SkillHive.Common
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates a signed JWT for the given user.
        /// academyId is null for students and superadmin.
        /// </summary>
        public string GenerateToken(int userId, string email, string role, int? academyId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
            };

            if (academyId.HasValue)
            {
                claims.Add(new Claim("academyId", academyId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Extracts the academyId claim from the current user's JWT.
        /// Returns null if the claim is missing (students and superadmin).
        /// </summary>
        public static int? GetAcademyId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("academyId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return null;

            return int.TryParse(claim, out var id) ? id : null;
        }

        /// <summary>
        /// Extracts the userId from the current user's JWT.
        /// </summary>
        public static int GetUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        /// <summary>
        /// Extracts the role from the current user's JWT.
        /// </summary>
        public static string GetRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Extracts the email from the current user's JWT.
        /// </summary>
        public static string GetEmail(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }
    }
}