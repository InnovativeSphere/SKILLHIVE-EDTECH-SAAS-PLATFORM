using Microsoft.AspNetCore.Mvc;

namespace SkillHive.Common
{
    public static class ApiResponse
    {
        public static IActionResult Success(object? data, string message = "Success")
        {
            return new OkObjectResult(new
            {
                success = true,
                statusCode = 200,
                message,
                data
            });
        }

        public static IActionResult Created(object? data, string message = "Created")
        {
            return new ObjectResult(new
            {
                success = true,
                statusCode = 201,
                message,
                data
            })
            {
                StatusCode = 201
            };
        }

        public static IActionResult BadRequest(string message = "Bad Request", object? data = null)
        {
            return new BadRequestObjectResult(new
            {
                success = false,
                statusCode = 400,
                message,
                data
            });
        }

        public static IActionResult Unauthorized(string message = "Unauthorized", object? data = null)
        {
            return new UnauthorizedObjectResult(new
            {
                success = false,
                statusCode = 401,
                message,
                data
            });
        }

        public static IActionResult Forbidden(string message = "Forbidden", object? data = null)
        {
            return new ObjectResult(new
            {
                success = false,
                statusCode = 403,
                message,
                data
            })
            {
                StatusCode = 403
            };
        }

        public static IActionResult NotFound(string message = "Not Found", object? data = null)
        {
            return new NotFoundObjectResult(new
            {
                success = false,
                statusCode = 404,
                message,
                data
            });
        }

        public static IActionResult Error(string message = "Internal Server Error", object? data = null)
        {
            return new ObjectResult(new
            {
                success = false,
                statusCode = 500,
                message,
                data
            })
            {
                StatusCode = 500
            };
        }

        public static IActionResult Paginated(
            object items,
            int totalRecords,
            int pageNumber,
            int pageSize,
            string message = "Success")
        {
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return new OkObjectResult(new
            {
                success = true,
                statusCode = 200,
                message,
                data = new
                {
                    items,
                    pagination = new
                    {
                        totalRecords,
                        pageNumber,
                        pageSize,
                        totalPages
                    }
                }
            });
        }
    }
}