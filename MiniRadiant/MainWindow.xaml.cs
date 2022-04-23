using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    class LightColor
    {
        public string ColorValues { get {
                return $"R: {color.X} G: {color.Y} B: {color.Z}";
            } }
        public SolidColorBrush WPFColor
        {
            get
            {
                return new SolidColorBrush(Color.FromRgb((byte)(color.X*255.0f), (byte)(color.Y * 255.0f), (byte)(color.Z * 255.0f)));
            }
        }
        public Vector3 color = new Vector3();
        public int Count { get; set; } = 0;
        public float Intensity { get; set; } = 1.0f;
        public float Deviance { get; set; } = 0.0f;
        public int Samples { get; set; } = 1;
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

        private Vector3? parseColor(string colorString)
        {
            string prefilteredColor = emptySpaceRegex.Replace(colorString, " ");
            string[] components = prefilteredColor.Split(' ');

            if (components.Length < 3)
            {
                Trace.WriteLine("Color with less than 3 components, skipping, weird.");
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

        private void parseMapLights()
        {
            lightColors.Clear();

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

                            Vector3? parsedColor = parseColor(entity["_color"]);
                            if (!parsedColor.HasValue)
                            {
                                Trace.WriteLine("Color parsing failed, skipping entity.");
                                continue;
                            }


                            if (lightColors.ContainsKey(parsedColor.Value)){
                                lightColors[parsedColor.Value].Count++;
                            }
                            else
                            {
                                lightColors.Add(parsedColor.Value, new LightColor() { Count = 1, color = parsedColor.Value });
                            }
                        }
                    }
                }
                i++;
            }
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
            }
        }

        private void saveMapBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
