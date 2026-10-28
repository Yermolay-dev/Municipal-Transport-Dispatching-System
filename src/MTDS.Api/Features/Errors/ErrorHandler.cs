using Microsoft.AspNetCore.Mvc;
using MTDS.Api.Features.Errors;
namespace MTDS.Api.Features.Errors;

class ErrorHandler
{
    public static IActionResult ForCode(int code)
    {
        switch (code)
        {
            case 400:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 400,
                        Status = "VALIDATION_ERROR",
                        Title = "Проверьте введеные данные"
                    });
                }
            case 401:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 401,
                        Status = "AUTH_REQUIRED",
                        Title = "Войдите в систему чтобы продолжить"
                    });
                }
            case 403:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 403,
                        Status = "FORBIDDEN",
                        Title = "Недостаточно прав для этого действия"
                    });
                }
            case 404:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 404,
                        Status = "NOT_FOUND",
                        Title = "Данные не найдены"
                    });
                }
            case 405:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 405,
                        Status = "VALIDATION_ERROR",
                        Title = "Метод не поддерживается"
                    });
                }
            case < 500:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = 500,
                        Status = "VALIDATION_ERROR",
                        Title = "Проверьте введеные данные"
                    });
                }
            default:
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Code = code,
                        Status = "SERVER_ERROR",
                        Title = "Ошибка на сервере. Повторите попытку позже"
                    });

                }
        }


    }
}