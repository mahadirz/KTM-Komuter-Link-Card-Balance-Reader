using MifareClassic;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace KTMLinkCard
{
    public partial class Form1 : Form
    {
        MifareTool mifare;
        string filePersistence = "persistence.json";

        public Form1()
        {
            InitializeComponent();
            try
            {
                mifare = new MifareTool();
                String[] readers = mifare.GetReaders();
                //always connect to first
                mifare.Connect(0);
                button1.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Persistence persistence;
            if (File.Exists(filePersistence))
            {
                persistence = Form1.ReadFromJsonFile<Persistence>(filePersistence);
            }
            else
            {
                persistence = new Persistence();
            }

            String id = MifareTool.ByteArrayToString(mifare.GetUid(),false);
            string key;
            if (!persistence.idKey.ContainsKey(id))
            {
                key = Prompt.ShowDialog("Enter Block 2 Key", "Key Required!");
            }
            else
            {
                key = persistence.idKey[id];
            }

            if (mifare.AuthenticateBlock(key, "08"))
            {
                //save the correct key
                if (!persistence.idKey.ContainsKey(id))
                {
                    persistence.idKey.Add(id, key);
                    Form1.WriteToJsonFile<Persistence>(filePersistence, persistence);
                }
                //read sector 8
                int readValue08 = MifareTool.ConvertBytesToInteger(mifare.ReadValueBlock("08"));
                int readValue09 = MifareTool.ConvertBytesToInteger(mifare.ReadValueBlock("09"));
                if (readValue08 == readValue09) {
                    label1.Text = "Sector 08 == Sector 09";
                    label1.ForeColor = Color.Green;
                }
                else
                {
                    label1.Text = "Sector 08 <> Sector 09";
                    label1.ForeColor = Color.Red;
                }
                double value = (double) readValue08 / 100;
                textBoxValue.Text = String.Format("{0:0.00}", value);
                button2.Enabled = true;
            }
            else
            {
                MessageBox.Show(this, "Authentication to sector 2 failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //set balance
            int balance = (int)(Double.Parse(textBoxValue.Text) * 100);
            if (mifare.WriteValueBlock("08", balance) && mifare.WriteValueBlock("09", balance))
                MessageBox.Show(this, "Set balance succeeded", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Writes the given object instance to a Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
        /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [JsonIgnore] attribute.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the Json file.</returns>
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

      
    }
}
