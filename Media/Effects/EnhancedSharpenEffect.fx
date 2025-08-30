// Parametry shadera
sampler2D input : register(s0); // Tekstura wejściowa (obraz wideo)
float sharpenAmount : register(c0); // Intensywność wyostrzania
float smoothAmount : register(c1); // Intensywność wygładzania artefaktów

// Funkcja główna shadera
float4 main(float2 uv : TEXCOORD) : COLOR
{
    // Pobierz piksel centralny
    float4 color = tex2D(input, uv);
    
    // Wyostrzanie (Sharpening)
    float4 sharpened = color * (1.0 + 4.0 * sharpenAmount);
    float4 neighbors = 0;
    float offset = 0.001; // Rozmiar próbki sąsiedztwa
    neighbors += tex2D(input, uv + float2(-offset, 0));
    neighbors += tex2D(input, uv + float2(offset, 0));
    neighbors += tex2D(input, uv + float2(0, -offset));
    neighbors += tex2D(input, uv + float2(0, offset));
    sharpened -= (neighbors / 4.0) * sharpenAmount;
    
    // Wygładzanie artefaktów (prosta średnia ważona)
    float4 smoothed = color;
    if (smoothAmount > 0.0)
    {
        float4 blur = 0;
        blur += tex2D(input, uv + float2(-offset * 2.0, 0)) * 0.1;
        blur += tex2D(input, uv + float2(offset * 2.0, 0)) * 0.1;
        blur += tex2D(input, uv + float2(0, -offset * 2.0)) * 0.1;
        blur += tex2D(input, uv + float2(0, offset * 2.0)) * 0.1;
        blur += color * 0.6; // Waga centralnego piksela
        smoothed = lerp(color, blur, smoothAmount);
    }
    
    // Połącz efekty
    return lerp(smoothed, sharpened, 0.5); // Balans między wygładzaniem a wyostrzaniem
}