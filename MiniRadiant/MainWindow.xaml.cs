using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniRadiant
{

    class LightEntity
    {
        public Vector3 position;
        public float baseIntensity;
    }

    class LightColor
    {
        public string ColorValues { get {
                return $"R: {color.X} G: {color.Y} B: {color.Z}";
            } }
        public SolidColorBrush WPFColor
        {
            get
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(color.X*255.0f), (byte)(color.Y * 255.0f), (byte)(color.Z * 255.0f)));
            }
        }
        public Vector3 color = new Vector3();
        public int Count { get; set; } = 0;
        public float Intensity { get; set; } = 1.0f;
        public float Deviance { get; set; } = 0.0f;
        public int Samples { get; set; } = 1;

        public List<LightEntity> lights = new List<LightEntity>();


        public bool OverrideDevianceSamples { get; set; } = false;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        Dictionary<Vector3,LightColor> lightColors = new Dictionary<Vector3, LightColor>();

        private void updateList()
        {
            colorsList.ItemsSource = null;
            colorsList.ItemsSource = lightColors.Values;
        }

        string mapFileData = null;

        Regex entitiesParseRegex = new Regex(@"\{(\s*""([^""]+)""[ \t]+""([^""]+)"")+\s*\}",RegexOptions.IgnoreCase|RegexOptions.Compiled);
        Regex emptySpaceRegex = new Regex(@"\s+",RegexOptions.IgnoreCase|RegexOptions.Compiled);

        private Vector3? parseVector3(string colorString)
        {
            string prefilteredColor = emptySpaceRegex.Replace(colorString, " ");
            string[] components = prefilteredColor.Split(' ');

            if (components.Length < 3)
            {
                Trace.WriteLine("Vector3 with less than 3 components, skipping, weird.");
                return null;
            }

            Vector3 parsedColor = new Vector3();

            bool parseSuccess = true;
            parseSuccess = parseSuccess && float.TryParse(components[0], out parsedColor.X);
            parseSuccess = parseSuccess && float.TryParse(components[1], out parsedColor.Y);
            parseSuccess = parseSuccess && float.TryParse(components[2], out parsedColor.Z);

            if (!parseSuccess) return null;

            return parsedColor;
        }

        Vector2[] mapBounds = null;

        private void parseMapLights()
        {

            lightColors.Clear();
            mapBounds = new Vector2[4] {
                new Vector2(){ X =0, Y=0 },
                new Vector2(){ X =0, Y=0 },
                new Vector2(){ X =0, Y=0 },
                new Vector2(){ X =0, Y=0 },
            };

            MatchCollection matches = entitiesParseRegex.Matches(mapFileData);
            int i = 0;
            foreach(Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    Dictionary<string, string> entity = new Dictionary<string, string>();

                    int lineCount = match.Groups[2].Captures.Count;
                    for (int c = 0; c < lineCount; c++)
                    {
                        //Trace.WriteLine($"{match.Groups[2].Captures[c].Value}:{match.Groups[3].Captures[c].Value}");
                        entity[match.Groups[2].Captures[c].Value] = match.Groups[3].Captures[c].Value;
                    }

                    if(entity.ContainsKey("classname") && entity["classname"] == "light")
                    {
                        if (entity.ContainsKey("_color"))
                        {

                            Vector3? parsedColor = parseVector3(entity["_color"]);
                            if (!parsedColor.HasValue)
                            {
                                Trace.WriteLine("Color parsing failed, skipping light.");
                                continue;
                            }
                            Vector3? parsedOrigin = parseVector3(entity["origin"]);
                            if (!parsedOrigin.HasValue)
                            {
                                Trace.WriteLine("Origin parsing failed, light won't be visible in preview.");
                            } else
                            {
                                float x = parsedOrigin.Value.X;
                                float y = parsedOrigin.Value.Y;
                                mapBounds[0].X = Math.Min(mapBounds[0].X,x);
                                mapBounds[0].Y = Math.Min(mapBounds[0].Y,y);
                                mapBounds[1].X = Math.Max(mapBounds[1].X,x);
                                mapBounds[1].Y = Math.Min(mapBounds[1].Y,y);
                                mapBounds[2].X = Math.Max(mapBounds[2].X,x);
                                mapBounds[2].Y = Math.Max(mapBounds[2].Y,y);
                                mapBounds[3].X = Math.Min(mapBounds[3].X,x);
                                mapBounds[3].Y = Math.Max(mapBounds[3].Y,y);
                            }


                            if (lightColors.ContainsKey(parsedColor.Value)){
                                lightColors[parsedColor.Value].Count++;
                            }
                            else
                            {
                                lightColors.Add(parsedColor.Value, new LightColor() { Count = 1, color = parsedColor.Value });
                            }
                            if (parsedOrigin.HasValue)
                            {
                                float baseIntensity = 1.0f;
                                foreach(string eKey in new string[] { "light", "_light", "scale" })
                                {
                                    if (entity.ContainsKey(eKey))
                                    {
                                        float oldScale = 1.0f;
                                        if(float.TryParse(entity[eKey], out oldScale))
                                        {
                                            baseIntensity *= oldScale;
                                        }
                                    }
                                }
                                lightColors[parsedColor.Value].lights.Add(new LightEntity() { baseIntensity= baseIntensity, position = parsedOrigin.Value});
                            }
                        }
                    }
                }
                i++;
            }
        }

        private string filterMapFile()
        {

            return entitiesParseRegex.Replace(mapFileData, (Match match) => {
                if (match.Groups.Count >= 4)
                {
                    Dictionary<string, string> entity = new Dictionary<string, string>();

                    int lineCount = match.Groups[2].Captures.Count;
                    for (int c = 0; c < lineCount; c++)
                    {
                        entity[match.Groups[2].Captures[c].Value] = match.Groups[3].Captures[c].Value;
                    }

                    if (entity.ContainsKey("classname") && entity["classname"] == "light")
                    {
                        if (entity.ContainsKey("_color"))
                        {

                            Vector3? parsedColor = parseVector3(entity["_color"]);
                            if (!parsedColor.HasValue)
                            {
                                return match.Value;
                            }

                            // check if some changes need to be done.
                            bool changesDone = false;
                            if (lightColors.ContainsKey(parsedColor.Value))
                            {
                                LightColor lc = lightColors[parsedColor.Value];
                                if(lc.Deviance != 0.0f && (!entity.ContainsKey("_deviance") || lc.OverrideDevianceSamples))
                                {
                                    entity["_deviance"] = lc.Deviance.ToString();
                                    changesDone = true;
                                }
                                if(lc.Samples != 1 && (!entity.ContainsKey("_samples") || lc.OverrideDevianceSamples))
                                {
                                    entity["_samples"] = lc.Samples.ToString();
                                    changesDone = true;
                                }
                                if(lc.Intensity != 1.0f)
                                {
                                    if (entity.ContainsKey("scale"))
                                    {
                                        float oldScale = 1.0f;
                                        float.TryParse(entity["scale"], out oldScale);
                                        entity["scale"] = (oldScale * lc.Intensity).ToString();
                                    }
                                    else
                                    {
                                        entity["scale"] = lc.Intensity.ToString();
                                    }
                                    changesDone = true;
                                }

                                if (changesDone)
                                {
                                    // Rewrite the entity
                                    StringBuilder sb = new StringBuilder();
                                    sb.AppendLine("{");
                                    foreach(KeyValuePair<string,string> kvp in entity)
                                    {
                                        sb.AppendLine($"\"{kvp.Key}\" \"{kvp.Value}\"");
                                    }
                                    sb.AppendLine("}");
                                    return sb.ToString();

                                } else
                                {
                                    return match.Value;
                                }

                            } else
                            {
                                return match.Value;
                            }
                        }
                    }
                }
                return match.Value;
            });
        }


        private void updatePreview()
        {

            if (mapBounds == null) return;

            int imageWidth = (int)miniMapContainer.ActualWidth;
            int imageHeight = (int)miniMapContainer.ActualHeight;

            if (imageWidth < 5 || imageHeight < 5) return; // avoid crashes and shit

            // We flip imageHeight and imageWidth because it's more efficient to work on rows than on columns. We later rotate the image into the proper position
            ByteImage miniMapImage = Helpers.BitmapToByteArray(new Bitmap(imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
            int stride = miniMapImage.stride;

            LightColor[] colorsToDraw = drawAllLightsCheck.IsChecked == true ? lightColors.Values.ToArray() : colorsList.SelectedItems.Cast<LightColor>().ToList().ToArray(); ;

            Vector2 size = mapBounds[2] - mapBounds[0];
            float gameUnitsWidth = size.X;
            float gameUnitsHeight = size.Y;
            Vector2 scale = new Vector2 { X=imageWidth / gameUnitsWidth,Y= imageHeight / gameUnitsHeight };
            Vector2 invertedScale = Vector2.One/ scale;

            bool linearFalloff = linearFalloffCheck.IsChecked == true;
            float exposureMultiplier = (float)Math.Pow(2, exposureSlider.Value);

            Vector2 pos2d;

            float[] floatImage = new float[miniMapImage.imageData.Length];
            foreach (LightColor lightColor in colorsToDraw)
            {
                foreach(LightEntity light in lightColor.lights)
                {
                    pos2d.X = light.position.X;
                    pos2d.Y = light.position.Y;
                    Vector2 centralPixel = (pos2d - mapBounds[0]) * scale;
                    int centerX = (int)centralPixel.X;
                    int centerY = (int)centralPixel.Y;

                    int distanceFromCenter = 0;
                    float maxIntensityThisRound = float.PositiveInfinity;
                    Vector2 tmp;
                    Vector3 tmp2;
                    while (maxIntensityThisRound > 1.0f)
                    {
                        maxIntensityThisRound = 0.0f;

                        for (int x=centerX- distanceFromCenter; x <= centerX + distanceFromCenter; x++)
                        {
                            if (x < 0 || x >= imageWidth) continue;
                            for (int y = centerY - distanceFromCenter; y <= centerY + distanceFromCenter; y++)
                            {
                                if ( y < 0 || y >= imageHeight ||
                                    (x != centerX - distanceFromCenter && x != centerX + distanceFromCenter && // TODO this is SUPER ugly and wasteful, come up with sth better pls.
                                    y != centerY - distanceFromCenter && y != centerY + distanceFromCenter)
                                    ) continue;
                                tmp.X = x; tmp.Y = y;
                                tmp = ((tmp - centralPixel) * invertedScale);
                                float distanceFactor = linearFalloff ? tmp.Length(): tmp.LengthSquared();
                                float intensityHere = lightColor.Intensity* exposureMultiplier* 10.0f * 255.0f * light.baseIntensity / distanceFactor; // 100 is magic number.
                                maxIntensityThisRound = Math.Max(maxIntensityThisRound, intensityHere);
                                tmp2 = lightColor.color * intensityHere;
                                floatImage[y * stride + (imageWidth- x-1) * 3] = floatImage[y * stride + x * 3]+tmp2.Z;
                                floatImage[y * stride + (imageWidth - x - 1) * 3+1] = floatImage[y * stride + x * 3 + 1]+tmp2.Y;
                                floatImage[y * stride + (imageWidth - x - 1) * 3+2] = floatImage[y * stride + x * 3 + 2]+tmp2.X;
                            }
                        }
                        distanceFromCenter++;
                    }
                }

            }

            bool saturatedClip = saturatedClipCheck.IsChecked == true;
            for (int i = 0; i < floatImage.Length; i+=3)
            {
                if (saturatedClip)
                {
                    float maxValue = Math.Max(Math.Max(floatImage[i], floatImage[i+1]), floatImage[i+2]);
                    float multiplier = maxValue <= 255.0f ? 1.0f : 255.0f/maxValue;
                   
                    miniMapImage.imageData[i] = (byte)Math.Clamp(multiplier*floatImage[i], 0.0f, 255.0f);
                    miniMapImage.imageData[i + 1] = (byte)Math.Clamp(multiplier * floatImage[i + 1], 0.0f, 255.0f);
                    miniMapImage.imageData[i + 2] = (byte)Math.Clamp(multiplier * floatImage[i + 2], 0.0f, 255.0f);
                } else
                {
                    miniMapImage.imageData[i] = (byte)Math.Clamp(floatImage[i], 0.0f, 255.0f);
                    miniMapImage.imageData[i + 1] = (byte)Math.Clamp(floatImage[i + 1], 0.0f, 255.0f);
                    miniMapImage.imageData[i + 2] = (byte)Math.Clamp(floatImage[i + 2], 0.0f, 255.0f);
                }
                
            }


            //statsImageBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
            Dispatcher.Invoke(() => {
                Bitmap miniMapImageBitmap = Helpers.ByteArrayToBitmap(miniMapImage);
                miniMap.Source = Helpers.BitmapToImageSource(miniMapImageBitmap);
                miniMapImageBitmap.Dispose();
            });
        }


        private void loadMapBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "map files (*.map)|*.map";
            if(ofd.ShowDialog() == true)
            {
                mapFileData = File.ReadAllText(ofd.FileName);
                parseMapLights();
                updateList();
                updatePreview();
            }
        }

        private void saveMapBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "map files (*.map)|*.map";
            if (sfd.ShowDialog() == true)
            {
                string filteredMapFile = filterMapFile();
                File.WriteAllText(sfd.FileName, filteredMapFile);
            }
        }

        private void colorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updatePreview();
        }

        private void drawAllLightsCheck_Click(object sender, RoutedEventArgs e)
        {

            updatePreview();
        }

        private void linearFalloffCheck_Click(object sender, RoutedEventArgs e)
        {

            updatePreview();
        }

        private void saturatedClipCheck_Click(object sender, RoutedEventArgs e)
        {

            updatePreview();
        }

        private void exposureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            updatePreview();
        }
    }
}
