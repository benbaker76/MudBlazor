// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Utilities
{
    public interface IGradientStop : IComparable<IGradientStop>
    {
        public float Position { get; set; }
    }

    public class ColorStop : IGradientStop
    {
        public float Position { get; set; }
        public MudColor Color { get; set; }

        public ColorStop(float position, MudColor color)
        {
            Position = position;
            Color = color;
        }

        public int CompareTo(IGradientStop other)
        {
            return Position.CompareTo(other.Position);
        }

        public override string ToString()
        {
            return $"background: {Color.ToString(MudColorOutputFormats.RGB)};";
        }
    }

    public class OpacityStop : IGradientStop
    {
        public float Position { get; set; }
        public float Opacity { get; set; }

        public OpacityStop(float position, float opacity)
        {
            Position = position;
            Opacity = opacity;
        }

        public int CompareTo(IGradientStop other)
        {
            return Position.CompareTo(other.Position);
        }
    }

    public class Gradient : ICloneable
    {
        public string Name { get; set; }
        public List<ColorStop> ColorStops { get; set; }
        public List<OpacityStop> OpacityStops { get; set; }

        public void AddStop(IGradientStop stop)
        {
            if (stop is ColorStop colorStop)
            {
                if (ColorStops == null)
                    ColorStops = new List<ColorStop>();

                ColorStops.Add(colorStop);
                ColorStops.Sort();  // Automatically sorts by position
            }
            else if (stop is OpacityStop opacityStop)
            {
                if (OpacityStops == null)
                    OpacityStops = new List<OpacityStop>();

                OpacityStops.Add(opacityStop);
                OpacityStops.Sort();  // Automatically sorts by position
            }
        }

        public object Clone()
        {
            return new Gradient
            {
                Name = Name,
                ColorStops = ColorStops?.Select(s => new ColorStop(s.Position, s.Color)).ToList(),
                OpacityStops = OpacityStops?.Select(s => new OpacityStop(s.Position, s.Opacity)).ToList()
            };
        }

        public string ToString(int degrees)
        {
            // Create a combined list of (Position, MudColor) tuples
            var combinedStops = new List<(float Position, MudColor Color)>();

            // Add color stops
            if (ColorStops != null)
            {
                foreach (var stop in ColorStops)
                {
                    // Default opacity is fully opaque (1.0f)
                    float opacity = 1.0f;

                    // Find a matching opacity stop for the color stop
                    if (OpacityStops != null && OpacityStops.Count > 0)
                    {
                        var matchingOpacityStop = OpacityStops
                            .OrderBy(o => Math.Abs(o.Position - stop.Position))
                            .FirstOrDefault();

                        if (matchingOpacityStop != null)
                        {
                            opacity = matchingOpacityStop.Opacity;
                        }
                    }

                    // Create a MudColor with the appropriate alpha (opacity)
                    var colorWithAlpha = new MudColor(stop.Color.R, stop.Color.G, stop.Color.B, opacity);
                    combinedStops.Add((stop.Position, colorWithAlpha));
                }
            }

            // Process opacity stops that don’t have a corresponding color stop
            if (OpacityStops != null)
            {
                foreach (var opacityStop in OpacityStops)
                {
                    // Check if there's already a color stop at the same position
                    if (!ColorStops.Any(s => Math.Abs(s.Position - opacityStop.Position) < 0.01))
                    {
                        // Find the closest color stop
                        var closestColorStop = ColorStops
                            .OrderBy(s => Math.Abs(s.Position - opacityStop.Position))
                            .FirstOrDefault();

                        if (closestColorStop != null)
                        {
                            // Create a MudColor with the closest color and the opacity stop's alpha
                            var colorWithAlpha = new MudColor(closestColorStop.Color.R, closestColorStop.Color.G, closestColorStop.Color.B, opacityStop.Opacity);
                            combinedStops.Add((opacityStop.Position, colorWithAlpha));
                        }
                    }
                }
            }

            // Sort combined stops by position to ensure proper order
            combinedStops = combinedStops.OrderBy(s => s.Position).ToList();

            // Build the final gradient string
            var gradientStops = new List<string>();
            foreach (var stop in combinedStops)
            {
                var colorString = stop.Color.ToString(stop.Color.A == 255 ? MudColorOutputFormats.RGB : MudColorOutputFormats.RGBA);
                gradientStops.Add($"{colorString} {stop.Position}%");
            }

            // Return the full gradient definition
            return $"linear-gradient({degrees}deg, {string.Join(", ", gradientStops)})";
        }

        public override string ToString()
        {
            return ToString(90);
        }
    }

    public class GradientCollection
    {
        public static List<Gradient> Gradients = new List<Gradient>
        {
            new Gradient
            {
                Name = "Foreground to Background",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(20.8f, new MudColor(0, 0, 0, 255)),
                    new ColorStop(80.6f, new MudColor(255, 255, 255, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Foreground to Transparent",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(0, 0, 0, 255)),
                    new ColorStop(100.0f, new MudColor(0, 0, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(80.9f, 0.0f),
                    new OpacityStop(20.8f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Red, Green",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(255, 0, 0, 255)),
                    new ColorStop(100.0f, new MudColor(0, 128, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Violet, Orange",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(100.0f, new MudColor(41, 10, 89, 255)),
                    new ColorStop(0.0f, new MudColor(255, 124, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Blue, Red, Yellow",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(0, 0, 255, 255)),
                    new ColorStop(100.0f, new MudColor(255, 255, 0, 255)),
                    new ColorStop(50.0f, new MudColor(255, 0, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Blue, Yellow, Blue",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(90.6f, new MudColor(11, 2, 170, 255)),
                    new ColorStop(10.9f, new MudColor(11, 2, 170, 255)),
                    new ColorStop(50.1f, new MudColor(255, 255, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Orange, Yellow, Orange",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(98.8f, new MudColor(255, 110, 2, 255)),
                    new ColorStop(0.0f, new MudColor(255, 110, 2, 255)),
                    new ColorStop(50.7f, new MudColor(255, 255, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Violet, Green, Orange",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(111, 21, 108, 255)),
                    new ColorStop(100.0f, new MudColor(253, 124, 0, 255)),
                    new ColorStop(50.4f, new MudColor(0, 96, 27, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Yellow, Violet, Orange, Blue",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(10.9f, new MudColor(249, 230, 0, 255)),
                    new ColorStop(90.6f, new MudColor(0, 40, 116, 255)),
                    new ColorStop(35.2f, new MudColor(111, 21, 108, 255)),
                    new ColorStop(65.7f, new MudColor(253, 124, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Copper",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(151, 70, 26, 255)),
                    new ColorStop(100.0f, new MudColor(239, 219, 205, 255)),
                    new ColorStop(31.1f, new MudColor(251, 216, 197, 255)),
                    new ColorStop(85.3f, new MudColor(108, 46, 22, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Chrome",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(41, 137, 204, 255)),
                    new ColorStop(100.0f, new MudColor(255, 255, 255, 255)),
                    new ColorStop(49.0f, new MudColor(255, 255, 255, 255)),
                    new ColorStop(49.3f, new MudColor(144, 106, 0, 255)),
                    new ColorStop(69.5f, new MudColor(217, 159, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Spectrum",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(255, 0, 0, 255)),
                    new ColorStop(100.0f, new MudColor(255, 0, 0, 255)),
                    new ColorStop(33.4f, new MudColor(0, 0, 255, 255)),
                    new ColorStop(50.7f, new MudColor(0, 255, 255, 255)),
                    new ColorStop(66.9f, new MudColor(0, 255, 0, 255)),
                    new ColorStop(83.0f, new MudColor(255, 255, 0, 255)),
                    new ColorStop(16.7f, new MudColor(255, 0, 255, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 1.0f),
                    new OpacityStop(100.0f, 1.0f),
                }
            },
            new Gradient
            {
                Name = "Transparent Rainbow",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(22.0f, new MudColor(255, 0, 0, 255)),
                    new ColorStop(34.3f, new MudColor(255, 252, 0, 255)),
                    new ColorStop(46.0f, new MudColor(1, 180, 57, 255)),
                    new ColorStop(55.7f, new MudColor(0, 234, 255, 255)),
                    new ColorStop(67.4f, new MudColor(0, 3, 144, 255)),
                    new ColorStop(78.9f, new MudColor(255, 0, 198, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(0.0f, 0.0f),
                    new OpacityStop(22.3f, 1.0f),
                    new OpacityStop(12.3f, 0.5f),
                    new OpacityStop(78.6f, 1.0f),
                    new OpacityStop(97.1f, 0.0f),
                    new OpacityStop(86.5f, 0.5f),
                }
            },
            new Gradient
            {
                Name = "Transparent Stripes",
                ColorStops = new List<ColorStop>
                {
                    new ColorStop(0.0f, new MudColor(0, 0, 0, 255)),
                    new ColorStop(100.0f, new MudColor(0, 0, 0, 255)),
                },
                OpacityStops = new List<OpacityStop>
                {
                    new OpacityStop(12.6f, 0.0f),
                    new OpacityStop(100.0f, 1.0f),
                    new OpacityStop(12.3f, 1.0f),
                    new OpacityStop(26.1f, 0.0f),
                    new OpacityStop(26.4f, 1.0f),
                    new OpacityStop(40.2f, 1.0f),
                    new OpacityStop(40.5f, 0.0f),
                    new OpacityStop(56.9f, 1.0f),
                    new OpacityStop(56.6f, 0.0f),
                    new OpacityStop(71.6f, 1.0f),
                    new OpacityStop(71.8f, 0.0f),
                    new OpacityStop(86.5f, 0.0f),
                    new OpacityStop(86.8f, 1.0f),
                }
            },
        };

        // Method to read gradients from a file (by file name)
        public static async Task ReadGradientCollection(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                await ReadGradientCollection(stream);
            }
        }

        // Method to read gradients from an HTTP client
        public static async Task ReadGradientCollection(HttpClient httpClient, string fileName)
        {
            using (var stream = await httpClient.GetStreamAsync(fileName))
            {
                await ReadGradientCollection(stream);
            }
        }

        // Method to read gradients from a stream (used by file and HTTP client methods)
        public static async Task ReadGradientCollection(Stream stream)
        {
            Gradients = new List<Gradient>();

            using (var reader = new StreamReader(stream))
            {
                string xmlContent = await reader.ReadToEndAsync();
                var doc = XDocument.Parse(xmlContent);

                var gradientElements = doc.Descendants("Gradient");

                foreach (var gradientElement in gradientElements)
                {
                    var gradient = new Gradient
                    {
                        Name = gradientElement.Attribute("Title")?.Value,
                        ColorStops = new List<ColorStop>(),
                        OpacityStops = new List<OpacityStop>()
                    };

                    // Parse ColorPoints
                    var colorPoints = gradientElement.Descendants("ColorPoint");
                    foreach (var colorPoint in colorPoints)
                    {
                        var position = ParseFloat(colorPoint.Attribute("Position")?.Value) * 100;
                        var color = ParseColor(colorPoint.Attribute("Color")?.Value);
                        gradient.ColorStops.Add(new ColorStop(position, color));
                    }

                    // Parse AlphaPoints (Opacity)
                    var alphaPoints = gradientElement.Descendants("AlphaPoint");
                    foreach (var alphaPoint in alphaPoints)
                    {
                        var position = ParseFloat(alphaPoint.Attribute("Position")?.Value) * 100;
                        var opacity = ParseFloat(alphaPoint.Attribute("Alpha")?.Value);
                        gradient.OpacityStops.Add(new OpacityStop(position, opacity));
                    }

                    Gradients.Add(gradient);
                }
            }
        }

        // Writing the gradients to file
        public static void WriteGradientCollection(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                WriteGradientCollection(stream);
            }
        }

        // Writing the gradients to a stream
        public static void WriteGradientCollection(Stream stream)
        {
            var doc = new XDocument(new XElement("Collection"));

            foreach (var gradient in Gradients)
            {
                var gradientElement = new XElement("Gradient",
                    new XAttribute("Title", gradient.Name),
                    new XAttribute("GammaCorrected", "False"));

                // Add color stops
                foreach (var colorStop in gradient.ColorStops)
                {
                    // Convert the RGB to XYZ
                    var (x, y, z) = ConvertRGBtoXYZ(colorStop.Color);

                    var colorElement = new XElement("ColorPoint",
                        new XAttribute("Position", colorStop.Position / 100), // Adjusting back to 0-1 range
                        new XAttribute("Color", $"{x}, {y}, {z}"));
                    gradientElement.Add(colorElement);
                }

                // Add opacity stops
                foreach (var opacityStop in gradient.OpacityStops)
                {
                    var alphaElement = new XElement("AlphaPoint",
                        new XAttribute("Position", opacityStop.Position / 100), // Adjusting back to 0-1 range
                        new XAttribute("Alpha", opacityStop.Opacity));
                    gradientElement.Add(alphaElement);
                }

                doc.Root.Add(gradientElement);
            }

            doc.Save(stream);
        }

        private static MudColor ParseColor(string colorString)
        {
            var colorParts = colorString.Split(',');
            if (colorParts.Length == 3)
            {
                var x = ParseFloat(colorParts[0]);
                var y = ParseFloat(colorParts[1]);
                var z = ParseFloat(colorParts[2]);

                // Convert XYZ to RGB
                var (r, g, b) = ConvertXYZToRGB(x, y, z);
                return new MudColor(r, g, b, 255);
            }
            return new MudColor();
        }

        private static (int, int, int) ConvertXYZToRGB(float x, float y, float z)
        {
            // Inverse gamma correction calculation for XYZ to RGB conversion
            double r = InvertGammaCorrection(0.032406 * x - 0.015372 * y - 0.004986 * z);
            double g = InvertGammaCorrection(-0.009689 * x + 0.018758 * y + 0.000415 * z);
            double b = InvertGammaCorrection(0.000557 * x - 0.002040 * y + 0.010570 * z);

            // Clamp values to 0-255 and return as integers
            return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
        }

        private static (double X, double Y, double Z) ConvertRGBtoXYZ(MudColor color)
        {
            // Normalize RGB values to [0, 1] range
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            // Apply inverse gamma correction (sRGB)
            r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : (r / 12.92);
            g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : (g / 12.92);
            b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : (b / 12.92);

            // Convert to XYZ color space
            double x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
            double y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
            double z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

            // Return as XYZ tuple
            return (x * 100, y * 100, z * 100);
        }

        private static double InvertGammaCorrection(double value)
        {
            if (value <= 0.0031308)
                return 12.92 * value;
            else
                return 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;
        }

        private static int ClampToByte(double value)
        {
            return (int)Math.Max(0, Math.Min(255, Math.Round(value * 255)));
        }

        private static float ParseFloat(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f;
        }
    }
}
