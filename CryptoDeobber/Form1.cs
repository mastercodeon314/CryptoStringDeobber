using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CryptoMainForm = CryptoObfuscatorDelegateRestorePlacesScout.MainForm;
using NameDeobfuscatorForm = SimpleNameDeobfuscator.MainForm;
namespace CryptoDeobber
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void openFileBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Browse for target assembly";
            openFileDialog.InitialDirectory = @"C:\Users\ADMIN\Desktop\Sammy Installers\RDPowerLG 5.9 Clean";
            openFileDialog.Filter = "All files (*.exe,*.dll)|*.exe;*.dll";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.filePathBox.Text = openFileDialog.FileName;
            }
        }

        private void cryptoDeob(string noDelegateFilePath)
        {
            Deobber stringDeob = new Deobber(noDelegateFilePath);
            //Deobber stringDeob = new Deobber(filePath);
            stringDeob.Patch();

            this.statusLbl.Text = "Status: String + constants deobbed, all deobfusctaion steps copmpleted";
        }

        private string delegateDeob(string renamedFilePath, string filePath)
        {
            CryptoMainForm cryptoFrm = new CryptoMainForm();
            cryptoFrm.filePathBox.Text = renamedFilePath;
            cryptoFrm.Button2Click(null, null);
            this.statusLbl.Text = "Status: " + cryptoFrm.statusLbl.Text;

            string noDelegateFilePath = Path.GetDirectoryName(filePath);
            if (!noDelegateFilePath.EndsWith("\\"))
            {
                noDelegateFilePath += "\\";
            }
            noDelegateFilePath = noDelegateFilePath + Path.GetFileNameWithoutExtension(filePath) + "_renamed_nodelegate" + Path.GetExtension(filePath);
            return noDelegateFilePath;
        }

        private string renameDeob(string filePath)
        {
            NameDeobfuscatorForm renameDeobFrm = new NameDeobfuscatorForm();
            renameDeobFrm.filePathBox.Text = this.filePathBox.Text;
            renameDeobFrm.smallRenameCheckBox.Checked = true;

            string renamedFilePath = Path.GetDirectoryName(filePath);
            if (!renamedFilePath.EndsWith("\\"))
            {
                renamedFilePath += "\\";
            }
            renamedFilePath = renamedFilePath + Path.GetFileNameWithoutExtension(filePath) + "_renamed" + Path.GetExtension(filePath);

            renameDeobFrm.Button2Click(null, null);

            this.statusLbl.ForeColor = Color.Blue;
            this.statusLbl.Text = "Status: " + renameDeobFrm.statusLbl.Text;

            return renamedFilePath;
        }

        private void deobBtn_Click(object sender, EventArgs e)
        {
            string filePath = this.filePathBox.Text;
            if (filePath != String.Empty)
            {
                if (File.Exists(filePath))
                {
                    string renamedFilePath = renameDeob(filePath);
                    string noDelegateFilePath = delegateDeob(renamedFilePath, filePath);
                    cryptoDeob(noDelegateFilePath);

                    /*                    string renamedFilePath = renameDeob(filePath);
                                        cryptoDeob(renamedFilePath);*/
                }
            }
           
        }
    }
}
