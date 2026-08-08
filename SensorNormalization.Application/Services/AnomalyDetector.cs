namespace SensorNormalization.Application.Services;

// Istatistiksel anomali tespiti: bir sensorun son N degerinden
// ortalama (mean) ve standart sapma (stdDev) hesaplanir. Yeni deger
// ortalamadan 3 standart sapmadan fazla uzaksa anomali sayilir.
// Boylece esik elle verilmez; sistem her sensorun normalini veriden ogrenir.
public static class AnomalyDetector
{
    private const int MinSamples = 20;   // Bu kadar veri yoksa istatistik guvenilmez.
    private const double SigmaThreshold = 3.0;

    public static bool IsAnomaly(IReadOnlyList<double> recentValues, double newValue)
    {
        // Yeterli gecmis yoksa anomali deme (henuz "normal" ogrenilmedi).
        if (recentValues is null || recentValues.Count < MinSamples)
            return false;

        double mean = recentValues.Average();

        double variance = recentValues
            .Select(v => (v - mean) * (v - mean))
            .Average();
        double stdDev = Math.Sqrt(variance);

        // Tum degerler ayniysa (stdDev=0) sapma tanimsiz; anomali deme.
        if (stdDev == 0)
            return false;

        return Math.Abs(newValue - mean) > SigmaThreshold * stdDev;
    }
}
