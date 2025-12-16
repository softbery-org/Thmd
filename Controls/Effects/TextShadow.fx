sampler2D input : register(s0);

float2 shadowOffset : register(c0); // przesunięcie cienia
float blurAmount : register(c1); // rozmycie
float4 shadowColor : register(c2); // kolor cienia + alpha

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 baseColor = tex2D(input, uv);
    float4 finalColor = baseColor;

    // tworzymy cień z przesunięciem
    float4 shadow = tex2D(input, uv + shadowOffset);

    // blur — proste rozmycie 5 próbek
    if (blurAmount > 0)
    {
        shadow += tex2D(input, uv + shadowOffset + float2(blurAmount, 0)) * 0.25;
        shadow += tex2D(input, uv + shadowOffset + float2(-blurAmount, 0)) * 0.25;
        shadow += tex2D(input, uv + shadowOffset + float2(0, blurAmount)) * 0.25;
        shadow += tex2D(input, uv + shadowOffset + float2(0, -blurAmount)) * 0.25;
    }

    // kolorujemy cień
    shadow *= shadowColor;

    // łączymy: cień pod spodem
    finalColor = shadow + baseColor;

    return finalColor;
}