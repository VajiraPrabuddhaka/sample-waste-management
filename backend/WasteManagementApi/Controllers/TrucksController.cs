using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.DTOs;
using WasteManagementApi.Models;

namespace WasteManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrucksController : ControllerBase
{
    private readonly WasteManagementDbContext _db;
    private readonly ILogger<TrucksController> _logger;

    public TrucksController(WasteManagementDbContext db, ILogger<TrucksController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IList<TruckSummary>>> GetTrucks([FromQuery] string? status = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var query = _db.Trucks.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TruckStatus>(status, true, out var truckStatus))
            query = query.Where(t => t.Status == truckStatus);

        var trucks = await query
            .Select(t => new TruckSummary(
                t.Id, t.Name, t.RegistrationNumber, t.Status.ToString(),
                t.FuelLevelPercent, t.BinsCollectedToday, t.CurrentRoute, t.LastUpdated))
            .ToListAsync();

        sw.Stop();
        _logger.LogInformation("GetTrucks: Status={Status}, Count={Count}, Duration={Duration}ms",
            status, trucks.Count, sw.ElapsedMilliseconds);

        return Ok(trucks);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TruckDetail>> GetTruck(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var truck = await _db.Trucks
            .Include(t => t.Collections.OrderByDescending(c => c.ScheduledAt).Take(10))
                .ThenInclude(c => c.Bin)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (truck is null)
        {
            _logger.LogWarning("GetTruck: Truck {TruckId} not found", id);
            return NotFound();
        }

        var recentCollections = truck.Collections
            .OrderByDescending(c => c.ScheduledAt)
            .Select(c => new CollectionSummary(
                c.Id, c.BinId, c.Bin?.Name ?? "", c.TruckId, truck.Name,
                c.Status.ToString(), c.ScheduledAt, c.StartedAt, c.CompletedAt,
                c.FillLevelAtCollection, c.Notes))
            .ToList();

        var detail = new TruckDetail(
            truck.Id, truck.Name, truck.RegistrationNumber, truck.Status.ToString(),
            truck.FuelLevelPercent, truck.BinsCollectedToday, truck.CurrentRoute, truck.LastUpdated,
            truck.CurrentLatitude, truck.CurrentLongitude, recentCollections);

        sw.Stop();
        _logger.LogInformation("GetTruck: TruckId={TruckId}, Name={Name}, Status={Status}, Duration={Duration}ms",
            id, truck.Name, truck.Status, sw.ElapsedMilliseconds);

        return Ok(detail);
    }

    [HttpPut("{id:int}/location")]
    public async Task<IActionResult> UpdateTruckLocation(int id, [FromBody] UpdateTruckLocationRequest request)
    {
        var truck = await _db.Trucks.FindAsync(id);
        if (truck is null)
        {
            _logger.LogWarning("UpdateTruckLocation: Truck {TruckId} not found", id);
            return NotFound();
        }

        truck.CurrentLatitude = request.Latitude;
        truck.CurrentLongitude = request.Longitude;
        truck.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("UpdateTruckLocation: TruckId={TruckId}, Lat={Lat}, Lon={Lon}",
            id, request.Latitude, request.Longitude);

        return NoContent();
    }
}
