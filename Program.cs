using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

// Service Registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WalletDb"));

builder.Services.AddScoped<AuditNotificationService>();

var app = builder.Build();
app.UseExceptionHandler();

var transferLock = new object();

// Database Initialization & Seed Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Wallets.Any())
    {
        var w1 = new Wallet { Id = 1, OwnerName = "Alice", Balance = 500 };
        var w2 = new Wallet { Id = 2, OwnerName = "Bob", Balance = 100 };
        db.Wallets.AddRange(w1, w2);

        db.Transactions.AddRange(
            new Transaction { Id = 1, WalletId = 1, Amount = 100, Type = "Deposit", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Transaction { Id = 2, WalletId = 1, Amount = 50, Type = "Withdraw", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Transaction { Id = 3, WalletId = 2, Amount = 100, Type = "Deposit", CreatedAt = DateTime.UtcNow.AddDays(-3) }
        );
        db.SaveChanges();
    }
}

// -------------------------------------------------------------
// Transaction History
// -------------------------------------------------------------
app.MapGet("/api/wallets/{id}/transactions", async (int id, AppDbContext db) =>
{
    var wallet = await db.Wallets
        .AsNoTracking()
        .FirstOrDefaultAsync(w => w.Id == id);

    if (wallet == null)
        return Results.NotFound("Wallet not found.");

    var transactions = await db.Transactions
        .AsNoTracking()
        .Where(t => t.WalletId == id)
        .Select(t => new
        {
            t.Id,
            t.Amount,
            t.Type,
            t.CreatedAt,
            WalletOwner = wallet.OwnerName
        })
        .ToListAsync();

    return Results.Ok(transactions);
});
// -------------------------------------------------------------
// Fund Transfer
// -------------------------------------------------------------
app.MapPost("/api/wallets/transfer", async ([FromBody] TransferRequest request, AppDbContext db, AuditNotificationService auditor) =>
{
    // Validate transfer amount
    if (request.Amount <= 0)
    {
        return Results.BadRequest(new
        {
            Message = "Transfer amount must be greater than zero."
        });
    }

    // Prevent transferring to the same wallet
    if (request.SenderWalletId == request.ReceiverWalletId)
    {
        return Results.BadRequest(new
        {
            Message = "Sender and receiver wallets must be different."
        });
    }

    var sender = await db.Wallets.FindAsync(request.SenderWalletId);
    var receiver = await db.Wallets.FindAsync(request.ReceiverWalletId);

    if (sender == null || receiver == null)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid sender or receiver wallet."
        });
    }

    lock (transferLock)
    {
        // Check balance inside the lock
        if (sender.Balance < request.Amount)
        {
            return Results.BadRequest(new
            {
                Message = "Insufficient funds."
            });
        }

        // Update balances
        sender.Balance -= request.Amount;
        receiver.Balance += request.Amount;

        // Record outgoing transaction
        db.Transactions.Add(new Transaction
        {
            WalletId = sender.Id,
            Amount = -request.Amount,
            Type = "TransferOut",
            CreatedAt = DateTime.UtcNow
        });

        // Record incoming transaction
        db.Transactions.Add(new Transaction
        {
            WalletId = receiver.Id,
            Amount = request.Amount,
            Type = "TransferIn",
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
    await auditor.SendAuditLogAsync(
        $"Transferred {request.Amount} from Wallet {sender.Id} to {receiver.Id}");

    return Results.Ok(new
        {
            Message = "Transfer successful",
            SenderBalance = sender.Balance
        });
    
});

app.Run();

// -------------------------------------------------------------
// Data Context & Models
// -------------------------------------------------------------
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
}

public class Wallet
{
    public int Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public record TransferRequest(int SenderWalletId, int ReceiverWalletId, decimal Amount);

// -------------------------------------------------------------
// Infrastructure Services
// -------------------------------------------------------------
public class AuditNotificationService
{
    public async Task SendAuditLogAsync(string message)
    {
        await Task.Delay(50);

        Console.WriteLine($"[AUDIT LOGGED]: {message}");
    }
}