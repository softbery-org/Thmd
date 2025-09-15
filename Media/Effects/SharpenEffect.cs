// Version: 0.1.6.37
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Effects;

namespace Thmd.Media.Effects;

public class EnhancedSharpenEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(EnhancedSharpenEffect), 0);
    public static readonly DependencyProperty SharpenAmountProperty = DependencyProperty.Register("SharpenAmount", typeof(float), typeof(EnhancedSharpenEffect), new PropertyMetadata(1.0f, PixelShaderConstantCallback(0)));
    public static readonly DependencyProperty SmoothAmountProperty = DependencyProperty.Register("SmoothAmount", typeof(float), typeof(EnhancedSharpenEffect), new PropertyMetadata(0.0f, PixelShaderConstantCallback(1)));

    public EnhancedSharpenEffect()
    {
        PixelShader = new PixelShader
        {
            UriSource = new Uri("pack://application:,,,/EnhancedSharpenEffect.ps")
        };
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(SharpenAmountProperty);
        UpdateShaderValue(SmoothAmountProperty);
    }

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public float SharpenAmount
    {
        get => (float)GetValue(SharpenAmountProperty);
        set => SetValue(SharpenAmountProperty, value);
    }

    public float SmoothAmount
    {
        get => (float)GetValue(SmoothAmountProperty);
        set => SetValue(SmoothAmountProperty, value);
    }
}
