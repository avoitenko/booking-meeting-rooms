using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;


namespace BookingMeetingRooms.Api.Filters;

public class SwaggerHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        // Додаємо заголовок X-UserId до всіх операцій
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-UserId",
            In = ParameterLocation.Header,
            Description = "User ID для авторизації (обов'язково). Приклад: 00000000-0000-0000-0000-000000000001",
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        });

        // Додаємо заголовок X-Role до всіх операцій
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Role",
            In = ParameterLocation.Header,
            Description = "Роль користувача: Employee або Admin (опційно, але потрібно для адмін-операцій)",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}
