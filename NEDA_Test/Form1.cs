using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using Nornion;

namespace NEDA_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string stdf_file = "";

        private void Form1_Load(object sender, EventArgs e)
        {
            dgv_TestLimit.AutoGenerateColumns = true;
            dgv_result.AutoGenerateColumns = true;
            dgv_SBin.AutoGenerateColumns = true;
            dgv_HBin.AutoGenerateColumns = true;
            dgv_TestCount.AutoGenerateColumns = true;
            dgv_PartCount.AutoGenerateColumns = true;
        }

        //Update STDF Extracted data to Form
        private void RenderStdfResult(NEDA nda)
        {
            //Extraction complete and no error detected
            labelStatus.Text = "Lot: " + nda.StdLotInfo["LOT_ID"].ToString() + " extract completed!";
            //lot info
            listBox1.Items.Clear();
            if (nda.StdLotInfo != null && nda.StdLotInfo.Count > 0)
            {
                foreach (string obj in nda.StdLotInfo.Keys)
                {
                    string fieldName = obj;
                    if (fieldName.Length < 30) fieldName = fieldName.PadRight(30 - fieldName.Length);
                    string filedValue = nda.StdLotInfo[obj].ToString();
                    if (filedValue.Length < 30) filedValue = filedValue.PadRight(30 - filedValue.Length);
                    listBox1.Items.Add(fieldName + " --- " + filedValue + " --- " + nda.StdLotInfo[obj].GetType().ToString());
                }
            }
            //Wafer info (if it is a wafer sort STDF)
            listBox2.Items.Clear();
            if (nda.StdfWaferInfo != null && nda.StdfWaferInfo.Count > 0)
            {
                foreach (string obj in nda.StdfWaferInfo.Keys)
                {
                    string fieldName = obj;
                    if (fieldName.Length < 30) fieldName = fieldName.PadRight(30 - fieldName.Length);
                    string filedValue = nda.StdfWaferInfo[obj].ToString();
                    if (filedValue.Length < 30) filedValue = filedValue.PadRight(30 - filedValue.Length);
                    listBox2.Items.Add(fieldName + " --- " + filedValue + " --- " + nda.StdfWaferInfo[obj].GetType().ToString());
                }
            }

            /***************************************************************************************************
             * All results in Datatable structure, refer to below TableName definition
             * TestLimits -- Datatable contains definition of all parametric tests [limits and unit]
             * TestData   -- Datatable contains results of all parametric tests [test reading]
             * HBin       -- Datatable contains Hardware bin information
             * SBin       -- Datatable contains software bin information 
             * TestCount  -- Datatable contains statistical count information [executed, failed...] of all tests
             * PartCount  -- Datatable contains site executed part count information [normally it is not used]
             ***************************************************************************************************/
            foreach (DataTable dt in nda.StdDataSet.Tables)
            {
                switch (dt.TableName)
                {
                    case "TestLimits": dgv_TestLimit.DataSource = nda.StdDataSet.Tables["TestLimits"]; break;
                    case "TestData":
                        {
                            dgv_result.AutoGenerateColumns = true;
                            dgv_result.DataSource = nda.StdDataSet.Tables["TestData"];
                        }
                        break;
                    case "SBin": dgv_SBin.DataSource = nda.StdDataSet.Tables["SBin"]; break;
                    case "HBin": dgv_HBin.DataSource = nda.StdDataSet.Tables["HBin"]; break;
                    case "TestCount": dgv_TestCount.DataSource = nda.StdDataSet.Tables["TestCount"]; break;
                    case "PartCount": dgv_PartCount.DataSource = nda.StdDataSet.Tables["PartCount"]; break;
                    default:; break;
                }
            }
        }

        //Initialize a NEDA instance
        NEDA nda = new NEDA();

        private void button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(stdf_file))
            {
                //Check Error flag after initialization
                if (nda.ErrorFlag)
                {//Some error happened during NEDA initialization, show Error msg
                    labelStatus.Text = nda.ErrorMsg;
                }
                else
                {
                    labelStatus.Text = "Start extracting [" + Path.GetFileName(stdf_file) + "], please wait!";
                    Application.DoEvents();

                    //Start STDF extraction
                    if (nda.ParseStdf(stdf_file))//check return flag, if return=false, there are errors detected during extracting
                    {
                        RenderStdfResult(nda);
                    }
                    else
                    {//Some error happened during extraction, show Error msg
                        labelStatus.Text = nda.ErrorMsg;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please click [Select File] to select STDF file first!");
            }
        }

        /************************** Run Async ***************************/
        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(stdf_file))
            {
                //Check Error flag after initialization
                if (nda.ErrorFlag)
                {//Some error happened during NEDA initialization, show Error msg
                    labelStatus.Text = nda.ErrorMsg;
                }
                else
                {
                    labelStatus.Text = "Start extracting [" + Path.GetFileName(stdf_file) + "], please wait!";
                    Application.DoEvents();
                    nda.ProgressChangedEvent = new ProgressChangedEventHandler(UpdateExtractProgress);
                    nda.WorkerCompleteEvent = new RunWorkerCompletedEventHandler(ExtractCompleteCallback);
                    //Start STDF extraction
                    nda.ParseStdfAsync(stdf_file);
                }
            }
            else
            {
                MessageBox.Show("Please click [Select File] to select STDF file first!");
            }
        }

        public void UpdateExtractProgress(object sender, ProgressChangedEventArgs e)
        {
            labelStatus.Text = e.ProgressPercentage.ToString() + "%";
        }

        public void ExtractCompleteCallback(object sender, RunWorkerCompletedEventArgs e)
        {
            labelStatus.Text = "Extraction complete!";
            //check return flag, if return=false, there are errors detected during extracting
            if (!nda.ErrorFlag)
            {
                RenderStdfResult(nda);
            }
            else
            {//Some error happened during extraction, show Error msg
                labelStatus.Text = nda.ErrorMsg;
            }
        }

        /***** Events of Form Controls *******/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Dispose NEDA object
            if(nda!=null)
                nda.Dispose();
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            string fileName = openFileDialog1.FileName;
            if(File.Exists(fileName))
            {
                stdf_file = fileName;
                labelStatus.Text = stdf_file;
            }
        }

        private void dgv_result_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            e.Column.FillWeight = 1;
        }
    }
}
