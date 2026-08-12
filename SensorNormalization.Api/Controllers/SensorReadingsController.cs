using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SensorNormalization.Application.Dto;
using SensorNormalization.Application.Services.Abstract;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Api.Controllers;

[ApiController]
[Route("api/sensor-readings")]
public class SensorReadingsController : ControllerBase
{
    private readonly ISensorReadingService _service;

    public SensorReadingsController(ISensorReadingService service)
    {
        _service = service;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(IReadOnlyList<SensorReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestPerTypeAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{sensorType}/latest")]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestByType(
        string sensorType, CancellationToken cancellationToken)
    {
        if (!TryParseSensorType(sensorType, out SensorType parsedType))
            return BadRequest($"Gecersiz sensorType: {sensorType}. Beklenen: {ValidSensorTypes()}.");

        var result = await _service.GetLatestByTypeAsync(parsedType, cancellationToken);
        if (result is null)
            return NotFound($"{sensorType} icin kayit bulunamadi.");

        return Ok(result);
    }

    [HttpGet("{sensorType}/history")]
    [ProducesResponseType(typeof(PagedResult<SensorReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        string sensorType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSensorType(sensorType, out SensorType parsedType))
            return BadRequest($"Gecersiz sensorType: {sensorType}. Beklenen: {ValidSensorTypes()}.");
        if (pageIndex < 0)
            return BadRequest("pageIndex 0 veya daha buyuk olmali.");
        if (pageSize < 1 || pageSize > 500)
            return BadRequest("pageSize 1 ile 500 arasinda olmali.");
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return BadRequest("from, to'dan buyuk olamaz.");

        var result = await _service.GetHistoryAsync(
            parsedType, from, to, pageIndex, pageSize, cancellationToken);
        return Ok(result);
    }

    // GET /api/sensor-readings/{sensorType}/summary?from=...&to=...
    // Araliktaki istatistik: adet, min, max, ortalama.
    [HttpGet("{sensorType}/summary")]
    [ProducesResponseType(typeof(SensorReadingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        string sensorType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSensorType(sensorType, out SensorType parsedType))
            return BadRequest($"Gecersiz sensorType: {sensorType}. Beklenen: {ValidSensorTypes()}.");
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return BadRequest("from, to'dan buyuk olamaz.");

        var result = await _service.GetSummaryAsync(parsedType, from, to, cancellationToken);
        return Ok(result);
    }

    private static bool TryParseSensorType(string text, out SensorType sensorType)
        => Enum.TryParse(text, ignoreCase: true, out sensorType)
           && Enum.IsDefined(sensorType);

    // Gecerli sensor tiplerini enum'dan otomatik uretir.
    // Yeni bir sensor tipi eklendiginde bu mesaj kendiliginden guncellenir.
    private static string ValidSensorTypes()
        => string.Join(", ", Enum.GetNames<SensorType>().Select(n => n.ToLowerInvariant()));
}