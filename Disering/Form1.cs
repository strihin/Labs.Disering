using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
namespace Disering
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Random Random = new Random();

        private static int FieldSize = 9;
        private static int AvailableColorsCount = 5;
        
        IList<Color> SelectedColors = new List<Color>();

        List<Color> Palette = new List<Color>();
        List<List<int>> ColorPermutations = new List<List<int>>();

        List<Color> DiseringCell = new List<Color>();

        public void GeneratePalette()
        {
            Palette.Clear();

            SelectedColors = SelectedColors.Distinct().ToList();

            ColorPermutations.Clear();

            for (int i = 0; i < FieldSize + 1; i++)
            {
                for (int j = 0; j < FieldSize + 1; j++)
                {
                    for (int k = 0; k < FieldSize + 1; k++)
                    {
                        for (int s = 0; s < FieldSize + 1; s++)
                        {
                            for (int p = 0; p < FieldSize + 1; p++)
                            {
                                ColorPermutations.Add(new List<int> { i, j, k, s, p });
                            }
                        }
                    }
                }
            }

            var notAllowedColors = Enumerable.Range(0, AvailableColorsCount).Skip(SelectedColors.Count).ToList();

            ColorPermutations.RemoveAll(p => notAllowedColors.Any(n => p[n] != 0) || p.Sum() != FieldSize);

            foreach (var permutations in ColorPermutations)
            {
                Palette.Add(GetPalleteColor(permutations));
            }
        }

        private Color GetPalleteColor(List<int> colorWeights)
        {
            int r = 0, g = 0, b = 0;

            for (var i = 0; i < SelectedColors.Count; i++)
            {
                r += colorWeights[i] * SelectedColors[i].R;
                g += colorWeights[i] * SelectedColors[i].G;
                b += colorWeights[i] * SelectedColors[i].B;
            }

            return Color.FromArgb(r / FieldSize, g / FieldSize, b / FieldSize);
        }

        private void GenerateDiseringCell(int b, int g, int r)
        {
            DiseringCell.Clear();

            Color pixelColor = Color.FromArgb(r, g, b);

            double distance = ColorDistance(pixelColor, Palette[0]), buffer;
            int index = 0;

            for (int i = 0; i < Palette.Count; i++)
            {
                buffer = ColorDistance(pixelColor, Palette[i]);
                if (buffer < distance)
                {
                    distance = buffer;
                    index = i;
                }
            }

            for (int i = 0; i < ColorPermutations[index].Count; i++)
            {
                for (int j = 0; j < ColorPermutations[index][i]; j++)
                {
                    DiseringCell.Add(SelectedColors[i]);
                }
            }

            DiseringCell = RandomMixColors(DiseringCell);
        }

        private List<Color> RandomMixColors(List<Color> inputList)
        {
            List<Color> randomList = new List<Color>();

            int randomIndex = 0;
            while (inputList.Count > 0)
            {
                randomIndex = Random.Next(0, inputList.Count); 
                randomList.Add(inputList[randomIndex]); 
                inputList.RemoveAt(randomIndex); 
            }

            return randomList;
        }

        private double ColorDistance(Color c1, Color c2)
        {
            return Math.Sqrt(0.3 * (c1.R - c2.R) * (c1.R - c2.R) + 0.59 * (c1.G - c2.G) * (c1.G - c2.G) + 0.11 * (c1.B - c2.B) * (c1.B - c2.B));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (SelectedColors.Count == AvailableColorsCount)
            {
                return;                
            }

            colorDialog1.ShowDialog();
            SelectedColors.Add(colorDialog1.Color);
            listBox1.Items.Add(colorDialog1.Color.Name);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SelectedColors.Clear();
            listBox1.Items.Clear();
        }

        private void Disering()
        {
            Bitmap inputBitmap = new Bitmap(pictureBox1.Image);
            Bitmap outputBitmap = new Bitmap(pictureBox1.Image.Width * 3, pictureBox1.Image.Height * 3);
//test commit by GUI
            BitmapData inputData = inputBitmap.LockBits(new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height), ImageLockMode.ReadWrite, inputBitmap.PixelFormat);
            BitmapData outputData = outputBitmap.LockBits(new Rectangle(0, 0, outputBitmap.Width, outputBitmap.Height), ImageLockMode.ReadWrite, inputBitmap.PixelFormat);

            IntPtr ptr1 = inputData.Scan0;
            IntPtr ptr2 = outputData.Scan0;

            byte[] inputBytes = new byte[inputData.Stride * inputBitmap.Height];
            byte[] outputBytes = new byte[outputData.Stride * outputBitmap.Height];

            Marshal.Copy(ptr1, inputBytes, 0, inputBytes.Length);
            Marshal.Copy(ptr2, outputBytes, 0, outputBytes.Length);

            int k = 0, r = 0;

            progressBar1.Maximum = inputBytes.Length;

            for (int i = 0, x = 0; x < inputBytes.Length - 3; i += 12, x += 4, k = 0)
            {
                GenerateDiseringCell(inputBytes[x], inputBytes[x + 1], inputBytes[x + 2]);

                if (x != 0 && x % inputData.Stride == 0)
                {
                    r += 2;
                }
                for (int j = 0, l = 0; j < FieldSize; j++)
                {
                    outputBytes[i + 4 * l + outputData.Stride * k + outputData.Stride * r] = DiseringCell[j].B;
                    outputBytes[i + 1 + 4 * l + outputData.Stride * k + outputData.Stride * r] = DiseringCell[j].G;
                    outputBytes[i + 2 + 4 * l + outputData.Stride * k + outputData.Stride * r] = DiseringCell[j].R;
                    outputBytes[i + 3 + 4 * l + outputData.Stride * k + outputData.Stride * r] = 255;

                    if ((j + 1) % 3 == 0)
                    {
                        k++;
                    }
                    if (l == 2)
                    {
                        l = 0;
                    }
                    else
                    {
                        l++;
                    }

                }

                progressBar1.PerformStep();

            }

            Marshal.Copy(outputBytes, 0, ptr2, outputBytes.Length);
            outputBitmap.UnlockBits(outputData);
            pictureBox2.Image = outputBitmap;
            pictureBox2.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (SelectedColors.Count < 2)
            {
                return;
            }

            progressBar1.Value = 0;

            GeneratePalette();
            Disering();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();

            pictureBox2.Image.Save(saveFileDialog1.FileName);
        }
    }
}
