using SensorNormalization.Application.Services;
using Xunit;

namespace SensorNormalization.Tests;

// AnomalyDetector istatistiksel (mean +/- 3*sigma) mantigini dogrular.
public class AnomalyDetectorTests
{
    [Fact]
    public void Yetersiz_veri_varsa_anomali_degildir()
    {
        var recent = new List<double> { 10, 12, 11, 13, 9 };
        bool result = AnomalyDetector.IsAnomaly(recent, 1000);
        Assert.False(result);
    }

    [Fact]
    public void Null_gecmis_anomali_degildir()
    {
        bool result = AnomalyDetector.IsAnomaly(null!, 500);
        Assert.False(result);
    }

    [Fact]
    public void Ortalamaya_yakin_deger_anomali_degildir()
    {
        var recent = Enumerable.Range(0, 25).Select(i => 20.0 + (i % 2)).ToList();
        bool result = AnomalyDetector.IsAnomaly(recent, 21);
        Assert.False(result);
    }

    [Fact]
    public void Uc_deger_anomalidir()
    {
        var recent = Enumerable.Range(0, 25).Select(i => 18.0 + (i % 5)).ToList();
        bool result = AnomalyDetector.IsAnomaly(recent, 100);
        Assert.True(result);
    }

    [Fact]
    public void Tum_degerler_ayni_ise_anomali_degildir()
    {
        var recent = Enumerable.Repeat(50.0, 25).ToList();
        bool result = AnomalyDetector.IsAnomaly(recent, 999);
        Assert.False(result);
    }

    [Fact]
    public void Negatif_yonde_uc_deger_anomalidir()
    {
        var recent = Enumerable.Range(0, 25).Select(i => 18.0 + (i % 5)).ToList();
        bool result = AnomalyDetector.IsAnomaly(recent, -50);
        Assert.True(result);
    }
}
