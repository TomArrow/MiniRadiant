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
        string ColorValues { get {
                return $"R: {color.X} G: {color.Y} B: {color.Z}";
            } }
        Vector3 color = new Vector3();
        int Count { get; set; } = 0;
        float Intensity { get; set; } = 1.0f;
        float Deviance { get; set; } = 0.0f;
        int Samples { get; set; } = 1;
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

        List<LightColor> lightColors = new List<LightColor>();

        private void updateList()
        {
            colorsList.ItemsSource = null;
            colorsList.ItemsSource = lightColors;
        }

        string mapFileData = null;

        Regex entitiesParseRegex = new Regex(@"\{(\s*""([^""]+)""[ \t]+""([^""]+)"")+\s*\}");

        private void parseMapLights()
        {
            MatchCollection matches = entitiesParseRegex.Matches(mapFileData);
            int i = 0;
            foreach(Match match in matches)
            {
                Trace.WriteLine("");
                Trace.WriteLine(i++);
                if (match.Groups.Count >= 4)
                {
                    Dictionary<string, string> entity = new Dictionary<string, string>();

                    int lineCount = match.Groups[2].Captures.Count;
                    for (int c = 0; c < lineCount; c++)
                    {
                        Trace.WriteLine($"{match.Groups[2].Captures[c].Value}:{match.Groups[3].Captures[c].Value}");
                        entity[match.Groups[2].Captures[c].Value] = match.Groups[3].Captures[c].Value;
                    }
                }
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
            }
        }

        private void saveMapBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
