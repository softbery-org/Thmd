// Version: 0.0.0.6
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

namespace Thmd.Controls.Effects
{
    /// <summary>
    /// 
    /// </summary>
    public class TextShadowEffect : ShaderEffect
    {
        private static readonly PixelShader _pixelShader = new PixelShader()
        {
            UriSource = new Uri("/Thmd;component/Controls/Effects/TextShadow.ps", UriKind.Relative)
        };
        /// <summary>
        /// 
        /// </summary>
        public TextShadowEffect()
        {
            PixelShader = _pixelShader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ShadowOffsetProperty);
            UpdateShaderValue(BlurAmountProperty);
            UpdateShaderValue(ShadowColorProperty);
        }

        // Input (render tekstu)
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty(
                "Input", typeof(TextShadowEffect), 0);

        /// <summary>
        /// 
        /// </summary>
        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        // ShadowOffset
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ShadowOffsetProperty =
            DependencyProperty.Register(
                "ShadowOffset",
                typeof(Point),
                typeof(TextShadowEffect),
                new UIPropertyMetadata(new Point(2, 2), PixelShaderConstantCallback(0)));

        /// <summary>
        /// 
        /// </summary>
        public Point ShadowOffset
        {
            get => (Point)GetValue(ShadowOffsetProperty);
            set => SetValue(ShadowOffsetProperty, value);
        }

        // BlurAmount
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty BlurAmountProperty =
            DependencyProperty.Register(
                "BlurAmount",
                typeof(double),
                typeof(TextShadowEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// 
        /// </summary>
        public double BlurAmount
        {
            get => (double)GetValue(BlurAmountProperty);
            set => SetValue(BlurAmountProperty, value);
        }

        // ShadowColor
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register(
                "ShadowColor",
                typeof(Color),
                typeof(TextShadowEffect),
                new UIPropertyMetadata(Colors.Black, PixelShaderConstantCallback(2)));

        /// <summary>
        /// 
        /// </summary>
        public Color ShadowColor
        {
            get => (Color)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }
    }
}
