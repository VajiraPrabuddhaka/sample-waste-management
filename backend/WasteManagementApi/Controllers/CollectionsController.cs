using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.DTOs;
using WasteManagementApi.Models;
using WasteManagementApi.Telemetry;

namespace WasteManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly WasteManagementDbContext _db;
    private readonly ILogger<CollectionsController> _logger;
    private readonly WasteManagementMetrics _metrics;

    public CollectionsController(WasteManagementDbContext db, ILogger<CollectionsController> logger, WasteManagementMetrics metrics)
    {
        _db = db;
        _logger = logger;
        _metrics = metrics;
    }

    private static CollectionSummary ToSummary(Collection c) =>
        new(c.Id, c.BinId, c.Bin?.Name ?? "", c.TruckId, c.Truck?.Name ?? "",
            c.Status.ToString(), c.ScheduledAt, c.StartedAt, c.CompletedAt,
            c.FillLevelAtCollection, c.Notes);

    [HttpGet]
    public async Task<ActionResult<IList<CollectionSummary>>> GetCollections([FromQuery] string? status = null)
    {
        var query = _db.Collections.Include(c => c.Bin).Include(c => c.Truck).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CollectionStatus>(status, true, out var collStatus))
            query = query.Where(c => c.Status == collStatus);

        var collections = await query.OrderByDescending(c => c.ScheduledAt).ToListAsync();

        _logger.LogInformation("GetCollections: Status={Status}, Count={Count}", status, collections.Count);
        return Ok(collections.Select(ToSummary).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CollectionSummary>> GetCollection(int id)
    {
        var collection = await _db.Collections
            .Include(c => c.Bin)
            .Include(c => c.Truck)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (collection is null)
        {
            _logger.LogWarning("GetCollection: Collection {CollectionId} not found", id);
            return NotFound();
        }

        return Ok(ToSummary(collection));
    }

    [HttpPost]
    public async Task<ActionResult<CollectionSummary>> ScheduleCollection([FromBody] ScheduleCollectionRequest request)
    {
        var binExists = await _db.WasteBins.AnyAsync(b => b.Id == request.BinId);
        if (!binExists) return BadRequest("Bin not found");

        var truckExists = await _db.Trucks.AnyAsync(t => t.Id == request.TruckId);
        if (!truckExists) return BadRequest("Truck not found");

        var collection = new Collection
        {
            BinId = request.BinId,
            TruckId = request.TruckId,
            Status = CollectionStatus.Scheduled,
            ScheduledAt = request.ScheduledAt,
            Notes = request.Notes
        };

        _db.Collections.Add(collection);
        await _db.SaveChangesAsync();

        await _db.Entry(collection).Reference(c => c.Bin).LoadAsync();
        await _db.Entry(collection).Reference(c => c.Truck).LoadAsync();

        _metrics.RecordCollectionScheduled();
        _logger.LogInformation("Collection scheduled: CollectionId={CollectionId}, BinId={BinId}, TruckId={TruckId}, ScheduledAt={ScheduledAt}",
            collection.Id, request.BinId, request.TruckId, request.ScheduledAt);

        using var activity = WasteManagementActivitySource.StartCollectionSchedule(request.BinId, request.TruckId, request.ScheduledAt);
        return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, ToSummary(collection));
    }

    [HttpPut("{id:int}/start")]
    public async Task<IActionResult> StartCollection(int id)
    {
        var collection = await _db.Collections.FindAsync(id);
        if (collection is null) return NotFound();
        if (collection.Status != CollectionStatus.Scheduled)
            return BadRequest($"Collection is {collection.Status}, cannot start");

        collection.Status = CollectionStatus.InProgress;
        collection.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Collection started: CollectionId={CollectionId}, BinId={BinId}, Scheduled->InProgress",
            id, collection.BinId);
        return NoContent();
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> CompleteCollection(int id, [FromBody] CompleteCollectionRequest request)
    {
        var collection = await _db.Collections.FindAsync(id);
        if (collection is null) return NotFound();
        if (collection.Status != CollectionStatus.InProgress)
            return BadRequest($"Collection is {collection.Status}, cannot complete");

        var now = DateTime.UtcNow;
        collection.Status = CollectionStatus.Completed;
        collection.CompletedAt = now;
        collection.FillLevelAtCollection = request.FillLevelAtCollection;
        if (!string.IsNullOrEmpty(request.Notes)) collection.Notes = request.Notes;

        // Update bin's last collected
        var bin = await _db.WasteBins.FindAsync(collection.BinId);
        if (bin is not null)
        {
            bin.LastCollected = now;
            bin.CurrentFillLevel = 5.0 + (new Random().NextDouble() * 10.0); // Reset to near-empty
        }

        // Update truck bins collected today
        var truck = await _db.Trucks.FindAsync(collection.TruckId);
        if (truck is not null) truck.BinsCollectedToday++;

        await _db.SaveChangesAsync();

        var duration = collection.StartedAt.HasValue
            ? (now - collection.StartedAt.Value).TotalMinutes
            : 0;

        _metrics.RecordCollectionCompleted(duration, request.FillLevelAtCollection ?? 0);
        using var activity = WasteManagementActivitySource.StartCollectionComplete(id, duration);
        _logger.LogInformation("Collection completed: CollectionId={CollectionId}, BinId={BinId}, FillLevel={FillLevel}, Duration={Duration}min",
            id, collection.BinId, request.FillLevelAtCollection, Math.Round(duration, 1));
        return NoContent();
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelCollection(int id)
    {
        var collection = await _db.Collections.FindAsync(id);
        if (collection is null) return NotFound();
        if (collection.Status == CollectionStatus.Completed)
            return BadRequest("Cannot cancel a completed collection");

        var previousStatus = collection.Status;
        collection.Status = CollectionStatus.Cancelled;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Collection cancelled: CollectionId={CollectionId}, BinId={BinId}, PreviousStatus={PreviousStatus}",
            id, collection.BinId, previousStatus);
        return NoContent();
    }
}
