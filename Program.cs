using System;
using System.Windows.Forms;
using System.IO;

namespace EFPSEProjectFixer
{
    internal class Program
    {

        //Placeholder image used when texture is missing (1x1 pink pixel png, with a nice file size)
        static readonly byte[] placeHolderImg = new byte[69] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xF0, 0x1F, 0x00, 0x04, 0x00, 0x01, 0xFF, 0xF3, 0x29, 0x25, 0xAF, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        static bool foundProblem = false;
        static bool errorOcurred = false;
        static bool consoleMode = false;

        static string[] LoadDat(string dir, string filename)
        {
            if (!File.Exists(dir + "\\" + filename))
            {
                Console.WriteLine(filename + " file not found!\nAre you sure you chose the right directory?");
                if (!consoleMode)
                    MessageBox.Show(filename + " file not found!\nAre you sure you chose the right directory?", filename + " not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(1);
            }

            try
            {
                return File.ReadAllLines(dir + "\\" + filename);
            }
            catch
            {
                Console.WriteLine("Failed to read " + filename + "!");
                if (!consoleMode)
                    MessageBox.Show("Failed to read " + filename + "!", "File read error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            //Shouldn't ever reach this point. Just to keep the compiler happy
            return null;
        }

        static void SaveDat(string dir, string filename, string[] contents)
        {
            try
            {
                File.WriteAllLines(dir + "\\" + filename, contents);
            }
            catch
            {
                errorOcurred = true;
                Console.WriteLine("Failed to save updated " + filename + " file.");
            }
        }

        static void RenameFile(string originalPath, string newPath)
        {
            if (File.Exists(originalPath))
            {
                try
                {
                    File.Move(originalPath, newPath);
                }
                catch
                {
                    errorOcurred = true;
                    Console.WriteLine("Failed to rename file: " + originalPath);
                }
            }
        }

        static void EnsureTexture(string path, string filename)
        {
            if (!path.EndsWith("\\"))
                path += "\\";

            string fullPath = path + filename;
            if (!File.Exists(fullPath))
            {
                foundProblem = true;

                Console.WriteLine("Creating placeholder texture for missing file: " + filename);
                try
                {
                    File.WriteAllBytes(fullPath, placeHolderImg);
                }
                catch
                {
                    errorOcurred = true;
                    Console.WriteLine("Failed to create placeholder texture file: " + fullPath);
                }

            }
        }


        static void FixTextures(string path)
        {
            string[] textureLines = LoadDat(path, "Textures.dat");

            for (int i = 0; i < textureLines.Length; i++)
            {
                string[] splitLine = textureLines[i].Split(' ');

                string currentName = splitLine[2];
                string newName = currentName;
                for (int j = 3; j < splitLine.Length - 1; j++)
                {
                    currentName += " " + splitLine[j];
                    newName += splitLine[j];
                }

                if (splitLine.Length > 4)
                {
                    foundProblem = true;

                    Console.WriteLine("Fixing file with space in its name: " + currentName);

                    string origPath = path + "\\Textures\\" + currentName;
                    string newPath = path + "\\Textures\\" + currentName;
                    RenameFile(origPath, newPath);
                }
                string fixedLine = splitLine[0] + " " + splitLine[1] + " " + newName + " " + splitLine[splitLine.Length - 1];

                EnsureTexture(path + "\\Textures\\", newName);

                textureLines[i] = fixedLine;
            }

            SaveDat(path, "Textures.dat", textureLines);
        }

        static void FixDecorations(string path)
        {
           string[] decorationLines = LoadDat(path, "Decorations.dat");

            for(int i = 0; i < decorationLines.Length; i+=8 )
            {
                string[] lineSplit = decorationLines[i].Split(' ');
                string currentName = lineSplit[0];
                string newName = currentName;
                for (int j = 1; j < lineSplit.Length - 2; j++)
                {
                    currentName += " " + lineSplit[j];
                    newName += lineSplit[j];
                }

                if(lineSplit.Length > 2)
                {
                    foundProblem = true;

                    Console.WriteLine("Fixing decoration with space in its name: " + currentName);

                    //Rename all original files, to remove spaces (if found)
                    for(int j = 0; j < 4; j++)
                    {
                        string origPath = path + "\\Sprites\\Decorations\\" + currentName + j + ".png";
                        string newPath = path + "\\Sprites\\Decorations\\" + newName + j + ".png";
                        RenameFile(origPath, newPath);
                    }

                    string[] soundNames = new string[] { "Damage", "Break" };

                    for (int j = 0; j < soundNames.Length; j++)
                    {
                        string origPath = path + "\\Sounds\\" + currentName + soundNames[j] + ".wav";
                        string newPath = path + "\\Sounds\\" + newName + soundNames[j] + ".wav";
                        RenameFile(origPath, newPath);
                    }

                    decorationLines[i] = newName + " " + lineSplit[lineSplit.Length - 1];
                }

                EnsureTexture(path + "\\Sprites\\Decorations\\", currentName + "0.png");    //Make sure texture used in map editor exists

            }


            SaveDat(path, "Decorations.dat", decorationLines);
        }

        static void FixEnemies(string path)
        {
            string[] enemyLines = LoadDat(path, "Enemies.dat");

            for (int i = 0; i < enemyLines.Length; i += 13)
            {
                string[] lineSplit = enemyLines[i].Split(' ');
                string currentName = lineSplit[0];
                string newName = currentName;
                for (int j = 1; j < lineSplit.Length - 2; j++)
                {
                    currentName += " " + lineSplit[j];
                    newName += lineSplit[j];
                }

                if (lineSplit.Length > 2)
                {
                    foundProblem = true;

                    Console.WriteLine("Fixing enemy with space in its name: " + currentName);

                    //Rename all original files, to remove spaces (if found)
                    for (int j = 0; j < 13; j++)
                    {
                        string origPath = path + "\\Sprites\\Enemies\\" + currentName + j + ".png";
                        string newPath = path + "\\Sprites\\Enemies\\" + newName + j + ".png";
                        RenameFile(origPath, newPath);
                    }

                    {
                        string origPath = path + "\\Sprites\\Enemies\\" + currentName + "Projectile.png";
                        string newPath = path + "\\Sprites\\Enemies\\" + newName + "Projectile.png";
                        RenameFile(origPath, newPath);
                    }

                    {
                        string origPath = path + "\\Sprites\\Particles\\" + currentName + "Blood.png";
                        string newPath = path + "\\Sprites\\Particles\\" + newName + "Blood.png";
                        RenameFile(origPath, newPath);
                    }

                    {
                        string origPath = path + "\\Sprites\\Particles\\" + currentName + "BloodFloor.png";
                        string newPath = path + "\\Sprites\\Particles\\" + newName + "BloodFloor.png";
                        RenameFile(origPath, newPath);
                    }

                    string[] soundNames = new string[] { "Act", "Attack", "Hurt", "Death" };

                    for (int j = 0; j < soundNames.Length; j++)
                    {
                        string origPath = path + "\\Sounds\\" + currentName + soundNames[j] + ".wav";
                        string newPath = path + "\\Sounds\\" + newName + soundNames[j] + ".wav";
                        RenameFile(origPath, newPath);
                    }

                    enemyLines[i] = newName + " " + lineSplit[lineSplit.Length - 1];
                }

                EnsureTexture(path + "\\Sprites\\Enemies\\", currentName + "0.png");    //Make sure texture used in map editor exists

            }


            SaveDat(path, "Enemies.dat", enemyLines);


        }

        //Fixes engine crash caused by doors being placed on empty tiles
        static void FixMaps(string path)
        {
            //+34

            string[] mapLines = LoadDat(path, "Maps.dat");

            for(int i =0; i < mapLines.Length; i+=34)
            {
                string mapName = mapLines[i];

                byte[] mapBytes;

                try
                {
                    mapBytes = File.ReadAllBytes(path + "\\Maps\\" + mapName);
                }
                catch
                {
                    Console.WriteLine("Failed to read map file: " + mapName);
                    continue;   //Editor won't crash if map is missing.
                }

                
                short width = BitConverter.ToInt16(mapBytes, 0);
                short height = BitConverter.ToInt16(mapBytes, 2);
                Console.WriteLine("Width: " + width);
                Console.WriteLine("Height: " + height);

                int tileCount = width * height * 9;
                short[] tileData = new short[tileCount];
                short[] modifierData = new short[tileCount];

                Buffer.BlockCopy(mapBytes, 4, tileData, 0, tileCount * 2);
                Buffer.BlockCopy(mapBytes, (tileCount*8) + 4, modifierData, 0, tileCount * 2);

                bool mapModified = false;
                for (int z = 0; z < 9; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            //Shorthand to make lines look sexier
                            int index = (z * width * height) +  y * width + x;
                            short mod = modifierData[index];

                            //If modifier is a door, and tile in same position is empty; remove the door.
                            if ((mod == 13 || mod == 35 || mod == 36 || mod == 37) && (tileData[index] == 0))
                            {
                                Console.WriteLine("Door at {0},{1} is placed on an empty tile!", x, y);
                                foundProblem = true;
                                modifierData[index] = 0;
                                mapModified = true;
                            }
                        }
                    }
                }

                //Copy updated modifiers back to map bytes
                Buffer.BlockCopy(modifierData, 0, mapBytes, (tileCount * 8) + 4, tileCount * 2);

                if (mapModified)
                {
                    try
                    {
                        File.WriteAllBytes(path + "\\Maps\\" + mapName, mapBytes);
                    }
                    catch
                    {
                        Console.WriteLine("Failed to write modified map file: " + mapName);
                        errorOcurred = true;
                        continue;
                    }
                }
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            string path = "";
            if (args != null && args.Length > 0)
            {
                consoleMode = true;
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

            FixTextures(path);

            FixDecorations(path);

            FixEnemies(path);

            //Make sure key textures exist, to prevent engine crash (not editor)
            for (int i = 0;i < 4; i++)
                EnsureTexture(path + "\\Sprites\\Keys\\", "Key" + i + ".png");

            FixMaps(path);


            if (errorOcurred)
            {
                Console.WriteLine("One or more errors were found in your project, which couldn't be fixed :(");
                if (!consoleMode)
                    MessageBox.Show("One or more errors were found in your project, which couldn't be fixed :(", "Unfixable issues found!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(3);
            }
            else if(foundProblem)
            {
                Console.WriteLine("One or more errors were successfully found and fixed in your project.");
                if (!consoleMode)
                    MessageBox.Show("One or more errors were successfully found and fixed in your project.", "Issues fixed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("No supported issues were detected in your project.");
                if (!consoleMode)
                    MessageBox.Show("No supported issues were detected in your project.", "No issues found!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
        }
    }
}
