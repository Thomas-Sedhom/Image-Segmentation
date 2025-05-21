using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        public string K_Constant_Value => K_Constant.Text;

        List<int> selectedSegments = new List<int>();

        public MainForm()
        {
            InitializeComponent();
            Instance = this;
            label7.Hide();
            numOfSelectedRegions.Text = "";
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image Files (JPG,PNG,BMP)|*.JPG;*.PNG;*.BMP"
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;

                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            if (ImageMatrix == null) return;
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }

        private async void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            if (DoGaussianFilter.Checked)
            {
                double sigma = double.Parse(txtGaussSigma.Text);
                int maskSize = (int)nudMaskSize.Value;
                ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            }
            ImageMatrix = await Segmentation.ImageProcess(ImageMatrix);
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DoGaussianFilter.Checked = true;
        }

        private void SaveOutputData()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*",
                RestoreDirectory = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image.Save(saveFileDialog.FileName, ImageFormat.Bmp);
            }
        }

        private void DoGaussianFilter_CheckedChanged(object sender, EventArgs e)
        {

            if (DoGaussianFilter.Checked)
            {
                Gauss_Settings_Panel.Show();
            }
            else
                Gauss_Settings_Panel.Hide();
        }

        private void Merge_Click(object sender, EventArgs e)
        {
            label7.Hide();
            numOfSelectedRegions.Hide();

            if (selectedSegments.Count < 2)
            {
                MessageBox.Show("Select at least two regions to merge.");
                return;
            }

            Segmentation.MergeMultipleSegments(ref ImageMatrix, selectedSegments);
            selectedSegments.Clear();
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }


        private void pictureBox2_Click(object sender, EventArgs e)
        {

            if (e is MouseEventArgs me)
            {
                Point coordinates = me.Location;

                if (pictureBox2.Image != null)
                {
                    if (coordinates.X < pictureBox2.Image.Width &&
                        coordinates.Y < pictureBox2.Image.Height)
                    {
                        int pixelId = coordinates.Y * ImageMatrix.GetLength(1) + coordinates.X;

                        if (!selectedSegments.Contains(pixelId))
                        {
                            selectedSegments.Add(pixelId);

                            Color pixelColor = ((Bitmap)pictureBox2.Image).GetPixel(coordinates.X, coordinates.Y);
                            MessageBox.Show($"Selected Region at ({coordinates.X},{coordinates.Y}) with Color: R: {pixelColor.R}, G: {pixelColor.G}, B: {pixelColor.B}",
                                            "Region Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Clicked outside image bounds");
                    }
                }
            }
            label7.Show();
            numOfSelectedRegions.Show();
            numOfSelectedRegions.Text = selectedSegments.Count.ToString();
            numOfSelectedRegions.Refresh();
        }


        private void SaveImage_Click(object sender, EventArgs e)
        {
            SaveOutputData();
            Segmentation.LogRegionInfoToFile();
        }
    }
}