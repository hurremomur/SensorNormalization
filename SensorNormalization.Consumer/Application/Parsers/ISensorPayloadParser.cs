using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Tum formata ozel parserlarin uydugu ortak sozlesme (Strategy deseni).
// Her parser hangi formati VE hangi sensor tipini isledigini bildirir.
public interface ISensorPayloadParser
{
    // Bu parserin isledigi format (Json / Xml / Csv).
    PayloadFormat Format { get; }

    // Bu parserin isledigi sensor tipi (Temperature / Humidity / Pressure / Light).
    // Factory, (Format + SensorType) ikilisiyle dogru parseri secer;
    // boylece ayni formatta birden cok sensor tipi cakismadan eklenebilir.
    SensorType SensorType { get; }

    // Ham mesaji alir, normalize edilmis SensorReading dondurur.
    SensorReading Parse(SensorRawReadingMessage message);
}
