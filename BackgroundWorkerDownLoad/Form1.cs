using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackgroundWorkerDownLoad
{

    public partial class Form1 : Form
    {
        public int DownloadSize = 0;
        public string downloadPath = null;
        RequestState requestState = null;
        long totalSize = 0;
        public Form1()
        {
            InitializeComponent();

            //string url = "http://download.microsoft.com/download/7/0/3/703455ee-a747-4cc8-bd3e-98a615c3aedb/dotNetFx35setup.exe";
            string url = "http://download.microsoft.com/download/9/5/A/95A9616B-7A37-4AF6-BC36-D6EA96C8DAAE/dotNetFx40_Full_x86_x64.exe";
            txtUrl.Text = url;
            this.btnPause.Enabled = false;
            //this.status = DownloadStatus.Initialized;
            // Get download Path

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(txtUrl.Text.Trim());
            HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.GetResponse();
            totalSize = response.ContentLength;
            response.Close();

            downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + Path.GetFileName(this.txtUrl.Text.Trim());
            if (File.Exists(downloadPath))
            {
                FileInfo fileInfo = new FileInfo(downloadPath);
                DownloadSize = (int)fileInfo.Length;
                progressBar1.Value = (int)((float)DownloadSize / (float)totalSize * 100);
            }

            // Enable support ReportProgress and Cancellation
            bgWorkerFileDownload.WorkerReportsProgress = true;
            bgWorkerFileDownload.WorkerSupportsCancellation = true;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if(bgWorkerFileDownload.IsBusy != true)
            {
                // Start the asynchronous operation
                // Fire DoWork Event 
                bgWorkerFileDownload.RunWorkerAsync();

                // Create an instance of the RequestState 
                requestState = new RequestState(downloadPath);
                requestState.filestream.Seek(DownloadSize, SeekOrigin.Begin);
                this.btnDownload.Enabled = false;
                this.btnPause.Enabled = true;
            }
            else
            {
                MessageBox.Show("正在执行操作，请稍后");
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (bgWorkerFileDownload.IsBusy && bgWorkerFileDownload.WorkerSupportsCancellation == true)
            {
                // Pause the asynchronous operation
                // Fire RunWorkerCompleted event
                bgWorkerFileDownload.CancelAsync();
            }
        }


        #region BackGroundWorker Event
        // Occurs when RunWorkerAsync is called.
        private void bgWorkerFileDownload_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the source of event
            BackgroundWorker bgworker = sender as BackgroundWorker;
            try
            {
                // Do the DownLoad operation
                // Initialize an HttpWebRequest object
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(txtUrl.Text.Trim());

                // If the part of the file have been downloaded, 
                // The server should start sending data from the DownloadSize to the end of the data in the HTTP entity.
                if (DownloadSize != 0)
                {
                    myHttpWebRequest.AddRange(DownloadSize);
                }

                // assign HttpWebRequest instance to its request field.
                requestState.request = myHttpWebRequest;
                requestState.response = (HttpWebResponse)myHttpWebRequest.GetResponse();
                requestState.streamResponse = requestState.response.GetResponseStream();
                int readSize = 0;
                while (true)
                {
                    if (bgworker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }

                    readSize = requestState.streamResponse.Read(requestState.BufferRead, 0, requestState.BufferRead.Length);
                    if (readSize > 0)
                    {
                        DownloadSize += readSize;
                        int percentComplete = (int)((float)DownloadSize / (float)totalSize * 1000);
                        requestState.filestream.Write(requestState.BufferRead, 0, readSize);

                        // 报告进度，引发ProgressChanged事件的发生
                        bgworker.ReportProgress(percentComplete);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        // Occurs when ReportProgress is called.
        private void bgWorkerFileDownload_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
        }

        // Occurs when the background operation has completed, has been canceled, or has raised an exception.
        private void bgWorkerFileDownload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                requestState.response.Close();
            }
            else if (e.Cancelled)
            {
                MessageBox.Show(String.Format("下载暂停，下载的文件地址为：{0}\n 已经下载的字节数为: {1}字节", downloadPath, DownloadSize));
                requestState.response.Close();
                requestState.filestream.Close();

                this.btnDownload.Enabled = true;
                this.btnPause.Enabled = false;
            }
            else
            {
                MessageBox.Show(String.Format("下载已完成，下载的文件地址为：{0}，文件的总字节数为: {1}字节", downloadPath, totalSize));

                this.btnDownload.Enabled = false;
                this.btnPause.Enabled = false;
                requestState.response.Close();
                requestState.filestream.Close();
            }
        }
        #endregion
    }
    public class RequestState
    {
        public int BufferSize = 2048;

        public byte[] BufferRead;
        public HttpWebRequest request;
        public HttpWebResponse response;
        public Stream streamResponse;

        public FileStream filestream;
        public RequestState(string downloadPath)
        {
            BufferRead = new byte[BufferSize];
            request = null;
            streamResponse = null;
            filestream = new FileStream(downloadPath, FileMode.OpenOrCreate);
        }
    }
}
