var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var approvalLimit = GetApprovalLimit();

app.MapGet("/payment/health", () => Results.Ok(new
{
    status = "UP",
    service = "payment-service",
    approvalLimit
}));

app.MapPost("/payment/authorize", (PaymentAuthorizeRequest request) =>
{
    if (request.Amount <= 0)
    {
        return Results.BadRequest(new PaymentAuthorizeResponse(
            Approved: false,
            TransactionId: null,
            Message: "Payment declined: amount must be greater than zero"));
    }

    if (request.Amount > approvalLimit)
    {
        return Results.Ok(new PaymentAuthorizeResponse(
            Approved: false,
            TransactionId: null,
            Message: $"Payment declined: amount exceeds approval limit {approvalLimit:0.00}"));
    }

    var transactionId = $"txn_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    return Results.Ok(new PaymentAuthorizeResponse(
        Approved: true,
        TransactionId: transactionId,
        Message: "Payment approved"));
});

app.Run();

static decimal GetApprovalLimit()
{
    var raw = Environment.GetEnvironmentVariable("PAYMENT_APPROVAL_LIMIT");
    return decimal.TryParse(raw, out var limit) ? limit : 500m;
}

public record PaymentAuthorizeRequest(
    string? OrderId,
    decimal Amount,
    string? Currency);

public record PaymentAuthorizeResponse(
    bool Approved,
    string? TransactionId,
    string Message);
