using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Consumer.Application.Parsers;

// Tum formata ozel parserlarin uydugu ortak sozlesme (Strategy deseni).
// Her parser hangi formati isledigini bildirir ve ham metni
// normalize edilmis bir SensorReading nesnesine cevirir.
public interface ISensorPayloadParser
{
    // Bu parserin isledigi format (Json / Xml / Csv).
    // Factory, gelen mesajin formatina gore dogru parseri bununla secer.
    PayloadFormat Format { get; }

    // Ham mesaji alir, normalize edilmis SensorReading dondurur.
    // Gecersiz/eksik veride anlamli bir hata (exception) firlatir.
    SensorReading Parse(SensorRawReadingMessage message);
}
