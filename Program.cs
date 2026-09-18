using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Service Registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WalletDb"));

builder.Services.AddSingleton<AuditNotificationService>();

var app = builder.Build();

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
    var sender = await db.Wallets.FindAsync(request.SenderWalletId);
    var receiver = await db.Wallets.FindAsync(request.ReceiverWalletId);

    if (sender == null || receiver == null)
        return Results.BadRequest("Invalid sender or receiver wallet.");

    if (sender.Balance >= request.Amount)
    {
        // Simulate background processing latency
        await Task.Delay(100);

        sender.Balance -= request.Amount;
        receiver.Balance += request.Amount;

        db.Transactions.Add(new Transaction
        {
            WalletId = sender.Id,
            Amount = -request.Amount,
            Type = "TransferOut",
            CreatedAt = DateTime.UtcNow
        });

        db.Transactions.Add(new Transaction
        {
            WalletId = receiver.Id,
            Amount = request.Amount,
            Type = "TransferIn",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        // Dispatch audit notification
        auditor.SendAuditLogSync($"Transferred {request.Amount} from Wallet {sender.Id} to {receiver.Id}");

        return Results.Ok(new { Message = "Transfer successful", SenderBalance = sender.Balance });
    }

    return Results.BadRequest("Insufficient funds.");
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
    private readonly IServiceProvider _serviceProvider;

    public AuditNotificationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SendAuditLogSync(string message)
    {
        Task.Run(async () =>
        {
            await Task.Delay(50);
            Console.WriteLine($"[AUDIT LOGGED]: {message}");
        }).Wait();
    }
}