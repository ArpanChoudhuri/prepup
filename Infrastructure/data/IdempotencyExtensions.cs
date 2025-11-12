
using Infra.Data;
using Infra.models;

namespace Infra.Data
{
    public static class IdempotencyExtensions
    {
        public static async Task<bool> TryMarkProcessedAsync(this AppDbContext db, long messageId, CancellationToken ct)
        {
            db.Processed.Add(new ProcessedMessage { MessageId = messageId });
            try { await db.SaveChangesAsync(ct); return true; }
            catch { db.ChangeTracker.Clear(); return false; } // already processed
        }
    }

}
