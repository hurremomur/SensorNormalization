using SensorNormalization.Consumer.Application.Parsers;
using SensorNormalization.Domain.Messages;
using Xunit;

namespace SensorNormalization.Tests;

// Icerikten format tespiti (odev 4 bonus) testleri.
public class ContentFormatDetectorTests
{
    [Fact]
    public void Json_icerigi_Json_olarak_tespit_edilir()
    {
        string payload = "{\"sensor_id\":\"TEMP-01\",\"reading_fahrenheit\":98.6}";
        Assert.Equal(PayloadFormat.Json, ContentFormatDetector.Detect(payload));
    }

    [Fact]
    public void Xml_icerigi_Xml_olarak_tespit_edilir()
    {
        string payload = "<HumidityReading><DeviceId>HUM-03</DeviceId></HumidityReading>";
        Assert.Equal(PayloadFormat.Xml, ContentFormatDetector.Detect(payload));
    }

    [Fact]
    public void Csv_icerigi_Csv_olarak_tespit_edilir()
    {
        string payload = "sensorId,value,unit\nPRES-02,1013.25,mbar";
        Assert.Equal(PayloadFormat.Csv, ContentFormatDetector.Detect(payload));
    }

    [Fact]
    public void Bos_icerik_hata_firlatir()
    {
        Assert.Throws<NotSupportedException>(() => ContentFormatDetector.Detect(""));
    }

    [Fact]
    public void Suslu_parantezle_baslayan_bozuk_metin_Json_sayilmaz()
    {
        // '{' ile baslar ama gecerli JSON degil -> Json dondurmemeli.
        // Virgul icermedigi icin de CSV olmaz -> tespit edilemez, hata.
        string payload = "{bu gecerli json degil";
        Assert.Throws<NotSupportedException>(() => ContentFormatDetector.Detect(payload));
    }
}
