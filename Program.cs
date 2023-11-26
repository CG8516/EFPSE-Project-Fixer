using System;
using System.Windows.Forms;
using System.IO;

namespace EFPSEProjectFixer
{
    internal class Program
    {

        //Placeholder image used when texture is missing (1x1 pink pixel png, with a nice file size)
        static readonly byte[] placeHolderImg = new byte[69] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xF0, 0x1F, 0x00, 0x04, 0x00, 0x01, 0xFF, 0xF3, 0x29, 0x25, 0xAF, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
        
        [STAThread]
        static void Main(string[] args)
        {
            string path = "";
            bool cmdLine = false;
            if (args != null && args.Length > 0)
            {
                cmdLine = true;
                if (Directory.Exists(args[0]))
                    path = args[0];
                else
                {
                    Console.WriteLine("Specified directory could not be found!");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("Choose the Data directory for your project");
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Choose the Data directory for your project";
                    DialogResult result = fbd.ShowDialog();

                    if (result != DialogResult.OK || fbd.SelectedPath == null)
                    {
                        MessageBox.Show("No directory specified!", "No directory specified", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        Environment.Exit(1);
                    }

                    path = fbd.SelectedPath;
                }
            }

            if (!File.Exists(path + "\\Textures.dat"))
            {
                Console.WriteLine("Textures.dat file not found!\nAre you sure you chose the right directory?");
                if(!cmdLine)
                    MessageBox.Show("Textures.dat file not found!\nAre you sure you chose the right directory?", "Textures.dat not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(1);
            }

            bool foundProblem = false;
            bool errorOcurred = false;

            string[] textureLines = File.ReadAllLines(path + "\\Textures.dat");

            for(int i =0; i < textureLines.Length; i++)
            {
                string[] splitLine = textureLines[i].Split(' ');

                string textureName = splitLine[2];
                for (int j = 3; j < splitLine.Length - 1; j++)
                    textureName += " " + splitLine[j];

                if (splitLine.Length > 4)
                {
                    foundProblem = true;

                    Console.WriteLine("Fixing file with space in its name: " + textureName);

                    string newFilename = splitLine[2];
                    for (int j = 3; j < splitLine.Length - 1; j++)
                        newFilename += splitLine[j];

                    if (File.Exists(path + "\\Textures\\" + textureName))
                    {
                        try
                        {
                            File.Move(path + "\\Textures\\" + textureName, path + "\\Textures\\" + newFilename);
                        }
                        catch
                        {
                            errorOcurred = true;
                            Console.WriteLine("Failed to move texture file :(");
                        }
                    }

                    textureName = newFilename;
                }
                string fixedLine = splitLine[0] + " " + splitLine[1] + " " + textureName + " " + splitLine[splitLine.Length - 1];

                if (!File.Exists(path + "\\Textures\\" + textureName))
                {
                    foundProblem = true;
                    
                    Console.WriteLine("Creating placeholder texture for missing file in Textures.dat: " + textureName);
                    try
                    {
                        File.WriteAllBytes(path + "\\Textures\\" + textureName, placeHolderImg);
                    }
                    catch
                    {
                        errorOcurred = true;
                        Console.WriteLine("Failed to create placeholder texture file :(");
                    }

                }

                textureLines[i] = fixedLine;
            }

            try
            {
                File.WriteAllLines(path + "\\Textures.dat", textureLines);
            }
            catch
            {
                errorOcurred = true;
                Console.WriteLine("Failed to save updated Textures.dat file :(");
            }

            if(errorOcurred)
            {
                Console.WriteLine("One or more errors were found in your project, which couldn't be fixed :(");
                if (!cmdLine)
                    MessageBox.Show("One or more errors were found in your project, which couldn't be fixed :(", "Unfixable issues found!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(3);
            }
            else if(foundProblem)
            {
                Console.WriteLine("One or more errors were successfully found and fixed in your project.");
                if (!cmdLine)
                    MessageBox.Show("One or more errors were successfully found and fixed in your project.", "Issues fixed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("No supported issues were detected in your project.");
                if (!cmdLine)
                    MessageBox.Show("No supported issues were detected in your project.", "No issues found!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
        }
    }
}
