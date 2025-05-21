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

        int seg1 = -1;
        int seg2 = -1;

        public MainForm()
        {
            InitializeComponent();
            Instance = this;
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
            if (seg1 == -1 || seg2 == -1) return;

            Segmentation.MergeTwoSegments(ref ImageMatrix, seg1, seg2);
            seg1 = seg2 = -1;
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
                        Bitmap bmp = (Bitmap)pictureBox2.Image;

                        Color pixelColor = bmp.GetPixel(coordinates.X, coordinates.Y);

                        MessageBox.Show($"Color at ({coordinates.X},{coordinates.Y}): R: {pixelColor.R} G: {pixelColor.G} B: {pixelColor.B}",
                                      "Region Selected",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);

                        if (seg1 == -1)
                            seg1 = coordinates.Y * ImageMatrix.GetLength(1) + coordinates.X;
                        else if (seg2 == -1)
                            seg2 = coordinates.Y * ImageMatrix.GetLength(1) + coordinates.X;

                    }
                    else
                    {
                        MessageBox.Show("Clicked outside image bounds");
                    }
                }
            }
        }

        private void SaveImage_Click(object sender, EventArgs e)
        {
            SaveOutputData();
            Segmentation.LogRegionInfoToFile();
        }
    }
}